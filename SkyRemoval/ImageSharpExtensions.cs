using Emgu.CV;
using Emgu.CV.Structure;
namespace SkyRemoval;

public static class ImageConversion
{
    public static Image<Rgb24> ConvertToImageSharp(this Image<Bgr, byte> emguImage)
    {
        var width = emguImage.Width;
        var height = emguImage.Height;


        var imageSharp = new Image<Rgb24>(width, height);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var color = emguImage[y, x];
                imageSharp[x, y] = new Rgb24((byte)color.Red, (byte)color.Green, (byte)color.Blue);
            }
        }


        return imageSharp;
    }

    public static Image<Bgr, byte> ConvertToEmguCv(this Image<Rgb24> imageSharp)
    {
        var width = imageSharp.Width;
        var height = imageSharp.Height;

        var emguImage = new Image<Bgr, byte>(width, height);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var color = imageSharp[x, y];
                
                emguImage[y, x] = new Bgr(color.B, color.G, color.R);
            }
        }

        return emguImage;
    }
}
