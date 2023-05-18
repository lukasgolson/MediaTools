using System.Diagnostics;
using Extractor.Commands;
using Extractor.Extensions;
using FFMediaToolkit.Decoding;
using Spectre.Console;
using TreeBasedCli;
namespace Extractor.Handlers;

public class ExtractAllCommandHandler : ILeafCommandHandler<ExtractAllCommand.Arguments>
{
    public async Task HandleAsync(ExtractAllCommand.Arguments arguments, LeafCommand executedCommand)
    {
        if (!Path.Exists(arguments.InputFile))
        {
            AnsiConsole.WriteLine(Resources.Resources.File_Does_Not_Exist);
            return;
        }

        Directory.CreateDirectory(arguments.OutputFolder);

        var file = MediaFile.Open(arguments.InputFile);


        var frameCount = file.Video.Info.NumberOfFrames ?? 0;
        AnsiConsole.WriteLine(Resources.Resources.Begin_Extraction, frameCount);

        var path = new TextPath(Path.GetFullPath(arguments.OutputFolder));


        AnsiConsole.Write(path);


        var stopWatch = new Stopwatch();
        stopWatch.Start();

        var frameIndex = 1;
        ulong queued = 0;
        ulong completed = 0;

        foreach (var frame in GetFrames(file, arguments.DropRatio))
        {
            var imageName = $"{Path.GetFileNameWithoutExtension(arguments.InputFile)}_{frameIndex}.{arguments.OutputFormat}";
            var output = Path.Join(arguments.OutputFolder, imageName);

            queued++;

            async void SaveFrame()
            {
                await frame.SaveAsync(output);
            }

            _ = Task.Run(SaveFrame).ContinueWith(delegate
            {
                completed++;
            });

            var stopWatchElapsedMilliseconds = stopWatch.ElapsedMilliseconds;
            var avgFrameProcessingTime = stopWatchElapsedMilliseconds / frameIndex;
            var totalTime = frameCount * avgFrameProcessingTime;
            var timeRemaining = totalTime - stopWatchElapsedMilliseconds;


            AnsiConsole.Write("\r" + string.Format(Resources.Resources.ProcessingFrame, frameIndex, frameCount, stopWatchElapsedMilliseconds * 0.001, totalTime * 0.001, timeRemaining * 0.001));


            frameIndex++;
        }


        AnsiConsole.Write(Environment.NewLine);


        while (queued > completed)
            AnsiConsole.Write("\r" + string.Format(Resources.Resources.WaitingIOSave, queued - completed));

        stopWatch.Stop();

        AnsiConsole.Write(Environment.NewLine);
        AnsiConsole.WriteLine(Resources.Resources.TaskCompleteText, stopWatch.ElapsedMilliseconds * 0.001);


    }
    private IEnumerable<Image<Bgr24>> GetFrames(MediaFile mediaFile, float dropRatio)
    {
        var frameSkipCount = (int)Math.Round(mediaFile.Video.Info.AvgFrameRate * dropRatio, 0);

        if (frameSkipCount == 0)
            frameSkipCount = 1;

        var skipCounter = 0;

        while (mediaFile.Video.TryGetNextFrame(out var imageData))
        {

            skipCounter++;

            if (skipCounter <= frameSkipCount)
                continue;


            yield return imageData.ToBitmap();

            skipCounter = 0;
        }
    }
}
