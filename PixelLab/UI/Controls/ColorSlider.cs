using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PixelLab.Utils;

namespace PixelLab.UI.Controls
{
    public class ColorSlider : Control
    {
        private float _value = 0f;
        private float _min = -255f;
        private float _max = 255f;
        private bool _isDragging = false;

        public event EventHandler ValueChanged;

        public float Value
        {
            get => _value;
            set
            {
                _value = Math.Clamp(value, _min, _max);
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public float Minimum
        {
            get => _min;
            set { _min = value; Invalidate(); }
        }

        public float Maximum
        {
            get => _max;
            set { _max = value; Invalidate(); }
        }

        public string LabelText { get; set; } = "Channel";
        public Color AccentColor { get; set; } = Color.FromArgb(99, 102, 241);

        public ColorSlider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            Height = 44;
            Width = 200;
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw track
            using var trackBrush = new SolidBrush(Color.FromArgb(30, 30, 40));
            Rectangle trackRect = new Rectangle(0, 24, Width, 6);
            g.FillRoundedRectangle(trackBrush, trackRect, 3);

            // Draw filled percentage
            float percentage = (_value - _min) / (_max - _min);
            if (percentage < 0) percentage = 0;
            if (percentage > 1) percentage = 1;

            int fillWidth = (int)(percentage * Width);
            if (fillWidth > 0 && fillWidth <= Width)
            {
                using var fillBrush = new SolidBrush(AccentColor);
                Rectangle fillRect = new Rectangle(0, 24, fillWidth, 6);
                g.FillRoundedRectangle(fillBrush, fillRect, 3);
            }

            // Draw Thumb
            int thumbX = fillWidth - 6;
            if (thumbX < 0) thumbX = 0;
            using var thumbBrush = new SolidBrush(Color.White);
            g.FillEllipse(thumbBrush, new Rectangle(thumbX, 21, 12, 12));

            // Draw Texts
            using var font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            using var textBrush = new SolidBrush(Color.FromArgb(200, 200, 210));
            g.DrawString(LabelText, font, textBrush, new PointF(0, 0));

            // Draw Value
            string valStr = _value.ToString("0.##");
            var textSize = g.MeasureString(valStr, font);
            g.DrawString(valStr, font, textBrush, new PointF(Width - textSize.Width, 0));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Y > 15)
            {
                _isDragging = true;
                UpdateValueFromMouse(e.X);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isDragging)
            {
                UpdateValueFromMouse(e.X);
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _isDragging = false;
            base.OnMouseUp(e);
        }

        private void UpdateValueFromMouse(int x)
        {
            float percentage = (float)x / Width;
            percentage = Math.Clamp(percentage, 0f, 1f);
            Value = _min + (percentage * (_max - _min));
        }
    }
}
