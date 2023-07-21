using System.Numerics;
namespace Extractor;

public static class ImageExtensions
{
    public static Image<Rgba32> CreateSquareImage(this Image image, Color backgroundColor)
    {

        var (width, height) = image.Size;
        var length = Math.Max(width, height);


        var newImage = new Image<Rgba32>(length, length);
        newImage.Mutate(x => x
            .BackgroundColor(backgroundColor)
            .DrawImage(image, new Point((length - width) / 2, (length - height) / 2), 1f));
        return newImage;
    }

    public static Image ProportionalResize(this Image image, int length)
    {

        image.Mutate(x =>
        {
            var (width, height) = image.Size;
            var aspectRatio = width / height;

            int newWidth;
            int newHeight;

            if (width > height)
            {
                newWidth = length;
                newHeight = length / aspectRatio;
            }
            else
            {
                newHeight = length;
                newWidth = length * aspectRatio;
            }

            x.Resize(newWidth, newHeight);
        });

        return image;
    }

    public static Image ClipTransparency(this Image src, float threshold)
    {
        src.Mutate(c => c.ProcessPixelRowsAsVector4(row =>
        {
            foreach (ref var pixel in row)
                pixel = pixel.W < threshold ? Vector4.Zero : pixel with
                {
                    W = 1
                };
        }));


        return src;
    }

    public static void AdjustBrightnessAndContrast(this Image<Rgba32> src, float brightnessThresholdHigh = 0.7f, float brightnessThresholdLow = 0.3f, float contrastThreshold = 0.1f)
    {
        float totalBrightness = 0;
        float totalVariance = 0;
        float totalPixels = src.Width * src.Height;


        // Calculate average brightness and total variance in a single loop
        for (var y = 0; y < src.Height; y++)
        for (var x = 0; x < src.Width; x++)
        {
            var pixel = src[x, y];
            var brightness = pixel.R / 255.0f * 0.3f + pixel.G / 255.0f * 0.59f + pixel.B / 255.0f * 0.11f; // Using NTSC weighting for brightness
            totalBrightness += brightness;
            totalVariance += brightness * brightness;
        }

        var avgBrightness = totalBrightness / totalPixels;
        // Calculate standard deviation
        var stdDev = (float)Math.Sqrt(totalVariance / totalPixels - avgBrightness * avgBrightness);

        // If brightness is high (for instance, above brightnessThresholdHigh) reduce it, otherwise if it's low (for instance, below brightnessThresholdLow) increase it
        var targetBrightness = avgBrightness > brightnessThresholdHigh ? -0.1f : avgBrightness < brightnessThresholdLow ? 0.1f : 0;
        // If contrast is low (for instance, below contrastThreshold) increase it
        var targetContrast = stdDev < contrastThreshold ? 0.2f : 0;

        // Adjust brightness and contrast
        src.Mutate(x => x
            .Brightness(targetBrightness)
            .Contrast(targetContrast));
    }
}
