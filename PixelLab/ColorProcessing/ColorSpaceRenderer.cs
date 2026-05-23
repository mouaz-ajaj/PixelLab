using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace PixelLab.ColorProcessing
{
    public enum ViewerMode { RGB, HSV, YUV, YCbCr, LAB, CMY }

    public class ColorSpaceRenderer
    {
        // ─── Rotation & Zoom ─────────────────────────────────
        private float _pitch = (float)(Math.PI / 6);
        private float _yaw = (float)(-Math.PI / 4);
        private float _zoom = 1.0f;
        private ViewerMode _mode = ViewerMode.RGB;
        private float _kFactor = 0f;

        // ─── Cached scatter data ─────────────────────────────
        private Color[] _sampledRgb = Array.Empty<Color>();
        private (float X, float Y, float Z, Color C)[] _scatter = Array.Empty<(float, float, float, Color)>();

        // ─── 12 edges of a cube (same topology reused) ──────
        private static readonly int[,] CubeEdges = {
            {0,1},{1,3},{3,2},{2,0},
            {4,5},{5,7},{7,6},{6,4},
            {0,4},{1,5},{2,6},{3,7}
        };

        // ─── 8 RGB cube corner values ────────────────────────
        private static readonly (int R, int G, int B)[] RgbCorners = {
            (0,0,0),(255,0,0),(0,255,0),(255,255,0),
            (0,0,255),(255,0,255),(0,255,255),(255,255,255)
        };

        // ═══════════════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════════════

        public void SetMode(ViewerMode mode)
        {
            _mode = mode;
            RecalculateScatter();
        }

        public void SetImage(Bitmap img)
        {
            SamplePixels(img);
            RecalculateScatter();
        }

        public void Rotate(float dYaw, float dPitch)
        {
            _yaw += dYaw;
            _pitch = Math.Clamp(_pitch + dPitch, (float)(-Math.PI / 2), (float)(Math.PI / 2));
        }

        public void Zoom(float delta)
        {
            _zoom = Math.Clamp(_zoom + delta, 0.3f, 5.0f);
        }

        public void SetK(float k)
        {
            _kFactor = Math.Clamp(k, 0f, 1f);
            if (_mode == ViewerMode.CMY) RecalculateScatter();
        }

        // ═══════════════════════════════════════════════════════
        //  RENDER
        // ═══════════════════════════════════════════════════════

        public void Render(Graphics g, int w, int h)
        {
            g.Clear(Color.FromArgb(14, 14, 20));

            switch (_mode)
            {
                case ViewerMode.RGB:   RenderRgbCube(g, w, h); break;
                case ViewerMode.HSV:   RenderHsvCylinder(g, w, h); break;
                case ViewerMode.YUV:   RenderMappedPolyhedron(g, w, h, "Y", "U", "V", MapCornersYUV); break;
                case ViewerMode.YCbCr: RenderMappedPolyhedron(g, w, h, "Y", "Cb", "Cr", MapCornersYCbCr); break;
                case ViewerMode.LAB:   RenderLabSolid(g, w, h); break;
                case ViewerMode.CMY:   RenderCmyCube(g, w, h); break;
            }

            RenderScatter(g, w, h);
        }

        // ═══════════════════════════════════════════════════════
        //  PROJECTION ENGINE
        // ═══════════════════════════════════════════════════════

        private PointF Project(float x, float y, float z, int w, int h)
        {
            float cosY = MathF.Cos(_yaw), sinY = MathF.Sin(_yaw);
            float x1 = x * cosY - z * sinY;
            float z1 = x * sinY + z * cosY;

            float cosP = MathF.Cos(_pitch), sinP = MathF.Sin(_pitch);
            float y2 = y * cosP - z1 * sinP;
            float x2 = x1;

            float scale = Math.Min(w, h) / 450f * _zoom;
            return new PointF(w / 2f + x2 * scale, h / 2f - y2 * scale);
        }

        // ═══════════════════════════════════════════════════════
        //  RGB CUBE
        // ═══════════════════════════════════════════════════════

        private void RenderRgbCube(Graphics g, int w, int h)
        {
            var proj = ProjectCorners(RgbCorners.Length, i =>
            {
                var c = RgbCorners[i];
                return (c.R - 127.5f, c.G - 127.5f, c.B - 127.5f);
            }, w, h);

            DrawEdges(g, proj);
            DrawCornerDots(g, proj, i => Color.FromArgb(RgbCorners[i].R, RgbCorners[i].G, RgbCorners[i].B));

            using var font = new Font("Segoe UI", 11f, FontStyle.Bold);
            g.DrawString("R", font, Brushes.Red, proj[1]);
            g.DrawString("G", font, Brushes.Lime, proj[2]);
            g.DrawString("B", font, Brushes.DeepSkyBlue, proj[4]);
            g.DrawString("W", font, Brushes.White, proj[7]);
        }

        // ═══════════════════════════════════════════════════════
        //  HSV CYLINDER
        // ═══════════════════════════════════════════════════════

        private void RenderHsvCylinder(Graphics g, int w, int h)
        {
            const int segments = 24;
            const float radius = 127.5f;
            float topY = 127.5f, botY = -127.5f;

            PointF[] topPts = new PointF[segments];
            PointF[] botPts = new PointF[segments];

            using var edgePen = new Pen(Color.FromArgb(80, 255, 255, 255), 1.2f);
            using var axisPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1f) { DashStyle = DashStyle.Dash };

            // Generate circles
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 2f * MathF.PI / segments;
                float x = radius * MathF.Cos(angle);
                float z = radius * MathF.Sin(angle);
                topPts[i] = Project(x, topY, z, w, h);
                botPts[i] = Project(x, botY, z, w, h);
            }

            // Draw circles with hue coloring
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                float hue = (float)i / segments * 360f;
                using var huePen = new Pen(ColorFromHue(hue), 1.5f);
                g.DrawLine(huePen, topPts[i], topPts[next]);
                g.DrawLine(edgePen, botPts[i], botPts[next]);
            }

            // Vertical lines (every 30°)
            for (int i = 0; i < segments; i += 2)
            {
                g.DrawLine(edgePen, topPts[i], botPts[i]);
            }

            // Center axis
            var axTop = Project(0, topY, 0, w, h);
            var axBot = Project(0, botY, 0, w, h);
            g.DrawLine(axisPen, axTop, axBot);

            using var font = new Font("Segoe UI", 11f, FontStyle.Bold);
            g.DrawString("V", font, Brushes.White, axTop);
            g.DrawString("H", font, new SolidBrush(Color.Gold), topPts[0]);
            g.DrawString("S", font, new SolidBrush(Color.MediumPurple),
                Project(radius * 0.6f, 0, 0, w, h));
        }

        // ═══════════════════════════════════════════════════════
        //  YUV / YCbCr POLYHEDRON  (mapped cube corners)
        // ═══════════════════════════════════════════════════════

        private delegate (float, float, float) CornerMapper(int r, int g, int b);

        private void RenderMappedPolyhedron(Graphics g, int w, int h,
            string labelY, string labelU, string labelV, CornerMapper mapper)
        {
            var proj = ProjectCorners(8, i =>
            {
                var c = RgbCorners[i];
                return mapper(c.R, c.G, c.B);
            }, w, h);

            DrawEdges(g, proj);
            DrawCornerDots(g, proj, i => Color.FromArgb(RgbCorners[i].R, RgbCorners[i].G, RgbCorners[i].B));

            // Find extremes for label placement
            using var font = new Font("Segoe UI", 11f, FontStyle.Bold);
            g.DrawString(labelY, font, Brushes.White, proj[7]);
            g.DrawString(labelU, font, Brushes.DeepSkyBlue, proj[4]);
            g.DrawString(labelV, font, Brushes.Red, proj[1]);

            // Draw axis cross through center
            using var axisPen = new Pen(Color.FromArgb(40, 255, 255, 255), 0.8f) { DashStyle = DashStyle.Dot };
            var center = Project(0, 0, 0, w, h);
            var xEnd = Project(140, 0, 0, w, h);
            var yEnd = Project(0, 140, 0, w, h);
            var zEnd = Project(0, 0, 140, w, h);
            g.DrawLine(axisPen, center, xEnd);
            g.DrawLine(axisPen, center, yEnd);
            g.DrawLine(axisPen, center, zEnd);
        }

        private static (float, float, float) MapCornersYUV(int r, int g, int b)
        {
            float y  =  0.299f * r + 0.587f * g + 0.114f * b;
            float u  = -0.169f * r - 0.331f * g + 0.500f * b + 128f;
            float v  =  0.500f * r - 0.419f * g - 0.081f * b + 128f;
            return (u - 127.5f, y - 127.5f, v - 127.5f);
        }

        private static (float, float, float) MapCornersYCbCr(int r, int g, int b)
        {
            float y  =  0.299f * r + 0.587f * g + 0.114f * b;
            float cb = -0.169f * r - 0.331f * g + 0.500f * b + 128f;
            float cr =  0.500f * r - 0.419f * g - 0.081f * b + 128f;
            return (cb - 127.5f, y - 127.5f, cr - 127.5f);
        }

        // ═══════════════════════════════════════════════════════
        //  LAB COLOR SOLID
        // ═══════════════════════════════════════════════════════

        private void RenderLabSolid(Graphics g, int w, int h)
        {
            // Draw axis cross
            using var axisPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1f) { DashStyle = DashStyle.Dash };
            var c = Project(0, 0, 0, w, h);
            g.DrawLine(axisPen, c, Project(140, 0, 0, w, h));
            g.DrawLine(axisPen, c, Project(-140, 0, 0, w, h));
            g.DrawLine(axisPen, c, Project(0, 140, 0, w, h));
            g.DrawLine(axisPen, c, Project(0, -140, 0, w, h));
            g.DrawLine(axisPen, c, Project(0, 0, 140, w, h));
            g.DrawLine(axisPen, c, Project(0, 0, -140, w, h));

            // Sample RGB cube surface → convert to LAB → draw boundary
            var surfacePoints = new List<(int R, int G, int B)>();
            const int step = 32;
            for (int r = 0; r <= 255; r += step)
                for (int gr = 0; gr <= 255; gr += step)
                    for (int b = 0; b <= 255; b += step)
                        if (r == 0 || r >= 255 - step || gr == 0 || gr >= 255 - step || b == 0 || b >= 255 - step)
                            surfacePoints.Add((r, gr, b));

            // Bulk convert via Emgu
            using var bgrImg = new Image<Bgr, byte>(surfacePoints.Count, 1);
            for (int i = 0; i < surfacePoints.Count; i++)
            {
                var sp = surfacePoints[i];
                bgrImg.Data[0, i, 0] = (byte)sp.B;
                bgrImg.Data[0, i, 1] = (byte)sp.G;
                bgrImg.Data[0, i, 2] = (byte)sp.R;
            }

            using var labImg = bgrImg.Convert<Lab, byte>();

            for (int i = 0; i < surfacePoints.Count; i++)
            {
                float l = labImg.Data[0, i, 0] - 127.5f; // vertical
                float a = labImg.Data[0, i, 1] - 127.5f; // x
                float b2 = labImg.Data[0, i, 2] - 127.5f; // z
                var sp = surfacePoints[i];
                var pt = Project(a, l, b2, w, h);
                using var brush = new SolidBrush(Color.FromArgb(120, sp.R, sp.G, sp.B));
                g.FillRectangle(brush, pt.X - 2, pt.Y - 2, 4, 4);
            }

            using var font = new Font("Segoe UI", 11f, FontStyle.Bold);
            g.DrawString("L*", font, Brushes.White, Project(0, 145, 0, w, h));
            g.DrawString("a*", font, Brushes.Red, Project(145, 0, 0, w, h));
            g.DrawString("b*", font, Brushes.DeepSkyBlue, Project(0, 0, 145, w, h));
        }

        // ═══════════════════════════════════════════════════════
        //  CMY CUBE
        // ═══════════════════════════════════════════════════════

        private void RenderCmyCube(Graphics g, int w, int h)
        {
            // CMY is inverse of RGB: C=255-R, M=255-G, Y=255-B
            var proj = ProjectCorners(8, i =>
            {
                var c = RgbCorners[i];
                float cm = 255 - c.R, m = 255 - c.G, y = 255 - c.B;
                return (cm - 127.5f, m - 127.5f, y - 127.5f);
            }, w, h);

            DrawEdges(g, proj);
            DrawCornerDots(g, proj, i =>
            {
                var c = RgbCorners[i];
                int r = (int)(c.R * (1 - _kFactor));
                int gr = (int)(c.G * (1 - _kFactor));
                int b = (int)(c.B * (1 - _kFactor));
                return Color.FromArgb(r, gr, b);
            });

            using var font = new Font("Segoe UI", 11f, FontStyle.Bold);
            // Corner 0 = RGB(0,0,0) → CMY(255,255,255) = "Black" in CMY
            // Corner 7 = RGB(255,255,255) → CMY(0,0,0) = "White" in CMY
            g.DrawString("C", font, Brushes.Cyan, proj[1]);       // high C
            g.DrawString("M", font, Brushes.Magenta, proj[2]);    // high M
            g.DrawString("Y", font, Brushes.Yellow, proj[4]);     // high Y
            g.DrawString("W", font, Brushes.White, proj[7]);      // White (origin in CMY)
        }

        // ═══════════════════════════════════════════════════════
        //  SCATTER RENDERING
        // ═══════════════════════════════════════════════════════

        private void RenderScatter(Graphics g, int w, int h)
        {
            if (_scatter.Length == 0) return;

            for (int i = 0; i < _scatter.Length; i++)
            {
                var s = _scatter[i];
                var pt = Project(s.X, s.Y, s.Z, w, h);
                using var brush = new SolidBrush(Color.FromArgb(180, s.C.R, s.C.G, s.C.B));
                g.FillRectangle(brush, pt.X - 1.5f, pt.Y - 1.5f, 3, 3);
            }
        }

        // ═══════════════════════════════════════════════════════
        //  PIXEL SAMPLING & SCATTER COMPUTATION
        // ═══════════════════════════════════════════════════════

        private void SamplePixels(Bitmap img)
        {
            int total = img.Width * img.Height;
            int step = Math.Max(1, (int)Math.Sqrt(total / 6000.0));
            var list = new List<Color>();

            using var lockBmp = new LockBitmap(img);
            lockBmp.LockBits();
            int depth = lockBmp.Depth / 8;
            int stride = lockBmp.Stride;
            byte[] px = lockBmp.Pixels;

            for (int y = 0; y < img.Height; y += step)
            {
                int rowOff = y * stride;
                for (int x = 0; x < img.Width; x += step)
                {
                    int i = rowOff + x * depth;
                    byte b = px[i], g = px[i + 1], r = px[i + 2];
                    list.Add(Color.FromArgb(r, g, b));
                }
            }
            lockBmp.UnlockBits();

            _sampledRgb = list.ToArray();
        }

        private void RecalculateScatter()
        {
            if (_sampledRgb.Length == 0) { _scatter = Array.Empty<(float, float, float, Color)>(); return; }

            int n = _sampledRgb.Length;

            switch (_mode)
            {
                case ViewerMode.RGB:
                    _scatter = new (float, float, float, Color)[n];
                    for (int i = 0; i < n; i++)
                    {
                        var c = _sampledRgb[i];
                        _scatter[i] = (c.R - 127.5f, c.G - 127.5f, c.B - 127.5f, c);
                    }
                    break;

                case ViewerMode.HSV:
                    RecalcScatterHSV(n);
                    break;

                case ViewerMode.YUV:
                    RecalcScatterWithMat(n, ColorConversion.Bgr2Yuv);
                    break;

                case ViewerMode.YCbCr:
                    RecalcScatterWithEmguType<Ycc>(n);
                    break;

                case ViewerMode.LAB:
                    RecalcScatterWithEmguType<Lab>(n);
                    break;

                case ViewerMode.CMY:
                    _scatter = new (float, float, float, Color)[n];
                    for (int i = 0; i < n; i++)
                    {
                        var c = _sampledRgb[i];
                        float cm = 255 - c.R, m = 255 - c.G, y = 255 - c.B;
                        int dr = (int)(c.R * (1 - _kFactor));
                        int dg = (int)(c.G * (1 - _kFactor));
                        int db = (int)(c.B * (1 - _kFactor));
                        _scatter[i] = (cm - 127.5f, m - 127.5f, y - 127.5f,
                            Color.FromArgb(dr, dg, db));
                    }
                    break;
            }
        }

        private void RecalcScatterHSV(int n)
        {
            using var bgrImg = new Image<Bgr, byte>(n, 1);
            for (int i = 0; i < n; i++)
            {
                var c = _sampledRgb[i];
                bgrImg.Data[0, i, 0] = c.B;
                bgrImg.Data[0, i, 1] = c.G;
                bgrImg.Data[0, i, 2] = c.R;
            }

            using var hsvImg = bgrImg.Convert<Hsv, byte>();
            _scatter = new (float, float, float, Color)[n];

            for (int i = 0; i < n; i++)
            {
                float hDeg = hsvImg.Data[0, i, 0] * 2f; // OpenCV byte: H is 0-180
                float s = hsvImg.Data[0, i, 1] / 255f;   // 0-1
                float v = hsvImg.Data[0, i, 2];           // 0-255

                float angle = hDeg * MathF.PI / 180f;
                float x = s * 127.5f * MathF.Cos(angle);
                float z = s * 127.5f * MathF.Sin(angle);
                float y = v - 127.5f;

                _scatter[i] = (x, y, z, _sampledRgb[i]);
            }
        }

        private void RecalcScatterWithEmguType<TColor>(int n) where TColor : struct, IColor
        {
            using var bgrImg = new Image<Bgr, byte>(n, 1);
            for (int i = 0; i < n; i++)
            {
                var c = _sampledRgb[i];
                bgrImg.Data[0, i, 0] = c.B;
                bgrImg.Data[0, i, 1] = c.G;
                bgrImg.Data[0, i, 2] = c.R;
            }

            using var converted = bgrImg.Convert<TColor, byte>();
            _scatter = new (float, float, float, Color)[n];

            for (int i = 0; i < n; i++)
            {
                float ch0 = converted.Data[0, i, 0] - 127.5f;
                float ch1 = converted.Data[0, i, 1] - 127.5f;
                float ch2 = converted.Data[0, i, 2] - 127.5f;
                _scatter[i] = (ch1, ch0, ch2, _sampledRgb[i]);
            }
        }

        private void RecalcScatterWithMat(int n, ColorConversion conversion)
        {
            using var bgrImg = new Image<Bgr, byte>(n, 1);
            for (int i = 0; i < n; i++)
            {
                var c = _sampledRgb[i];
                bgrImg.Data[0, i, 0] = c.B;
                bgrImg.Data[0, i, 1] = c.G;
                bgrImg.Data[0, i, 2] = c.R;
            }

            using var mat = new Mat();
            CvInvoke.CvtColor(bgrImg, mat, conversion);
            using var result = mat.ToImage<Bgr, byte>(); // 3-channel container

            _scatter = new (float, float, float, Color)[n];
            for (int i = 0; i < n; i++)
            {
                float ch0 = result.Data[0, i, 0] - 127.5f;
                float ch1 = result.Data[0, i, 1] - 127.5f;
                float ch2 = result.Data[0, i, 2] - 127.5f;
                _scatter[i] = (ch1, ch0, ch2, _sampledRgb[i]);
            }
        }

        // ═══════════════════════════════════════════════════════
        //  DRAWING HELPERS
        // ═══════════════════════════════════════════════════════

        private PointF[] ProjectCorners(int count, Func<int, (float, float, float)> posFunc, int w, int h)
        {
            var pts = new PointF[count];
            for (int i = 0; i < count; i++)
            {
                var (x, y, z) = posFunc(i);
                pts[i] = Project(x, y, z, w, h);
            }
            return pts;
        }

        private void DrawEdges(Graphics g, PointF[] proj)
        {
            using var pen = new Pen(Color.FromArgb(100, 255, 255, 255), 1.3f);
            for (int i = 0; i < 12; i++)
                g.DrawLine(pen, proj[CubeEdges[i, 0]], proj[CubeEdges[i, 1]]);
        }

        private void DrawCornerDots(Graphics g, PointF[] proj, Func<int, Color> colorFunc)
        {
            for (int i = 0; i < proj.Length; i++)
            {
                using var brush = new SolidBrush(colorFunc(i));
                g.FillEllipse(brush, proj[i].X - 5, proj[i].Y - 5, 10, 10);
            }
        }

        private static Color ColorFromHue(float hDeg)
        {
            float h = hDeg / 60f;
            int hi = (int)h % 6;
            float f = h - (int)h;
            byte v = 255;
            byte q = (byte)(255 * (1 - f));
            byte t = (byte)(255 * f);
            return hi switch
            {
                0 => Color.FromArgb(v, t, 0),
                1 => Color.FromArgb(q, v, 0),
                2 => Color.FromArgb(0, v, t),
                3 => Color.FromArgb(0, q, v),
                4 => Color.FromArgb(t, 0, v),
                _ => Color.FromArgb(v, 0, q),
            };
        }
    }
}
