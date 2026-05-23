using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace PixelLab.ColorProcessing
{
    public record PixelColorInfo(
        byte R, byte G, byte B,
        float H, float S, float V,
        float C, float M, float Y, float K,
        float Y_YCbCr, float Cb, float Cr,
        float Y_YUV, float U, float V_YUV,
        float L, float A_Lab, float B_Lab
    );

    public partial class ColorEngine
    {
        public PixelColorInfo GetPixelInfo(byte r, byte g, byte b)
        {
            // Create a 1x1 BGR pixel
            using var pixel = new Image<Bgr, byte>(1, 1);
            pixel.Data[0, 0, 0] = b;
            pixel.Data[0, 0, 1] = g;
            pixel.Data[0, 0, 2] = r;

            // HSV via Emgu
            using var hsv = pixel.Convert<Hsv, byte>();
            float hue = hsv.Data[0, 0, 0] * 2f;
            float sat = hsv.Data[0, 0, 1] / 2.55f;
            float val = hsv.Data[0, 0, 2] / 2.55f;

            // YCbCr via Emgu
            using var ycc = pixel.Convert<Ycc, byte>();
            float yYCbCr = ycc.Data[0, 0, 0];
            float cb = ycc.Data[0, 0, 1];
            float cr = ycc.Data[0, 0, 2];

            // YUV via CvInvoke
            using var yuvMat = new Mat();
            CvInvoke.CvtColor(pixel, yuvMat, ColorConversion.Bgr2Yuv);
            using var yuvImg = yuvMat.ToImage<Bgr, byte>();
            float yYUV = yuvImg.Data[0, 0, 0];
            float u = yuvImg.Data[0, 0, 1];
            float vYuv = yuvImg.Data[0, 0, 2];

            // LAB via Emgu
            using var lab = pixel.Convert<Lab, byte>();
            float lVal = lab.Data[0, 0, 0] * 100f / 255f;
            float aLab = lab.Data[0, 0, 1] - 128f;
            float bLab = lab.Data[0, 0, 2] - 128f;

            // CMYK via ColorMath (manual)
            ColorMath.RgbToCmyk(r, g, b, out float c, out float m, out float y, out float k);

            return new PixelColorInfo(r, g, b, hue, sat, val, c, m, y, k,
                yYCbCr, cb, cr, yYUV, u, vYuv, lVal, aLab, bLab);
        }
    }
}
