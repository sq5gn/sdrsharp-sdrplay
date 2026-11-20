namespace SDRSharp.SDRplay
{
    partial class StatusPanel
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
            mainTableLayoutPanel = new SDRSharp.PanView.DoubleBufferedTableLayoutPanel();
            refreshTimer = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            //
            // mainTableLayoutPanel
            //
            mainTableLayoutPanel.AutoSize = true;
            mainTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            mainTableLayoutPanel.ColumnCount = 2;
            mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            mainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
            mainTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            mainTableLayoutPanel.RowCount = 0;
            mainTableLayoutPanel.TabIndex = 0;
            //
            // refreshTimer
            //
            refreshTimer.Enabled = false;
            refreshTimer.Interval = 500;
            refreshTimer.Tick += refreshTimer_Tick;
            //
            // StatusPanel
            //
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            Controls.Add(mainTableLayoutPanel);
            Name = "StatusPanel";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private SDRSharp.PanView.DoubleBufferedTableLayoutPanel mainTableLayoutPanel;
        private System.Windows.Forms.Timer refreshTimer;
    }
}
