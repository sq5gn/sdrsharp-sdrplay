namespace SDRSharp.SDRplay
{
    partial class SettingsPanel
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            enableCheckBox = new System.Windows.Forms.CheckBox();
            stateLabel = new System.Windows.Forms.Label();
            portLabel = new System.Windows.Forms.Label();
            portNumericUpDown = new SDRSharp.PanView.DoubleBufferedNumericUpDown();
            lnaCaptionLabel = new System.Windows.Forms.Label();
            lnaValueLabel = new System.Windows.Forms.Label();
            lnaTrackBar = new SDRSharp.PanView.DoubleBufferedTrackBar();
            agcLabel = new System.Windows.Forms.Label();
            agcComboBox = new SDRSharp.PanView.DoubleBufferedComboBox();
            setPointLabel = new System.Windows.Forms.Label();
            setPointNumericUpDown = new SDRSharp.PanView.DoubleBufferedNumericUpDown();
            ifGainCaptionLabel = new System.Windows.Forms.Label();
            ifGainValueLabel = new System.Windows.Forms.Label();
            ifGainTrackBar = new SDRSharp.PanView.DoubleBufferedTrackBar();
            rateLabel = new System.Windows.Forms.Label();
            rateComboBox = new SDRSharp.PanView.DoubleBufferedComboBox();
            antennaLabel = new System.Windows.Forms.Label();
            antennaComboBox = new SDRSharp.PanView.DoubleBufferedComboBox();
            biasTCheckBox = new System.Windows.Forms.CheckBox();
            rfNotchCheckBox = new System.Windows.Forms.CheckBox();
            dabNotchCheckBox = new System.Windows.Forms.CheckBox();
            amNotchCheckBox = new System.Windows.Forms.CheckBox();
            ppmLabel = new System.Windows.Forms.Label();
            ppmNumericUpDown = new SDRSharp.PanView.DoubleBufferedNumericUpDown();
            receiverCheckBox = new System.Windows.Forms.CheckBox();
            clientCheckBox = new System.Windows.Forms.CheckBox();
            toggleTableLayoutPanel = new SDRSharp.PanView.DoubleBufferedTableLayoutPanel();
            mainTableLayoutPanel = new SDRSharp.PanView.DoubleBufferedTableLayoutPanel();
            refreshTimer = new System.Windows.Forms.Timer(components);
            toolTip = new System.Windows.Forms.ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)portNumericUpDown).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lnaTrackBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)setPointNumericUpDown).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ifGainTrackBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ppmNumericUpDown).BeginInit();
            toggleTableLayoutPanel.SuspendLayout();
            mainTableLayoutPanel.SuspendLayout();
            SuspendLayout();
            //
            // enableCheckBox
            //
            enableCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            enableCheckBox.AutoSize = true;
            enableCheckBox.Margin = new System.Windows.Forms.Padding(3);
            enableCheckBox.Name = "enableCheckBox";
            enableCheckBox.TabIndex = 0;
            enableCheckBox.Text = "Enabled";
            toolTip.SetToolTip(enableCheckBox, "Start / stop the SpyServer bridge on 127.0.0.1");
            enableCheckBox.CheckedChanged += enableCheckBox_ValueChanged;
            //
            // stateLabel
            //
            stateLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            stateLabel.AutoSize = true;
            stateLabel.Margin = new System.Windows.Forms.Padding(3);
            stateLabel.Name = "stateLabel";
            stateLabel.TabIndex = 1;
            stateLabel.Text = "Stopped";
            stateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // portLabel
            //
            portLabel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            portLabel.AutoSize = true;
            portLabel.Margin = new System.Windows.Forms.Padding(3);
            portLabel.Name = "portLabel";
            portLabel.TabIndex = 2;
            portLabel.Text = "TCP Port";
            portLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // portNumericUpDown
            //
            portNumericUpDown.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            portNumericUpDown.Font = new System.Drawing.Font("Segoe UI", 11F);
            portNumericUpDown.Margin = new System.Windows.Forms.Padding(3);
            portNumericUpDown.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            portNumericUpDown.Minimum = new decimal(new int[] { 1024, 0, 0, 0 });
            portNumericUpDown.MinimumSize = new System.Drawing.Size(30, 0);
            portNumericUpDown.Name = "portNumericUpDown";
            portNumericUpDown.TabIndex = 3;
            portNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            portNumericUpDown.Value = new decimal(new int[] { 5555, 0, 0, 0 });
            portNumericUpDown.ValueChanged += portNumericUpDown_ValueChanged;
            //
            // lnaCaptionLabel
            //
            lnaCaptionLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            lnaCaptionLabel.AutoSize = true;
            lnaCaptionLabel.Margin = new System.Windows.Forms.Padding(3);
            lnaCaptionLabel.Name = "lnaCaptionLabel";
            lnaCaptionLabel.TabIndex = 4;
            lnaCaptionLabel.Text = "LNA State";
            //
            // lnaValueLabel
            //
            lnaValueLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            lnaValueLabel.AutoSize = true;
            lnaValueLabel.Margin = new System.Windows.Forms.Padding(3);
            lnaValueLabel.Name = "lnaValueLabel";
            lnaValueLabel.TabIndex = 5;
            lnaValueLabel.Text = "0 (0 dB)";
            lnaValueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lnaTrackBar
            //
            lnaTrackBar.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            mainTableLayoutPanel.SetColumnSpan(lnaTrackBar, 2);
            lnaTrackBar.Margin = new System.Windows.Forms.Padding(3);
            lnaTrackBar.Maximum = 9;
            lnaTrackBar.Minimum = 0;
            lnaTrackBar.Name = "lnaTrackBar";
            lnaTrackBar.Size = new System.Drawing.Size(227, 45);
            lnaTrackBar.TabIndex = 6;
            lnaTrackBar.TickFrequency = 1;
            lnaTrackBar.Value = 0;
            toolTip.SetToolTip(lnaTrackBar, "LNA state: higher state = more RF gain reduction (dB from the SDRplay table for this band)");
            lnaTrackBar.ValueChanged += lnaTrackBar_Scroll;
            //
            // agcLabel
            //
            agcLabel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            agcLabel.AutoSize = true;
            agcLabel.Margin = new System.Windows.Forms.Padding(3);
            agcLabel.Name = "agcLabel";
            agcLabel.TabIndex = 7;
            agcLabel.Text = "AGC";
            agcLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // agcComboBox
            //
            agcComboBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            agcComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            agcComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            agcComboBox.Font = new System.Drawing.Font("Segoe UI", 11F);
            agcComboBox.Items.AddRange(new object[] { "Off", "Fast", "Medium", "Slow" });
            agcComboBox.Margin = new System.Windows.Forms.Padding(3);
            agcComboBox.Name = "agcComboBox";
            agcComboBox.TabIndex = 8;
            toolTip.SetToolTip(agcComboBox, "Receiver AGC speed (loop bandwidth): Fast 100 Hz, Medium 50 Hz, Slow 5 Hz; Off = manual IF gain reduction");
            agcComboBox.SelectedIndexChanged += agcComboBox_SelectedIndexChanged;
            //
            // setPointLabel
            //
            setPointLabel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            setPointLabel.AutoSize = true;
            setPointLabel.Margin = new System.Windows.Forms.Padding(3);
            setPointLabel.Name = "setPointLabel";
            setPointLabel.TabIndex = 9;
            setPointLabel.Text = "AGC Setpoint (dBFS)";
            setPointLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // setPointNumericUpDown
            //
            setPointNumericUpDown.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            setPointNumericUpDown.Font = new System.Drawing.Font("Segoe UI", 11F);
            setPointNumericUpDown.Margin = new System.Windows.Forms.Padding(3);
            setPointNumericUpDown.Maximum = new decimal(new int[] { 20, 0, 0, -2147483648 });
            setPointNumericUpDown.Minimum = new decimal(new int[] { 72, 0, 0, -2147483648 });
            setPointNumericUpDown.MinimumSize = new System.Drawing.Size(30, 0);
            setPointNumericUpDown.Name = "setPointNumericUpDown";
            setPointNumericUpDown.TabIndex = 10;
            setPointNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            setPointNumericUpDown.Value = new decimal(new int[] { 60, 0, 0, -2147483648 });
            setPointNumericUpDown.ValueChanged += setPointNumericUpDown_ValueChanged;
            //
            // ifGainCaptionLabel
            //
            ifGainCaptionLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            ifGainCaptionLabel.AutoSize = true;
            ifGainCaptionLabel.Margin = new System.Windows.Forms.Padding(3);
            ifGainCaptionLabel.Name = "ifGainCaptionLabel";
            ifGainCaptionLabel.TabIndex = 11;
            ifGainCaptionLabel.Text = "IF Gain Reduction";
            //
            // ifGainValueLabel
            //
            ifGainValueLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            ifGainValueLabel.AutoSize = true;
            ifGainValueLabel.Margin = new System.Windows.Forms.Padding(3);
            ifGainValueLabel.Name = "ifGainValueLabel";
            ifGainValueLabel.TabIndex = 12;
            ifGainValueLabel.Text = "50 dB";
            ifGainValueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // ifGainTrackBar
            //
            ifGainTrackBar.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            mainTableLayoutPanel.SetColumnSpan(ifGainTrackBar, 2);
            ifGainTrackBar.Margin = new System.Windows.Forms.Padding(3);
            ifGainTrackBar.Maximum = 59;
            ifGainTrackBar.Minimum = 20;
            ifGainTrackBar.Name = "ifGainTrackBar";
            ifGainTrackBar.Size = new System.Drawing.Size(227, 45);
            ifGainTrackBar.TabIndex = 13;
            ifGainTrackBar.TickFrequency = 5;
            ifGainTrackBar.Value = 50;
            toolTip.SetToolTip(ifGainTrackBar, "IF gain reduction, used while the AGC is off");
            ifGainTrackBar.ValueChanged += ifGainTrackBar_Scroll;
            //
            // rateLabel
            //
            rateLabel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            rateLabel.AutoSize = true;
            rateLabel.Margin = new System.Windows.Forms.Padding(3);
            rateLabel.Name = "rateLabel";
            rateLabel.TabIndex = 15;
            rateLabel.Text = "Sample Rate";
            rateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // rateComboBox
            //
            rateComboBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            rateComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            rateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            rateComboBox.DropDownWidth = 230;
            rateComboBox.Font = new System.Drawing.Font("Segoe UI", 11F);
            rateComboBox.Margin = new System.Windows.Forms.Padding(3);
            rateComboBox.Name = "rateComboBox";
            rateComboBox.TabIndex = 16;
            toolTip.SetToolTip(rateComboBox, "RSPduo sample rate and IF filter; applies at the next connection of the SDR# source");
            rateComboBox.SelectedIndexChanged += rateComboBox_SelectedIndexChanged;
            //
            // antennaLabel
            //
            antennaLabel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            antennaLabel.AutoSize = true;
            antennaLabel.Margin = new System.Windows.Forms.Padding(3);
            antennaLabel.Name = "antennaLabel";
            antennaLabel.TabIndex = 17;
            antennaLabel.Text = "Antenna";
            antennaLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // antennaComboBox
            //
            antennaComboBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            antennaComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            antennaComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            antennaComboBox.Font = new System.Drawing.Font("Segoe UI", 11F);
            antennaComboBox.Margin = new System.Windows.Forms.Padding(3);
            antennaComboBox.Name = "antennaComboBox";
            antennaComboBox.TabIndex = 18;
            toolTip.SetToolTip(antennaComboBox, "RSPduo input: tuner 1 (50 ohm or Hi-Z, Hi-Z below 60 MHz) or tuner 2");
            antennaComboBox.SelectedIndexChanged += antennaComboBox_SelectedIndexChanged;
            //
            // biasTCheckBox
            //
            biasTCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            biasTCheckBox.AutoSize = true;
            biasTCheckBox.Margin = new System.Windows.Forms.Padding(3);
            biasTCheckBox.Name = "biasTCheckBox";
            biasTCheckBox.TabIndex = 19;
            biasTCheckBox.Text = "Bias-T";
            toolTip.SetToolTip(biasTCheckBox, "DC supply on the tuner 2 antenna input (for an active antenna or LNA)");
            biasTCheckBox.CheckedChanged += biasTCheckBox_CheckedChanged;
            //
            // rfNotchCheckBox
            //
            rfNotchCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            rfNotchCheckBox.AutoSize = true;
            rfNotchCheckBox.Margin = new System.Windows.Forms.Padding(3);
            rfNotchCheckBox.Name = "rfNotchCheckBox";
            rfNotchCheckBox.TabIndex = 20;
            rfNotchCheckBox.Text = "MW/FM Notch";
            toolTip.SetToolTip(rfNotchCheckBox, "Broadcast MW/FM notch filter");
            rfNotchCheckBox.CheckedChanged += rfNotchCheckBox_CheckedChanged;
            //
            // amNotchCheckBox
            //
            amNotchCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            amNotchCheckBox.AutoSize = true;
            amNotchCheckBox.Margin = new System.Windows.Forms.Padding(3);
            amNotchCheckBox.Name = "amNotchCheckBox";
            amNotchCheckBox.TabIndex = 21;
            amNotchCheckBox.Text = "AM Notch";
            toolTip.SetToolTip(amNotchCheckBox, "AM notch filter of tuner 1");
            amNotchCheckBox.CheckedChanged += amNotchCheckBox_CheckedChanged;
            //
            // dabNotchCheckBox
            //
            dabNotchCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dabNotchCheckBox.AutoSize = true;
            dabNotchCheckBox.Margin = new System.Windows.Forms.Padding(3);
            dabNotchCheckBox.Name = "dabNotchCheckBox";
            dabNotchCheckBox.TabIndex = 22;
            dabNotchCheckBox.Text = "DAB Notch";
            toolTip.SetToolTip(dabNotchCheckBox, "DAB notch filter");
            dabNotchCheckBox.CheckedChanged += dabNotchCheckBox_CheckedChanged;
            //
            // ppmLabel
            //
            ppmLabel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ppmLabel.AutoSize = true;
            ppmLabel.Margin = new System.Windows.Forms.Padding(3);
            ppmLabel.Name = "ppmLabel";
            ppmLabel.TabIndex = 23;
            ppmLabel.Text = "Frequency Correction (ppm)";
            ppmLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // ppmNumericUpDown
            //
            ppmNumericUpDown.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ppmNumericUpDown.DecimalPlaces = 2;
            ppmNumericUpDown.Font = new System.Drawing.Font("Segoe UI", 11F);
            ppmNumericUpDown.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            ppmNumericUpDown.Margin = new System.Windows.Forms.Padding(3);
            ppmNumericUpDown.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            ppmNumericUpDown.Minimum = new decimal(new int[] { 100, 0, 0, -2147483648 });
            ppmNumericUpDown.MinimumSize = new System.Drawing.Size(30, 0);
            ppmNumericUpDown.Name = "ppmNumericUpDown";
            ppmNumericUpDown.TabIndex = 24;
            ppmNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            toolTip.SetToolTip(ppmNumericUpDown, "Correction of the RSPduo reference oscillator, parts per million");
            ppmNumericUpDown.ValueChanged += ppmNumericUpDown_ValueChanged;
            //
            // receiverCheckBox
            //
            receiverCheckBox.Appearance = System.Windows.Forms.Appearance.Button;
            receiverCheckBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            receiverCheckBox.FlatAppearance.BorderSize = 0;
            receiverCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            receiverCheckBox.Margin = new System.Windows.Forms.Padding(3);
            receiverCheckBox.Name = "receiverCheckBox";
            receiverCheckBox.Size = new System.Drawing.Size(40, 40);
            receiverCheckBox.TabIndex = 25;
            toolTip.SetToolTip(receiverCheckBox, "Show / hide the RSPduo status");
            receiverCheckBox.CheckedChanged += receiverCheckBox_CheckedChanged;
            //
            // clientCheckBox
            //
            clientCheckBox.Appearance = System.Windows.Forms.Appearance.Button;
            clientCheckBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            clientCheckBox.FlatAppearance.BorderSize = 0;
            clientCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            clientCheckBox.Margin = new System.Windows.Forms.Padding(3);
            clientCheckBox.Name = "clientCheckBox";
            clientCheckBox.Size = new System.Drawing.Size(40, 40);
            clientCheckBox.TabIndex = 26;
            toolTip.SetToolTip(clientCheckBox, "Show / hide the SpyServer client status");
            clientCheckBox.CheckedChanged += clientCheckBox_CheckedChanged;
            //
            // toggleTableLayoutPanel
            //
            toggleTableLayoutPanel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            toggleTableLayoutPanel.AutoSize = true;
            toggleTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            toggleTableLayoutPanel.ColumnCount = 2;
            toggleTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            toggleTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            toggleTableLayoutPanel.Controls.Add(receiverCheckBox, 0, 0);
            toggleTableLayoutPanel.Controls.Add(clientCheckBox, 1, 0);
            toggleTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            toggleTableLayoutPanel.Name = "toggleTableLayoutPanel";
            toggleTableLayoutPanel.RowCount = 1;
            toggleTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            toggleTableLayoutPanel.TabIndex = 14;
            //
            // mainTableLayoutPanel
            //
            mainTableLayoutPanel.AutoSize = true;
            mainTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            mainTableLayoutPanel.ColumnCount = 2;
            mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            mainTableLayoutPanel.Controls.Add(enableCheckBox, 0, 0);
            mainTableLayoutPanel.Controls.Add(stateLabel, 1, 0);
            mainTableLayoutPanel.Controls.Add(portLabel, 0, 1);
            mainTableLayoutPanel.Controls.Add(portNumericUpDown, 1, 1);
            mainTableLayoutPanel.Controls.Add(lnaCaptionLabel, 0, 2);
            mainTableLayoutPanel.Controls.Add(lnaValueLabel, 1, 2);
            mainTableLayoutPanel.Controls.Add(lnaTrackBar, 0, 3);
            mainTableLayoutPanel.Controls.Add(agcLabel, 0, 4);
            mainTableLayoutPanel.Controls.Add(agcComboBox, 1, 4);
            mainTableLayoutPanel.Controls.Add(setPointLabel, 0, 5);
            mainTableLayoutPanel.Controls.Add(setPointNumericUpDown, 1, 5);
            mainTableLayoutPanel.Controls.Add(ifGainCaptionLabel, 0, 6);
            mainTableLayoutPanel.Controls.Add(ifGainValueLabel, 1, 6);
            mainTableLayoutPanel.Controls.Add(ifGainTrackBar, 0, 7);
            mainTableLayoutPanel.Controls.Add(rateLabel, 0, 8);
            mainTableLayoutPanel.Controls.Add(rateComboBox, 1, 8);
            mainTableLayoutPanel.Controls.Add(antennaLabel, 0, 9);
            mainTableLayoutPanel.Controls.Add(antennaComboBox, 1, 9);
            mainTableLayoutPanel.Controls.Add(biasTCheckBox, 0, 10);
            mainTableLayoutPanel.Controls.Add(rfNotchCheckBox, 1, 10);
            mainTableLayoutPanel.Controls.Add(amNotchCheckBox, 0, 11);
            mainTableLayoutPanel.Controls.Add(dabNotchCheckBox, 1, 11);
            mainTableLayoutPanel.Controls.Add(ppmLabel, 0, 12);
            mainTableLayoutPanel.Controls.Add(ppmNumericUpDown, 1, 12);
            mainTableLayoutPanel.Controls.Add(toggleTableLayoutPanel, 0, 13);
            mainTableLayoutPanel.SetColumnSpan(toggleTableLayoutPanel, 2);
            mainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
            mainTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            mainTableLayoutPanel.RowCount = 14;
            mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            mainTableLayoutPanel.TabIndex = 0;
            //
            // refreshTimer
            //
            refreshTimer.Enabled = true;
            refreshTimer.Interval = 500;
            refreshTimer.Tick += refreshTimer_Tick;
            //
            // SettingsPanel
            //
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            Controls.Add(mainTableLayoutPanel);
            Name = "SettingsPanel";
            ((System.ComponentModel.ISupportInitialize)portNumericUpDown).EndInit();
            ((System.ComponentModel.ISupportInitialize)lnaTrackBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)setPointNumericUpDown).EndInit();
            ((System.ComponentModel.ISupportInitialize)ifGainTrackBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)ppmNumericUpDown).EndInit();
            toggleTableLayoutPanel.ResumeLayout(false);
            toggleTableLayoutPanel.PerformLayout();
            mainTableLayoutPanel.ResumeLayout(false);
            mainTableLayoutPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.CheckBox enableCheckBox;
        private System.Windows.Forms.Label stateLabel;
        private System.Windows.Forms.Label portLabel;
        private SDRSharp.PanView.DoubleBufferedNumericUpDown portNumericUpDown;
        private System.Windows.Forms.Label lnaCaptionLabel;
        private System.Windows.Forms.Label lnaValueLabel;
        private SDRSharp.PanView.DoubleBufferedTrackBar lnaTrackBar;
        private System.Windows.Forms.Label agcLabel;
        private SDRSharp.PanView.DoubleBufferedComboBox agcComboBox;
        private System.Windows.Forms.Label setPointLabel;
        private SDRSharp.PanView.DoubleBufferedNumericUpDown setPointNumericUpDown;
        private System.Windows.Forms.Label ifGainCaptionLabel;
        private System.Windows.Forms.Label ifGainValueLabel;
        private SDRSharp.PanView.DoubleBufferedTrackBar ifGainTrackBar;
        private System.Windows.Forms.Label rateLabel;
        private SDRSharp.PanView.DoubleBufferedComboBox rateComboBox;
        private System.Windows.Forms.Label antennaLabel;
        private SDRSharp.PanView.DoubleBufferedComboBox antennaComboBox;
        private System.Windows.Forms.CheckBox biasTCheckBox;
        private System.Windows.Forms.CheckBox rfNotchCheckBox;
        private System.Windows.Forms.CheckBox dabNotchCheckBox;
        private System.Windows.Forms.CheckBox amNotchCheckBox;
        private System.Windows.Forms.Label ppmLabel;
        private SDRSharp.PanView.DoubleBufferedNumericUpDown ppmNumericUpDown;
        private System.Windows.Forms.CheckBox receiverCheckBox;
        private System.Windows.Forms.CheckBox clientCheckBox;
        private SDRSharp.PanView.DoubleBufferedTableLayoutPanel toggleTableLayoutPanel;
        private SDRSharp.PanView.DoubleBufferedTableLayoutPanel mainTableLayoutPanel;
        private System.Windows.Forms.Timer refreshTimer;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
