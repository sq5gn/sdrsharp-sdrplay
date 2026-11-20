using System;
using System.Windows.Forms;

namespace SDRSharp.SDRplay
{
    // Base of the front panels: caption / value rows, refreshed every 500 ms while the panel is shown
    // and the bridge is enabled (VDK 05-ui.md 5.9).
    public partial class StatusPanel : UserControl
    {
        protected readonly SpyServerBridge _bridge;
        protected readonly SDRplaySource _source;

        // For the WinForms designer only.
        public StatusPanel()
        {
            InitializeComponent();
        }

        protected StatusPanel(SpyServerBridge bridge, SDRplaySource source, string title)
        {
            _bridge = bridge;
            _source = source;

            InitializeComponent();

            Text = title;
            Visible = false;
            AutoScroll = true;
        }

        // The host docks the panel Fill; a docked table does not count for AutoScroll, so the scroll area
        // is the table's natural size: scroll bars appear when the window is smaller than the rows.
        protected override void OnLayout(LayoutEventArgs e)
        {
            var size = mainTableLayoutPanel.GetPreferredSize(System.Drawing.Size.Empty);
            if (AutoScrollMinSize != size)
            {
                AutoScrollMinSize = size;
            }
            base.OnLayout(e);
        }

        public void StopRefresh()
        {
            refreshTimer.Enabled = false;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            refreshTimer.Enabled = Visible;
            if (Visible)
            {
                refreshTimer_Tick(null, null);
            }
        }

        // Called by the refresh timer while the panel is shown and the bridge is enabled.
        protected virtual void UpdateValues()
        {
        }

        protected Label AddRow(string caption)
        {
            var row = mainTableLayoutPanel.RowCount;
            mainTableLayoutPanel.RowCount = row + 1;
            mainTableLayoutPanel.RowStyles.Add(new RowStyle());

            var captionLabel = new Label();
            captionLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            captionLabel.AutoSize = true;
            captionLabel.Margin = new Padding(3);
            captionLabel.Text = caption;
            captionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            var valueLabel = new Label();
            valueLabel.Anchor = AnchorStyles.Right;
            valueLabel.AutoSize = true;
            valueLabel.Margin = new Padding(3);
            valueLabel.Text = "-";
            valueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            mainTableLayoutPanel.Controls.Add(captionLabel, 0, row);
            mainTableLayoutPanel.Controls.Add(valueLabel, 1, row);
            return valueLabel;
        }

        private void refreshTimer_Tick(object sender, EventArgs e)
        {
            if (_bridge == null || !_bridge.Enabled)
            {
                return;
            }
            UpdateValues();
        }
    }
}
