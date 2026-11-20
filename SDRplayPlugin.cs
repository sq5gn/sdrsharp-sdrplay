using SDRSharp.Common;
using SDRSharp.Radio;
using System.Windows.Forms;

namespace SDRSharp.SDRplay
{
    public class SDRplayPlugin : ISharpPlugin, ICanLazyLoadGui, ISupportStatus, IExtendedNameProvider
    {
        private const string CategoryName = "Source";

        private ISharpControl _control;
        private SDRplaySource _source;
        private SpyServerBridge _bridge;
        private SettingsPanel _configGui;
        private ReceiverPanel _receiverPanel;
        private ClientPanel _clientPanel;
        private DiversityPanel _diversityPanel;

        public UserControl Gui
        {
            get
            {
                LoadGui();
                return _configGui;
            }
        }

        public bool IsActive => _bridge.Enabled;

        public string DisplayName => "SDRplay RSPduo";

        public string Category => CategoryName;

        public string MenuItemName => "SDRplay RSPduo";

        public void Initialize(ISharpControl control)
        {
            _control = control;

            _source = new SDRplaySource();
            _source.SampleRateIndex = Utils.GetIntSetting("plugin.sdrplay.sampleRate", RspDuoSampleRates.Default);
            _source.LnaState = Utils.GetIntSetting("plugin.sdrplay.lnaState", 0);
            _source.AgcMode = Utils.GetIntSetting("plugin.sdrplay.agcMode", SDRplayApi.Agc50Hz);
            _source.AgcSetPoint = Utils.GetIntSetting("plugin.sdrplay.agcSetPoint", -60);
            _source.IFGainReduction = Utils.GetIntSetting("plugin.sdrplay.ifGainReduction", 50);
            _source.Antenna = Utils.GetIntSetting("plugin.sdrplay.antenna", RspDuoAntenna.Default);
            _source.BiasT = Utils.GetBooleanSetting("plugin.sdrplay.biasT", false);
            _source.RfNotch = Utils.GetBooleanSetting("plugin.sdrplay.rfNotch", false);
            _source.DabNotch = Utils.GetBooleanSetting("plugin.sdrplay.dabNotch", false);
            _source.AmNotch = Utils.GetBooleanSetting("plugin.sdrplay.amNotch", false);
            _source.Ppm = Utils.GetDoubleSetting("plugin.sdrplay.ppm", 0);
            _source.Mode = Utils.GetIntSetting("plugin.sdrplay.mode", RspDuoModes.Default);
            _source.Output = Utils.GetIntSetting("plugin.sdrplay.output", SDRplayApi.TunerA);
            _source.LnaStateB = Utils.GetIntSetting("plugin.sdrplay.lnaStateB", 0);
            _source.IFGainReductionB = Utils.GetIntSetting("plugin.sdrplay.ifGainReductionB", 50);
            _source.AutoAfterRetune = Utils.GetBooleanSetting("plugin.sdrplay.autoAfterRetune");
            _source.Combiner.SetWeight(Utils.GetDoubleSetting("plugin.sdrplay.diversityPhase", 0), Utils.GetDoubleSetting("plugin.sdrplay.diversityGain", 0));
            _bridge = new SpyServerBridge(_source);
            _bridge.Port = Utils.GetIntSetting("plugin.sdrplay.port", 5555);
            _bridge.Enabled = Utils.GetBooleanSetting("plugin.sdrplay.enabled");

            _receiverPanel = new ReceiverPanel(_bridge, _source);
            _control.RegisterFrontControl(_receiverPanel, PluginPosition.Bottom);
            _clientPanel = new ClientPanel(_bridge, _source);
            _control.RegisterFrontControl(_clientPanel, PluginPosition.Bottom);
            _diversityPanel = new DiversityPanel(_bridge, _source, _control);
            _control.RegisterFrontControl(_diversityPanel, PluginPosition.Right);
        }

        public void Close()
        {
            Utils.SaveSetting("plugin.sdrplay.enabled", _bridge.Enabled);
            Utils.SaveSetting("plugin.sdrplay.port", _bridge.Port);
            Utils.SaveSetting("plugin.sdrplay.sampleRate", _source.SampleRateIndex);
            Utils.SaveSetting("plugin.sdrplay.lnaState", _source.LnaState);
            Utils.SaveSetting("plugin.sdrplay.agcMode", _source.AgcMode);
            Utils.SaveSetting("plugin.sdrplay.agcSetPoint", _source.AgcSetPoint);
            Utils.SaveSetting("plugin.sdrplay.ifGainReduction", _source.IFGainReduction);
            Utils.SaveSetting("plugin.sdrplay.antenna", _source.Antenna);
            Utils.SaveSetting("plugin.sdrplay.biasT", _source.BiasT);
            Utils.SaveSetting("plugin.sdrplay.rfNotch", _source.RfNotch);
            Utils.SaveSetting("plugin.sdrplay.dabNotch", _source.DabNotch);
            Utils.SaveSetting("plugin.sdrplay.amNotch", _source.AmNotch);
            Utils.SaveSetting("plugin.sdrplay.ppm", _source.Ppm);
            Utils.SaveSetting("plugin.sdrplay.mode", _source.Mode);
            Utils.SaveSetting("plugin.sdrplay.output", _source.Output);
            Utils.SaveSetting("plugin.sdrplay.lnaStateB", _source.LnaStateB);
            Utils.SaveSetting("plugin.sdrplay.ifGainReductionB", _source.IFGainReductionB);
            Utils.SaveSetting("plugin.sdrplay.autoAfterRetune", _source.AutoAfterRetune);
            Utils.SaveSetting("plugin.sdrplay.diversityPhase", _source.Combiner.Phase);
            Utils.SaveSetting("plugin.sdrplay.diversityGain", _source.Combiner.GainDB);
            _bridge.Enabled = false;
            _receiverPanel.StopRefresh();
            _clientPanel.StopRefresh();
            _diversityPanel.StopRefresh();
            if (_configGui != null)
            {
                _configGui.StopRefresh();
            }
            _bridge.Dispose();
            _source.Dispose();
        }

        public void LoadGui()
        {
            if (_configGui == null)
            {
                _configGui = new SettingsPanel(_bridge, _source, _control, _receiverPanel, _clientPanel, _diversityPanel);
            }
        }
    }
}
