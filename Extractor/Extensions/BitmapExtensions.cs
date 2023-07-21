using Emgu.CV;
using Emgu.CV.Structure;
using FFMediaToolkit.Graphics;
namespace Extractor.Extensions;

public static class BitmapExtensions
{
    public static Image ToBitmap(this ImageData imageData)
    {
        return Image.LoadPixelData<Bgr24>(imageData.Data, imageData.ImageSize.Width, imageData.ImageSize.Height);
    }

    public static Image<Bgr24> ToBitmap(this Image<Bgr, byte> imageData)
    {
        return Image.LoadPixelData<Bgr24>(imageData.Bytes, imageData.Width, imageData.Height);
    }

    public static Image<Bgr, byte> ToEmguBitmap(this ImageData imageData)
    {
        var emguBitmap = new Image<Bgr, byte>(imageData.ImageSize.Width, imageData.ImageSize.Height);
        emguBitmap.Bytes = imageData.Data.ToArray();
        return emguBitmap;
    }
}
