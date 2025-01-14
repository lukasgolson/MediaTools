using Extractor.Commands.GDI;
using TreeBasedCli;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using Spectre.Console;
using Image = System.Drawing.Image;


namespace Extractor.Handlers.GDI;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class GDIValidateCommandHandler : ILeafCommandHandler<GdiValidateCommand.Arguments>
{
    public Task HandleAsync(GdiValidateCommand.Arguments arguments, LeafCommand executedCommand)
    {
        var error = false;

        try
        {
            using var stream = new FileStream(arguments.InputFile, FileMode.Open, FileAccess.Read);
            // Load the image
            using var loadedImage = Image.FromStream(stream);
            AnsiConsole.MarkupLine("[green]Image loaded successfully[/]");
            
         
            
            if (IsDecoderAvailable(loadedImage.RawFormat))
            {
                AnsiConsole.MarkupLine($"[green]Decoder ({loadedImage.RawFormat}) for the image format is installed on this computer[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]No decoder ({loadedImage.RawFormat}) found for the image format on this computer[/]");
                error = true;
            }
            
            if (IsEncoderAvailable(loadedImage.RawFormat))
            {
                AnsiConsole.MarkupLine($"[green]Encoder ({loadedImage.RawFormat}) for the image format is installed on this computer[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]No encoder ({loadedImage.RawFormat}) found for the image format on this computer[/]");
                error = true;
            }
            
            foreach (var property in loadedImage.PropertyItems)
            {
                AnsiConsole.MarkupLine($"[green]Found metadata: {property.Id}[/]");
            }
            
            if (loadedImage.PixelFormat is PixelFormat.DontCare or PixelFormat.Undefined)
            {
                AnsiConsole.MarkupLine("[red]Invalid or unsupported pixel format[/]");
                error = true;
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]Pixel format: {loadedImage.PixelFormat}[/]");
            }

            // Scale the image as a test
            using var scaledImage = GetScaledImage(loadedImage, 800, 600); // Example dimensions
            AnsiConsole.MarkupLine("[green]Image resized successfully[/]");


            _ = loadedImage.GetThumbnailImage(100, 100, () => false, IntPtr.Zero);
            AnsiConsole.MarkupLine("[green]Image thumbnail retrieved succesfully[/]");

            if (loadedImage is Bitmap bitmap && (bitmap.Flags & (int)System.Drawing.Imaging.ImageFlags.HasAlpha) != 0)
            {
                AnsiConsole.MarkupLine("[yellow]Image contains transparency (alpha channel detected)[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[green]Image does not contain transparency[/]");
            }
        }

        catch (Exception ex)
        {
            var exceptionType = ex.GetType();

            AnsiConsole.MarkupLine($"[red]Error loading image: {exceptionType.Name}[/]");
            
            if (arguments.stackTrace)
            {
                AnsiConsole.WriteException(ex);
            }

            error = true;
        }

        AnsiConsole.MarkupLine(error ? "[red]Image is not GDI+ compatible[/]" : "[green]Image is GDI+ compatible[/]");

        return Task.CompletedTask;
    }


    private static Image GetScaledImage(Image original, int maxWidth, int maxHeight)
    {
        // Calculate scaling factor
        double scale = Math.Min((double)maxWidth / original.Width, (double)maxHeight / original.Height);

        // Handle cases where no scaling is necessary
        if (scale >= 1)
            return (Image)original.Clone();

        int scaledWidth = (int)(original.Width * scale);
        int scaledHeight = (int)(original.Height * scale);

        Bitmap scaledBitmap = new Bitmap(scaledWidth, scaledHeight);
        using (Graphics g = Graphics.FromImage(scaledBitmap))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(original, 0, 0, scaledWidth, scaledHeight);
        }

        return scaledBitmap;
    }
    
    
    public static bool IsDecoderAvailable(ImageFormat format)
    {
        var decoders = ImageCodecInfo.GetImageDecoders();
        return decoders.Any(decoder => decoder.FormatID == format.Guid);
    }
    
    public static bool IsEncoderAvailable(ImageFormat format)
    {
        var encoders = ImageCodecInfo.GetImageEncoders();
        return encoders.Any(encoder => encoder.FormatID == format.Guid);
    }
}