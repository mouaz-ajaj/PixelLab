using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PixelLab.ColorProcessing;

namespace PixelLab.UI.Forms
{
    public class ColorCubeForm : Form
    {
        private CubeRenderer _renderer;
        private Point _lastMouse;
        private bool _isDragging;

        public ColorCubeForm()
        {
            _renderer = new CubeRenderer();

            Text = "3D RGB Color Space Interactive Viewer";
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(14, 14, 20); // BG_CANVAS
            
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            Paint += OnPaint;
            
            // Helpful Label
            var lblHint = new Label
            {
                Text = "Drag with your Mouse to rotate the RGB Color Cube",
                ForeColor = Color.FromArgb(120, 120, 140),
                Left = 20, Top = 20, Width = 400,
                Font = new Font("Segoe UI", 10f), BackColor = Color.Transparent
            };
            Controls.Add(lblHint);
        }

        private void OnPaint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            _renderer.Render(e.Graphics, ClientSize.Width, ClientSize.Height);
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _lastMouse = e.Location;
            }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                float dx = e.X - _lastMouse.X;
                float dy = e.Y - _lastMouse.Y;

                // Adjust sensitivity
                _renderer.Rotate(dx * 0.01f, dy * 0.01f);
                _lastMouse = e.Location;
                Invalidate();
            }
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            _isDragging = false;
        }
    }
}
