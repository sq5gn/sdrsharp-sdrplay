using System.Windows.Forms;

namespace SDRSharp.SDRplay
{
    // Front panel: what the RSPduo is doing.
    public class ReceiverPanel : StatusPanel
    {
        private readonly Label _deviceValue;
        private readonly Label _frequencyValue;
        private readonly Label _sampleRateValue;
        private readonly Label _decimationValue;
        private readonly Label _gainReductionValue;
        private readonly Label _systemGainValue;
        private readonly Label _overloadValue;
        private readonly Label _measuredRateValue;
        private readonly Label _overrunsValue;
        private readonly Label _failureValue;

        public ReceiverPanel(SpyServerBridge bridge, SDRplaySource source)
            : base(bridge, source, "RSPduo")
        {
            _deviceValue = AddRow("Device");
            _frequencyValue = AddRow("Frequency");
            _sampleRateValue = AddRow("Sample Rate");
            _decimationValue = AddRow("Decimation Stage");
            _gainReductionValue = AddRow("Gain Reduction (LNA + IF)");
            _systemGainValue = AddRow("System Gain");
            _overloadValue = AddRow("ADC Overload");
            _measuredRateValue = AddRow("Measured Rate");
            _overrunsValue = AddRow("Overruns");
            _failureValue = AddRow("Device Status");
        }

        protected override void UpdateValues()
        {
            _deviceValue.Text = _bridge.DeviceName.Length > 0 ? _bridge.DeviceName : "-";
            _frequencyValue.Text = _bridge.Frequency.ToString("N0") + " Hz";
            _sampleRateValue.Text = _bridge.SampleRate.ToString("N0") + " S/s";
            _decimationValue.Text = _bridge.DecimationStage + " (/" + (1 << _bridge.DecimationStage) + ")";
            var ifReduction = _source.ReportedIFGainReduction;
            if (_source.IsStreaming && ifReduction >= 0)
            {
                _gainReductionValue.Text = _source.ReportedLnaGainReduction + " + " + ifReduction + " dB";
                _systemGainValue.Text = _source.ReportedGain.ToString("0.0") + " dB";
            }
            else
            {
                _gainReductionValue.Text = "-";
                _systemGainValue.Text = "-";
            }
            _overloadValue.Text = _source.IsStreaming ? (_source.Overload ? "DETECTED" : "no") : "-";
            _measuredRateValue.Text = _bridge.MeasuredSampleRate.ToString("N0") + " S/s";
            _overrunsValue.Text = _bridge.Overruns + " blocks";
            _failureValue.Text = _bridge.SourceFailure.Length > 0 ? _bridge.SourceFailure : "OK";
        }
    }
}
