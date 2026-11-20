using SDRSharp.PanView;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace SDRSharp.SDRplay
{
    // Phase wheel of the Diversity window: 0 deg at the top, clockwise. Drag with the left button to set the
    // phase, the mouse wheel steps 1 deg (10 deg with Shift). Drawn off the UI thread into NativeBitmaps by a
    // render timer (VDK 05-ui.md 5.8): the UI thread only stores the phase and sets a flag.
    internal sealed class PhaseWheel : Control
    {
        private const int FramePeriod = 40; // ms: at most 25 frames per second
        private const double WheelStep = 1; // deg per mouse wheel notch
        private const double ShiftWheelStep = 10; // deg per notch with Shift

        private readonly object _framelock = new object();
        private System.Timers.Timer _timer;
        private NativeBitmap _frame;
        private NativeBitmap _background;
        private Color _backColor = Color.Black;
        private Color _foreColor = Color.White;
        private Font _font; // copied on the UI thread: the render timer never reads Control.Font
        private double _phase; // deg, written on the UI thread, read by the render timer
        private bool _dragging;
        private volatile bool _performIsNeeded;
        private volatile bool _backgroundIsNeeded = true;

        public event EventHandler PhaseChanged;

        public PhaseWheel()
        {
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, false);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, false);
            SetStyle(ControlStyles.ResizeRedraw, false);
            SetStyle(ControlStyles.Selectable, true);
            UpdateStyles();
            _timer = new System.Timers.Timer();
            _timer.Interval = FramePeriod;
            _timer.Elapsed += timer_Elapsed;
        }

        // Degrees 0..360, clockwise. Setting it does not raise PhaseChanged (the user's moves do).
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double Phase
        {
            get { return Interlocked.CompareExchange(ref _phase, 0, 0); }
            set
            {
                var phase = Wrap(value);
                if (Phase != phase)
                {
                    Interlocked.Exchange(ref _phase, phase);
                    _performIsNeeded = true;
                }
            }
        }

        // UI thread, at creation and on "ThemeIsDark".
        public void SetColors(Color backColor, Color foreColor)
        {
            lock (_framelock)
            {
                _backColor = backColor;
                _foreColor = foreColor;
            }
            _backgroundIsNeeded = true;
            _performIsNeeded = true;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                Focus();
                _dragging = true;
                Capture = true;
                SetFromPoint(e.Location);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                SetFromPoint(e.Location);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                _dragging = false;
                Capture = false;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            var step = (ModifierKeys & Keys.Shift) != 0 ? ShiftWheelStep : WheelStep;
            SetByUser(Phase + Math.Sign(e.Delta) * step);
            // The wheel turns the phase: the parent window must not scroll on the same notch (VDK 05-ui.md 5.2 rule 6).
            var handled = e as HandledMouseEventArgs;
            if (handled != null)
            {
                handled.Handled = true;
            }
        }

        private void SetFromPoint(Point point)
        {
            var dx = point.X - ClientSize.Width / 2.0;
            var dy = point.Y - ClientSize.Height / 2.0;
            if (dx == 0 && dy == 0)
            {
                return;
            }
            // 0 deg at the top, clockwise.
            SetByUser(Math.Round(Math.Atan2(dx, -dy) * 180 / Math.PI));
        }

        private void SetByUser(double phase)
        {
            Phase = phase;
            var handler = PhaseChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_performIsNeeded)
            {
                return;
            }
            if (!Monitor.TryEnter(_framelock))
            {
                return;
            }
            try
            {
                if (_frame == null)
                {
                    return;
                }
                _performIsNeeded = false;
                if (_backgroundIsNeeded)
                {
                    _backgroundIsNeeded = false;
                    DrawBackground(_background.Graphics);
                }
                _frame.CopyFrom(_background, 0, 0);
                DrawPointer(_frame.Graphics);
                _frame.Graphics.Flush();
                _frame.Render(0, 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Phase wheel frame skipped: " + ex.Message);
            }
            finally
            {
                Monitor.Exit(_framelock);
            }
        }

        // The dial: circle, a tick every 10 deg (longer every 30 deg), the labels outside the circle.
        private void DrawBackground(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var backBrush = new SolidBrush(_backColor))
            {
                graphics.FillRectangle(backBrush, 0, 0, _background.Width, _background.Height);
            }
            float cx;
            float cy;
            float radius;
            Geometry(out cx, out cy, out radius);
            if (radius <= 4)
            {
                return;
            }
            using (var pen = new Pen(Color.FromArgb(160, _foreColor), 1))
            {
                graphics.DrawEllipse(pen, cx - radius, cy - radius, 2 * radius, 2 * radius);
                for (var angle = 0; angle < 360; angle += 10)
                {
                    var inner = angle % 30 == 0 ? radius * 0.86f : radius * 0.93f;
                    graphics.DrawLine(pen, Point(cx, cy, inner, angle), Point(cx, cy, radius, angle));
                }
            }
            using (var textBrush = new SolidBrush(_foreColor))
            {
                for (var angle = 0; angle < 360; angle += 90)
                {
                    var text = angle + "°";
                    var size = graphics.MeasureString(text, _font);
                    var anchor = Point(cx, cy, radius + 3 + size.Height / 2, angle);
                    graphics.DrawString(text, _font, textBrush, anchor.X - size.Width / 2, anchor.Y - size.Height / 2);
                }
            }
        }

        private void DrawPointer(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float cx;
            float cy;
            float radius;
            Geometry(out cx, out cy, out radius);
            if (radius <= 4)
            {
                return;
            }
            var phase = Phase;
            var tip = Point(cx, cy, radius * 0.8f, phase);
            using (var pen = new Pen(_foreColor, 3))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                graphics.DrawLine(pen, cx, cy, tip.X, tip.Y);
            }
            using (var brush = new SolidBrush(_foreColor))
            {
                graphics.FillEllipse(brush, cx - 5, cy - 5, 10, 10);
                graphics.FillEllipse(brush, tip.X - 6, tip.Y - 6, 12, 12);
            }
        }

        // The circle leaves room for the labels outside it on every side.
        private void Geometry(out float cx, out float cy, out float radius)
        {
            var width = _background.Width;
            var height = _background.Height;
            cx = width / 2f;
            cy = height / 2f;
            var labelBand = _font.Height + 6;
            radius = Math.Min(width, height) / 2f - labelBand;
        }

        private static PointF Point(float cx, float cy, float radius, double degrees)
        {
            var radians = degrees * Math.PI / 180;
            return new PointF(cx + (float)(radius * Math.Sin(radians)), cy - (float)(radius * Math.Cos(radians)));
        }

        private static double Wrap(double phase)
        {
            if (!double.IsFinite(phase))
            {
                return 0;
            }
            phase %= 360;
            if (phase < 0)
            {
                phase += 360;
            }
            return phase;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            CreateBitmaps();
            _timer.Enabled = true;
        }

        protected override void OnResize(EventArgs e)
        {
            CreateBitmaps();
            base.OnResize(e);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            lock (_framelock)
            {
                _font = Font;
            }
            _backgroundIsNeeded = true;
            _performIsNeeded = true;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            lock (_framelock)
            {
                DisposeBitmaps();
            }
            base.OnHandleDestroyed(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            _performIsNeeded = true;
        }

        private void CreateBitmaps()
        {
            lock (_framelock)
            {
                _font = Font;
                DisposeBitmaps();
                if (IsHandleCreated && ClientSize.Width > 0 && ClientSize.Height > 0)
                {
                    _frame = new NativeBitmap(this);
                    _background = new NativeBitmap(this);
                    _backgroundIsNeeded = true;
                    _performIsNeeded = true;
                }
            }
        }

        // Under _framelock.
        private void DisposeBitmaps()
        {
            if (_frame != null)
            {
                _frame.Dispose();
                _frame = null;
            }
            if (_background != null)
            {
                _background.Dispose();
                _background = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _timer != null)
            {
                _timer.Elapsed -= timer_Elapsed;
                _timer.Dispose();
                _timer = null;
            }
            lock (_framelock)
            {
                DisposeBitmaps();
            }
            base.Dispose(disposing);
        }
    }
}
