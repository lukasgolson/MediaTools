using FFMediaToolkit.Graphics;
namespace Extractor;

public static class BitmapExtensions
{
    public static Image<Bgr24> ToBitmap(this ImageData imageData)
    {
        return Image.LoadPixelData<Bgr24>(imageData.Data, imageData.ImageSize.Width, imageData.ImageSize.Height);
    }
}
