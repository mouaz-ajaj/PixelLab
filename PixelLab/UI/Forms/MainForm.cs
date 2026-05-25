using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using PixelLab.Core;
using PixelLab.UI.Controls;
using PixelLab.Utils;
using PixelLab.ColorProcessing;

namespace PixelLab.UI.Forms
{
    public class MainForm : Form
    {
        // ─── Core Logic ──────────────────────────────────────────────
        private readonly ImageWorkspace _workspace;
        private readonly ColorEngine _colorEngine;

        // ─── Controls ───────────────────────────────────────────────
        private Panel pnlSidebar = null!;
        private Panel pnlTopbar = null!;
        private Panel pnlCanvas = null!;
        private Panel pnlRightSidebar = null!;
        
        private PictureBox pictureBoxImage = null!;
        private DarkButton btnOpenImage = null!;
        private Label lblAppName = null!;
        private Label lblTagline = null!;
        private Label lblDropHint = null!;
        private Label lblFileName = null!;
        
        // Right Sidebar Controls
        private ComboBox cmbColorSpace = null!;
        private ColorSlider slider1 = null!;
        private ColorSlider slider2 = null!;
        private ColorSlider slider3 = null!;
        private ColorSlider slider4 = null!;
        private CheckBox chkEnableC1 = null!;
        private CheckBox chkEnableC2 = null!;
        private CheckBox chkEnableC3 = null!;
        private DarkButton btnReset = null!;
        private CheckBox chkQuantize = null!;
        private ColorSlider sliderQuantize = null!;
        private DarkButton btnSave = null!;
        private Label lblColorSpace = null!;
        private PixelInspectorPanel inspectorPanel = null!;

        private StatCard statFile = null!;
        private StatCard statDimensions = null!;
        private StatCard statFormat = null!;
        private StatCard statSize = null!;
        private StatCard statDepth = null!;
        private Panel pnlDivider = null!;

        // ─── Zoom and Pan State ──────────────────────────────────────
        private float _zoomFactor = 1.0f;
        private bool _isPanning = false;
        private Point _screenPanStart;
        private Point _mouseDownPos;
        private bool _hasDragged = false;

        // ─── Palette ─────────────────────────────────────────────────
        private readonly Color BG_DEEP = Color.FromArgb(10, 10, 14);
        private readonly Color BG_SURFACE = Color.FromArgb(18, 18, 24);
        private readonly Color BG_PANEL = Color.FromArgb(24, 24, 32);
        private readonly Color BG_CANVAS = Color.FromArgb(14, 14, 20);
        private readonly Color ACCENT = Color.FromArgb(99, 102, 241);
        private readonly Color ACCENT_DIM = Color.FromArgb(49, 51, 120);
        private readonly Color GOLD = Color.FromArgb(234, 179, 8);
        private readonly Color TEXT_PRI = Color.FromArgb(241, 241, 245);
        private readonly Color TEXT_SEC = Color.FromArgb(120, 120, 140);
        private readonly Color TEXT_MUTED = Color.FromArgb(60, 60, 80);
        private readonly Color BORDER = Color.FromArgb(40, 40, 55);

        public MainForm()
        {
            _workspace = new ImageWorkspace();
            _colorEngine = new ColorEngine();
            _workspace.ImageChanged += Workspace_ImageChanged;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = "PixelLab";
            Size = new Size(1600, 840);
            MinimumSize = new Size(950, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BG_DEEP;
            ForeColor = TEXT_PRI;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            FormBorderStyle = FormBorderStyle.Sizable;

            AllowDrop = true;
            DragEnter += MainForm_DragEnter;
            DragDrop += MainForm_DragDrop;

            BuildSidebar();
            BuildRightSidebar();
            BuildTopbar();
            BuildInspectorPanel();
            BuildCanvas();
        }

        private void BuildSidebar()
        {
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = BG_PANEL,
                Padding = new Padding(0)
            };
            pnlSidebar.Paint += PnlSidebar_Paint;

            lblAppName = new Label
            {
                Text = "PIXEL", Left = 24, Top = 32, Width = 80, Height = 32,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = TEXT_PRI, BackColor = Color.Transparent
            };

            var lblLab = new Label
            {
                Text = "LAB", Left = 98, Top = 32, Width = 60, Height = 32,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = ACCENT, BackColor = Color.Transparent
            };

            lblTagline = new Label
            {
                Text = "Image Studio", Left = 24, Top = 64, Width = 172, Height = 20,
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                ForeColor = TEXT_MUTED, BackColor = Color.Transparent
            };

            pnlDivider = new Panel
            {
                Left = 24, Top = 96, Width = 172, Height = 1, BackColor = BORDER
            };

            btnOpenImage = new DarkButton
            {
                Text = "  Open Image", Left = 24, Top = 120, Width = 172, Height = 44,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = TEXT_PRI, BackColor = ACCENT,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnOpenImage.FlatAppearance.BorderSize = 0;
            btnOpenImage.Click += BtnOpenImage_Click;

            var btnCubeViewer = new DarkButton
            {
                Text = "  3D Color Viewer", Left = 24, Top = 180, Width = 172, Height = 44,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = TEXT_PRI, BackColor = BG_SURFACE,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnCubeViewer.FlatAppearance.BorderSize = 0;
            btnCubeViewer.Click += (s, e) =>
            {
                var img = pictureBoxImage.Image as Bitmap ?? _workspace.CommittedImage;
                new ColorSpaceViewerForm(img).ShowDialog();
            };

            statFile = new StatCard("File", ACCENT_DIM) { Left = 24, Top = 260 };
            statDimensions = new StatCard("Dimensions", ACCENT_DIM) { Left = 24, Top = 328 };
            statFormat = new StatCard("Format", ACCENT_DIM) { Left = 24, Top = 396 };
            statSize = new StatCard("Size", GOLD) { Left = 24, Top = 464 };
            statDepth = new StatCard("Color Depth", ACCENT_DIM) { Left = 24, Top = 532 };

            pnlSidebar.Controls.AddRange(new Control[]
            {
                lblAppName, lblLab, lblTagline, pnlDivider, btnOpenImage, btnCubeViewer,
                statFile, statDimensions, statFormat, statSize, statDepth
            });

            Controls.Add(pnlSidebar);
        }

        private void BuildRightSidebar()
        {
            pnlRightSidebar = new Panel
            {
                Dock = DockStyle.Right,
                Width = 280,
                BackColor = BG_PANEL,
                Padding = new Padding(0)
            };
            pnlRightSidebar.Paint += PnlSidebar_Paint;

            lblColorSpace = new Label { Text = "Color Space", Left = 24, Top = 32, Width = 100, ForeColor = TEXT_SEC, Font = new Font("Segoe UI", 9f) };
            cmbColorSpace = new ComboBox { Left = 24, Top = 56, Width = 232, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = BG_SURFACE, ForeColor = TEXT_PRI, FlatStyle = FlatStyle.Flat };
            cmbColorSpace.Items.AddRange(new object[] { "RGB", "HSV", "CMYK", "YCbCr", "YUV", "LAB" });
            cmbColorSpace.SelectedIndex = 0;
            cmbColorSpace.SelectedIndexChanged += OnColorSpaceChanged;

            slider1 = new ColorSlider { Left = 24, Top = 100, Width = 232, LabelText = "Red", Minimum = -255, Maximum = 255, Value = 0, AccentColor = Color.FromArgb(239, 68, 68) };
            slider2 = new ColorSlider { Left = 24, Top = 160, Width = 232, LabelText = "Green", Minimum = -255, Maximum = 255, Value = 0, AccentColor = Color.FromArgb(34, 197, 94) };
            slider3 = new ColorSlider { Left = 24, Top = 220, Width = 232, LabelText = "Blue", Minimum = -255, Maximum = 255, Value = 0, AccentColor = Color.FromArgb(59, 130, 246) };
            slider4 = new ColorSlider { Left = 24, Top = 280, Width = 232, LabelText = "Black", Minimum = -1f, Maximum = 1f, Value = 0, AccentColor = Color.FromArgb(70, 70, 70), Visible = false };

            slider1.ValueChanged += OnSliderChanged;
            slider2.ValueChanged += OnSliderChanged;
            slider3.ValueChanged += OnSliderChanged;
            slider4.ValueChanged += OnSliderChanged;

            chkEnableC1 = new CheckBox { Text = "Enable R", Left = 24, Top = 340, ForeColor = TEXT_PRI, Checked = true, AutoSize = true };
            chkEnableC2 = new CheckBox { Text = "Enable G", Left = 105, Top = 340, ForeColor = TEXT_PRI, Checked = true, AutoSize = true };
            chkEnableC3 = new CheckBox { Text = "Enable B", Left = 185, Top = 340, ForeColor = TEXT_PRI, Checked = true, AutoSize = true };

            chkEnableC1.CheckedChanged += OnChannelToggleChanged;
            chkEnableC2.CheckedChanged += OnChannelToggleChanged;
            chkEnableC3.CheckedChanged += OnChannelToggleChanged;

            btnReset = new DarkButton { Text = "Reset Image", Left = 24, Top = 390, Width = 232, Height = 40, BackColor = BORDER, ForeColor = TEXT_PRI };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.Click += (s, e) => {
                _workspace.ResetImage();
                slider1.Value = 0; slider2.Value = 0; slider3.Value = 0; slider4.Value = 0;
                chkQuantize.Checked = false;
                sliderQuantize.Value = 8;
            };

            chkQuantize = new CheckBox { Text = "Limit Colors (Quantize)", Left = 24, Top = 450, ForeColor = TEXT_PRI, Checked = false, AutoSize = true };
            chkQuantize.CheckedChanged += (s, e) => {
                sliderQuantize.Enabled = chkQuantize.Checked;
                ApplyColorModifications();
            };

            sliderQuantize = new ColorSlider { Left = 24, Top = 480, Width = 232, LabelText = "Colors Count", Minimum = 2, Maximum = 64, Value = 8, AccentColor = Color.FromArgb(234, 179, 8), Enabled = false };
            sliderQuantize.ValueChanged += (s, e) => ApplyColorModifications();

            btnSave = new DarkButton { Text = "Save Image", Left = 24, Top = 550, Width = 232, Height = 40, BackColor = ACCENT, ForeColor = TEXT_PRI };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => {
                if (pictureBoxImage.Image is not Bitmap currentImg)
                {
                    MessageBox.Show("No image to save.", "PixelLab", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var dlg = new SaveFileDialog
                {
                    Title = "Save Modified Image",
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|Bitmap Image|*.bmp",
                    DefaultExt = "png",
                    FileName = "modified_image"
                };

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        System.Drawing.Imaging.ImageFormat format = System.Drawing.Imaging.ImageFormat.Png;
                        string ext = Path.GetExtension(dlg.FileName).ToLower();
                        if (ext == ".jpg" || ext == ".jpeg") format = System.Drawing.Imaging.ImageFormat.Jpeg;
                        else if (ext == ".bmp") format = System.Drawing.Imaging.ImageFormat.Bmp;

                        currentImg.Save(dlg.FileName, format);
                        MessageBox.Show("Image saved successfully.", "PixelLab", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error saving image:\n" + ex.Message, "PixelLab", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            pnlRightSidebar.Controls.AddRange(new Control[] {
                lblColorSpace, cmbColorSpace, slider1, slider2, slider3, slider4,
                chkEnableC1, chkEnableC2, chkEnableC3, btnReset,
                chkQuantize, sliderQuantize, btnSave
            });

            Controls.Add(pnlRightSidebar);
        }

        private void PnlSidebar_Paint(object sender, PaintEventArgs e)
        {
            if (sender is Panel pnl)
            {
                using var pen = new Pen(BORDER, 1);
                if (pnl == pnlSidebar)
                {
                    e.Graphics.DrawLine(pen, pnl.Width - 1, 0, pnl.Width - 1, pnl.Height);
                }
                else
                {
                    e.Graphics.DrawLine(pen, 0, 0, 0, pnl.Height);
                }
                var rect = new Rectangle(0, 0, pnl.Width, 120);
                using var brush = new LinearGradientBrush(rect, Color.FromArgb(30, ACCENT), Color.Transparent, LinearGradientMode.Vertical);
                e.Graphics.FillRectangle(brush, rect);
            }
        }

        private void BuildTopbar()
        {
            pnlTopbar = new Panel
            {
                Dock = DockStyle.Top, Height = 52, BackColor = BG_SURFACE, Padding = new Padding(0)
            };
            pnlTopbar.Paint += PnlTopbar_Paint;

            lblFileName = new Label
            {
                Text = "No image loaded", Left = 240, Top = 14, Width = 500, Height = 24,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = TEXT_SEC, BackColor = Color.Transparent
            };

            var lblVersion = new Label
            {
                Text = "v1.0", Left = 1000, Top = 16, Width = 40, Height = 20,
                Font = new Font("Segoe UI", 8f), ForeColor = TEXT_MUTED,
                BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            pnlTopbar.Controls.AddRange(new Control[] { lblFileName, lblVersion });
            Controls.Add(pnlTopbar);
        }

        private void PnlTopbar_Paint(object sender, PaintEventArgs e)
        {
            using var pen = new Pen(BORDER, 1);
            e.Graphics.DrawLine(pen, 220, pnlTopbar.Height - 1, pnlTopbar.Width, pnlTopbar.Height - 1);
            using var brush = new SolidBrush(ACCENT);
            e.Graphics.FillRectangle(brush, 220, 0, 3, pnlTopbar.Height);
        }

        private void BuildCanvas()
        {
            pnlCanvas = new Panel
            {
                Dock = DockStyle.Fill, BackColor = BG_CANVAS, Padding = new Padding(24),
                AutoScroll = true
            };
            pnlCanvas.Paint += PnlCanvas_Paint;
            pnlCanvas.Resize += (s, e) => UpdateCanvasLayout();

            pictureBoxImage = new PictureBox
            {
                Dock = DockStyle.None, SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.Transparent
            };
            pictureBoxImage.AllowDrop = true;
            pictureBoxImage.DragEnter += MainForm_DragEnter;
            pictureBoxImage.DragDrop += MainForm_DragDrop;
            pictureBoxImage.MouseClick += PictureBox_MouseClick;
            pictureBoxImage.MouseDown += PictureBox_MouseDown;
            pictureBoxImage.MouseMove += PictureBox_MouseMove;
            pictureBoxImage.MouseUp += PictureBox_MouseUp;
            pictureBoxImage.MouseWheel += PictureBoxImage_MouseWheel;
            pictureBoxImage.MouseEnter += (s, e) => pictureBoxImage.Focus();
            pictureBoxImage.Cursor = Cursors.Cross;

            lblDropHint = new Label
            {
                Text = "Drop an image here\nor click Open Image",
                TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 13f, FontStyle.Regular),
                ForeColor = TEXT_MUTED, BackColor = Color.Transparent
            };

            pnlCanvas.Controls.Add(pictureBoxImage);
            pnlCanvas.Controls.Add(lblDropHint);
            Controls.Add(pnlCanvas);
            
            pnlCanvas.BringToFront(); // Ensure Fill dock works between Left and Right
        }

        private void PnlCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (_workspace.OriginalImage != null) return;
            var g = e.Graphics;
            var rect = new Rectangle(24, 24, pnlCanvas.Width - 49, pnlCanvas.Height - 49);
            using var pen = new Pen(BORDER, 1.5f);
            pen.DashStyle = DashStyle.Dash;
            pen.DashPattern = new float[] { 8, 6 };
            g.DrawRoundedRectangle(pen, rect, 16);
        }

        private void BtnOpenImage_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Choose an image",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff|All Files|*.*"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                LoadImageAndInfo(dlg.FileName);
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                e.Effect = files.Length > 0 && ImageHelper.IsImageFile(files[0])
                    ? DragDropEffects.Copy : DragDropEffects.None;
            }
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;
            if (files.Length > 0) LoadImageAndInfo(files[0]);
        }

        private void LoadImageAndInfo(string path)
        {
            try
            {
                _workspace.LoadImage(path);

                var fi = new FileInfo(path);
                lblFileName.Text = fi.Name;
                lblFileName.ForeColor = TEXT_PRI;

                statFile.Value = fi.Name.Length > 16 ? fi.Name.Substring(0, 14) + "…" : fi.Name;
                statFormat.Value = fi.Extension.ToUpper().Replace(".", "") + " Image";
                statSize.Value = ImageHelper.GetSizeString(fi.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading image:\n" + ex.Message, "PixelLab", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Workspace_ImageChanged(object? sender, EventArgs e)
        {
            var img = _workspace.CurrentImage;
            if (img != null)
            {
                if (pictureBoxImage.Image != null && pictureBoxImage.Image != _workspace.OriginalImage) 
                {
                    pictureBoxImage.Image.Dispose();
                }
                
                pictureBoxImage.Image = new Bitmap(img);
                pictureBoxImage.BringToFront();
                lblDropHint.Visible = false;

                statDimensions.Value = $"{img.Width} × {img.Height}";
                statDepth.Value = img.PixelFormat.ToString().Replace("Format", "");
                
                _zoomFactor = 1.0f; // Reset zoom on new image
                UpdateCanvasLayout();

                ApplyColorModifications(); // Reapply current slider states to the newly loaded image
            }
            pnlCanvas.Invalidate();
        }

        // ─── Interaction Logic ────────────────────────────────────

        private void OnColorSpaceChanged(object? sender, EventArgs e)
        {
            // Commit current edits before switching so they persist
            if (pictureBoxImage.Image is Bitmap currentBmp && _workspace.CommittedImage != null)
            {
                _workspace.CommitEdits(currentBmp);
            }

            slider1.ValueChanged -= OnSliderChanged;
            slider2.ValueChanged -= OnSliderChanged;
            slider3.ValueChanged -= OnSliderChanged;
            slider4.ValueChanged -= OnSliderChanged;

            slider1.Value = 0; slider2.Value = 0; slider3.Value = 0; slider4.Value = 0;
            slider4.Visible = false;

            if (cmbColorSpace.SelectedItem?.ToString() == "RGB")
            {
                slider1.LabelText = "Red Shift"; slider1.Minimum = -255; slider1.Maximum = 255; slider1.AccentColor = Color.FromArgb(239, 68, 68);
                slider2.LabelText = "Green Shift"; slider2.Minimum = -255; slider2.Maximum = 255; slider2.AccentColor = Color.FromArgb(34, 197, 94);
                slider3.LabelText = "Blue Shift"; slider3.Minimum = -255; slider3.Maximum = 255; slider3.AccentColor = Color.FromArgb(59, 130, 246);
                chkEnableC1.Enabled = true; chkEnableC2.Enabled = true; chkEnableC3.Enabled = true;
            }
            else if (cmbColorSpace.SelectedItem?.ToString() == "HSV")
            {
                slider1.LabelText = "Hue Shift"; slider1.Minimum = -180; slider1.Maximum = 180; slider1.AccentColor = Color.FromArgb(234, 179, 8);
                slider2.LabelText = "Saturation"; slider2.Minimum = -1f; slider2.Maximum = 1f; slider2.AccentColor = Color.FromArgb(168, 85, 247);
                slider3.LabelText = "Lightness"; slider3.Minimum = -1f; slider3.Maximum = 1f; slider3.AccentColor = Color.FromArgb(220, 220, 220);
                chkEnableC1.Enabled = false; chkEnableC2.Enabled = false; chkEnableC3.Enabled = false;
            }
            else if (cmbColorSpace.SelectedItem?.ToString() == "CMYK")
            {
                slider1.LabelText = "Cyan Shift"; slider1.Minimum = -1f; slider1.Maximum = 1f; slider1.AccentColor = Color.FromArgb(6, 182, 212);
                slider2.LabelText = "Magenta Shift"; slider2.Minimum = -1f; slider2.Maximum = 1f; slider2.AccentColor = Color.FromArgb(217, 70, 239);
                slider3.LabelText = "Yellow Shift"; slider3.Minimum = -1f; slider3.Maximum = 1f; slider3.AccentColor = Color.FromArgb(234, 179, 8);
                slider4.LabelText = "Black Shift"; slider4.Minimum = -1f; slider4.Maximum = 1f; slider4.AccentColor = Color.FromArgb(100, 100, 100);
                slider4.Visible = true;
                chkEnableC1.Enabled = false; chkEnableC2.Enabled = false; chkEnableC3.Enabled = false;
            }
            else if (cmbColorSpace.SelectedItem?.ToString() == "YCbCr")
            {
                slider1.LabelText = "Luma (Y)"; slider1.Minimum = -255; slider1.Maximum = 255; slider1.AccentColor = Color.FromArgb(220, 220, 220);
                slider2.LabelText = "Chroma Blue (Cb)"; slider2.Minimum = -255; slider2.Maximum = 255; slider2.AccentColor = Color.FromArgb(59, 130, 246);
                slider3.LabelText = "Chroma Red (Cr)"; slider3.Minimum = -255; slider3.Maximum = 255; slider3.AccentColor = Color.FromArgb(239, 68, 68);
                chkEnableC1.Enabled = false; chkEnableC2.Enabled = false; chkEnableC3.Enabled = false;
            }
            else if (cmbColorSpace.SelectedItem?.ToString() == "YUV")
            {
                slider1.LabelText = "Luma (Y)"; slider1.Minimum = -255; slider1.Maximum = 255; slider1.AccentColor = Color.FromArgb(220, 220, 220);
                slider2.LabelText = "Chroma (U)"; slider2.Minimum = -128; slider2.Maximum = 127; slider2.AccentColor = Color.FromArgb(59, 130, 246);
                slider3.LabelText = "Chroma (V)"; slider3.Minimum = -128; slider3.Maximum = 127; slider3.AccentColor = Color.FromArgb(239, 68, 68);
                chkEnableC1.Enabled = false; chkEnableC2.Enabled = false; chkEnableC3.Enabled = false;
            }
            else if (cmbColorSpace.SelectedItem?.ToString() == "LAB")
            {
                slider1.LabelText = "Lightness (L)"; slider1.Minimum = -100; slider1.Maximum = 100; slider1.AccentColor = Color.FromArgb(220, 220, 220);
                slider2.LabelText = "Green-Red (a*)"; slider2.Minimum = -128; slider2.Maximum = 127; slider2.AccentColor = Color.FromArgb(239, 68, 68);
                slider3.LabelText = "Blue-Yellow (b*)"; slider3.Minimum = -128; slider3.Maximum = 127; slider3.AccentColor = Color.FromArgb(59, 130, 246);
                chkEnableC1.Enabled = false; chkEnableC2.Enabled = false; chkEnableC3.Enabled = false;
            }

            slider1.ValueChanged += OnSliderChanged;
            slider2.ValueChanged += OnSliderChanged;
            slider3.ValueChanged += OnSliderChanged;
            slider4.ValueChanged += OnSliderChanged;

            ApplyColorModifications();
        }

        private void OnSliderChanged(object? sender, EventArgs e)
        {
            ApplyColorModifications();
        }

        private void OnChannelToggleChanged(object? sender, EventArgs e)
        {
            ApplyColorModifications();
        }

        private void ApplyColorModifications()
        {
            if (_workspace.CommittedImage == null) return;

            Bitmap workingImage = new Bitmap(_workspace.CommittedImage);
            ColorSpace space = ColorSpace.RGB;
            string sel = cmbColorSpace.SelectedItem?.ToString() ?? "RGB";
            if (sel == "HSV") space = ColorSpace.HSV;
            else if (sel == "CMYK") space = ColorSpace.CMYK;
            else if (sel == "YCbCr") space = ColorSpace.YCbCr;
            else if (sel == "YUV") space = ColorSpace.YUV;
            else if (sel == "LAB") space = ColorSpace.LAB;

            if (slider1.Value != 0) workingImage = _colorEngine.ModifyComponent(workingImage, space, 0, slider1.Value);
            if (slider2.Value != 0) workingImage = _colorEngine.ModifyComponent(workingImage, space, 1, slider2.Value);
            if (slider3.Value != 0) workingImage = _colorEngine.ModifyComponent(workingImage, space, 2, slider3.Value);
            if (slider4.Visible && slider4.Value != 0) workingImage = _colorEngine.ModifyComponent(workingImage, space, 3, slider4.Value);

            if (space == ColorSpace.RGB && (!chkEnableC1.Checked || !chkEnableC2.Checked || !chkEnableC3.Checked))
            {
                workingImage = _colorEngine.IsolateChannels(workingImage, chkEnableC1.Checked, chkEnableC2.Checked, chkEnableC3.Checked);
            }

            if (chkQuantize.Checked)
            {
                workingImage = _colorEngine.QuantizeColors(workingImage, (int)sliderQuantize.Value);
            }

            if (pictureBoxImage.Image != null && pictureBoxImage.Image != _workspace.OriginalImage && pictureBoxImage.Image != _workspace.CurrentImage)
            {
                pictureBoxImage.Image.Dispose();
            }
            
            pictureBoxImage.Image = workingImage;
        }

        // ─── Inspector Panel ─────────────────────────────────────

        private void BuildInspectorPanel()
        {
            inspectorPanel = new PixelInspectorPanel();
            Controls.Add(inspectorPanel);
        }

        private void PictureBox_MouseClick(object? sender, MouseEventArgs e)
        {
            if (_hasDragged) return;
            if (pictureBoxImage.Image is not Bitmap bmp) return;

            int imgX = Math.Clamp((int)((float)e.X / pictureBoxImage.Width * bmp.Width), 0, bmp.Width - 1);
            int imgY = Math.Clamp((int)((float)e.Y / pictureBoxImage.Height * bmp.Height), 0, bmp.Height - 1);

            Color px = bmp.GetPixel(imgX, imgY);
            var info = _colorEngine.GetPixelInfo(px.R, px.G, px.B);
            inspectorPanel.UpdatePixel(px, imgX, imgY, info);
        }

        private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _mouseDownPos = e.Location;
                _screenPanStart = Cursor.Position;
                _hasDragged = false;
                if (_zoomFactor > 1.0f)
                {
                    _isPanning = true;
                    pictureBoxImage.Cursor = Cursors.Hand;
                }
            }
        }

        private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point currentScreenPos = Cursor.Position;
                int dist = (currentScreenPos.X - _screenPanStart.X) * (currentScreenPos.X - _screenPanStart.X) + 
                           (currentScreenPos.Y - _screenPanStart.Y) * (currentScreenPos.Y - _screenPanStart.Y);
                if (dist > 16)
                {
                    _hasDragged = true;
                }

                if (_isPanning)
                {
                    int deltaX = currentScreenPos.X - _screenPanStart.X;
                    int deltaY = currentScreenPos.Y - _screenPanStart.Y;
                    
                    _screenPanStart = currentScreenPos;

                    pnlCanvas.AutoScrollPosition = new Point(
                        -pnlCanvas.AutoScrollPosition.X - deltaX,
                        -pnlCanvas.AutoScrollPosition.Y - deltaY
                    );
                }
            }
        }

        private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                pictureBoxImage.Cursor = Cursors.Cross;
            }
        }

        private void PictureBoxImage_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (pictureBoxImage.Image == null) return;

            if (e is HandledMouseEventArgs hme)
            {
                hme.Handled = true;
            }

            float oldZoom = _zoomFactor;

            if (e.Delta > 0)
            {
                _zoomFactor *= 1.15f;
            }
            else
            {
                _zoomFactor /= 1.15f;
            }

            _zoomFactor = Math.Clamp(_zoomFactor, 0.2f, 10f);

            if (Math.Abs(_zoomFactor - oldZoom) > 0.001f)
            {
                UpdateCanvasLayout();
            }
        }

        private void UpdateCanvasLayout()
        {
            if (pictureBoxImage.Image == null) return;

            var img = pictureBoxImage.Image;
            int canvasW = pnlCanvas.ClientSize.Width - 48;
            int canvasH = pnlCanvas.ClientSize.Height - 48;
            if (canvasW <= 0 || canvasH <= 0) return;

            float imgAspect = (float)img.Width / img.Height;
            float canvasAspect = (float)canvasW / canvasH;

            int fitW, fitH;
            if (imgAspect > canvasAspect)
            {
                fitW = canvasW;
                fitH = (int)(fitW / imgAspect);
            }
            else
            {
                fitH = canvasH;
                fitW = (int)(fitH * imgAspect);
            }

            int newW = (int)(fitW * _zoomFactor);
            int newH = (int)(fitH * _zoomFactor);

            pictureBoxImage.Size = new Size(newW, newH);

            int posX = 24;
            int posY = 24;

            if (newW < pnlCanvas.ClientSize.Width)
            {
                posX = (pnlCanvas.ClientSize.Width - newW) / 2;
            }
            if (newH < pnlCanvas.ClientSize.Height)
            {
                posY = (pnlCanvas.ClientSize.Height - newH) / 2;
            }

            int finalX = newW < pnlCanvas.ClientSize.Width ? posX : 24;
            int finalY = newH < pnlCanvas.ClientSize.Height ? posY : 24;

            pictureBoxImage.Location = new Point(finalX + pnlCanvas.AutoScrollPosition.X, finalY + pnlCanvas.AutoScrollPosition.Y);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _workspace.Dispose();
            pictureBoxImage.Image?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
