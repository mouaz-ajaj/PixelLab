using System;

namespace PixelLab.ColorProcessing
{
    public static class ColorMath
    {
        public static void RgbToCmyk(float r, float g, float b, out float c, out float m, out float y, out float k)
        {
            float cP = 1f - (r / 255f);
            float mP = 1f - (g / 255f);
            float yP = 1f - (b / 255f);

            k = Math.Min(cP, Math.Min(mP, yP));

            if (k == 1f) { c = 0; m = 0; y = 0; }
            else
            {
                c = (cP - k) / (1f - k);
                m = (mP - k) / (1f - k);
                y = (yP - k) / (1f - k);
            }
        }

        public static void CmykToRgb(float c, float m, float y, float k, out byte r, out byte g, out byte b)
        {
            r = (byte)Math.Clamp(255f * (1f - c) * (1f - k), 0, 255);
            g = (byte)Math.Clamp(255f * (1f - m) * (1f - k), 0, 255);
            b = (byte)Math.Clamp(255f * (1f - y) * (1f - k), 0, 255);
        }
    }
}
