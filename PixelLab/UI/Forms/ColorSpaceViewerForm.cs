using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PixelLab.ColorProcessing;

namespace PixelLab.UI.Forms
{
    public class ColorSpaceViewerForm : Form
    {
        private readonly ColorSpaceRenderer _renderer;
        private Point _lastMouse;
        private bool _isDragging;
        private ComboBox _cmbMode = null!;
        private TrackBar _trkK = null!;
        private Label _lblK = null!;
        private Panel _pnlKContainer = null!;

        // Dark palette
        private readonly Color BG_CANVAS = Color.FromArgb(14, 14, 20);
        private readonly Color BG_PANEL = Color.FromArgb(24, 24, 32);
        private readonly Color TEXT_PRI = Color.FromArgb(241, 241, 245);
        private readonly Color TEXT_SEC = Color.FromArgb(120, 120, 140);
        private readonly Color ACCENT = Color.FromArgb(99, 102, 241);
        private readonly Color BORDER = Color.FromArgb(40, 40, 55);

        public ColorSpaceViewerForm(Bitmap? sourceImage = null)
        {
            _renderer = new ColorSpaceRenderer();

            if (sourceImage != null)
            {
                _renderer.SetImage(sourceImage);
            }

            Text = "3D Color Space Viewer — PixelLab";
            Size = new Size(900, 700);
            MinimumSize = new Size(640, 480);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = BG_CANVAS;
            ForeColor = TEXT_PRI;
            Font = new Font("Segoe UI", 9.5f);

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw, true);

            BuildControls();

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseWheel += OnMouseWheel;
            Paint += OnPaint;
        }

        private void BuildControls()
        {
            // ─── Top bar ────────────────────────────────────
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = BG_PANEL,
                Padding = new Padding(16, 0, 16, 0)
            };

            var lblTitle = new Label
            {
                Text = "Color Space",
                Left = 16, Top = 14, Width = 100,
                ForeColor = TEXT_SEC,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.Transparent
            };

            _cmbMode = new ComboBox
            {
                Left = 120, Top = 12, Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(18, 18, 24),
                ForeColor = TEXT_PRI,
                FlatStyle = FlatStyle.Flat
            };
            _cmbMode.Items.AddRange(new object[] { "RGB Cube", "HSV Cylinder", "YUV Space", "YCbCr Space", "LAB Solid", "CMY Cube" });
            _cmbMode.SelectedIndex = 0;
            _cmbMode.SelectedIndexChanged += OnModeChanged;

            var lblHint = new Label
            {
                Text = "Drag to rotate  •  Scroll to zoom",
                Left = 300, Top = 16, Width = 300,
                ForeColor = TEXT_SEC,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            pnlTop.Controls.AddRange(new Control[] { lblTitle, _cmbMode, lblHint });

            // ─── K Slider (CMY mode only) ───────────────────
            _pnlKContainer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = BG_PANEL,
                Visible = false
            };

            var lblKTitle = new Label
            {
                Text = "K (Black)",
                Left = 16, Top = 12, Width = 70,
                ForeColor = TEXT_SEC,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.Transparent
            };

            _trkK = new TrackBar
            {
                Left = 90, Top = 6, Width = 300,
                Minimum = 0, Maximum = 100, Value = 0,
                TickFrequency = 10, SmallChange = 1, LargeChange = 10,
                BackColor = BG_PANEL
            };
            _trkK.ValueChanged += (s, e) =>
            {
                float k = _trkK.Value / 100f;
                _renderer.SetK(k);
                _lblK.Text = $"{_trkK.Value}%";
                Invalidate();
            };

            _lblK = new Label
            {
                Text = "0%",
                Left = 400, Top = 12, Width = 50,
                ForeColor = ACCENT,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            _pnlKContainer.Controls.AddRange(new Control[] { lblKTitle, _trkK, _lblK });

            Controls.Add(pnlTop);
            Controls.Add(_pnlKContainer);
        }

        private void OnModeChanged(object? sender, EventArgs e)
        {
            ViewerMode mode = _cmbMode.SelectedIndex switch
            {
                0 => ViewerMode.RGB,
                1 => ViewerMode.HSV,
                2 => ViewerMode.YUV,
                3 => ViewerMode.YCbCr,
                4 => ViewerMode.LAB,
                5 => ViewerMode.CMY,
                _ => ViewerMode.RGB
            };

            _renderer.SetMode(mode);
            _pnlKContainer.Visible = (mode == ViewerMode.CMY);
            Invalidate();
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
                Cursor = Cursors.SizeAll;
            }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                float dx = e.X - _lastMouse.X;
                float dy = e.Y - _lastMouse.Y;
                _renderer.Rotate(dx * 0.01f, dy * 0.01f);
                _lastMouse = e.Location;
                Invalidate();
            }
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            _isDragging = false;
            Cursor = Cursors.Default;
        }

        private void OnMouseWheel(object? sender, MouseEventArgs e)
        {
            float delta = e.Delta > 0 ? 0.1f : -0.1f;
            _renderer.Zoom(delta);
            Invalidate();
        }
    }
}
