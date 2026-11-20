using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SDRSharp.SDRplay
{
    public enum BridgeState
    {
        Stopped,
        Listening,
        Connected,
        Failed
    }

    // SpyServer-compatible server on 127.0.0.1: one client at a time, IQ int16 only.
    // Threads: accept/receive (commands and status replies), sender (IQ messages). Socket writes are serialized by _writeLock.
    public class SpyServerBridge : IDisposable
    {
        private readonly IIQSource _source;
        private readonly object _writeLock = new object();
        private readonly object _controlLock = new object();
        private readonly byte[] _commandBuffer = new byte[SpyServerProtocol.CommandHeaderSize + SpyServerProtocol.MaxCommandBodySize];
        private readonly byte[] _statusBuffer = new byte[SpyServerProtocol.MessageHeaderSize + SpyServerProtocol.ClientSyncSize];
        private readonly byte[] _iqMessage;

        private int _port = 5555;
        private volatile bool _enabled;
        private TcpListener _listener;
        private Thread _acceptThread;
        private Thread _sessionThread;
        private volatile bool _sessionEnding; // the client has gone; the session thread is stopping the stream
        private volatile TcpClient _client;
        private NetworkStream _stream;
        private Thread _sendThread;
        private volatile bool _sending;
        private uint _sequence;

        private volatile BridgeState _state = BridgeState.Stopped;
        private volatile string _failure = "";
        private volatile string _clientName = "";
        private volatile string _lastCommand = "";
        private volatile int _clientVersion;
        private volatile int _streamingMode;
        private volatile int _iqFormat;
        private volatile int _digitalGain;
        private volatile bool _streamingEnabled;
        private volatile int _sequenceShown;

        public SpyServerBridge(IIQSource source)
        {
            _source = source;
            _iqMessage = new byte[SpyServerProtocol.MessageHeaderSize + 4 * source.BlockSamples];
        }

        // Port and Enabled may be set from a worker thread (stopping waits for the RSPduo, about 1.5 s):
        // _controlLock keeps the start / stop sequences in order.
        public int Port
        {
            get { return _port; }
            set
            {
                lock (_controlLock)
                {
                    if (_port != value)
                    {
                        _port = value;
                        if (_enabled)
                        {
                            Enabled = false;
                            Enabled = true;
                        }
                    }
                }
            }
        }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                lock (_controlLock)
                {
                    if (value)
                    {
                        if (!_enabled)
                        {
                            Start();
                        }
                    }
                    else
                    {
                        _enabled = false;
                        if (_acceptThread != null)
                        {
                            Stop();
                        }
                        else
                        {
                            _state = BridgeState.Stopped;
                        }
                    }
                }
            }
        }

        public BridgeState State => _state;

        public string Failure => _failure;

        public string ClientName => _clientName;

        public int ClientVersion => _clientVersion;

        public string LastCommand => _lastCommand;

        public int StreamingMode => _streamingMode;

        // The bridge sends no FFT stream: the client must ask for IQ only ("Use full IQ" in SDR#). 0 = not set yet.
        public bool FullIQRequested => _streamingMode == 0 || _streamingMode == SpyServerProtocol.StreamModeIQOnly;

        public int IQFormat => _iqFormat;

        public int DigitalGain => _digitalGain;

        public bool StreamingEnabled => _streamingEnabled;

        public int Sequence => _sequenceShown;

        public long Frequency => _source.Frequency;

        public int DecimationStage => _source.DecimationStage;

        public double SampleRate => _source.MaximumSampleRate >> _source.DecimationStage;

        public int GainIndex => _source.GainIndex;

        public string DeviceName => _source.DeviceName;

        public string SourceFailure => _source.Failure;

        public double MeasuredSampleRate => _source.MeasuredSampleRate;

        public int Overruns => _source.Overruns;

        public void Dispose()
        {
            Enabled = false;
        }

        private void Start()
        {
            if (_acceptThread != null)
            {
                Stop();
            }
            try
            {
                _listener = new TcpListener(IPAddress.Loopback, _port);
                _listener.Start(1);
            }
            catch (SocketException ex)
            {
                _listener = null;
                _failure = "Port " + _port + ": " + ex.Message;
                _state = BridgeState.Failed;
                return;
            }
            _failure = "";
            _state = BridgeState.Listening;
            // The accept loop runs while _enabled: set it before the thread starts.
            _enabled = true;
            _acceptThread = new Thread(AcceptLoop);
            _acceptThread.Name = "SDRplay SpyServer";
            _acceptThread.IsBackground = true;
            _acceptThread.Start();
        }

        // Order: no new session (accept thread joined), then the open session's socket is closed and its thread joined.
        private void Stop()
        {
            _listener.Stop();
            _acceptThread.Join();
            _acceptThread = null;
            var client = _client;
            if (client != null)
            {
                client.Close();
            }
            if (_sessionThread != null)
            {
                _sessionThread.Join();
                _sessionThread = null;
            }
            _listener = null;
            _state = BridgeState.Stopped;
        }

        // Accepts clients while enabled; a client arriving during a session is closed at once, so it fails fast
        // instead of waiting in the listen queue.
        private void AcceptLoop()
        {
            while (_enabled)
            {
                TcpClient client;
                try
                {
                    client = _listener.AcceptTcpClient();
                }
                catch (SocketException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                // A session whose client has gone is still stopping the RSPduo (Uninit, measured 1.5 s):
                // the new client waits for it instead of being refused, so a quick reconnect works.
                if (_sessionThread != null && _sessionThread.IsAlive && (_sessionEnding || PeerClosed(_client)))
                {
                    _sessionThread.Join();
                }
                if (_sessionThread != null && _sessionThread.IsAlive)
                {
                    client.Close();
                    _lastCommand = "Second client refused (one client at a time)";
                    continue;
                }
                if (_sessionThread != null)
                {
                    _sessionThread.Join();
                }
                client.NoDelay = true;
                _sessionEnding = false;
                _sequence = 0; // no sender thread runs between sessions
                _sequenceShown = 0;
                _client = client;
                _stream = client.GetStream();
                _state = BridgeState.Connected;
                _sessionThread = new Thread(SessionLoop);
                _sessionThread.Name = "SDRplay SpyServer session";
                _sessionThread.IsBackground = true;
                _sessionThread.Start();
            }
        }

        // The client closed its side: readable with no data. A client usually sends "streaming off" before
        // closing, and the session thread is still stopping the RSPduo when the next client arrives.
        private static bool PeerClosed(TcpClient client)
        {
            if (client == null)
            {
                return true;
            }
            try
            {
                var socket = client.Client;
                return socket.Poll(0, SelectMode.SelectRead) && socket.Available == 0;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
            catch (SocketException)
            {
                return true;
            }
        }

        private void SessionLoop()
        {
            try
            {
                ReceiveLoop();
            }
            catch (IOException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException)
            {
            }
            EndSession();
        }

        private void EndSession()
        {
            _sessionEnding = true;
            var client = _client;
            if (client != null)
            {
                client.Close();
            }
            StopSending();
            _client = null;
            _stream = null;
            _clientName = "";
            _streamingEnabled = false;
            // Per-session values: the next client starts from "not set".
            _streamingMode = 0;
            _iqFormat = 0;
            _digitalGain = 0;
            if (_enabled)
            {
                _state = BridgeState.Listening;
            }
        }

        private void ReceiveLoop()
        {
            var stream = _stream;
            while (_enabled)
            {
                stream.ReadExactly(_commandBuffer, 0, SpyServerProtocol.CommandHeaderSize);
                var command = BinaryPrimitives.ReadUInt32LittleEndian(_commandBuffer.AsSpan(0, 4));
                var bodySize = BinaryPrimitives.ReadUInt32LittleEndian(_commandBuffer.AsSpan(4, 4));
                if (bodySize > SpyServerProtocol.MaxCommandBodySize)
                {
                    _lastCommand = "Command " + command + ": body " + bodySize + " B over the limit, disconnected";
                    return;
                }
                if (bodySize > 0)
                {
                    stream.ReadExactly(_commandBuffer, SpyServerProtocol.CommandHeaderSize, (int)bodySize);
                }
                var body = _commandBuffer.AsSpan(SpyServerProtocol.CommandHeaderSize, (int)bodySize);
                switch (command)
                {
                    case SpyServerProtocol.CmdHello:
                        OnHello(body);
                        break;
                    case SpyServerProtocol.CmdSetSetting:
                        OnSetSetting(body);
                        break;
                    case SpyServerProtocol.CmdPing:
                        _lastCommand = "PING";
                        SendStatus(SpyServerProtocol.MsgPong, 0);
                        break;
                    default:
                        _lastCommand = "Command " + command + ", body " + bodySize + " B (ignored)";
                        break;
                }
            }
        }

        private void OnHello(ReadOnlySpan<byte> body)
        {
            if (body.Length >= 4)
            {
                _clientVersion = (int)BinaryPrimitives.ReadUInt32LittleEndian(body);
                _clientName = Encoding.ASCII.GetString(body.Slice(4));
            }
            _lastCommand = "HELLO";
            _source.BeginSession();
            SendDeviceInfo();
            SendClientSync();
        }

        private void OnSetSetting(ReadOnlySpan<byte> body)
        {
            if (body.Length < 8)
            {
                _lastCommand = "SET_SETTING: body " + body.Length + " B (ignored)";
                return;
            }
            var setting = BinaryPrimitives.ReadUInt32LittleEndian(body);
            var value = BinaryPrimitives.ReadUInt32LittleEndian(body.Slice(4));
            _lastCommand = "SET_SETTING " + setting + " = " + value;
            switch (setting)
            {
                case SpyServerProtocol.SettingStreamingMode:
                    _streamingMode = (int)value;
                    break;
                case SpyServerProtocol.SettingStreamingEnabled:
                    if (value != 0)
                    {
                        StartSending();
                    }
                    else
                    {
                        StopSending();
                    }
                    break;
                case SpyServerProtocol.SettingGain:
                    _source.GainIndex = (int)value;
                    SendClientSync();
                    break;
                case SpyServerProtocol.SettingIQFormat:
                    _iqFormat = (int)value;
                    break;
                case SpyServerProtocol.SettingIQFrequency:
                    _source.Frequency = value;
                    SendClientSync();
                    break;
                case SpyServerProtocol.SettingIQDecimation:
                    _source.DecimationStage = (int)value;
                    SendClientSync();
                    break;
                case SpyServerProtocol.SettingIQDigitalGain:
                    _digitalGain = (int)value;
                    break;
            }
        }

        private void StartSending()
        {
            if (_sendThread != null)
            {
                if (_sendThread.IsAlive)
                {
                    return;
                }
                // The sender ended by itself (device removed, write error): clean up, then start again.
                StopSending();
            }
            _source.Start();
            _sending = true;
            _streamingEnabled = true;
            _sendThread = new Thread(SendLoop);
            _sendThread.Name = "SDRplay SpyServer IQ";
            _sendThread.IsBackground = true;
            _sendThread.Start();
        }

        private void StopSending()
        {
            _sending = false;
            _streamingEnabled = false;
            _source.Stop();
            if (_sendThread != null)
            {
                _sendThread.Join();
                _sendThread = null;
            }
        }

        private void SendLoop()
        {
            var stream = _stream;
            var overruns = _source.Overruns;
            while (_sending)
            {
                var samples = _source.Read(_iqMessage, SpyServerProtocol.MessageHeaderSize);
                if (samples == 0)
                {
                    break;
                }
                // Blocks dropped on a full ring leave a gap in the sequence, so the client can see the loss.
                var dropped = _source.Overruns - overruns;
                if (dropped > 0)
                {
                    overruns += dropped;
                    _sequence = unchecked(_sequence + (uint)dropped);
                }
                var bodySize = 4 * samples;
                WriteHeader(_iqMessage, SpyServerProtocol.MsgInt16IQ, SpyServerProtocol.StreamTypeIQ, _sequence, bodySize);
                try
                {
                    lock (_writeLock)
                    {
                        stream.Write(_iqMessage, 0, SpyServerProtocol.MessageHeaderSize + bodySize);
                    }
                }
                catch (IOException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                _sequence = unchecked(_sequence + 1);
                _sequenceShown = (int)_sequence;
            }
            _sending = false;
            _streamingEnabled = false;
        }

        private void SendDeviceInfo()
        {
            var b = _statusBuffer.AsSpan(SpyServerProtocol.MessageHeaderSize);
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(0), SpyServerProtocol.DeviceAirspyOne);
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(4), 1); // DeviceSerial
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(8), _source.MaximumSampleRate);
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(12), _source.MaximumBandwidth);
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(16), _source.DecimationStageCount);
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(20), 0); // GainStageCount
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(24), _source.MaximumGainIndex);
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(28), _source.MinimumFrequency);
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(32), _source.MaximumFrequency);
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(36), _source.Resolution);
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(40), 0); // MinimumIQDecimation
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(44), SpyServerProtocol.FormatInt16); // ForcedIQFormat
            SendStatus(SpyServerProtocol.MsgDeviceInfo, SpyServerProtocol.DeviceInfoSize);
        }

        private void SendClientSync()
        {
            var frequency = (uint)Math.Min(Math.Max(_source.Frequency, 0), uint.MaxValue);
            var b = _statusBuffer.AsSpan(SpyServerProtocol.MessageHeaderSize);
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(0), 1); // CanControl
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(4), (uint)_source.GainIndex);
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(8), frequency); // DeviceCenterFrequency
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(12), frequency); // IQCenterFrequency
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(16), frequency); // FFTCenterFrequency
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(20), _source.MinimumFrequency);
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(24), _source.MaximumFrequency);
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(28), _source.MinimumFrequency);
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(32), _source.MaximumFrequency);
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(36), 0); // SupportsZooming
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(40), 0); // SupportsSharpFFT
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(44), 0); // SupportsSharpIQ
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(48), 0); // SupportsOptimizedStreaming
            BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(52), 0); // SharpPCMCoders
            SendStatus(SpyServerProtocol.MsgClientSync, SpyServerProtocol.ClientSyncSize);
        }

        // Sends _statusBuffer: header + bodySize bytes already written after the header. Receive thread only.
        private void SendStatus(uint messageType, int bodySize)
        {
            WriteHeader(_statusBuffer, messageType, SpyServerProtocol.StreamTypeStatus, 0, bodySize);
            lock (_writeLock)
            {
                _stream.Write(_statusBuffer, 0, SpyServerProtocol.MessageHeaderSize + bodySize);
            }
        }

        private static void WriteHeader(byte[] buffer, uint messageType, uint streamType, uint sequence, int bodySize)
        {
            var h = buffer.AsSpan();
            BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(0), SpyServerProtocol.ProtocolVersion);
            BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(4), messageType);
            BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(8), streamType);
            BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(12), sequence);
            BinaryPrimitives.WriteUInt32LittleEndian(h.Slice(16), (uint)bodySize);
        }
    }
}
