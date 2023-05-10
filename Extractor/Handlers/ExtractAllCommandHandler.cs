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

        var queued = 0;
        var completed = 0;

        while (file.Video.TryGetNextFrame(out var imageData))
        {
            skipCounter++;

            if (skipCounter <= arguments.FrameSkipCount)
                continue;

            var imageName = $"{Path.GetFileNameWithoutExtension(arguments.InputFile)}_{frameIndex}{arguments.OutputFormat}";
            var output = Path.Join(arguments.OutputFolder, imageName);

            var bitmap = imageData.ToBitmap();

            queued++;


            Task.Run(() =>
                {
                    bitmap.SaveAsync(output);
                    completed++;
                }
            );

            var adjustedFrameIndex = frameIndex != 0 ? frameIndex : 1;
            var elapsed = stopWatch.ElapsedMilliseconds;
            var avgTime = elapsed / adjustedFrameIndex;
            var remainingFrames = frameCount - adjustedFrameIndex;
            var totalTime = remainingFrames * avgTime;
            var timeRemaining = totalTime - elapsed;

            Console.Write("\r" + string.Format(Resources.Resources.ProcessingFrame, frameIndex + 1, frameCount, elapsed * 0.001, totalTime * 0.001, Math.Max(0, timeRemaining * 0.001)));

            frameIndex++;
            skipCounter = 0;
        }


        Console.Write(Environment.NewLine);


        while (queued > completed)
            Console.Write("\r" + string.Format(Resources.Resources.WaitingIOSave, queued - completed));

        stopWatch.Stop();

        Console.Write(Environment.NewLine);
        Console.WriteLine(Resources.Resources.TaskCompleteText, stopWatch.ElapsedMilliseconds * 0.001);


        return Task.CompletedTask;
    }
}
