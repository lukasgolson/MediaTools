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


        var actualFrameCount = file.Video.Info.NumberOfFrames;
        var finalFrameCount = actualFrameCount * (1 - arguments.DropRatio);
        AnsiConsole.WriteLine(Resources.Resources.Begin_Extraction, actualFrameCount ?? 0, arguments.DropRatio, finalFrameCount ?? 0);

        AnsiConsole.Write(new TextPath(Path.GetFullPath(arguments.OutputFolder)));

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
                completed++;
            }




            _ = Task.Run(SaveFrame);

            var stopWatchElapsedMilliseconds = stopWatch.ElapsedMilliseconds;
            var avgFrameProcessingTime = stopWatchElapsedMilliseconds / frameIndex;
            var totalTime = finalFrameCount * avgFrameProcessingTime;
            double timeRemaining = (totalTime ?? 0) - stopWatchElapsedMilliseconds;


            AnsiConsole.Write("\r" + string.Format(Resources.Resources.ProcessingFrame, frameIndex, finalFrameCount, stopWatchElapsedMilliseconds * 0.001, totalTime * 0.001, Math.Max(timeRemaining * 0.001, 0d)));


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
