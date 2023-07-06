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

    public static IEnumerable<Image> GetFrames(this MediaFile mediaFile, float dropRatio)
    {
        switch (dropRatio)
        {
            case < 0 or > 1:
                throw new ArgumentException(Resources.Resources.DropRatioRequirements, nameof(dropRatio));
            case >= 1:
                yield break;
        }

        var keepFrameNumber = dropRatio > 0 ? 1 / (1 - dropRatio) : 1;

        uint frameCounter = 0;
        while (mediaFile.Video.TryGetNextFrame(out var imageData))
        {
            frameCounter++;

            if (dropRatio < 1 && frameCounter % (int)keepFrameNumber != 0)
                continue;

            yield return imageData.ToBitmap();
        }
    }
}
