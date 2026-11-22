using SDRSharp.Common;
using SDRSharp.PanView;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SDRSharp.SDRplay
{
    public partial class SettingsPanel : UserControl
    {
        private SpyServerBridge _bridge;
        private SDRplaySource _source;
        private ISharpControl _control;
        private ReceiverPanel _receiverPanel;
        private ClientPanel _clientPanel;
        private DiversityPanel _diversityPanel;
        private Bitmap _receiverIcon;
        private Bitmap _receiverDarkIcon;
        private Bitmap _clientIcon;
        private Bitmap _clientDarkIcon;
        private Bitmap _diversityIcon;
        private Bitmap _diversityDarkIcon;
        private int _bridgeCalls; // bridge start / stop calls still running on a worker thread
        private bool _antennaListDiversity; // antennaComboBox holds RspDuoAntenna.DiversityNames
        private bool _fillingAntenna; // antennaComboBox is being refilled: not a choice of the user

        public SettingsPanel(SpyServerBridge bridge, SDRplaySource source, ISharpControl control, ReceiverPanel receiverPanel, ClientPanel clientPanel, DiversityPanel diversityPanel)
        {
            _bridge = bridge;
            _source = source;
            _control = control;
            _receiverPanel = receiverPanel;
            _clientPanel = clientPanel;
            _diversityPanel = diversityPanel;

            InitializeComponent();

            this.AutoSize = true;
            this.Dock = DockStyle.Top;

            _receiverIcon = LoadIcon("receiver.png");
            _receiverDarkIcon = LoadIcon("receiver_dark.png");
            _clientIcon = LoadIcon("client.png");
            _clientDarkIcon = LoadIcon("client_dark.png");
            _diversityIcon = LoadIcon("diversity.png");
            _diversityDarkIcon = LoadIcon("diversity_dark.png");

            enableCheckBox.Checked = _bridge.Enabled;
            portNumericUpDown.Value = Math.Min(Math.Max(_bridge.Port, (int)portNumericUpDown.Minimum), (int)portNumericUpDown.Maximum);
            UpdateLnaRange();
            lnaTrackBar.Value = Math.Min(Math.Max(_source.LnaState, lnaTrackBar.Minimum), lnaTrackBar.Maximum);
            agcComboBox.SelectedIndex = Math.Min(Math.Max(_source.AgcMode, 0), agcComboBox.Items.Count - 1);
            setPointNumericUpDown.Value = Math.Min(Math.Max(_source.AgcSetPoint, (int)setPointNumericUpDown.Minimum), (int)setPointNumericUpDown.Maximum);
            ifGainTrackBar.Value = Math.Min(Math.Max(_source.IFGainReduction, ifGainTrackBar.Minimum), ifGainTrackBar.Maximum);
            rateComboBox.Items.AddRange(RspDuoSampleRates.Names);
            rateComboBox.SelectedIndex = _source.SampleRateIndex;
            FillAntennaComboBox();
            biasTCheckBox.Checked = _source.BiasT;
            rfNotchCheckBox.Checked = _source.RfNotch;
            dabNotchCheckBox.Checked = _source.DabNotch;
            amNotchCheckBox.Checked = _source.AmNotch;
            ppmNumericUpDown.Value = (decimal)Math.Round(_source.Ppm, 2);
            modeComboBox.Items.AddRange(RspDuoModes.Names);
            modeComboBox.SelectedIndex = _source.Mode;
            UpdateLnaRangeB();
            lnaBTrackBar.Value = Math.Min(Math.Max(_source.LnaStateB, lnaBTrackBar.Minimum), lnaBTrackBar.Maximum);
            ifGainBTrackBar.Value = Math.Min(Math.Max(_source.IFGainReductionB, ifGainBTrackBar.Minimum), ifGainBTrackBar.Maximum);
            _receiverPanel.VisibleChanged += receiverPanel_VisibleChanged;
            _clientPanel.VisibleChanged += clientPanel_VisibleChanged;
            _diversityPanel.VisibleChanged += diversityPanel_VisibleChanged;
            _control.PropertyChanged += control_PropertyChanged;
            receiverPanel_VisibleChanged(null, null);
            clientPanel_VisibleChanged(null, null);
            diversityPanel_VisibleChanged(null, null);
            ApplyTheme();

            enableCheckBox_ValueChanged(null, null);
            portNumericUpDown_ValueChanged(null, null);
            lnaTrackBar_Scroll(null, null);
            agcComboBox_SelectedIndexChanged(null, null);
            setPointNumericUpDown_ValueChanged(null, null);
            ifGainTrackBar_Scroll(null, null);
            lnaBTrackBar_Scroll(null, null);
            ifGainBTrackBar_Scroll(null, null);
            UpdateModeControls();
            refreshTimer_Tick(null, null);
        }

        private void modeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _source.Mode = modeComboBox.SelectedIndex;
            UpdateModeControls();
            refreshTimer_Tick(null, null);
        }

        private void lnaBTrackBar_Scroll(object sender, EventArgs e)
        {
            if (_source.LnaStateB != lnaBTrackBar.Value)
            {
                _source.LnaStateB = lnaBTrackBar.Value;
            }
            lnaBValueLabel.Text = LnaTextB();
        }

        private void ifGainBTrackBar_Scroll(object sender, EventArgs e)
        {
            if (_source.IFGainReductionB != ifGainBTrackBar.Value)
            {
                _source.IFGainReductionB = ifGainBTrackBar.Value;
            }
            ifGainBValueLabel.Text = _source.IFGainReductionB + " dB";
        }

        // Tuner 2 takes tuner 1's gain; the track bars send it to the source.
        private void copyGainButton_Click(object sender, EventArgs e)
        {
            lnaBTrackBar.Value = Math.Min(Math.Max(lnaTrackBar.Value, lnaBTrackBar.Minimum), lnaBTrackBar.Maximum);
            ifGainBTrackBar.Value = ifGainTrackBar.Value;
        }

        // Which controls apply in the mode chosen for the next session. Diversity: AGC off, 2 MS/s, tuner 2 on
        // 50 ohm and tuner 1 on 50 ohm or Hi-Z, gain per tuner, Bias-T on tuner 2 and AM notch on tuner 1 always available.
        private void UpdateModeControls()
        {
            var diversity = _source.Mode == RspDuoModes.Diversity;
            lnaBCaptionLabel.Visible = diversity;
            lnaBValueLabel.Visible = diversity;
            lnaBTrackBar.Visible = diversity;
            ifGainBCaptionLabel.Visible = diversity;
            ifGainBValueLabel.Visible = diversity;
            ifGainBTrackBar.Visible = diversity;
            copyGainButton.Visible = diversity;
            lnaCaptionLabel.Text = diversity ? "LNA State (Tuner 1)" : "LNA State";
            ifGainCaptionLabel.Text = diversity ? "IF Gain Reduction (Tuner 1)" : "IF Gain Reduction";
            var manual = _source.AgcMode == SDRplayApi.AgcDisable;
            agcComboBox.Enabled = !diversity;
            setPointNumericUpDown.Enabled = !diversity && !manual;
            ifGainTrackBar.Enabled = diversity || manual;
            rateComboBox.Enabled = !diversity;
            FillAntennaComboBox();
            // The AM notch belongs to tuner 1, the Bias-T to the tuner 2 port.
            amNotchCheckBox.Enabled = diversity || _source.Antenna != RspDuoAntenna.Tuner2_50Ohm;
            biasTCheckBox.Enabled = diversity || _source.Antenna == RspDuoAntenna.Tuner2_50Ohm;
        }

        public void StopRefresh()
        {
            refreshTimer.Enabled = false;
        }

        private void enableCheckBox_ValueChanged(object sender, EventArgs e)
        {
            if (_bridge.Enabled != enableCheckBox.Checked)
            {
                var enabled = enableCheckBox.Checked;
                RunOnBridge(() => _bridge.Enabled = enabled);
            }
            refreshTimer_Tick(null, null);
        }

        // Stopping the bridge waits for the RSPduo to stop (about 1.5 s): it runs off the UI thread, and the
        // refresh timer leaves the Enabled box alone until it is done. The bridge orders the calls (_controlLock).
        private void RunOnBridge(Action action)
        {
            Interlocked.Increment(ref _bridgeCalls);
            Task.Run(() =>
            {
                try
                {
                    action();
                }
                finally
                {
                    Interlocked.Decrement(ref _bridgeCalls);
                }
            });
        }

        private void portNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            var port = (int)portNumericUpDown.Value;
            if (_bridge.Port != port)
            {
                // A new port restarts the bridge, which stops the RSPduo: off the UI thread.
                RunOnBridge(() => _bridge.Port = port);
            }
        }

        private void lnaTrackBar_Scroll(object sender, EventArgs e)
        {
            if (_source.LnaState != lnaTrackBar.Value)
            {
                _source.LnaState = lnaTrackBar.Value;
            }
            lnaValueLabel.Text = LnaText();
        }

        private void agcComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_source.AgcMode != agcComboBox.SelectedIndex)
            {
                _source.AgcMode = agcComboBox.SelectedIndex;
            }
            UpdateModeControls();
        }

        private void setPointNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (_source.AgcSetPoint != (int)setPointNumericUpDown.Value)
            {
                _source.AgcSetPoint = (int)setPointNumericUpDown.Value;
            }
        }

        private void ifGainTrackBar_Scroll(object sender, EventArgs e)
        {
            if (_source.IFGainReduction != ifGainTrackBar.Value)
            {
                _source.IFGainReduction = ifGainTrackBar.Value;
            }
            ifGainValueLabel.Text = _source.IFGainReduction + " dB";
        }

        private void rateComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _source.SampleRateIndex = rateComboBox.SelectedIndex;
        }

        // Single Tuner mode: the three antenna inputs; Diversity: the port of tuner 1 (tuner 2 is on 50 ohm).
        private void FillAntennaComboBox()
        {
            var diversity = _source.Mode == RspDuoModes.Diversity;
            if (_antennaListDiversity == diversity && antennaComboBox.Items.Count > 0)
            {
                return;
            }
            _fillingAntenna = true;
            antennaComboBox.Items.Clear();
            antennaComboBox.Items.AddRange(diversity ? RspDuoAntenna.DiversityNames : RspDuoAntenna.Names);
            antennaComboBox.SelectedIndex = diversity ? (_source.DiversityHiZ ? 1 : 0) : _source.Antenna;
            _antennaListDiversity = diversity;
            _fillingAntenna = false;
        }

        private void antennaComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_fillingAntenna)
            {
                return;
            }
            if (_antennaListDiversity)
            {
                var hiZ = antennaComboBox.SelectedIndex == 1;
                if (_source.DiversityHiZ != hiZ)
                {
                    _source.DiversityHiZ = hiZ;
                }
            }
            else if (_source.Antenna != antennaComboBox.SelectedIndex)
            {
                _source.Antenna = antennaComboBox.SelectedIndex;
            }
            UpdateModeControls();
            refreshTimer_Tick(null, null);
        }

        private void biasTCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_source.BiasT != biasTCheckBox.Checked)
            {
                _source.BiasT = biasTCheckBox.Checked;
            }
        }

        private void rfNotchCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_source.RfNotch != rfNotchCheckBox.Checked)
            {
                _source.RfNotch = rfNotchCheckBox.Checked;
            }
        }

        private void dabNotchCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_source.DabNotch != dabNotchCheckBox.Checked)
            {
                _source.DabNotch = dabNotchCheckBox.Checked;
            }
        }

        private void amNotchCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_source.AmNotch != amNotchCheckBox.Checked)
            {
                _source.AmNotch = amNotchCheckBox.Checked;
            }
        }

        private void ppmNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (_source.Ppm != (double)ppmNumericUpDown.Value)
            {
                _source.Ppm = (double)ppmNumericUpDown.Value;
            }
        }

        private void receiverCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            _receiverPanel.Visible = receiverCheckBox.Checked;
        }

        private void clientCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            _clientPanel.Visible = clientCheckBox.Checked;
        }

        private void diversityCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            _diversityPanel.Visible = diversityCheckBox.Checked;
        }

        // The host showed or hid the window: the toggle only reports it (VDK 05-ui.md 5.9).
        private void receiverPanel_VisibleChanged(object sender, EventArgs e)
        {
            receiverCheckBox.CheckedChanged -= receiverCheckBox_CheckedChanged;
            receiverCheckBox.Checked = _receiverPanel.Visible;
            receiverCheckBox.CheckedChanged += receiverCheckBox_CheckedChanged;
        }

        private void clientPanel_VisibleChanged(object sender, EventArgs e)
        {
            clientCheckBox.CheckedChanged -= clientCheckBox_CheckedChanged;
            clientCheckBox.Checked = _clientPanel.Visible;
            clientCheckBox.CheckedChanged += clientCheckBox_CheckedChanged;
        }

        private void diversityPanel_VisibleChanged(object sender, EventArgs e)
        {
            diversityCheckBox.CheckedChanged -= diversityCheckBox_CheckedChanged;
            diversityCheckBox.Checked = _diversityPanel.Visible;
            diversityCheckBox.CheckedChanged += diversityCheckBox_CheckedChanged;
        }

        private void control_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ThemeIsDark":
                    ApplyTheme();
                    break;
            }
        }

        // What the theme pass does not reach: the toggle icons and their checked colour (VDK 05-ui.md 5.2 rule 1).
        private void ApplyTheme()
        {
            var dark = _control.ThemeIsDark;
            receiverCheckBox.BackgroundImage = dark ? _receiverDarkIcon : _receiverIcon;
            clientCheckBox.BackgroundImage = dark ? _clientDarkIcon : _clientIcon;
            diversityCheckBox.BackgroundImage = dark ? _diversityDarkIcon : _diversityIcon;
            receiverCheckBox.FlatAppearance.CheckedBackColor = dark ? ThemeService.DarkButtonColor : ThemeService.LightButtonColor;
            clientCheckBox.FlatAppearance.CheckedBackColor = dark ? ThemeService.DarkButtonColor : ThemeService.LightButtonColor;
            diversityCheckBox.FlatAppearance.CheckedBackColor = dark ? ThemeService.DarkButtonColor : ThemeService.LightButtonColor;
        }

        private static Bitmap LoadIcon(string name)
        {
            using (var stream = typeof(SettingsPanel).Assembly.GetManifestResourceStream("SDRSharp.SDRplay.Icons." + name))
            {
                using (var image = Image.FromStream(stream))
                {
                    return new Bitmap(image);
                }
            }
        }

        // The number of LNA states depends on the band of the tuned frequency.
        private void UpdateLnaRange()
        {
            var maximum = _source.LnaStateCount - 1;
            if (lnaTrackBar.Maximum != maximum)
            {
                lnaTrackBar.Maximum = maximum;
            }
        }

        private void UpdateLnaRangeB()
        {
            var maximum = _source.LnaStateCountB - 1;
            if (lnaBTrackBar.Maximum != maximum)
            {
                lnaBTrackBar.Maximum = maximum;
            }
        }

        // "state (reduction dB)": the LNA gain reduction from the SDRplay table for the tuned band.
        private string LnaText()
        {
            return LnaText(_source.LnaState, _source.LnaReductionDB);
        }

        private string LnaTextB()
        {
            return LnaText(_source.LnaStateB, _source.LnaReductionDBB);
        }

        private static string LnaText(int state, int reduction)
        {
            return state + " (" + (reduction > 0 ? "-" : "") + reduction + " dB)";
        }

        private void refreshTimer_Tick(object sender, EventArgs e)
        {
            switch (_bridge.State)
            {
                case BridgeState.Listening:
                    stateLabel.Text = "Listening";
                    break;
                case BridgeState.Connected:
                    if (_bridge.SourceFailure.Length > 0)
                    {
                        // The full text is in the Status panel (Device Status): a long line here would wrap and grow the panel.
                        stateLabel.Text = "RSPduo error (see Status)";
                    }
                    else if (!_bridge.FullIQRequested)
                    {
                        stateLabel.Text = "Tick \"Use full IQ\" in the source";
                    }
                    else if (_source.Mode != _source.ActiveMode)
                    {
                        stateLabel.Text = "Reconnect the source for the new mode";
                    }
                    else if (_source.Mode == RspDuoModes.Single && _source.SampleRateIndex != _source.ActiveSampleRateIndex)
                    {
                        stateLabel.Text = "Reconnect the source for the new rate";
                    }
                    else
                    {
                        stateLabel.Text = _bridge.StreamingEnabled ? "Streaming" : "Client connected";
                    }
                    break;
                case BridgeState.Failed:
                    stateLabel.Text = "Failed: " + _bridge.Failure;
                    break;
                default:
                    stateLabel.Text = "Stopped";
                    break;
            }
            if (Volatile.Read(ref _bridgeCalls) == 0 && enableCheckBox.Checked != _bridge.Enabled)
            {
                enableCheckBox.Checked = _bridge.Enabled;
            }
            UpdateLnaRange();
            if (lnaTrackBar.Value != _source.LnaState)
            {
                lnaTrackBar.Value = Math.Min(_source.LnaState, lnaTrackBar.Maximum);
            }
            lnaValueLabel.Text = LnaText();
            UpdateLnaRangeB();
            if (lnaBTrackBar.Value != _source.LnaStateB)
            {
                lnaBTrackBar.Value = Math.Min(_source.LnaStateB, lnaBTrackBar.Maximum);
            }
            lnaBValueLabel.Text = LnaTextB();
        }
    }
}
