using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace PixelLab.ColorProcessing
{
    public partial class ColorEngine
    {
        public Bitmap QuantizeColors(Bitmap sourceImage, int colorCount)
        {
            if (colorCount < 2) colorCount = 2;
            if (colorCount > 256) colorCount = 256;

            int width = sourceImage.Width;
            int height = sourceImage.Height;

            // Always work in 24bpp RGB to guarantee absolute consistency and safe memory indexing
            Bitmap orig24 = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Bitmap result = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(orig24))
            {
                g.DrawImageUnscaled(sourceImage, 0, 0);
            }

            using (LockBitmap lockOriginal = new LockBitmap(orig24))
            using (LockBitmap lockResult = new LockBitmap(result))
            {
                lockOriginal.LockBits();
                lockResult.LockBits();

                byte[] origPixels = lockOriginal.Pixels;
                byte[] resPixels = lockResult.Pixels;
                int depth = 3; // Guaranteed 24bpp
                int stride = lockOriginal.Stride;

                // 1. Uniformly sample ~5000 pixels to build a representative training set
                List<Color> samplePixels = new List<Color>();
                int totalPixels = width * height;
                int sampleStep = Math.Max(1, totalPixels / 5000);
                
                for (int i = 0; i < origPixels.Length; i += sampleStep * depth)
                {
                    if (i + 2 < origPixels.Length)
                    {
                        samplePixels.Add(Color.FromArgb(origPixels[i + 2], origPixels[i + 1], origPixels[i]));
                    }
                }

                if (samplePixels.Count == 0)
                {
                    lockOriginal.UnlockBits();
                    lockResult.UnlockBits();
                    orig24.Dispose();
                    return result;
                }

                // 2. K-Means clustering
                Random rand = new Random(42);
                List<float[]> centers = new List<float[]>();
                
                // Initialize starting centers with random samples
                for (int c = 0; c < colorCount; c++)
                {
                    Color p = samplePixels[rand.Next(samplePixels.Count)];
                    centers.Add(new float[] { p.R, p.G, p.B });
                }

                int maxIterations = 12;
                for (int iter = 0; iter < maxIterations; iter++)
                {
                    List<float[]>[] groups = new List<float[]>[colorCount];
                    for (int c = 0; c < colorCount; c++) groups[c] = new List<float[]>();

                    foreach (var p in samplePixels)
                    {
                        int bestCenter = 0;
                        float minDist = float.MaxValue;

                        for (int c = 0; c < colorCount; c++)
                        {
                            float dr = p.R - centers[c][0];
                            float dg = p.G - centers[c][1];
                            float db = p.B - centers[c][2];
                            float dist = dr * dr + dg * dg + db * db;

                            if (dist < minDist)
                            {
                                minDist = dist;
                                bestCenter = c;
                            }
                        }
                        groups[bestCenter].Add(new float[] { p.R, p.G, p.B });
                    }

                    bool changed = false;
                    for (int c = 0; c < colorCount; c++)
                    {
                        if (groups[c].Count > 0)
                        {
                            float sumR = 0, sumG = 0, sumB = 0;
                            foreach (var p in groups[c])
                            {
                                sumR += p[0];
                                sumG += p[1];
                                sumB += p[2];
                            }

                            float newR = sumR / groups[c].Count;
                            float newG = sumG / groups[c].Count;
                            float newB = sumB / groups[c].Count;

                            if (Math.Abs(newR - centers[c][0]) > 0.5f ||
                                Math.Abs(newG - centers[c][1]) > 0.5f ||
                                Math.Abs(newB - centers[c][2]) > 0.5f)
                            {
                                centers[c][0] = newR;
                                centers[c][1] = newG;
                                centers[c][2] = newB;
                                changed = true;
                            }
                        }
                    }

                    if (!changed) break;
                }

                // 3. Map centers to byte values
                byte[][] centersCache = centers.Select(c => new byte[] {
                    (byte)Math.Clamp(c[0], 0f, 255f), // R
                    (byte)Math.Clamp(c[1], 0f, 255f), // G
                    (byte)Math.Clamp(c[2], 0f, 255f)  // B
                }).ToArray();

                // 4. Parallel full resolution pixel mapping
                Parallel.For(0, height, y =>
                {
                    int offset = y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        int i = offset + x * depth;
                        byte b = origPixels[i];
                        byte g = origPixels[i + 1];
                        byte r = origPixels[i + 2];

                        int bestCenter = 0;
                        float minDist = float.MaxValue;

                        for (int c = 0; c < colorCount; c++)
                        {
                            float dr = r - centers[c][0];
                            float dg = g - centers[c][1];
                            float db = b - centers[c][2];
                            float dist = dr * dr + dg * dg + db * db;

                            if (dist < minDist)
                            {
                                minDist = dist;
                                bestCenter = c;
                            }
                        }

                        resPixels[i] = centersCache[bestCenter][2];     // B
                        resPixels[i + 1] = centersCache[bestCenter][1]; // G
                        resPixels[i + 2] = centersCache[bestCenter][0]; // R
                    }
                });

                lockOriginal.UnlockBits();
                lockResult.UnlockBits();
            }

            orig24.Dispose();
            return result;
        }
    }
}
