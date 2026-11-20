using System.Windows.Forms;

namespace SDRSharp.SDRplay
{
    // Front panel: what the SpyServer client (the SDR# source) asked for and what the bridge sends.
    public class ClientPanel : StatusPanel
    {
        private readonly Label _clientValue;
        private readonly Label _protocolValue;
        private readonly Label _formatValue;
        private readonly Label _modeValue;
        private readonly Label _streamingValue;
        private readonly Label _digitalGainValue;
        private readonly Label _sequenceValue;
        private readonly Label _lastCommandValue;

        public ClientPanel(SpyServerBridge bridge, SDRplaySource source)
            : base(bridge, source, "SpyServer Client")
        {
            _clientValue = AddRow("Client");
            _protocolValue = AddRow("Client Protocol");
            _formatValue = AddRow("IQ Format");
            _modeValue = AddRow("Streaming Mode");
            _streamingValue = AddRow("Streaming");
            _digitalGainValue = AddRow("Digital Gain");
            _sequenceValue = AddRow("IQ Messages Sent");
            _lastCommandValue = AddRow("Last Command");
        }

        protected override void UpdateValues()
        {
            var connected = _bridge.State == BridgeState.Connected;
            var version = _bridge.ClientVersion;
            _clientValue.Text = connected ? _bridge.ClientName : "none";
            _protocolValue.Text = connected ? ((version >> 24) & 0xFF) + "." + ((version >> 16) & 0xFF) + "." + (version & 0xFFFF) : "-";
            _formatValue.Text = _bridge.IQFormat == SpyServerProtocol.FormatInt16 ? "INT16" : _bridge.IQFormat == 0 ? "-" : "format " + _bridge.IQFormat + " requested, sent as INT16";
            _modeValue.Text = _bridge.StreamingMode == SpyServerProtocol.StreamModeIQOnly ? "IQ only" : _bridge.StreamingMode == 0 ? "-" : "mode " + _bridge.StreamingMode + ": no FFT sent, tick \"Use full IQ\"";
            _streamingValue.Text = _bridge.StreamingEnabled ? "on" : "off";
            _digitalGainValue.Text = _bridge.DigitalGain == -1 ? "auto" : _bridge.DigitalGain.ToString();
            _sequenceValue.Text = ((uint)_bridge.Sequence).ToString("N0");
            _lastCommandValue.Text = _bridge.LastCommand;
        }
    }
}
