using SDRSharp.Common;
using SDRSharp.PanView;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SDRSharp.SDRplay
{
    // Front panel "Diversity": what goes to SDR# (tuner 1, tuner 2 or A + w * B), the weight
    // w = g * e^(j phi) on the phase wheel and the gain slider, the Null / Max auto, and the measurements.
    // A separate docked window by the user's choice, so that the plugin window keeps the receiver settings.
    public class DiversityPanel : StatusPanel
    {
        private const int GainSteps = 10; // track bar steps per dB
        private const int MinWheelSize = 160; // px
        private const int MaxWheelSize = 300; // px

        private readonly ISharpControl _control;
        private readonly DiversityCombiner _combiner;
        private readonly DoubleBufferedComboBox _outputComboBox;
        private readonly PhaseWheel _phaseWheel;
        private readonly Label _phaseValue;
        private readonly Label _gainValue;
        private readonly DoubleBufferedTrackBar _gainTrackBar;
        private readonly CheckBox _autoAfterRetuneCheckBox;
        private readonly Label _statusValue;
        private readonly Label _lastValue;
        private readonly Label _referenceValue;

        public DiversityPanel(SpyServerBridge bridge, SDRplaySource source, ISharpControl control)
            : base(bridge, source, "Diversity")
        {
            _control = control;
            _combiner = source.Combiner;
            // Every row follows the width of the window (the wheel, the slider and the buttons stretch, the values are
            // short): no horizontal scroll bar.
            ScrollsHorizontally = false;

            _outputComboBox = new DoubleBufferedComboBox();
            _outputComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _outputComboBox.DrawMode = DrawMode.OwnerDrawFixed;
            _outputComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _outputComboBox.Font = new Font("Segoe UI", 11F);
            _outputComboBox.Margin = new Padding(3);
            _outputComboBox.Items.AddRange(RspDuoModes.OutputNames);
            _outputComboBox.SelectedIndex = RspDuoModes.OutputIndex(_source.Output);
            _outputComboBox.SelectedIndexChanged += outputComboBox_SelectedIndexChanged;
            AddControlRow(Caption("Output"), _outputComboBox);

            _phaseValue = AddRow("Phase (Tuner 2)");
            _phaseWheel = new PhaseWheel();
            _phaseWheel.Anchor = AnchorStyles.None;
            _phaseWheel.Margin = new Padding(3);
            _phaseWheel.Size = new Size(220, 220);
            _phaseWheel.Phase = _combiner.Phase;
            _phaseWheel.PhaseChanged += phaseWheel_PhaseChanged;
            AddSpanRow(_phaseWheel);

            _gainValue = AddRow("Gain (Tuner 2)");
            _gainTrackBar = new DoubleBufferedTrackBar();
            _gainTrackBar.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _gainTrackBar.Margin = new Padding(3);
            _gainTrackBar.Minimum = (int)(DiversityCombiner.MinGainDB * GainSteps);
            _gainTrackBar.Maximum = (int)(DiversityCombiner.MaxGainDB * GainSteps);
            _gainTrackBar.TickFrequency = 10 * GainSteps;
            _gainTrackBar.Size = new Size(227, 45);
            _gainTrackBar.Value = GainToTrackBar(_combiner.GainDB);
            _gainTrackBar.ValueChanged += gainTrackBar_Scroll;
            AddSpanRow(_gainTrackBar);

            var nullButton = ActionButton("Null", "Measure the reference band (Mark; the whole band without one) and null the signal in it");
            nullButton.Click += nullButton_Click;
            var maxButton = ActionButton("Max", "Measure the SDR# filter passband and add the tuners in phase there (maximal ratio combining)");
            maxButton.Click += maxButton_Click;
            var buttonTable = new DoubleBufferedTableLayoutPanel();
            buttonTable.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            buttonTable.AutoSize = true;
            buttonTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonTable.ColumnCount = 2;
            buttonTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            buttonTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            buttonTable.RowCount = 1;
            buttonTable.RowStyles.Add(new RowStyle());
            buttonTable.Margin = new Padding(0);
            buttonTable.Controls.Add(nullButton, 0, 0);
            buttonTable.Controls.Add(maxButton, 1, 0);
            AddSpanRow(buttonTable);

            _referenceValue = AddRow("Reference (Null)");
            var markButton = ActionButton("Mark", "Mark the SDR# filter passband as the reference band: move the filter onto the unwanted signal, click Mark, move back, click Null");
            markButton.Click += markButton_Click;
            var clearButton = ActionButton("Clear", "No reference band: Null measures the whole band");
            clearButton.Click += clearButton_Click;
            var referenceTable = new DoubleBufferedTableLayoutPanel();
            referenceTable.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            referenceTable.AutoSize = true;
            referenceTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            referenceTable.ColumnCount = 2;
            referenceTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            referenceTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            referenceTable.RowCount = 1;
            referenceTable.RowStyles.Add(new RowStyle());
            referenceTable.Margin = new Padding(0);
            referenceTable.Controls.Add(markButton, 0, 0);
            referenceTable.Controls.Add(clearButton, 1, 0);
            AddSpanRow(referenceTable);

            _autoAfterRetuneCheckBox = new CheckBox();
            _autoAfterRetuneCheckBox.Anchor = AnchorStyles.Left;
            _autoAfterRetuneCheckBox.AutoSize = true;
            _autoAfterRetuneCheckBox.Margin = new Padding(3);
            _autoAfterRetuneCheckBox.Text = "Auto after retune";
            _autoAfterRetuneCheckBox.Checked = _source.AutoAfterRetune;
            _autoAfterRetuneCheckBox.CheckedChanged += autoAfterRetuneCheckBox_CheckedChanged;
            AddSpanRow(_autoAfterRetuneCheckBox);

            _statusValue = AddRow("Status");
            _lastValue = AddRow("Last Auto");

            var toolTip = new ToolTip();
            toolTip.SetToolTip(_outputComboBox, "What goes to SDR#: tuner 1, tuner 2, or tuner 1 + tuner 2 with the phase and gain below");
            toolTip.SetToolTip(_phaseWheel, "Phase of tuner 2 in the sum: drag, or turn the mouse wheel (1 deg, Shift 10 deg)");
            toolTip.SetToolTip(_gainTrackBar, "Level of tuner 2 relative to tuner 1 in the sum");
            toolTip.SetToolTip(_autoAfterRetuneCheckBox, "Repeat the last Null / Max after every frequency change (a retune changes the phase between the tuners)");
            toolTip.SetToolTip(_lastValue, "Mode, band measured (ref = reference band, filter = SDR# filter, whole = whole band; whole* = the reference or the filter is wider than 25 kHz or outside the stream), coherence c of the tuners in it");

            _control.PropertyChanged += control_PropertyChanged;
            ApplyTheme();
            phaseWheel_PhaseChanged(null, null);
            gainTrackBar_Scroll(null, null);
            UpdateReference();
        }

        // The wheel fills the width of the window (a docked window is as wide as the user makes it).
        protected override void OnClientSizeChanged(EventArgs e)
        {
            if (_phaseWheel != null)
            {
                // The wheel takes what the other rows leave of the window, in width and in height.
                var otherRows = RowsHeight - _phaseWheel.Height;
                var side = Math.Min(ClientSize.Width - 6, ClientSize.Height - otherRows);
                side = Math.Max(MinWheelSize, Math.Min(side, MaxWheelSize));
                if (_phaseWheel.Width != side)
                {
                    _phaseWheel.Size = new Size(side, side);
                }
            }
            base.OnClientSizeChanged(e);
        }

        private static Label Caption(string text)
        {
            var label = new Label();
            label.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label.AutoSize = true;
            label.Margin = new Padding(3);
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private Button ActionButton(string text, string toolTip)
        {
            var button = new Button();
            button.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            button.AutoSize = true;
            button.FlatStyle = FlatStyle.Flat;
            button.Margin = new Padding(3);
            button.Text = text;
            new ToolTip().SetToolTip(button, toolTip);
            return button;
        }

        private void outputComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _source.Output = RspDuoModes.OutputOf(_outputComboBox.SelectedIndex);
        }

        private void phaseWheel_PhaseChanged(object sender, EventArgs e)
        {
            if (sender != null)
            {
                _combiner.Phase = _phaseWheel.Phase;
            }
            _phaseValue.Text = _combiner.Phase.ToString("0") + "°";
        }

        private void gainTrackBar_Scroll(object sender, EventArgs e)
        {
            var gain = (double)_gainTrackBar.Value / GainSteps;
            // The track bar follows an auto result (refresh tick): only a move of the user sets the gain.
            if (GainToTrackBar(_combiner.GainDB) != _gainTrackBar.Value)
            {
                _combiner.GainDB = gain;
            }
            _gainValue.Text = _combiner.GainDB.ToString("0.0") + " dB";
        }

        private void nullButton_Click(object sender, EventArgs e)
        {
            _combiner.RequestAuto(DiversityCombiner.AutoNull);
        }

        private void maxButton_Click(object sender, EventArgs e)
        {
            _combiner.RequestAuto(DiversityCombiner.AutoMax);
        }

        private void markButton_Click(object sender, EventArgs e)
        {
            long low;
            long high;
            if (_combiner.GetFilterBand(out low, out high))
            {
                _combiner.SetReference(low, high);
            }
            UpdateReference();
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            _combiner.ClearReference();
            UpdateReference();
        }

        // Absolute edges, kHz: the band stays on its station while the VFO moves.
        private void UpdateReference()
        {
            long low;
            long high;
            if (_combiner.GetReference(out low, out high))
            {
                _referenceValue.Text = (low / 1000.0).ToString("0.00") + "-" + (high / 1000.0).ToString("0.00") + " kHz";
            }
            else
            {
                _referenceValue.Text = "Whole band";
            }
        }

        private void autoAfterRetuneCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            _source.AutoAfterRetune = _autoAfterRetuneCheckBox.Checked;
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

        // The wheel is a custom control: the theme pass does not set its colours (VDK 05-ui.md 5.2 rule 1).
        private void ApplyTheme()
        {
            _phaseWheel.BackColor = _control.ThemePanelColor;
            _phaseWheel.SetColors(_control.ThemePanelColor, _control.ThemeForeColor);
        }

        private static int GainToTrackBar(double gainDB)
        {
            var value = (int)Math.Round(gainDB * GainSteps);
            return Math.Min(Math.Max(value, (int)(DiversityCombiner.MinGainDB * GainSteps)), (int)(DiversityCombiner.MaxGainDB * GainSteps));
        }

        protected override void UpdateValues()
        {
            // An auto result moves the wheel and the slider.
            _phaseWheel.Phase = _combiner.Phase;
            _phaseValue.Text = _combiner.Phase.ToString("0") + "°";
            var gain = GainToTrackBar(_combiner.GainDB);
            if (_gainTrackBar.Value != gain)
            {
                _gainTrackBar.Value = gain;
            }
            _gainValue.Text = _combiner.GainDB.ToString("0.0") + " dB";

            var diversity = _source.IsStreaming && _source.ActiveMode == RspDuoModes.Diversity;
            if (!_source.IsStreaming)
            {
                _statusValue.Text = "Not streaming";
            }
            else if (!diversity)
            {
                _statusValue.Text = "Mode is Single Tuner";
            }
            else if (_combiner.AutoPending)
            {
                _statusValue.Text = "Measuring...";
            }
            else
            {
                _statusValue.Text = "Running";
            }
            _lastValue.Text = LastAuto();
            UpdateReference();
        }

        // The mode, the band it measured and the coherence of the tuners in that band.
        private string LastAuto()
        {
            string band;
            switch (_combiner.LastBand)
            {
                case DiversityCombiner.BandWhole:
                    band = "whole";
                    break;
                case DiversityCombiner.BandReference:
                    band = "ref";
                    break;
                case DiversityCombiner.BandFilter:
                    band = "filter";
                    break;
                case DiversityCombiner.BandReferenceOutside:
                    band = "whole*";
                    break;
                case DiversityCombiner.BandFilterOutside:
                    band = "whole*";
                    break;
                default:
                    return "-";
            }
            var mode = _combiner.LastMode == DiversityCombiner.AutoNull ? "Null" : "Max";
            return mode + ", " + band + ", c " + _combiner.LastCoherence.ToString("0.000");
        }
    }
}
