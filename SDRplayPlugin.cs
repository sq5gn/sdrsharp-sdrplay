using SDRSharp.Common;
using SDRSharp.PanView;
using SDRSharp.Radio;
using System;
using System.ComponentModel;
using System.Drawing;
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
        private bool _cursorOverReference; // paint handler only: the hover title is ours to clear
        private Font _overloadFont; // paint handler only: created on the first OVL, disposed in Close

        private static readonly Color ReferenceColor = Color.FromArgb(30, 180, 170);

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
            _source.DiversityHiZ = Utils.GetBooleanSetting("plugin.sdrplay.diversityHiZ", false);
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
            if (Utils.GetBooleanSetting("plugin.sdrplay.referenceEnabled"))
            {
                _source.Combiner.SetReference(Utils.GetLongSetting("plugin.sdrplay.referenceLow", 0), Utils.GetLongSetting("plugin.sdrplay.referenceHigh", 0));
            }
            _control.PropertyChanged += control_PropertyChanged;
            UpdateBands();
            _bridge = new SpyServerBridge(_source);
            _bridge.Port = Utils.GetIntSetting("plugin.sdrplay.port", 5555);
            _bridge.Enabled = Utils.GetBooleanSetting("plugin.sdrplay.enabled");

            _receiverPanel = new ReceiverPanel(_bridge, _source);
            _control.RegisterFrontControl(_receiverPanel, PluginPosition.Bottom);
            _clientPanel = new ClientPanel(_bridge, _source);
            _control.RegisterFrontControl(_clientPanel, PluginPosition.Bottom);
            _diversityPanel = new DiversityPanel(_bridge, _source, _control);
            _control.RegisterFrontControl(_diversityPanel, PluginPosition.Right);
            _control.SpectrumAnalyzerCustomPaint += spectrumAnalyzer_CustomPaint;
        }

        // On the SDR# spectrum: the reference band in Diversity mode and the ADC overload indicator (VDK 05-ui.md 5.5).
        private void spectrumAnalyzer_CustomPaint(object sender, CustomPaintEventArgs e)
        {
            // Only while the plugin streams: SDR# may play another source with the plugin off.
            if (!_bridge.Enabled || !_source.IsStreaming)
            {
                ClearReferenceTitle(e);
                return;
            }

            Utils.ConfigureGraphics(e.Graphics, false);

            var spectrum = (SpectrumAnalyzer)sender;
            var diversity = _source.ActiveMode == RspDuoModes.Diversity;
            PaintReference(spectrum, e, diversity);
            PaintOverload(spectrum, e, diversity);
        }

        // Red "OVL" badge in the top right corner of the spectrum while the API reports an ADC overload;
        // in Diversity it names the tuner (T1, T2 or both).
        private void PaintOverload(SpectrumAnalyzer spectrum, CustomPaintEventArgs e, bool diversity)
        {
            var a = _source.Overload;
            var b = diversity && _source.OverloadB;
            if (!a && !b)
            {
                return;
            }
            var text = !diversity ? "OVL" : a && b ? "OVL T1 T2" : a ? "OVL T1" : "OVL T2";
            if (_overloadFont == null)
            {
                _overloadFont = new Font(spectrum.Font.FontFamily, 9f, FontStyle.Bold);
            }
            var size = e.Graphics.MeasureString(text, _overloadFont);
            var width = (int)Math.Ceiling(size.Width) + 8;
            var height = (int)Math.Ceiling(size.Height) + 4;
            var x = spectrum.Width - Waterfall.AxisMargin - width - 4;
            var y = Waterfall.AxisMargin + 4;
            e.Graphics.FillRectangle(Brushes.Red, x, y, width, height);
            e.Graphics.DrawString(text, _overloadFont, Brushes.White, x + 4, y + 2);
        }

        private void PaintReference(SpectrumAnalyzer spectrum, CustomPaintEventArgs e, bool diversity)
        {
            long low;
            long high;
            if (!diversity || !_source.Combiner.GetReference(out low, out high))
            {
                ClearReferenceTitle(e);
                return;
            }

            var x1 = (int)Math.Max(Waterfall.AxisMargin, spectrum.FrequencyToPoint(low));
            var x2 = (int)Math.Min(spectrum.Width - Waterfall.AxisMargin, spectrum.FrequencyToPoint(high));
            if (x2 <= x1)
            {
                ClearReferenceTitle(e);
                return;
            }
            var top = Waterfall.AxisMargin;
            var bottom = spectrum.Height - Waterfall.AxisMargin;
            using (var brush = new SolidBrush(Color.FromArgb(60, ReferenceColor)))
            {
                e.Graphics.FillRectangle(brush, x1, top, x2 - x1, bottom - top);
            }
            using (var pen = new Pen(ReferenceColor, 2))
            {
                e.Graphics.DrawLine(pen, x1, top, x1, bottom);
                e.Graphics.DrawLine(pen, x2, top, x2, bottom);
            }
            var cursor = e.CursorPosition;
            if (cursor.X >= x1 && cursor.X <= x2 && cursor.Y >= top && cursor.Y <= bottom)
            {
                _cursorOverReference = true;
                e.CustomTitle = "Diversity reference band (Null)";
            }
            else
            {
                ClearReferenceTitle(e);
            }
        }

        private void ClearReferenceTitle(CustomPaintEventArgs e)
        {
            if (_cursorOverReference)
            {
                _cursorOverReference = false;
                e.CustomTitle = string.Empty;
            }
        }

        // The SDR# filter and the frequency domain reach the combiner at once, for a Max or a repeat after a retune;
        // storing them is all the handler does.
        private void control_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Frequency":
                case "FilterBandwidth":
                case "DetectorType":
                case "FrequencyShift":
                case "FrequencyShiftEnabled":
                case "SwapIq":
                    UpdateBands();
                    break;
            }
        }

        private void UpdateBands()
        {
            long low;
            long high;
            FilterPassband(_control, out low, out high);
            _source.Combiner.SetFilterBand(low, high);
            _source.Combiner.FrequencyShift = _control.FrequencyShiftEnabled ? _control.FrequencyShift : 0;
            _source.Combiner.SpectrumInverted = _control.SwapIq;
        }

        // Passband of the SDR# channel filter in the display domain, Hz (VDK 02-stream-hooks.md 2.4: USB [0, +bw],
        // LSB [-bw, 0], the other modes centred on the VFO).
        private static void FilterPassband(ISharpControl control, out long low, out long high)
        {
            var vfo = control.Frequency;
            long bandwidth = control.FilterBandwidth;
            switch (control.DetectorType)
            {
                case DetectorType.USB:
                    low = vfo;
                    high = vfo + bandwidth;
                    break;
                case DetectorType.LSB:
                    low = vfo - bandwidth;
                    high = vfo;
                    break;
                default:
                    low = vfo - bandwidth / 2;
                    high = vfo + bandwidth / 2;
                    break;
            }
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
            Utils.SaveSetting("plugin.sdrplay.diversityHiZ", _source.DiversityHiZ);
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
            long referenceLow;
            long referenceHigh;
            Utils.SaveSetting("plugin.sdrplay.referenceEnabled", _source.Combiner.GetReference(out referenceLow, out referenceHigh));
            Utils.SaveSetting("plugin.sdrplay.referenceLow", referenceLow);
            Utils.SaveSetting("plugin.sdrplay.referenceHigh", referenceHigh);
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
            _control.SpectrumAnalyzerCustomPaint -= spectrumAnalyzer_CustomPaint;
            if (_overloadFont != null)
            {
                _overloadFont.Dispose();
                _overloadFont = null;
            }
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
