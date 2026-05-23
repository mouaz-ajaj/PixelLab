using System.Drawing;

namespace PixelLab.ColorProcessing
{
    public interface IColorConverter
    {
        /// <summary>
        /// Converts the given bitmap to a specific color space representation if needed, 
        /// or extracts a specific channel representing it visually.
        /// </summary>
        Bitmap ConvertTo(Bitmap sourceImage, ColorSpace targetSpace);

        /// <summary>
        /// Modifies a specific component of the color space and reconstructs the RGB image.
        /// Example: For HSV, increasing saturation across the entire image.
        /// </summary>
        Bitmap ModifyComponent(Bitmap sourceImage, ColorSpace space, int channelIndex, float valueAdjustment);
    }
}
