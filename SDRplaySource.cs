using Microsoft.Win32;
using SDRSharp.Radio;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SDRSharp.SDRplay
{
    // RSPduo in Single Tuner mode (tuner A) through SDRplay API 3.15.
    // Threads: the bridge receive thread calls Start / Stop and the setters (API calls under _apiLock);
    // the API stream thread calls OnStreamA (interleave into _staging, Write to the ring, never waits);
    // the bridge sender thread calls Read (Acquire / Release, overload acknowledge).
    public unsafe partial class SDRplaySource : IIQSource, IDisposable
    {
        private const int SlotSamples = 4096; // IQ samples per ring slot (16 KiB)
        private const double RingDuration = 0.25; // sec of samples the ring holds
        private const int MaxDecimationStage = 5; // factor 32
        private const double MeasureWindow = 1.0; // sec

        private readonly object _apiLock = new object();
        private SDRplayDevice _device;
        private IntPtr _channel; // SDRplayRxChannelParams*, owned by the API from GetDeviceParams until ReleaseDevice
        private GCHandle _self;
        private bool _apiOpen;
        private bool _selected;
        private volatile bool _streaming;
        private volatile CircularBuffer _ring;
        private UnsafeBuffer _staging;

        private long _frequency = 100000000;
        private volatile int _decimationStage;
        private volatile int _sampleRateIndex = RspDuoSampleRates.Default; // requested in the window
        private volatile int _activeRateIndex = RspDuoSampleRates.Default; // fixed for the client session
        private volatile bool _overloadPending;
        private volatile int _lnaState;
        private volatile int _agcMode = SDRplayApi.Agc50Hz;
        private volatile int _agcSetPoint = -60; // dBFS
        private volatile int _ifGainReduction = 50; // dB
        private volatile bool _overload;
        private volatile int _reportedIFGainReduction = -1; // dB, -1 = not reported yet
        private volatile int _reportedLnaGainReduction = -1; // dB
        private double _reportedGain; // dB, system gain from the GainChange event
        private volatile int _antenna = RspDuoAntenna.Default;
        private volatile bool _biasT;
        private volatile bool _rfNotch;
        private volatile bool _dabNotch;
        private volatile bool _amNotch;
        private double _ppm;
        private IntPtr _deviceParams; // SDRplayDeviceParams*, owned by the API like _channel

        private volatile string _deviceName = "";
        private string _serial = "";
        private volatile string _failure = "";
        private volatile int _overruns;
        private double _measuredSampleRate;
        private long _measureStart;
        private long _measureSamples;

        static SDRplaySource()
        {
            NativeLibrary.SetDllImportResolver(typeof(SDRplaySource).Assembly, ResolveLibrary);
        }

        public uint MaximumSampleRate => (uint)RspDuoSampleRates.SampleRate[_activeRateIndex];

        public uint MaximumBandwidth => (uint)RspDuoSampleRates.BwType[_activeRateIndex] * 1000;

        // The rate for the next client session (DEVICE_INFO is sent once per session).
        public int SampleRateIndex
        {
            get { return _sampleRateIndex; }
            set { _sampleRateIndex = RspDuoSampleRates.Clamp(value); }
        }

        public int ActiveSampleRateIndex => _activeRateIndex;

        public void BeginSession()
        {
            _activeRateIndex = _sampleRateIndex;
        }

        public uint DecimationStageCount => MaxDecimationStage;

        // 0: the SDR# gain slider has no range; the gain is set in the plugin window.
        public uint MaximumGainIndex => 0;

        public uint MinimumFrequency => 1000;

        public uint MaximumFrequency => 2000000000;

        public uint Resolution => 14;

        public int BlockSamples => SlotSamples;

        public string DeviceName => _deviceName;

        public string Failure => _failure;

        public double MeasuredSampleRate => Interlocked.CompareExchange(ref _measuredSampleRate, 0, 0);

        public int Overruns => _overruns;

        public long Frequency
        {
            get { return Interlocked.Read(ref _frequency); }
            set
            {
                // The frequency, the antenna and the LNA state change together under _apiLock: the LNA clamp
                // always uses the frequency and port it is sent with.
                lock (_apiLock)
                {
                    Interlocked.Exchange(ref _frequency, value);
                    // A band with fewer LNA states: the state above its range becomes the band's last state. The API
                    // rejects a frequency whose band has no such LNA state (measured: OutOfRange going from LNA 8 at
                    // 100 MHz to 6.3 MHz), so the clamped state goes in the same Update as the frequency.
                    var maxState = RspDuoGainTable.StateCount(value, IsHiZ(_antenna, value)) - 1;
                    var reason = SDRplayApi.UpdateTunerFrf;
                    if (_lnaState > maxState)
                    {
                        _lnaState = maxState;
                        reason |= SDRplayApi.UpdateTunerGr;
                    }
                    if (_streaming)
                    {
                        var channel = (SDRplayRxChannelParams*)_channel;
                        channel->TunerParams.RfFreq.RfHz = value;
                        if ((reason & SDRplayApi.UpdateTunerGr) != 0)
                        {
                            SetGain(channel);
                        }
                        Check("Update Frf", sdrplay_api_Update(_device.Dev, _device.Tuner, reason, SDRplayApi.UpdateExt1None));
                    }
                }
            }
        }

        public int LnaState
        {
            get { return _lnaState; }
            set
            {
                lock (_apiLock)
                {
                    _lnaState = Math.Min(Math.Max(value, 0), LnaStateCount - 1);
                    UpdateGain();
                }
            }
        }

        public int AgcMode
        {
            get { return _agcMode; }
            set
            {
                _agcMode = Math.Min(Math.Max(value, SDRplayApi.AgcDisable), SDRplayApi.Agc5Hz);
                UpdateAgc();
                if (_agcMode == SDRplayApi.AgcDisable)
                {
                    // The manual IF gain reduction takes over from the AGC's last value.
                    UpdateGain();
                }
            }
        }

        public int AgcSetPoint
        {
            get { return _agcSetPoint; }
            set
            {
                _agcSetPoint = Math.Min(Math.Max(value, RspDuoGainTable.MinAgcSetPoint), RspDuoGainTable.MaxAgcSetPoint);
                UpdateAgc();
            }
        }

        // Used while the AGC is off; with the AGC on, the AGC sets the IF gain reduction.
        public int IFGainReduction
        {
            get { return _ifGainReduction; }
            set
            {
                _ifGainReduction = Math.Min(Math.Max(value, RspDuoGainTable.MinIFGainReduction), RspDuoGainTable.MaxIFGainReduction);
                UpdateGain();
            }
        }

        // Number of LNA states at the tuned frequency on the selected port.
        public int LnaStateCount => RspDuoGainTable.StateCount(Frequency, IsHiZ(_antenna, Frequency));

        public int LnaReductionDB => RspDuoGainTable.ReductionDB(Frequency, IsHiZ(_antenna, Frequency), _lnaState);

        // RspDuoAntenna value. Changing the tuner while streaming uses SwapRspDuoActiveTuner (API spec 3.18).
        public int Antenna
        {
            get { return _antenna; }
            set
            {
                var antenna = RspDuoAntenna.Clamp(value);
                lock (_apiLock)
                {
                    var previous = _antenna;
                    var previousLnaState = _lnaState;
                    _antenna = antenna;
                    var reason = 0u;
                    if (_lnaState > LnaStateCount - 1)
                    {
                        _lnaState = LnaStateCount - 1;
                        reason |= SDRplayApi.UpdateTunerGr;
                    }
                    if (!_streaming || previous == antenna)
                    {
                        return;
                    }
                    if (RspDuoAntenna.Tuner(previous) != RspDuoAntenna.Tuner(antenna))
                    {
                        if (!SwapTuner())
                        {
                            // The device stays on the previous tuner: so does the plugin, or the next change
                            // would compare against the wrong tuner.
                            _antenna = previous;
                            _lnaState = previousLnaState;
                            UpdateDeviceName();
                        }
                        return;
                    }
                    // Same tuner 1, other port.
                    var channel = (SDRplayRxChannelParams*)_channel;
                    channel->RspDuoTunerParams.Tuner1AmPortSel = RspDuoAntenna.AmPort(antenna);
                    SetGain(channel);
                    Check("Update AmPortSelect", sdrplay_api_Update(_device.Dev, _device.Tuner, reason | SDRplayApi.UpdateRspDuoAmPortSelect, SDRplayApi.UpdateExt1None));
                }
            }
        }

        public bool BiasT
        {
            get { return _biasT; }
            set
            {
                _biasT = value;
                UpdateTunerOption("Update BiasTControl", SDRplayApi.UpdateRspDuoBiasTControl);
            }
        }

        // MW/FM broadcast notch.
        public bool RfNotch
        {
            get { return _rfNotch; }
            set
            {
                _rfNotch = value;
                UpdateTunerOption("Update RfNotchControl", SDRplayApi.UpdateRspDuoRfNotchControl);
            }
        }

        public bool DabNotch
        {
            get { return _dabNotch; }
            set
            {
                _dabNotch = value;
                UpdateTunerOption("Update RfDabNotchControl", SDRplayApi.UpdateRspDuoRfDabNotchControl);
            }
        }

        // AM notch of tuner 1; sent only while tuner 1 is active.
        public bool AmNotch
        {
            get { return _amNotch; }
            set
            {
                _amNotch = value;
                if (_device.Tuner == SDRplayApi.TunerA || !_streaming)
                {
                    UpdateTunerOption("Update Tuner1AmNotchControl", SDRplayApi.UpdateRspDuoTuner1AmNotchControl);
                }
            }
        }

        // Frequency correction of the reference oscillator.
        public double Ppm
        {
            get { return Interlocked.CompareExchange(ref _ppm, 0, 0); }
            set
            {
                // A non-numeric value (NaN from a damaged setting) would reach the API and the panel: use 0.
                if (!double.IsFinite(value))
                {
                    value = 0;
                }
                Interlocked.Exchange(ref _ppm, Math.Min(Math.Max(value, -MaxPpm), MaxPpm));
                lock (_apiLock)
                {
                    if (_streaming)
                    {
                        ((SDRplayDeviceParams*)_deviceParams)->DevParams->Ppm = Ppm;
                        Check("Update Ppm", sdrplay_api_Update(_device.Dev, _device.Tuner, SDRplayApi.UpdateDevPpm, SDRplayApi.UpdateExt1None));
                    }
                }
            }
        }

        public const double MaxPpm = 100;

        public bool Overload => _overload;

        public int ReportedIFGainReduction => _reportedIFGainReduction;

        public int ReportedLnaGainReduction => _reportedLnaGainReduction;

        public double ReportedGain => Interlocked.CompareExchange(ref _reportedGain, 0, 0);

        public bool IsStreaming => _streaming;

        public int DecimationStage
        {
            get { return _decimationStage; }
            set
            {
                _decimationStage = Math.Min(Math.Max(value, 0), MaxDecimationStage);
                lock (_apiLock)
                {
                    if (_streaming)
                    {
                        SetDecimation((SDRplayRxChannelParams*)_channel);
                        Check("Update Decimation", sdrplay_api_Update(_device.Dev, _device.Tuner, SDRplayApi.UpdateCtrlDecimation, SDRplayApi.UpdateExt1None));
                    }
                }
            }
        }

        // The SDR# gain slider is not used (MaximumGainIndex = 0): the index is ignored.
        public int GainIndex
        {
            get { return 0; }
            set { }
        }

        public void Start()
        {
            lock (_apiLock)
            {
                if (_streaming)
                {
                    return;
                }
                _failure = "";
                try
                {
                    Open();
                }
                catch (DllNotFoundException ex)
                {
                    _failure = "sdrplay_api.dll not found (SDRplay API 3 installed?): " + ex.Message;
                }
                catch (EntryPointNotFoundException ex)
                {
                    _failure = "sdrplay_api.dll: " + ex.Message;
                }
                if (!_streaming)
                {
                    CloseDevice();
                }
            }
        }

        public void Stop()
        {
            lock (_apiLock)
            {
                CloseDevice();
            }
        }

        public int Read(byte[] buffer, int offset)
        {
            var ring = _ring;
            if (ring == null)
            {
                return 0;
            }
            if (_overloadPending)
            {
                _overloadPending = false;
                lock (_apiLock)
                {
                    if (_streaming)
                    {
                        Check("Update OverloadMsgAck", sdrplay_api_Update(_device.Dev, _device.Tuner, SDRplayApi.UpdateCtrlOverloadMsgAck, SDRplayApi.UpdateExt1None));
                    }
                }
            }
            var slot = ring.Acquire(true);
            if (slot == null)
            {
                return 0;
            }
            fixed (byte* destination = &buffer[offset])
            {
                Buffer.MemoryCopy(slot, destination, buffer.Length - offset, SlotSamples * 4);
            }
            ring.Release();

            _measureSamples += SlotSamples;
            var now = Stopwatch.GetTimestamp();
            var elapsed = (double)(now - _measureStart) / Stopwatch.Frequency;
            if (elapsed >= MeasureWindow)
            {
                Interlocked.Exchange(ref _measuredSampleRate, _measureSamples / elapsed);
                _measureSamples = 0;
                _measureStart = now;
            }
            return SlotSamples;
        }

        public void Dispose()
        {
            Stop();
            if (_ring != null)
            {
                _ring.Dispose();
                _ring = null;
            }
            if (_staging != null)
            {
                _staging.Dispose();
                _staging = null;
            }
            if (_self.IsAllocated)
            {
                _self.Free();
            }
        }

        // Called under _apiLock. Leaves _streaming true on success; on failure sets _failure and returns.
        private void Open()
        {
            if (!Check("Open", sdrplay_api_Open()))
            {
                return;
            }
            _apiOpen = true;

            float version;
            if (!Check("ApiVersion", sdrplay_api_ApiVersion(&version)))
            {
                return;
            }
            if (version != SDRplayApi.Version)
            {
                _failure = "API version " + version.ToString("0.00") + ", the plugin is written for " + SDRplayApi.Version.ToString("0.00");
                return;
            }

            sdrplay_api_LockDeviceApi();
            var devices = new SDRplayDevice[SDRplayApi.MaxDevices];
            uint count = 0;
            int result;
            fixed (SDRplayDevice* list = devices)
            {
                result = sdrplay_api_GetDevices(list, &count, SDRplayApi.MaxDevices);
            }
            var index = -1;
            if (result == SDRplayApi.Success)
            {
                for (var i = 0; i < (int)count; i++)
                {
                    if (devices[i].HwVer == SDRplayApi.RSPduoId && (devices[i].RspDuoMode & SDRplayApi.RspDuoModeMaster) != 0)
                    {
                        index = i;
                        break;
                    }
                }
            }
            if (index < 0)
            {
                sdrplay_api_UnlockDeviceApi();
                if (Check("GetDevices", result))
                {
                    _failure = "No free RSPduo found (" + count + " SDRplay device(s) listed)";
                }
                return;
            }

            _device = devices[index];
            _device.RspDuoMode = SDRplayApi.RspDuoModeSingleTuner;
            _device.Tuner = RspDuoAntenna.Tuner(_antenna);
            fixed (SDRplayDevice* device = &_device)
            {
                result = sdrplay_api_SelectDevice(device);
            }
            sdrplay_api_UnlockDeviceApi();
            if (!Check("SelectDevice", result))
            {
                return;
            }
            _selected = true;

            SDRplayDeviceParams* deviceParams = null;
            if (!Check("GetDeviceParams", sdrplay_api_GetDeviceParams(_device.Dev, &deviceParams)))
            {
                return;
            }
            var channel = ChannelOf(deviceParams);
            if (deviceParams == null || deviceParams->DevParams == null || channel == null)
            {
                _failure = "GetDeviceParams returned no parameters";
                return;
            }
            _deviceParams = (IntPtr)deviceParams;
            deviceParams->DevParams->FsFreq.FsHz = RspDuoSampleRates.SampleRate[_activeRateIndex];
            deviceParams->DevParams->Ppm = Ppm;
            SetTunerOptions(channel);
            channel->TunerParams.RfFreq.RfHz = Frequency;
            channel->TunerParams.BwType = RspDuoSampleRates.BwType[_activeRateIndex];
            channel->TunerParams.IfType = SDRplayApi.IFZero;
            SetDecimation(channel);
            SetGain(channel);
            SetAgc(channel);
            _channel = (IntPtr)channel;
            _overload = false;
            _reportedIFGainReduction = -1;
            _reportedLnaGainReduction = -1;

            fixed (byte* serial = _device.SerNo)
            {
                _serial = Marshal.PtrToStringAnsi((IntPtr)serial);
            }
            UpdateDeviceName();

            if (_ring != null)
            {
                _ring.Dispose();
            }
            var slots = (int)(RspDuoSampleRates.SampleRate[_activeRateIndex] * RingDuration / SlotSamples) + 1;
            _ring = new CircularBuffer(SlotSamples * 4, slots);
            _measureSamples = 0;
            _measureStart = Stopwatch.GetTimestamp();
            Interlocked.Exchange(ref _measuredSampleRate, 0);
            _overruns = 0; // per stream: the status shows this session's losses
            _overloadPending = false;
            if (!_self.IsAllocated)
            {
                _self = GCHandle.Alloc(this);
            }

            var callbacks = new SDRplayCallbackFns();
            callbacks.StreamACbFn = &OnStreamA;
            callbacks.StreamBCbFn = &OnStreamB;
            callbacks.EventCbFn = &OnEvent;
            // The stream callback may run before Init returns: it reads _ring, set above.
            _streaming = true;
            if (!Check("Init", sdrplay_api_Init(_device.Dev, &callbacks, (void*)GCHandle.ToIntPtr(_self))))
            {
                _streaming = false;
            }
        }

        // Called under _apiLock: stops the stream, closes the ring (a waiting Read returns 0), releases the device and the API.
        private void CloseDevice()
        {
            if (_streaming)
            {
                _streaming = false;
                Check("Uninit", sdrplay_api_Uninit(_device.Dev));
            }
            var ring = _ring;
            if (ring != null)
            {
                ring.Close();
            }
            if (_selected)
            {
                fixed (SDRplayDevice* device = &_device)
                {
                    sdrplay_api_ReleaseDevice(device);
                }
                _selected = false;
            }
            _channel = IntPtr.Zero;
            _deviceParams = IntPtr.Zero;
            Interlocked.Exchange(ref _measuredSampleRate, 0); // no stream: no rate
            if (_apiOpen)
            {
                sdrplay_api_Close();
                _apiOpen = false;
            }
        }

        private void UpdateGain()
        {
            lock (_apiLock)
            {
                if (_streaming)
                {
                    SetGain((SDRplayRxChannelParams*)_channel);
                    Check("Update Gr", sdrplay_api_Update(_device.Dev, _device.Tuner, SDRplayApi.UpdateTunerGr, SDRplayApi.UpdateExt1None));
                }
            }
        }

        private void UpdateAgc()
        {
            lock (_apiLock)
            {
                if (_streaming)
                {
                    SetAgc((SDRplayRxChannelParams*)_channel);
                    Check("Update Agc", sdrplay_api_Update(_device.Dev, _device.Tuner, SDRplayApi.UpdateCtrlAgc, SDRplayApi.UpdateExt1None));
                }
            }
        }

        private static bool IsHiZ(int antenna, long frequency)
        {
            return antenna == RspDuoAntenna.Tuner1HiZ && frequency < RspDuoGainTable.HiZMaxFrequency;
        }

        // The channel parameters of the selected tuner (single tuner mode: rxChannelB for tuner B).
        private SDRplayRxChannelParams* ChannelOf(SDRplayDeviceParams* deviceParams)
        {
            if (deviceParams == null)
            {
                return null;
            }
            return _device.Tuner == SDRplayApi.TunerB ? deviceParams->RxChannelB : deviceParams->RxChannelA;
        }

        private void UpdateDeviceName()
        {
            _deviceName = "RSPduo " + _serial + ", " + RspDuoAntenna.Names[_antenna];
        }

        private void SetTunerOptions(SDRplayRxChannelParams* channel)
        {
            // The Bias-T is on the tuner 2 port: with tuner 1 selected it is sent off.
            channel->RspDuoTunerParams.BiasTEnable = (byte)(_biasT && _antenna == RspDuoAntenna.Tuner2_50Ohm ? 1 : 0);
            channel->RspDuoTunerParams.Tuner1AmPortSel = RspDuoAntenna.AmPort(_antenna);
            channel->RspDuoTunerParams.Tuner1AmNotchEnable = (byte)(_amNotch ? 1 : 0);
            channel->RspDuoTunerParams.RfNotchEnable = (byte)(_rfNotch ? 1 : 0);
            channel->RspDuoTunerParams.RfDabNotchEnable = (byte)(_dabNotch ? 1 : 0);
        }

        private void UpdateTunerOption(string call, uint reason)
        {
            lock (_apiLock)
            {
                if (_streaming)
                {
                    SetTunerOptions((SDRplayRxChannelParams*)_channel);
                    Check(call, sdrplay_api_Update(_device.Dev, _device.Tuner, reason, SDRplayApi.UpdateExt1None));
                }
            }
        }

        // Called under _apiLock while streaming. The API keeps the settings on the swap, except B to A
        // with the Hi-Z port (API spec 3.18): the gain and the tuner options are sent again afterwards.
        // Returns false when the API refused the swap (the device stays on its tuner).
        private bool SwapTuner()
        {
            // The API updates the tuner in the device enumeration structure (API spec 3.18).
            int result;
            fixed (int* tunerPtr = &_device.Tuner)
            {
                result = sdrplay_api_SwapRspDuoActiveTuner(_device.Dev, tunerPtr, RspDuoAntenna.AmPort(_antenna));
            }
            if (!Check("SwapRspDuoActiveTuner", result))
            {
                return false;
            }
            var tuner = _device.Tuner;
            var channel = ChannelOf((SDRplayDeviceParams*)_deviceParams);
            if (channel == null)
            {
                _failure = "SwapRspDuoActiveTuner: no channel parameters for the new tuner";
                return true;
            }
            _channel = (IntPtr)channel;
            UpdateDeviceName();
            SetTunerOptions(channel);
            SetGain(channel);
            SetAgc(channel);
            var reason = SDRplayApi.UpdateTunerGr | SDRplayApi.UpdateCtrlAgc | SDRplayApi.UpdateRspDuoBiasTControl
                | SDRplayApi.UpdateRspDuoRfNotchControl | SDRplayApi.UpdateRspDuoRfDabNotchControl;
            if (tuner == SDRplayApi.TunerA)
            {
                reason |= SDRplayApi.UpdateRspDuoAmPortSelect | SDRplayApi.UpdateRspDuoTuner1AmNotchControl;
            }
            Check("Update after swap", sdrplay_api_Update(_device.Dev, _device.Tuner, reason, SDRplayApi.UpdateExt1None));
            return true;
        }

        private void SetGain(SDRplayRxChannelParams* channel)
        {
            channel->TunerParams.Gain.LNAstate = (byte)_lnaState;
            // With the AGC on, the AGC owns the IF gain reduction: leave its current value.
            if (_agcMode == SDRplayApi.AgcDisable)
            {
                channel->TunerParams.Gain.GRdB = _ifGainReduction;
            }
        }

        private void SetAgc(SDRplayRxChannelParams* channel)
        {
            channel->CtrlParams.Agc.Enable = _agcMode;
            // The API accepts a narrower setpoint range at the higher sample rates.
            channel->CtrlParams.Agc.SetPointDBfs = Math.Max(_agcSetPoint, RspDuoSampleRates.MinAgcSetPoint(RspDuoSampleRates.SampleRate[_activeRateIndex]));
        }

        private void SetDecimation(SDRplayRxChannelParams* channel)
        {
            var stage = _decimationStage;
            channel->CtrlParams.Decimation.Enable = (byte)(stage > 0 ? 1 : 0);
            channel->CtrlParams.Decimation.DecimationFactor = (byte)(1 << stage);
            channel->CtrlParams.Decimation.WideBandSignal = 0;
        }

        // Returns true on success; otherwise records the failure with the API's error text.
        private bool Check(string call, int result)
        {
            if (result == SDRplayApi.Success)
            {
                // A failed Update is a transient state: the next successful Update clears it.
                if (call.StartsWith("Update") && _failure.StartsWith("Update"))
                {
                    _failure = "";
                }
                return true;
            }
            _failure = call + ": " + Marshal.PtrToStringAnsi((IntPtr)sdrplay_api_GetErrorString(result)) + " (" + result + ")";
            return false;
        }

        // API stream thread: interleave I/Q into the staging buffer and hand it to the ring without waiting.
        private void WriteSamples(short* xi, short* xq, int count)
        {
            var ring = _ring;
            if (ring == null || count <= 0)
            {
                return;
            }
            if (_staging == null || _staging.Length < count)
            {
                if (_staging != null)
                {
                    _staging.Dispose();
                }
                _staging = UnsafeBuffer.Create(count, 2 * sizeof(short));
            }
            var stagingPtr = (short*)_staging;
            for (var i = 0; i < count; i++)
            {
                stagingPtr[2 * i] = xi[i];
                stagingPtr[2 * i + 1] = xq[i];
            }
            if (!ring.Write((byte*)stagingPtr, 4 * count, false))
            {
                if (_overruns < int.MaxValue)
                {
                    _overruns++;
                }
            }
        }

        private void OnDeviceEvent(int eventId, void* parameters)
        {
            switch (eventId)
            {
                case SDRplayApi.EventGainChange:
                    var gain = (SDRplayGainCbParam*)parameters;
                    _reportedIFGainReduction = (int)gain->GRdB;
                    _reportedLnaGainReduction = (int)gain->LnaGRdB;
                    Interlocked.Exchange(ref _reportedGain, gain->CurrGain);
                    break;
                case SDRplayApi.EventPowerOverloadChange:
                    _overload = *(int*)parameters == SDRplayApi.OverloadDetected;
                    _overloadPending = true;
                    break;
                case SDRplayApi.EventDeviceRemoved:
                    _failure = "Device removed";
                    CloseRingFromEvent();
                    break;
                case SDRplayApi.EventDeviceFailure:
                    _failure = "Device failure";
                    CloseRingFromEvent();
                    break;
            }
        }

        private void CloseRingFromEvent()
        {
            var ring = _ring;
            if (ring != null)
            {
                ring.Close();
            }
        }

        private static IntPtr ResolveLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName != LibraryName)
            {
                return IntPtr.Zero;
            }
            // The SDRplay installer records its folder in the registry; the DLL is not on the PATH.
            var folder = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\SDRplay\Service\API", "Install_Dir", null) as string;
            if (folder != null)
            {
                IntPtr handle;
                if (NativeLibrary.TryLoad(Path.Combine(folder, Environment.Is64BitProcess ? "x64" : "x86", LibraryName + ".dll"), out handle))
                {
                    return handle;
                }
            }
            return IntPtr.Zero;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void OnStreamA(short* xi, short* xq, SDRplayStreamCbParams* parameters, uint numSamples, uint reset, void* context)
        {
            var source = (SDRplaySource)GCHandle.FromIntPtr((IntPtr)context).Target;
            source.WriteSamples(xi, xq, (int)numSamples);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void OnStreamB(short* xi, short* xq, SDRplayStreamCbParams* parameters, uint numSamples, uint reset, void* context)
        {
            // Single tuner mode has one stream; if tuner B delivers it on this callback, it is the same output.
            var source = (SDRplaySource)GCHandle.FromIntPtr((IntPtr)context).Target;
            source.WriteSamples(xi, xq, (int)numSamples);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void OnEvent(int eventId, int tuner, void* parameters, void* context)
        {
            var source = (SDRplaySource)GCHandle.FromIntPtr((IntPtr)context).Target;
            source.OnDeviceEvent(eventId, parameters);
        }

        #region Native Methods

        private const string LibraryName = "sdrplay_api";

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial int sdrplay_api_Open();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial int sdrplay_api_Close();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial int sdrplay_api_ApiVersion(float* apiVer);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial int sdrplay_api_LockDeviceApi();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial int sdrplay_api_UnlockDeviceApi();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial int sdrplay_api_GetDevices(SDRplayDevice* devices, uint* numDevs, uint maxDevs);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial int sdrplay_api_SelectDevice(SDRplayDevice* device);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial int sdrplay_api_ReleaseDevice(SDRplayDevice* device);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial byte* sdrplay_api_GetErrorString(int err);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial int sdrplay_api_GetDeviceParams(IntPtr dev, SDRplayDeviceParams** deviceParams);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial int sdrplay_api_Init(IntPtr dev, SDRplayCallbackFns* callbackFns, void* cbContext);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial int sdrplay_api_Uninit(IntPtr dev);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial int sdrplay_api_Update(IntPtr dev, int tuner, uint reasonForUpdate, uint reasonForUpdateExt1);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial int sdrplay_api_SwapRspDuoActiveTuner(IntPtr dev, int* currentTuner, int tuner1AmPortSel);

        #endregion
    }
}
