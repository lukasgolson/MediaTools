using System.Diagnostics;
using Extractor.Commands;
using FFMediaToolkit.Decoding;
using TreeBasedCli;
namespace Extractor.Handlers;

public class ExtractAllCommandHandler : ILeafCommandHandler<ExtractAllCommand.Arguments>
{
    public Task HandleAsync(ExtractAllCommand.Arguments arguments, LeafCommand executedCommand)
    {
        if (!Path.Exists(arguments.InputFile))
        {
            Console.WriteLine(Resources.Resources.File_Does_Not_Exist);
            return Task.CompletedTask;
        }

        Directory.CreateDirectory(arguments.OutputFolder);

        var file = MediaFile.Open(arguments.InputFile);
        var frameCount = file.Video.Info.NumberOfFrames.GetValueOrDefault(0) / arguments.FrameSkipCount;



        Console.WriteLine(Resources.Resources.Begin_Extraction, frameCount);

        var stopWatch = new Stopwatch();
        stopWatch.Start();

        var frameIndex = 0;
        var skipCounter = 0;
        while (file.Video.TryGetNextFrame(out var imageData))
        {
            skipCounter++;

            if (skipCounter <= arguments.FrameSkipCount)
                continue;

            var imageName = $"{Path.GetFileNameWithoutExtension(arguments.InputFile)}_{frameIndex}{arguments.OutputFormat}";
            var output = Path.Join(arguments.OutputFolder, imageName);

            imageData.ToBitmap().Save(output);

            var adjustedFrameIndex = frameIndex != 0 ? frameIndex : 1;
            var elapsed = stopWatch.ElapsedMilliseconds;
            var avgTime = elapsed / adjustedFrameIndex;
            var remainingFrames = frameCount - adjustedFrameIndex;
            var timeRemaining = remainingFrames * avgTime;


            var statusMessage = $"Frame {frameIndex + 1} of {frameCount}. {elapsed * 0.001:0.00} of {timeRemaining * 0.001:0} seconds";

            Console.Write("\r" + statusMessage);

            frameIndex++;
            skipCounter = 0;
        }


        stopWatch.Stop();


        return Task.CompletedTask;
    }
}
