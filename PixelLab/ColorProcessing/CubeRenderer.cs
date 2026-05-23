using System;
using System.Drawing;

namespace PixelLab.ColorProcessing
{
    public class CubeRenderer
    {
        private class Point3D
        {
            public float X, Y, Z;
            public Point3D(float x, float y, float z) { X = x; Y = y; Z = z; }
        }

        private float _pitch = (float)(Math.PI / 6); // roughly 30 deg
        private float _yaw = (float)(-Math.PI / 4);  // -45 deg

        public void Rotate(float deltaYaw, float deltaPitch)
        {
            _yaw += deltaYaw;
            _pitch += deltaPitch;

            // clamp pitch to avoid flipping
            if (_pitch > Math.PI / 2) _pitch = (float)(Math.PI / 2);
            if (_pitch < -Math.PI / 2) _pitch = (float)(-Math.PI / 2);
        }

        private PointF Project(Point3D p, int width, int height)
        {
            // Center the 0-255 RGB values to pivot exactly at center
            float x0 = p.X - 127.5f;
            float y0 = p.Y - 127.5f;
            float z0 = p.Z - 127.5f;

            // Apply Yaw (rotate around Y axis)
            float x1 = x0 * (float)Math.Cos(_yaw) - z0 * (float)Math.Sin(_yaw);
            float z1 = x0 * (float)Math.Sin(_yaw) + z0 * (float)Math.Cos(_yaw);
            float y1 = y0;

            // Apply Pitch (rotate around X axis)
            float y2 = y1 * (float)Math.Cos(_pitch) - z1 * (float)Math.Sin(_pitch);
            float z2 = y1 * (float)Math.Sin(_pitch) + z1 * (float)Math.Cos(_pitch);
            float x2 = x1;

            // Scale to fit visually
            float scale = Math.Min(width, height) / 450f;
            return new PointF((width / 2f) + x2 * scale, (height / 2f) - y2 * scale); // Invert Y for screen drawing
        }

        public void Render(Graphics g, int width, int height)
        {
            g.Clear(Color.FromArgb(14, 14, 20)); // Matches BG_CANVAS

            // Define the 8 corners of the RGB Cube
            Point3D[] corners = new Point3D[]
            {
                new Point3D(0, 0, 0),        // 0: Black
                new Point3D(255, 0, 0),      // 1: Red
                new Point3D(0, 255, 0),      // 2: Green
                new Point3D(255, 255, 0),    // 3: Yellow
                new Point3D(0, 0, 255),      // 4: Blue
                new Point3D(255, 0, 255),    // 5: Magenta
                new Point3D(0, 255, 255),    // 6: Cyan
                new Point3D(255, 255, 255)   // 7: White
            };

            PointF[] proj = new PointF[8];
            for (int i = 0; i < 8; i++) proj[i] = Project(corners[i], width, height);

            // Cube edges connections
            int[,] edges = new int[,]
            {
                {0, 1}, {1, 3}, {3, 2}, {2, 0}, // Z=0 plane
                {4, 5}, {5, 7}, {7, 6}, {6, 4}, // Z=255 plane
                {0, 4}, {1, 5}, {2, 6}, {3, 7}  // Cross edges connecting planes
            };

            using var edgePen = new Pen(Color.FromArgb(100, 255, 255, 255), 1.5f);
            
            // Draw edges
            for (int i = 0; i < 12; i++)
            {
                g.DrawLine(edgePen, proj[edges[i, 0]], proj[edges[i, 1]]);
            }

            // Draw colorful outer surface points of the RGB cube
            int step = 32;
            for (int r = 0; r <= 255; r += step)
            {
                for (int gr = 0; gr <= 255; gr += step)
                {
                    for (int b = 0; b <= 255; b += step)
                    {
                        // Draw only elements on the outer shell for transparency and 3D perception
                        if (r == 0 || r >= 255-step || gr == 0 || gr >= 255-step || b == 0 || b >= 255-step)
                        {
                            var pt = Project(new Point3D(r, gr, b), width, height);
                            using var brush = new SolidBrush(Color.FromArgb(r, gr, b));
                            g.FillEllipse(brush, pt.X - 3, pt.Y - 3, 6, 6);
                        }
                    }
                }
            }

            // Draw axis labels
            using var font = new Font("Segoe UI", 12f, FontStyle.Bold);
            g.DrawString("R", font, Brushes.Red, proj[1]);
            g.DrawString("G", font, Brushes.Lime, proj[2]);
            g.DrawString("B", font, Brushes.DeepSkyBlue, proj[4]);
            g.DrawString("W", font, Brushes.White, proj[7]);
        }
    }
}
