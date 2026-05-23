using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PixelLab.ColorProcessing;

namespace PixelLab.UI.Controls
{
    public class PixelInspectorPanel : Panel
    {
        private readonly Color BG = Color.FromArgb(18, 18, 24);
        private readonly Color BORDER = Color.FromArgb(40, 40, 55);
        private readonly Color ACCENT = Color.FromArgb(99, 102, 241);
        private readonly Color TEXT_PRI = Color.FromArgb(241, 241, 245);
        private readonly Color TEXT_DIM = Color.FromArgb(80, 80, 100);

        private Panel pnlSwatch = null!;
        private Label lblHex = null!;
        private Label lblCoords = null!;
        private Label lblRgb = null!;
        private Label lblHsv = null!;
        private Label lblCmyk = null!;
        private Label lblYCbCr = null!;
        private Label lblYuv = null!;
        private Label lblLab = null!;
        private Label lblHint = null!;

        private Color _currentColor = Color.Empty;

        public PixelInspectorPanel()
        {
            Dock = DockStyle.Bottom;
            Height = 90;
            BackColor = BG;
            Padding = new Padding(16, 8, 16, 8);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BuildLayout();
        }

        private void BuildLayout()
        {
            var monoFont = new Font("Consolas", 9.5f);
            var smallFont = new Font("Consolas", 8.5f);

            // ─── Left: Swatch + HEX + Coords ──────
            pnlSwatch = new Panel { Left = 14, Top = 14, Width = 44, Height = 44, BackColor = Color.FromArgb(30, 30, 40) };
            pnlSwatch.Paint += (s, e) =>
            {
                if (_currentColor != Color.Empty)
                {
                    using var brush = new SolidBrush(_currentColor);
                    e.Graphics.FillRectangle(brush, 2, 2, 40, 40);
                }
                using var pen = new Pen(BORDER, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, 43, 43);
            };

            lblHex = new Label { Left = 64, Top = 16, Width = 70, Height = 18, Font = monoFont, ForeColor = ACCENT, BackColor = Color.Transparent };
            lblCoords = new Label { Left = 56, Top = 38, Width = 100, Height = 18, Font = smallFont, ForeColor = TEXT_DIM, BackColor = Color.Transparent };

            // ─── Column 1: RGB, CMYK, YUV ──────
            int col1 = 160;
            lblRgb  = new Label { Left = col1, Top = 12, Width = 220, Height = 20, Font = monoFont, ForeColor = TEXT_PRI, BackColor = Color.Transparent };
            lblCmyk = new Label { Left = col1, Top = 34, Width = 280, Height = 20, Font = monoFont, ForeColor = TEXT_PRI, BackColor = Color.Transparent };
            lblYuv  = new Label { Left = col1, Top = 56, Width = 220, Height = 20, Font = monoFont, ForeColor = TEXT_PRI, BackColor = Color.Transparent };

            // ─── Column 2: HSV, YCbCr, LAB ─────
            int col2 = 440;
            lblHsv   = new Label { Left = col2, Top = 12, Width = 260, Height = 20, Font = monoFont, ForeColor = TEXT_PRI, BackColor = Color.Transparent };
            lblYCbCr = new Label { Left = col2, Top = 34, Width = 260, Height = 20, Font = monoFont, ForeColor = TEXT_PRI, BackColor = Color.Transparent };
            lblLab   = new Label { Left = col2, Top = 56, Width = 260, Height = 20, Font = monoFont, ForeColor = TEXT_PRI, BackColor = Color.Transparent };

            // ─── Hint (initial state) ──────────
            lblHint = new Label
            {
                Text = "Click on any pixel in the image to inspect its color values",
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10f), ForeColor = TEXT_DIM, BackColor = Color.Transparent
            };

            Controls.AddRange(new Control[] { pnlSwatch, lblHex, lblCoords, lblRgb, lblHsv, lblCmyk, lblYCbCr, lblYuv, lblLab, lblHint });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(BORDER, 1);
            e.Graphics.DrawLine(pen, 0, 0, Width, 0);
            var rect = new Rectangle(0, 0, Width, 4);
            using var brush = new LinearGradientBrush(rect, Color.FromArgb(30, ACCENT), Color.Transparent, LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(brush, rect);
        }

        public void UpdatePixel(Color color, int px, int py, PixelColorInfo info)
        {
            _currentColor = color;
            lblHint.Visible = false;
            pnlSwatch.Visible = true;
            pnlSwatch.Invalidate();

            lblHex.Text    = $"#{info.R:X2}{info.G:X2}{info.B:X2}";
            lblCoords.Text = $"({px}, {py})";
            lblRgb.Text    = $"RGB:   ({info.R}, {info.G}, {info.B})";
            lblHsv.Text    = $"HSV:   ({info.H:F0}\u00b0, {info.S:F0}%, {info.V:F0}%)";
            lblCmyk.Text   = $"CMYK:  ({info.C:P0}, {info.M:P0}, {info.Y:P0}, {info.K:P0})";
            lblYCbCr.Text  = $"YCbCr: ({info.Y_YCbCr:F0}, {info.Cb:F0}, {info.Cr:F0})";
            lblYuv.Text    = $"YUV:   ({info.Y_YUV:F0}, {info.U:F0}, {info.V_YUV:F0})";
            lblLab.Text    = $"LAB:   ({info.L:F1}, {info.A_Lab:F1}, {info.B_Lab:F1})";
        }
    }
}
