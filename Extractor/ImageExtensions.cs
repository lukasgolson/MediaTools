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
}
