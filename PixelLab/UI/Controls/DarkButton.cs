using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PixelLab.Utils;

namespace PixelLab.UI.Controls
{
    public class DarkButton : Button
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var brush = new SolidBrush(BackColor);
            g.FillRoundedRectangle(brush, rect, 8);

            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            using var font = new Font("Segoe UI", 9.5f);
            using var fgBrush = new SolidBrush(ForeColor);
            g.DrawString(Text, font, fgBrush, rect, sf);
        }
    }
}
