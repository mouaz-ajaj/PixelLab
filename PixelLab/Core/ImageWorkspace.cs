using System;
using System.Drawing;

namespace PixelLab.Core
{
    public class ImageWorkspace : IDisposable
    {
        public Bitmap? OriginalImage { get; private set; }
        public Bitmap? CurrentImage { get; private set; }
        
        /// <summary>
        /// Holds the image with all committed (confirmed) edits.
        /// New slider adjustments are applied on top of this base.
        /// </summary>
        public Bitmap? CommittedImage { get; private set; }
        
        public event EventHandler? ImageChanged;

        public void LoadImage(string imagePath)
        {
            DisposeImages();
            using (var tmp = new Bitmap(imagePath))
            {
                OriginalImage = new Bitmap(tmp);
            }
            CurrentImage = new Bitmap(OriginalImage);
            CommittedImage = new Bitmap(OriginalImage);
            ImageChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ApplyTransformation(Bitmap newImage)
        {
            CurrentImage?.Dispose();
            CurrentImage = new Bitmap(newImage);
            ImageChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Commits the given image as the new working base.
        /// Called when switching color spaces so edits persist.
        /// </summary>
        public void CommitEdits(Bitmap editedImage)
        {
            CommittedImage?.Dispose();
            CommittedImage = new Bitmap(editedImage);
        }

        public void ResetImage()
        {
            if (OriginalImage != null)
            {
                CurrentImage?.Dispose();
                CurrentImage = new Bitmap(OriginalImage);
                CommittedImage?.Dispose();
                CommittedImage = new Bitmap(OriginalImage);
                ImageChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void DisposeImages()
        {
            OriginalImage?.Dispose();
            OriginalImage = null;
            CurrentImage?.Dispose();
            CurrentImage = null;
            CommittedImage?.Dispose();
            CommittedImage = null;
        }

        public void Dispose()
        {
            DisposeImages();
        }
    }
}
