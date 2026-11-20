using System.Windows.Forms;

namespace SDRSharp.SDRplay
{
    // Front panel: what the RSPduo is doing. In Diversity the per-tuner rows show "T1 ... | T2 ...".
    public class ReceiverPanel : StatusPanel
    {
        private readonly Label _deviceValue;
        private readonly Label _modeValue;
        private readonly Label _frequencyValue;
        private readonly Label _sampleRateValue;
        private readonly Label _decimationValue;
        private readonly Label _gainReductionValue;
        private readonly Label _systemGainValue;
        private readonly Label _levelValue;
        private readonly Label _overloadValue;
        private readonly Label _measuredRateValue;
        private readonly Label _overrunsValue;
        private readonly Label _failureValue;

        public ReceiverPanel(SpyServerBridge bridge, SDRplaySource source)
            : base(bridge, source, "RSPduo")
        {
            _deviceValue = AddRow("Device");
            _modeValue = AddRow("Mode");
            _frequencyValue = AddRow("Frequency");
            _sampleRateValue = AddRow("Sample Rate");
            _decimationValue = AddRow("Decimation Stage");
            _gainReductionValue = AddRow("Gain Reduction (LNA + IF)");
            _systemGainValue = AddRow("System Gain");
            _levelValue = AddRow("Signal Level");
            _overloadValue = AddRow("ADC Overload");
            _measuredRateValue = AddRow("Measured Rate");
            _overrunsValue = AddRow("Overruns");
            _failureValue = AddRow("Device Status");
        }

        protected override void UpdateValues()
        {
            var streaming = _source.IsStreaming;
            var diversity = streaming && _source.ActiveMode == RspDuoModes.Diversity;
            _deviceValue.Text = _bridge.DeviceName.Length > 0 ? _bridge.DeviceName : "-";
            _modeValue.Text = streaming ? RspDuoModes.Names[_source.ActiveMode] : "-";
            _frequencyValue.Text = _bridge.Frequency.ToString("N0") + " Hz";
            _sampleRateValue.Text = _bridge.SampleRate.ToString("N0") + " S/s";
            _decimationValue.Text = _bridge.DecimationStage + " (/" + (1 << _bridge.DecimationStage) + ")";
            var ifReduction = _source.ReportedIFGainReduction;
            if (streaming && ifReduction >= 0)
            {
                var a = _source.ReportedLnaGainReduction + " + " + ifReduction + " dB";
                var gainA = _source.ReportedGain.ToString("0.0") + " dB";
                if (diversity)
                {
                    var ifReductionB = _source.ReportedIFGainReductionB;
                    var b = ifReductionB >= 0 ? _source.ReportedLnaGainReductionB + " + " + ifReductionB + " dB" : "-";
                    var gainB = ifReductionB >= 0 ? _source.ReportedGainB.ToString("0.0") + " dB" : "-";
                    _gainReductionValue.Text = "T1 " + a + " | T2 " + b;
                    _systemGainValue.Text = "T1 " + gainA + " | T2 " + gainB;
                }
                else
                {
                    _gainReductionValue.Text = a;
                    _systemGainValue.Text = gainA;
                }
            }
            else
            {
                _gainReductionValue.Text = "-";
                _systemGainValue.Text = "-";
            }
            _levelValue.Text = diversity ? "T1 " + Level(_source.LevelA) + " | T2 " + Level(_source.LevelB) : "-";
            if (!streaming)
            {
                _overloadValue.Text = "-";
            }
            else if (diversity)
            {
                _overloadValue.Text = "T1 " + (_source.Overload ? "DETECTED" : "no") + " | T2 " + (_source.OverloadB ? "DETECTED" : "no");
            }
            else
            {
                _overloadValue.Text = _source.Overload ? "DETECTED" : "no";
            }
            _measuredRateValue.Text = _bridge.MeasuredSampleRate.ToString("N0") + " S/s";
            _overrunsValue.Text = _bridge.Overruns + " blocks";
            _failureValue.Text = _bridge.SourceFailure.Length > 0 ? _bridge.SourceFailure : "OK";
        }

        private static string Level(double dbfs)
        {
            return double.IsNaN(dbfs) ? "-" : dbfs.ToString("0.0") + " dBFS";
        }
    }
}
