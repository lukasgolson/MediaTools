using System.Diagnostics;
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

        // Run progress bar
        AnsiConsole.Progress()
            .Start(ctx =>
            {
                var loadTask = ctx.AddTask("[green]Loading image[/]");
                var validateDecoderTask = ctx.AddTask("[green]Validating decoder[/]");
                var pixelFormatTask = ctx.AddTask("[green]Validating pixel format[/]");
                var transparencyTask = ctx.AddTask("[green]Checking transparency[/]");
                var dimensionsTask = ctx.AddTask("[green]Checking max image dimensions[/]");


                var integrityTask = ctx.AddTask("[green]Validating image integrity[/]");
                var resizeTask = ctx.AddTask("[green]Resizing image[/]");


                try
                {
                    using var stream = new FileStream(arguments.InputFile, FileMode.Open, FileAccess.Read);
                    using var loadedImage = Image.FromStream(stream);

                    error = RunTestTask(loadTask, () => { loadTask.Increment(100); });


                    error = RunTestTask(validateDecoderTask, () =>
                    {
                        AnsiConsole.MarkupLine(
                            IsDecoderAvailable(loadedImage.RawFormat)
                                ? "[green]Decoder for the image format ({0}) is installed on this computer[/]"
                                : "[red]No decoder found for the image format ({0}) on this computer[/]",
                            loadedImage.RawFormat);

                        validateDecoderTask.Increment(100);
                    });


                    error = RunTestTask(pixelFormatTask, () =>
                    {
                        if (loadedImage.PixelFormat is PixelFormat.DontCare or PixelFormat.Undefined)
                        {
                            AnsiConsole.MarkupLine("[red]Invalid or unsupported pixel format[/]");
                            error = true;
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[green]Pixel format: {0}[/]", loadedImage.PixelFormat);
                        }
                    });

                    error = RunTestTask(transparencyTask, () =>
                    {
                        if (loadedImage is Bitmap bitmap && (bitmap.Flags & (int)ImageFlags.HasAlpha) != 0)
                        {
                            AnsiConsole.MarkupLine("[yellow]Image contains transparency (alpha channel detected)[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[green]Image does not contain transparency[/]");
                        }
                    });


                    dimensionsTask.IsIndeterminate = true;

                    int maxWidth;
                    int maxHeight;
                    (maxWidth, maxHeight) = CalculateMaxResolution(loadedImage.Width, loadedImage.Height, loadedImage.PixelFormat);
                    
                  
                    
                    AnsiConsole.MarkupLine("[green]Max image size: {0}x{1}. Tested image has a size of {2}x{3}[/]", maxWidth, maxHeight, loadedImage.Width, loadedImage.Height);

                    
                    if (loadedImage.Width > maxWidth || loadedImage.Height > maxHeight)
                    {
                        AnsiConsole.MarkupLine("[red]Image exceeds maximum dimensions[/]");
                        error = true;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[green]Image dimensions within limits[/]");
                    }
                    
                    dimensionsTask.Increment(100);
                    
                    
                    


                    error = RunTestTask(integrityTask, () =>
                    {
                        GC.Collect();

                        _ = new Bitmap(loadedImage);
                        AnsiConsole.MarkupLine("[green]Image integrity validated (full decode successful)[/]");
                    });


                    error = RunTestTask(resizeTask, () =>
                    {
                        GC.Collect();

                        using var scaledImage = GetScaledImage(loadedImage, 800, 600);
                        AnsiConsole.MarkupLine("[green]Image resized successfully[/]");
                    });
                }
                catch (Exception ex)
                {
                    HandleTaskException(ex, arguments);
                    error = true;
                }
            });

        AnsiConsole.MarkupLine(error ? "[red]Image is not GDI+ compatible[/]" : "[green]Image is GDI+ compatible[/]");

        return Task.CompletedTask;
    }

    private static int FindMaxImageSize(PixelFormat pixelFormat, int step = 1000, int high = int.MaxValue)
    {
        var low = 100;

        // Gradually increase resolution until failure
        while (low < high)
        {
            try
            {
                using var bitmap = new Bitmap(low, low, pixelFormat);
                low += step;
            }
            catch
            {
                high = low;  // Upper bound found
                low -= step; // Step back to the last successful size
                break;
            }
        }

        // Perform binary search within the identified bounds
        while (low < high - 1)
        {
            var mid = low + (high - low) / 2;
            try
            {
                using var bitmap = new Bitmap(mid, mid, pixelFormat);
                low = mid; // Success, move lower bound up
            }
            catch
            {
                high = mid; // Failure, move upper bound down
            }
        }
        
        GC.Collect();

        return (int)(low * 0.75); // Largest successful resolution, with a 25% margin
    }
    
    
    (int NewWidth, int NewHeight) CalculateMaxResolution(int originalWidth, int originalHeight, PixelFormat pixelFormat, int alignment = 4)
    {
        // Memory limit in bytes (1.5 GB, 500 MB per channel)
        long memoryLimit = 200L * 1024 * 1024 * 1024 / 100;

        // Determine effective bytes per pixel (GDI+ often stores as 32bpp)
        int bytesPerPixel = pixelFormat switch
        {
            PixelFormat.Format8bppIndexed => 1,
            PixelFormat.Format24bppRgb => 4, // Stored as 32bpp internally
            PixelFormat.Format32bppArgb => 4,
            _ => throw new NotSupportedException($"PixelFormat {pixelFormat} is not supported.")
        };

        // Calculate aspect ratio
        double aspectRatio = (double)originalWidth / originalHeight;

        // Estimate the initial height
        double estimatedHeight = Math.Sqrt(memoryLimit / (bytesPerPixel * aspectRatio));

        while (true)
        {
            // Calculate new dimensions
            int height = (int)Math.Floor(estimatedHeight);
            int width = (int)Math.Floor(aspectRatio * height);

            // Calculate stride (aligned to the specified alignment)
            int stride = (width * bytesPerPixel + alignment - 1) / alignment * alignment;

            // Calculate total memory usage
            long totalMemory = (long)stride * height;

            // If memory fits within the limit, return the dimensions
            if (totalMemory <= memoryLimit)
            {
                return (width, height);
            }

            // Recalculate height to fit within the memory limit
            estimatedHeight = Math.Sqrt((double)memoryLimit / (stride * bytesPerPixel));
        }
    }




    private bool RunTestTask(ProgressTask progressTask, Action action)
    {
        try
        {
            progressTask.StartTask();
            action();
            progressTask.Increment(100);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error in task '{0}': {1}[/]", progressTask.Description,
                ex.Message);
            return true;
        }

        return false;
    }

    private void HandleTaskException(Exception ex, GdiValidateCommand.Arguments arguments)
    {
        var exceptionType = ex.GetType();
        AnsiConsole.MarkupLine("[red]Error: {0}[/]", exceptionType.Name);

        if (arguments.stackTrace)
        {
            AnsiConsole.WriteException(ex);
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }
    }

    private static Image GetScaledImage(Image original, int maxWidth, int maxHeight)
    {
        double scale = Math.Min((double)maxWidth / original.Width, (double)maxHeight / original.Height);

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