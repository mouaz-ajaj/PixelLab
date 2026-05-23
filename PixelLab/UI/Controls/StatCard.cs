using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PixelLab.Utils;

namespace PixelLab.UI.Controls
{
    public class StatCard : Panel
    {
        private Color _accentColor;
        private Label _lblSub;
        private Label _val;
        private readonly Color BORDER = Color.FromArgb(40, 40, 55);

        public string Title 
        { 
            get => _lblSub.Text; 
            set => _lblSub.Text = value.ToUpper(); 
        }

        public string Value 
        { 
            get => _val.Text; 
            set => _val.Text = value; 
        }

        public StatCard(string title, Color accentColor)
        {
            _accentColor = accentColor;
            
            Width = 172;
            Height = 56;
            BackColor = Color.FromArgb(18, 18, 24);
            Padding = new Padding(12, 8, 12, 8);

            _lblSub = new Label
            {
                Text = title.ToUpper(),
                Left = 14,
                Top = 8,
                Width = 144,
                Height = 16,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 60, 80),
                BackColor = Color.Transparent
            };

            _val = new Label
            {
                Text = "—",
                Left = 14,
                Top = 26,
                Width = 144,
                Height = 22,
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                ForeColor = Color.FromArgb(241, 241, 245),
                BackColor = Color.Transparent
            };

            Controls.AddRange(new Control[] { _lblSub, _val });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(BORDER, 1);
            g.DrawRoundedRectangle(pen, new Rectangle(0, 0, Width - 1, Height - 1), 8);
            using var bar = new SolidBrush(_accentColor);
            g.FillRoundedRectangle(bar, new Rectangle(0, 0, 3, Height), 2);
        }
    }
}
