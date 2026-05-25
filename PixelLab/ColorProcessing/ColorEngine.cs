using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace PixelLab.ColorProcessing
{
    public partial class ColorEngine 
    {
    
        public Bitmap ModifyComponent(Bitmap sourceImage, ColorSpace space, int channelIndex, float valueAdjustment)
        {
            if (space == ColorSpace.CMYK)
            {
                return ModifyComponentCMYK(sourceImage, channelIndex, valueAdjustment);
            }

            using Image<Bgr, byte> bgrByte = BitmapToImage(sourceImage);

            if (space == ColorSpace.RGB)
            {
                int emguChannel = channelIndex == 0 ? 2 : (channelIndex == 2 ? 0 : 1);
                Image<Gray, byte>[] channels = bgrByte.Split();
                
                unsafe
                {
                    byte* ptr = (byte*)channels[emguChannel].Mat.DataPointer;
                    int step = channels[emguChannel].Mat.Step;
                    int cols = channels[emguChannel].Cols;
                    int rows = channels[emguChannel].Rows;
                    for (int y = 0; y < rows; y++)
                    {
                        byte* row = ptr + y * step;
                        for (int x = 0; x < cols; x++)
                        {
                            int val = row[x] + (int)valueAdjustment;
                            row[x] = (byte)(val < 0 ? 0 : (val > 255 ? 255 : val));
                        }
                    }
                }

                using Image<Bgr, byte> result = new Image<Bgr, byte>(channels);
                foreach (var ch in channels) ch.Dispose();
                return ImageToBitmap(result);
            }

            using Image<Bgr, float> bgrFloat = bgrByte.Convert<Bgr, float>();

            if (space == ColorSpace.HSV || space == ColorSpace.LAB)
            {
                bgrFloat._Mul(1.0 / 255.0);
            }

            if (space == ColorSpace.HSV)
            {
                using Image<Hsv, float> hsv = bgrFloat.Convert<Hsv, float>();
                unsafe
                {
                    float* ptr = (float*)hsv.Mat.DataPointer;
                    int step = hsv.Mat.Step / sizeof(float);
                    int cols = hsv.Cols;
                    int rows = hsv.Rows;
                    for (int y = 0; y < rows; y++)
                    {
                        float* row = ptr + y * step;
                        for (int x = 0; x < cols; x++)
                        {
                            int idx = x * 3;
                            if (channelIndex == 0)
                            {
                                float h = row[idx] + valueAdjustment;
                                h %= 360f;
                                if (h < 0) h += 360f;
                                row[idx] = h;
                            }
                            else if (channelIndex == 1) row[idx + 1] = Math.Clamp(row[idx + 1] + valueAdjustment, 0f, 1f);
                            else if (channelIndex == 2) row[idx + 2] = Math.Clamp(row[idx + 2] + valueAdjustment, 0f, 1f);
                        }
                    }
                }

                using Image<Bgr, float> resBgrFloat = hsv.Convert<Bgr, float>();
                resBgrFloat._Mul(255.0);
                return ImageToBitmap(resBgrFloat.Convert<Bgr, byte>());
            }
            else if (space == ColorSpace.LAB)
            {
                using Image<Lab, float> lab = bgrFloat.Convert<Lab, float>();
                unsafe
                {
                    float* ptr = (float*)lab.Mat.DataPointer;
                    int step = lab.Mat.Step / sizeof(float);
                    int cols = lab.Cols;
                    int rows = lab.Rows;
                    for (int y = 0; y < rows; y++)
                    {
                        float* row = ptr + y * step;
                        for (int x = 0; x < cols; x++)
                        {
                            int idx = x * 3;
                            if (channelIndex == 0) row[idx] = Math.Clamp(row[idx] + valueAdjustment, 0f, 100f);
                            else if (channelIndex == 1) row[idx + 1] = Math.Clamp(row[idx + 1] + valueAdjustment, -128f, 127f);
                            else if (channelIndex == 2) row[idx + 2] = Math.Clamp(row[idx + 2] + valueAdjustment, -128f, 127f);
                        }
                    }
                }

                using Image<Bgr, float> resBgrFloat = lab.Convert<Bgr, float>();
                resBgrFloat._Mul(255.0);
                return ImageToBitmap(resBgrFloat.Convert<Bgr, byte>());
            }
            else if (space == ColorSpace.YCbCr)
            {
                using Image<Ycc, float> ycc = bgrFloat.Convert<Ycc, float>();
                unsafe
                {
                    float* ptr = (float*)ycc.Mat.DataPointer;
                    int step = ycc.Mat.Step / sizeof(float);
                    int cols = ycc.Cols;
                    int rows = ycc.Rows;
                    for (int y = 0; y < rows; y++)
                    {
                        float* row = ptr + y * step;
                        for (int x = 0; x < cols; x++)
                        {
                            int idx = x * 3;
                            if (channelIndex == 0) row[idx] = Math.Clamp(row[idx] + valueAdjustment, 0f, 255f);
                            else if (channelIndex == 1) row[idx + 1] = Math.Clamp(row[idx + 1] + valueAdjustment, 0f, 255f);
                            else if (channelIndex == 2) row[idx + 2] = Math.Clamp(row[idx + 2] + valueAdjustment, 0f, 255f);
                        }
                    }
                }

                return ImageToBitmap(ycc.Convert<Bgr, float>().Convert<Bgr, byte>());
            }
            else if (space == ColorSpace.YUV)
            {
                using Mat yuvMat = new Mat();
                CvInvoke.CvtColor(bgrFloat, yuvMat, ColorConversion.Bgr2Yuv);
                using Image<Bgr, float> yuvImg = yuvMat.ToImage<Bgr, float>(); 
                
                unsafe
                {
                    float* ptr = (float*)yuvImg.Mat.DataPointer;
                    int step = yuvImg.Mat.Step / sizeof(float);
                    int cols = yuvImg.Cols;
                    int rows = yuvImg.Rows;
                    for (int y = 0; y < rows; y++)
                    {
                        float* row = ptr + y * step;
                        for (int x = 0; x < cols; x++)
                        {
                            int idx = x * 3;
                            if (channelIndex == 0) row[idx] = Math.Clamp(row[idx] + valueAdjustment, 0f, 255f);
                            else if (channelIndex == 1) row[idx + 1] = Math.Clamp(row[idx + 1] + valueAdjustment, -128f, 128f);
                            else if (channelIndex == 2) row[idx + 2] = Math.Clamp(row[idx + 2] + valueAdjustment, -128f, 128f);
                        }
                    }
                }

                using Mat resMat = new Mat();
                CvInvoke.CvtColor(yuvImg, resMat, ColorConversion.Yuv2Bgr);
                return ImageToBitmap(resMat.ToImage<Bgr, float>().Convert<Bgr, byte>());
            }

            return new Bitmap(sourceImage);
        }

        private Bitmap ModifyComponentCMYK(Bitmap sourceImage, int channelIndex, float valueAdjustment)
        {
            Bitmap result = new Bitmap(sourceImage.Width, sourceImage.Height, sourceImage.PixelFormat);
            using (Graphics g = Graphics.FromImage(result)) g.DrawImageUnscaled(sourceImage, 0, 0);

            using (LockBitmap lockBitmap = new LockBitmap(result))
            {
                lockBitmap.LockBits();
                byte[] pixels = lockBitmap.Pixels;
                int depth = lockBitmap.Depth / 8;
                int stride = lockBitmap.Stride;

                for (int y = 0; y < lockBitmap.Height; y++)
                {
                    int offset = y * stride;
                    for (int x = 0; x < lockBitmap.Width; x++)
                    {
                        int i = offset + x * depth;
                        byte oldB = pixels[i];
                        byte oldG = pixels[i + 1];
                        byte oldR = pixels[i + 2];

                        ColorMath.RgbToCmyk(oldR, oldG, oldB, out float c, out float m, out float yCmyk, out float k);

                        if (channelIndex == 0) c = Math.Clamp(c + valueAdjustment, 0f, 1f);
                        else if (channelIndex == 1) m = Math.Clamp(m + valueAdjustment, 0f, 1f);
                        else if (channelIndex == 2) yCmyk = Math.Clamp(yCmyk + valueAdjustment, 0f, 1f);
                        else if (channelIndex == 3) k = Math.Clamp(k + valueAdjustment, 0f, 1f);

                        ColorMath.CmykToRgb(c, m, yCmyk, k, out byte r, out byte g, out byte b);

                        pixels[i] = b;
                        pixels[i + 1] = g;
                        pixels[i + 2] = r;
                    }
                }
                lockBitmap.UnlockBits();
            }

            return result;
        }

        private Image<Bgr, byte> BitmapToImage(Bitmap bmp)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            
            // Map the unmanaged pointer directly to EmguCV
            using Image<Bgr, byte> unmanagedImg = new Image<Bgr, byte>(bmp.Width, bmp.Height, data.Stride, data.Scan0);
            Image<Bgr, byte> clone = unmanagedImg.Clone(); // Clone to own the unmanaged memory
            
            bmp.UnlockBits(data);
            return clone;
        }

        private Bitmap ImageToBitmap(Image<Bgr, byte> img)
        {
            // Map EmguCV unmanaged memory to WinForms Bitmap
            using Bitmap unmanagedBmp = new Bitmap(img.Width, img.Height, img.Mat.Step, System.Drawing.Imaging.PixelFormat.Format24bppRgb, img.Mat.DataPointer);
            Bitmap clone = new Bitmap(unmanagedBmp); // Clone to detach from Emgu's memory lifecycle
            return clone;
        }

        public Bitmap IsolateChannels(Bitmap sourceImage, bool enableR, bool enableG, bool enableB)
        {
            using Image<Bgr, byte> img = BitmapToImage(sourceImage);
            Image<Gray, byte>[] channels = img.Split();
            
            // Emgu BGR order: 0=B, 1=G, 2=R
            if (!enableB) channels[0].SetValue(new Gray(0));
            if (!enableG) channels[1].SetValue(new Gray(0));
            if (!enableR) channels[2].SetValue(new Gray(0));

            using Image<Bgr, byte> result = new Image<Bgr, byte>(channels);
            foreach (var ch in channels) ch.Dispose();
            
            return ImageToBitmap(result);
        }
    }
}
