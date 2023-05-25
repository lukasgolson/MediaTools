using Extractor.Extensions;
using FFMediaToolkit.Decoding;
using Spectre.Console;
namespace Extractor.Handlers;

public static class MediaFileHelpers
{
    public static void PrintFileInfo(this MediaFile file)
    {

        var info = file.Video.Info;

        AnsiConsole.Write(new TextPath(file.Info.FilePath));

        AnsiConsole.WriteLine(Resources.Resources.InfoLine1, info.NumberOfFrames ?? 0, info.FrameSize, info.IsVariableFrameRate, info.AvgFrameRate);
        AnsiConsole.WriteLine(Resources.Resources.InfoLine2, info.Rotation, info.IsInterlaced, info.PixelFormat, info.Duration, info.CodecName);


        AnsiConsole.Write(Resources.Resources.MetadataHeader);
        foreach (var pair in info.Metadata)
        {
            AnsiConsole.Write(Resources.Resources.MetadataPair, pair.Key, pair.Value);
            AnsiConsole.Write(@" ");
        }
    }

    public static IEnumerable<Image<Bgr24>> GetFrames(this MediaFile mediaFile, float dropRatio)
    {
        var absoluteDropRatio = Math.Abs(dropRatio);
        float dropFrameNumber = 0;

        var dropFrames = false;
        if (absoluteDropRatio > 0)
        {
            dropFrameNumber = 1 / absoluteDropRatio;
            dropFrames = true;
        }


        uint frameCounter = 0;
        while (mediaFile.Video.TryGetNextFrame(out var imageData))
        {
            frameCounter++;

            if (dropFrames && frameCounter % dropFrameNumber != 0)
                continue;

            yield return imageData.ToBitmap();
        }
    }
}
