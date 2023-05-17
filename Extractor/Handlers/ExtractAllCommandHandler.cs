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

        var frameSkipCount = (int)Math.Round(file.Video.Info.AvgFrameRate * arguments.DropRatio, 0);

        if (frameSkipCount == 0)
            frameSkipCount = 1;

        var frameCount = Math.Round(file.Video.Info.AvgFrameRate * file.Video.Info.Duration.TotalSeconds / frameSkipCount, 0);

        Console.WriteLine(Resources.Resources.Begin_Extraction, frameCount);

        var stopWatch = new Stopwatch();
        stopWatch.Start();

        var frameIndex = 1;
        var skipCounter = 0;

        ulong queued = 0;
        ulong completed = 0;

        while (file.Video.TryGetNextFrame(out var imageData))
        {
            skipCounter++;

            if (skipCounter <= frameSkipCount)
                continue;

            var imageName = $"{Path.GetFileNameWithoutExtension(arguments.InputFile)}_{frameIndex}.{arguments.OutputFormat}";
            var output = Path.Join(arguments.OutputFolder, imageName);
            var bitmap = imageData.ToBitmap();

            queued++;
            Task.Run(async () =>
                {
                    await bitmap.SaveAsync(output);
                    completed++;
                }
            );


            var stopWatchElapsedMilliseconds = stopWatch.ElapsedMilliseconds;
            var avgFrameProcessingTime = stopWatchElapsedMilliseconds / frameIndex;
            var remainingFrames = frameCount - frameIndex;
            var totalTime = remainingFrames * avgFrameProcessingTime;
            var timeRemaining = totalTime - stopWatchElapsedMilliseconds;

            Console.Write("\r" + string.Format(Resources.Resources.ProcessingFrame, frameIndex, frameCount, stopWatchElapsedMilliseconds * 0.001, totalTime * 0.001, Math.Max(0, timeRemaining * 0.001)));

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
