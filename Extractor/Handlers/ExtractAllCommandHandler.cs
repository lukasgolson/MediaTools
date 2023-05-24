using System.Diagnostics;
using Extractor.Commands;
using Extractor.Extensions;
using FFMediaToolkit.Decoding;
using Spectre.Console;
using TreeBasedCli;
using TreeBasedCli.Exceptions;
namespace Extractor.Handlers;

public class ExtractAllCommandHandler : ILeafCommandHandler<ExtractAllCommand.Arguments>
{
    public async Task HandleAsync(ExtractAllCommand.Arguments arguments, LeafCommand executedCommand)
    {

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        await AnsiConsole.Progress()
            .AutoClear(false) // Do not remove the task list when done
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new RemainingTimeColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var checkArgumentsTask = ctx.AddTask("[green]Validating Command Arguments[/]", true, 2);
                var framesProgressTask = ctx.AddTask("[green]Opening Media File[/]");

                CheckArguments(arguments, checkArgumentsTask);

                await ExtractFrames(arguments, framesProgressTask);
            });

        stopwatch.Stop();

        AnsiConsole.MarkupLineInterpolated($"[green]Finished extracting frames in {Math.Round(stopwatch.ElapsedMilliseconds * 0.001, 2)} seconds.[/]");
    }

    private static void CheckArguments(ExtractAllCommand.Arguments arguments, ProgressTask progress)
    {
        if (!File.Exists(arguments.InputFile))
            throw new MessageOnlyException($"[red]Input file {arguments.InputFile} does not exist...");

        progress.Increment(1);

        Directory.CreateDirectory(arguments.OutputFolder);
        progress.Increment(1);
    }

    private Task ExtractFrames(ExtractAllCommand.Arguments arguments, ProgressTask progress)
    {
        var file = MediaFile.Open(arguments.InputFile);

        // Determine video info required to calculate frame count for progress.
        var actualFrameCount = file.Video.Info.NumberOfFrames ?? 0;
        var finalFrameCount = MathF.Round(actualFrameCount * (1 - arguments.DropRatio));
        progress.MaxValue(finalFrameCount);

        progress.Description("[green]Extracting Frames[/]");

        ulong frameIndex = 0;
        ulong completed = 0;
        foreach (var frame in GetFrames(file, arguments.DropRatio))
        {
            var imageName = $"{Path.GetFileNameWithoutExtension(arguments.InputFile)}_{frameIndex + 1}.{arguments.OutputFormat}";
            var output = Path.Join(arguments.OutputFolder, imageName);

            async void SaveFrame()
            {
                await frame.SaveAsync(output);
                progress.Increment(1);
                completed++;
            }

            _ = Task.Run(SaveFrame);
            frameIndex++;
        }

        ulong lastFrameIndex = 0;
        // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
        while (frameIndex > completed)
        {
            if (frameIndex != lastFrameIndex)
                progress.IsIndeterminate().Description($"[red] waiting on {frameIndex - completed} frame(s) to save[/]");

            lastFrameIndex = frameIndex;
        }
        progress.IsIndeterminate(false).Description("[green]Extracting Frames[/]");

        return Task.CompletedTask;
    }

    private static IEnumerable<Image<Bgr24>> GetFrames(MediaFile mediaFile, float dropRatio)
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
