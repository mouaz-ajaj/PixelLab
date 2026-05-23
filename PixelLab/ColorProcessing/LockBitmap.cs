using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PixelLab.ColorProcessing
{
    /// <summary>
    /// Wrapper to manipulate a Bitmap's pixels directly in memory using LockBits.
    /// Accessing the raw byte array is significantly faster than using Bitmap.GetPixel
    /// or unsafe generic pointers for global passes.
    /// </summary>
    public class LockBitmap : IDisposable
    {
        private Bitmap _source;
        private IntPtr _iptr = IntPtr.Zero;
        private BitmapData? _bitmapData = null;

        public byte[] Pixels { get; set; } = Array.Empty<byte>();
        public int Depth { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Stride { get; private set; }

        public LockBitmap(Bitmap source)
        {
            _source = source;
        }

        public void LockBits()
        {
            Width = _source.Width;
            Height = _source.Height;
            Rectangle rect = new Rectangle(0, 0, Width, Height);

            Depth = Image.GetPixelFormatSize(_source.PixelFormat);
            if (Depth != 24 && Depth != 32)
            {
                throw new ArgumentException("Only 24 and 32 bpp images are supported for fast manipulation.");
            }

            _bitmapData = _source.LockBits(rect, ImageLockMode.ReadWrite, _source.PixelFormat);
            
            int step = Depth / 8;
            Stride = _bitmapData.Stride;
            
            // Allocate memory for the pixel array
            int byteCount = Math.Abs(Stride) * Height;
            Pixels = new byte[byteCount];
            _iptr = _bitmapData.Scan0;

            // Copy data from pointer to array
            Marshal.Copy(_iptr, Pixels, 0, Pixels.Length);
        }

        public void UnlockBits()
        {
            if (_bitmapData == null) return;
            
            // Copy data from array back to pointer
            Marshal.Copy(Pixels, 0, _iptr, Pixels.Length);
            _source.UnlockBits(_bitmapData);
            _bitmapData = null;
        }

        public void Dispose()
        {
            if (_bitmapData != null)
            {
                _source.UnlockBits(_bitmapData);
                _bitmapData = null;
            }
        }
    }
}
