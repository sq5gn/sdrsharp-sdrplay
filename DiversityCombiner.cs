using SDRSharp.Radio;
using System;
using System.Threading;

namespace SDRSharp.SDRplay
{
    // Diversity combination of the two RSPduo tuners (issue #36, stages 3 and 5): out = A + w * B, w = g * e^(j phi).
    // Pairs the packets of tuner A and tuner B with the same first sample number (measured: always equal) and
    // measures both streams over windows of about 100 ms.
    // Auto (closed form, Rab = sum A * conj(B), Raa = sum |A|^2, Rbb = sum |B|^2):
    //   Null: w = -Rab / Rbb, the least-squares weight: minimum output power, a null on the strongest signal;
    //   Max:  w = Rab / Raa, maximal ratio combining for equal tuner noise: B in phase with A, weighted |hB| / |hA|.
    // Null measures the reference band when one is set, Max the SDR# filter passband (BandEstimator, 1 s); without
    // a band, or with a band wider than MaxBandWidth or outside the stream, both measure the whole stream (100 ms).
    // Frequencies of the bands are in the SDR# display domain; the stream centre is in the device domain.
    // Threads: the UI thread sets the weight, the bands and requests an auto; the source's command thread sets the
    // centre and repeats the auto after a retune; the API stream threads call Pair under the source's write lock
    // (one caller at a time); the display reads the published values.
    public unsafe sealed class DiversityCombiner : IDisposable
    {
        public const int AutoNone = 0;
        public const int AutoNull = 1;
        public const int AutoMax = 2;

        // What the last auto measured.
        public const int BandNone = -1;
        public const int BandWhole = 0;
        public const int BandReference = 1;
        public const int BandFilter = 2;
        public const int BandReferenceOutside = 3; // reference set, but too wide or outside the stream: whole band
        public const int BandFilterOutside = 4;

        public const double MinGainDB = -30;
        public const double MaxGainDB = 30;
        public const double MaxBandWidth = 25000; // Hz

        private const double WindowDuration = 0.1; // sec of samples per measurement
        private const double BandWindowDuration = 1.0; // sec of samples per band measurement (SSB speech has pauses)
        private const double SettleDuration = 0.05; // sec skipped after an auto request (a retune settles)
        private const double MaxBandEdge = 0.45; // x sample rate: the band stays inside the flat part of the stream
        private const double FullScalePower = 32768.0 * 32768.0;
        private const int AutoModeMask = 3;
        private const int MaxAutoSerial = int.MaxValue >> 2;

        private readonly object _weightLock = new object();
        private float _weightReal; // w, read by Pair under _weightLock
        private float _weightImag;
        private double _phase; // deg, 0..360, clockwise
        private double _gainDB;

        // Bands and requests, under _bandLock.
        private readonly object _bandLock = new object();
        private long _centerFrequency; // Hz, device domain
        private long _frequencyShift; // Hz, display - device
        private bool _spectrumInverted;
        private bool _hasFilter;
        private long _filterLow; // Hz, display domain
        private long _filterHigh;
        private bool _hasReference;
        private long _referenceLow; // Hz, display domain
        private long _referenceHigh;
        private int _autoSerial;
        private int _requestBand = BandWhole;
        private double _requestOffset; // Hz from the stream centre
        private double _requestWidth; // Hz

        private volatile int _autoToken; // serial << 2 | auto mode; mode AutoNone = nothing pending
        private volatile int _lastAuto = AutoNone;
        private volatile int _sampleRate = 2000000; // S/s of the packets (after the API's decimation)

        // Stream thread only (Pair).
        private UnsafeBuffer _pending;
        private int _pendingTuner;
        private uint _pendingNumber;
        private int _pendingCount;
        private int _activeToken = -1;
        private int _autoActive = AutoNone;
        private BandEstimator _estimator;
        private int _estimatorBand = BandWhole;
        private int _skip;
        private int _windowLength;
        private int _windowCount;
        private double _sumAA;
        private double _sumBB;
        private double _sumOut;
        private double _sumABReal;
        private double _sumABImag;
        private int _windowSaturated;

        // Display (published per window).
        private double _powerA;
        private double _powerB;
        private double _powerOut;
        private double _coherence;
        private double _lastCoherence;
        private volatile int _lastBand = BandNone;
        private volatile int _lastMode = AutoNone;
        private volatile int _saturated; // samples clipped in the last window
        private volatile int _unpaired; // packets without a partner since Reset, saturating
        private volatile bool _measured;

        public DiversityCombiner()
        {
            SetWeight(0, 0);
        }

        // Clockwise degrees, as drawn on the phase wheel.
        public double Phase
        {
            get { return Interlocked.CompareExchange(ref _phase, 0, 0); }
            set { SetWeight(value, GainDB); }
        }

        // Level of B relative to A in the sum, dB.
        public double GainDB
        {
            get { return Interlocked.CompareExchange(ref _gainDB, 0, 0); }
            set { SetWeight(Phase, value); }
        }

        public void SetWeight(double phase, double gainDB)
        {
            if (!double.IsFinite(phase))
            {
                phase = 0;
            }
            if (!double.IsFinite(gainDB))
            {
                gainDB = 0;
            }
            phase = WrapPhase(phase);
            gainDB = Math.Min(Math.Max(gainDB, MinGainDB), MaxGainDB);
            var gain = Math.Pow(10, gainDB / 20);
            var radians = -phase * Math.PI / 180;
            lock (_weightLock)
            {
                _weightReal = (float)(gain * Math.Cos(radians));
                _weightImag = (float)(gain * Math.Sin(radians));
                Interlocked.Exchange(ref _phase, phase);
                Interlocked.Exchange(ref _gainDB, gainDB);
            }
        }

        // Device frequency of the stream centre, Hz (set by the source at the start and at every retune).
        public long CenterFrequency
        {
            set
            {
                lock (_bandLock)
                {
                    if (_centerFrequency != value)
                    {
                        _centerFrequency = value;
                        RestartPending();
                    }
                }
            }
        }

        // SDR# display frequency minus device frequency (FrequencyShift when enabled), Hz.
        public long FrequencyShift
        {
            set
            {
                lock (_bandLock)
                {
                    if (_frequencyShift != value)
                    {
                        _frequencyShift = value;
                        RestartPending();
                    }
                }
            }
        }

        // SDR# SwapIq: the display shows the stream mirrored.
        public bool SpectrumInverted
        {
            set
            {
                lock (_bandLock)
                {
                    if (_spectrumInverted != value)
                    {
                        _spectrumInverted = value;
                        RestartPending();
                    }
                }
            }
        }

        // Passband of the SDR# channel filter, display domain, Hz (the plugin updates it on every change).
        public void SetFilterBand(long low, long high)
        {
            var newLow = Math.Min(low, high);
            var newHigh = Math.Max(low, high);
            lock (_bandLock)
            {
                if (newLow != _filterLow || newHigh != _filterHigh)
                {
                    _filterLow = newLow;
                    _filterHigh = newHigh;
                    _hasFilter = _filterHigh > _filterLow;
                    RestartPending();
                }
            }
        }

        public bool GetFilterBand(out long low, out long high)
        {
            lock (_bandLock)
            {
                low = _filterLow;
                high = _filterHigh;
                return _hasFilter;
            }
        }

        // Reference band for Null, display domain, Hz.
        public void SetReference(long low, long high)
        {
            lock (_bandLock)
            {
                _referenceLow = Math.Min(low, high);
                _referenceHigh = Math.Max(low, high);
                _hasReference = _referenceHigh > _referenceLow;
                RestartPending();
            }
        }

        public void ClearReference()
        {
            lock (_bandLock)
            {
                _hasReference = false;
                RestartPending();
            }
        }

        public bool GetReference(out long low, out long high)
        {
            lock (_bandLock)
            {
                low = _referenceLow;
                high = _referenceHigh;
                return _hasReference;
            }
        }

        // AutoNull or AutoMax: measured after the settle time, then applied.
        public void RequestAuto(int mode)
        {
            if (mode == AutoNull || mode == AutoMax)
            {
                lock (_bandLock)
                {
                    _lastAuto = mode;
                    NewToken(mode);
                }
            }
        }

        // Under _bandLock: a new token for the mode; the stream thread starts the measurement again.
        private void NewToken(int mode)
        {
            if (_autoSerial < MaxAutoSerial)
            {
                _autoSerial++;
            }
            else
            {
                _autoSerial = 1;
            }
            _autoToken = _autoSerial << 2 | mode;
        }

        // Under _bandLock: a band, the centre or the frequency domain changed while an auto is pending (after a
        // retune the source repeats the auto before SDR# moves its filter); the measurement starts again.
        private void RestartPending()
        {
            var mode = _autoToken & AutoModeMask;
            if (mode != AutoNone)
            {
                NewToken(mode);
            }
        }

        // After a retune: the phase between the tuners is new (issue #36, stage 1b); repeats the last auto.
        public void RepeatAuto()
        {
            RequestAuto(_lastAuto);
        }

        public bool AutoPending => (_autoToken & AutoModeMask) != AutoNone;

        public int SampleRate
        {
            get { return _sampleRate; }
            set { _sampleRate = Math.Max(value, 1); }
        }

        // dBFS per window; NaN before the first window.
        public double LevelA => Level(ref _powerA);

        public double LevelB => Level(ref _powerB);

        public double LevelOut => Level(ref _powerOut);

        // |Rab| / sqrt(Raa * Rbb), 0..1: how much of the two streams is the same signal.
        public double Coherence => _measured ? Interlocked.CompareExchange(ref _coherence, 0, 0) : double.NaN;

        // The last auto: its mode, the band it measured (Band*), the coherence in that band.
        public int LastMode => _lastMode;

        public int LastBand => _lastBand;

        public double LastCoherence => Interlocked.CompareExchange(ref _lastCoherence, 0, 0);

        public int Saturated => _saturated;

        public int Unpaired => _unpaired;

        // Stream start (no stream thread running): forgets the pending packet and the measurements; a pending auto
        // starts again.
        public void Reset()
        {
            _pendingTuner = 0;
            _pendingCount = 0;
            _activeToken = -1;
            _autoActive = AutoNone;
            DisposeEstimator();
            _skip = 0;
            _unpaired = 0;
            _saturated = 0;
            _measured = false;
            ResetWindow();
        }

        // Under _bandLock, when the measurement starts: the band it measures, as an offset and a width in the
        // stream, from the bands, the centre and the sample rate of that moment.
        private void SelectBand(int mode)
        {
            _requestBand = BandWhole;
            long low;
            long high;
            int band;
            int outside;
            if (mode == AutoNull && _hasReference)
            {
                low = _referenceLow;
                high = _referenceHigh;
                band = BandReference;
                outside = BandReferenceOutside;
            }
            else if (mode == AutoMax && _hasFilter)
            {
                low = _filterLow;
                high = _filterHigh;
                band = BandFilter;
                outside = BandFilterOutside;
            }
            else
            {
                return;
            }
            double lowOffset = low - _frequencyShift - _centerFrequency;
            double highOffset = high - _frequencyShift - _centerFrequency;
            if (_spectrumInverted)
            {
                var mirrored = -lowOffset;
                lowOffset = -highOffset;
                highOffset = mirrored;
            }
            var edge = MaxBandEdge * _sampleRate;
            var width = highOffset - lowOffset;
            if (width <= 0 || width > MaxBandWidth || lowOffset < -edge || highOffset > edge)
            {
                _requestBand = outside;
                return;
            }
            _requestBand = band;
            _requestOffset = (lowOffset + highOffset) / 2;
            _requestWidth = width;
        }

        // Stream thread. Returns the number of combined IQ samples written to output (interleaved int16, at least
        // 2 * count shorts), 0 while the packet waits for its partner. write = false: measures only.
        public int Pair(int tuner, uint number, short* xi, short* xq, int count, short* output, bool write)
        {
            if (count <= 0)
            {
                return 0;
            }
            if (_pendingCount == 0 || _pendingTuner == tuner || _pendingNumber != number || _pendingCount != count)
            {
                if (_pendingCount != 0 && _unpaired < int.MaxValue)
                {
                    _unpaired++;
                }
                Store(tuner, number, xi, xq, count);
                return 0;
            }
            var pendingPtr = (short*)_pending;
            _pendingCount = 0;
            if (tuner == SDRplayApi.TunerB)
            {
                return Combine(pendingPtr, xi, xq, count, output, write);
            }
            return CombineSwapped(xi, xq, pendingPtr, count, output, write);
        }

        private void Store(int tuner, uint number, short* xi, short* xq, int count)
        {
            if (_pending == null || _pending.Length < count)
            {
                if (_pending != null)
                {
                    _pending.Dispose();
                }
                _pending = UnsafeBuffer.Create(count, 2 * sizeof(short));
            }
            var pendingPtr = (short*)_pending;
            for (var i = 0; i < count; i++)
            {
                pendingPtr[2 * i] = xi[i];
                pendingPtr[2 * i + 1] = xq[i];
            }
            _pendingTuner = tuner;
            _pendingNumber = number;
            _pendingCount = count;
        }

        // a: tuner A interleaved (the pending packet), bi / bq: tuner B.
        private int Combine(short* a, short* bi, short* bq, int count, short* output, bool write)
        {
            float wr;
            float wi;
            lock (_weightLock)
            {
                wr = _weightReal;
                wi = _weightImag;
            }
            BeginPacket(count);
            var accumulate = _skip == 0;
            double sumAA = 0;
            double sumBB = 0;
            double sumOut = 0;
            double sumABReal = 0;
            double sumABImag = 0;
            var saturated = 0;
            for (var i = 0; i < count; i++)
            {
                float ar = a[2 * i];
                float ai = a[2 * i + 1];
                float br = bi[i];
                float bim = bq[i];
                var outReal = ar + wr * br - wi * bim;
                var outImag = ai + wr * bim + wi * br;
                if (accumulate)
                {
                    sumAA += ar * ar + ai * ai;
                    sumBB += br * br + bim * bim;
                    sumABReal += ar * br + ai * bim;
                    sumABImag += ai * br - ar * bim;
                    sumOut += outReal * outReal + outImag * outImag;
                }
                if (write)
                {
                    output[2 * i] = Saturate(outReal, ref saturated);
                    output[2 * i + 1] = Saturate(outImag, ref saturated);
                }
            }
            if (accumulate && _estimator != null)
            {
                _estimator.Feed(a, a + 1, 2, bi, bq, 1, count);
            }
            EndPacket(count, accumulate, sumAA, sumBB, sumOut, sumABReal, sumABImag, saturated);
            return write ? count : 0;
        }

        // B arrived first: the pending packet is B, the current one A.
        private int CombineSwapped(short* ai, short* aq, short* b, int count, short* output, bool write)
        {
            float wr;
            float wi;
            lock (_weightLock)
            {
                wr = _weightReal;
                wi = _weightImag;
            }
            BeginPacket(count);
            var accumulate = _skip == 0;
            double sumAA = 0;
            double sumBB = 0;
            double sumOut = 0;
            double sumABReal = 0;
            double sumABImag = 0;
            var saturated = 0;
            for (var i = 0; i < count; i++)
            {
                float ar = ai[i];
                float aim = aq[i];
                float br = b[2 * i];
                float bim = b[2 * i + 1];
                var outReal = ar + wr * br - wi * bim;
                var outImag = aim + wr * bim + wi * br;
                if (accumulate)
                {
                    sumAA += ar * ar + aim * aim;
                    sumBB += br * br + bim * bim;
                    sumABReal += ar * br + aim * bim;
                    sumABImag += aim * br - ar * bim;
                    sumOut += outReal * outReal + outImag * outImag;
                }
                if (write)
                {
                    output[2 * i] = Saturate(outReal, ref saturated);
                    output[2 * i + 1] = Saturate(outImag, ref saturated);
                }
            }
            if (accumulate && _estimator != null)
            {
                _estimator.Feed(ai, aq, 1, b, b + 1, 2, count);
            }
            EndPacket(count, accumulate, sumAA, sumBB, sumOut, sumABReal, sumABImag, saturated);
            return write ? count : 0;
        }

        // A new auto request restarts the window after the settle time and rebuilds the band measurement (the
        // allocation happens here, once per request); the skip counts down per packet.
        private void BeginPacket(int count)
        {
            var token = _autoToken;
            if (token != _activeToken)
            {
                _activeToken = token;
                _autoActive = token & AutoModeMask;
                ResetWindow();
                _skip = _autoActive != AutoNone ? (int)(_sampleRate * SettleDuration) : 0;
                RebuildEstimator();
            }
            if (_skip > 0)
            {
                _skip = _skip > count ? _skip - count : 0;
            }
        }

        private void RebuildEstimator()
        {
            int band;
            double offset;
            double width;
            lock (_bandLock)
            {
                SelectBand(_autoActive);
                band = _requestBand;
                offset = _requestOffset;
                width = _requestWidth;
            }
            DisposeEstimator();
            _estimatorBand = band;
            if (_autoActive != AutoNone && (band == BandReference || band == BandFilter))
            {
                _estimator = new BandEstimator(_sampleRate, offset, width, BandWindowDuration);
            }
        }

        private void EndPacket(int count, bool accumulated, double sumAA, double sumBB, double sumOut, double sumABReal, double sumABImag, int saturated)
        {
            if (!accumulated)
            {
                return;
            }
            if (_estimator != null && _estimator.Done)
            {
                FinishBand();
            }
            _sumAA += sumAA;
            _sumBB += sumBB;
            _sumOut += sumOut;
            _sumABReal += sumABReal;
            _sumABImag += sumABImag;
            _windowSaturated = Math.Min(_windowSaturated + saturated, int.MaxValue - 2 * count);
            if (_windowCount < _windowLength - count)
            {
                _windowCount += count;
                return;
            }
            _windowCount += count;
            Publish();
            ResetWindow();
        }

        private void Publish()
        {
            var n = (double)_windowCount;
            Interlocked.Exchange(ref _powerA, _sumAA / n / FullScalePower);
            Interlocked.Exchange(ref _powerB, _sumBB / n / FullScalePower);
            Interlocked.Exchange(ref _powerOut, _sumOut / n / FullScalePower);
            var coherence = Coherent(_sumAA, _sumBB, _sumABReal, _sumABImag);
            Interlocked.Exchange(ref _coherence, coherence);
            _saturated = _windowSaturated;
            _measured = true;
            // A band measurement in progress applies its own result (FinishBand).
            if (_autoActive == AutoNone || _estimator != null)
            {
                return;
            }
            Apply(_sumAA, _sumBB, _sumABReal, _sumABImag, coherence);
        }

        private void FinishBand()
        {
            var estimator = _estimator;
            Apply(estimator.SumAA, estimator.SumBB, estimator.SumABReal, estimator.SumABImag, Coherent(estimator.SumAA, estimator.SumBB, estimator.SumABReal, estimator.SumABImag));
            DisposeEstimator();
        }

        // Sets w from the sums for the active auto, then clears the request unless a newer one arrived.
        private void Apply(double sumAA, double sumBB, double sumABReal, double sumABImag, double coherence)
        {
            var auto = _autoActive;
            // w for out = A + w * B (Rab = sum A * conj(B)); no power in the band: the weight stays.
            double wr = 0;
            double wi = 0;
            if (auto == AutoNull && sumBB > 0)
            {
                wr = -sumABReal / sumBB;
                wi = -sumABImag / sumBB;
            }
            else if (auto == AutoMax && sumAA > 0)
            {
                wr = sumABReal / sumAA;
                wi = sumABImag / sumAA;
            }
            var magnitude = Math.Sqrt(wr * wr + wi * wi);
            if (magnitude > 0)
            {
                // The phase wheel turns clockwise: phase = -arg(w).
                SetWeight(-Math.Atan2(wi, wr) * 180 / Math.PI, 20 * Math.Log10(magnitude));
            }
            Interlocked.Exchange(ref _lastCoherence, coherence);
            _lastBand = _estimatorBand;
            _lastMode = auto;
            var done = _activeToken & ~AutoModeMask;
            if (Interlocked.CompareExchange(ref _autoToken, done, _activeToken) == _activeToken)
            {
                _activeToken = done;
            }
            _autoActive = AutoNone;
        }

        private static double Coherent(double sumAA, double sumBB, double sumABReal, double sumABImag)
        {
            var norm = Math.Sqrt(sumAA * sumBB);
            return norm > 0 ? Math.Sqrt(sumABReal * sumABReal + sumABImag * sumABImag) / norm : 0;
        }

        private void ResetWindow()
        {
            _windowLength = Math.Max((int)(_sampleRate * WindowDuration), 4096);
            _windowCount = 0;
            _sumAA = 0;
            _sumBB = 0;
            _sumOut = 0;
            _sumABReal = 0;
            _sumABImag = 0;
            _windowSaturated = 0;
        }

        private void DisposeEstimator()
        {
            if (_estimator != null)
            {
                _estimator.Dispose();
                _estimator = null;
            }
        }

        private static short Saturate(float value, ref int saturated)
        {
            var rounded = (int)Math.Round(value);
            if (rounded > short.MaxValue)
            {
                saturated++;
                return short.MaxValue;
            }
            if (rounded < short.MinValue)
            {
                saturated++;
                return short.MinValue;
            }
            return (short)rounded;
        }

        private double Level(ref double power)
        {
            var p = Interlocked.CompareExchange(ref power, 0, 0);
            return _measured && p > 0 ? 10 * Math.Log10(p) : double.NaN;
        }

        private static double WrapPhase(double phase)
        {
            phase %= 360;
            if (phase < 0)
            {
                phase += 360;
            }
            return phase;
        }

        public void Dispose()
        {
            DisposeEstimator();
            if (_pending != null)
            {
                _pending.Dispose();
                _pending = null;
            }
        }
    }
}
