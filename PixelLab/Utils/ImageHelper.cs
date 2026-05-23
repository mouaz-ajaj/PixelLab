using System;
using System.IO;

namespace PixelLab.Utils
{
    public static class ImageHelper
    {
        public static bool IsImageFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" ||
                   ext == ".bmp" || ext == ".gif" || ext == ".tif" ||
                   ext == ".tiff";
        }
        
        public static string GetSizeString(long bytes)
        {
            double kb = bytes / 1024.0;
            return kb >= 1024
                ? $"{kb / 1024.0:F2} MB"
                : $"{kb:F1} KB";
        }
    }
}
