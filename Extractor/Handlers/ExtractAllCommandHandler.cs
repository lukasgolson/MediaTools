using System.Diagnostics;
using Extractor.Commands;
using FFMediaToolkit.Decoding;
using Spectre.Console;
using TreeBasedCli;
using TreeBasedCli.Exceptions;
namespace Extractor.Handlers;

public class ExtractAllCommandHandler : ILeafCommandHandler<ExtractAllCommand.ExtractAllArguments>
{
    public async Task HandleAsync(ExtractAllCommand.ExtractAllArguments extractAllArguments, LeafCommand executedCommand)
    {

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var finalFrameCount = 0;

        await AnsiConsole.Progress()
            .AutoClear(false) // Do not remove the task list when done
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new ElapsedTimeColumn(), new RemainingTimeColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var checkArgumentsProgressTask = ctx.AddTask("[green]Validating Command Arguments[/]", true, 2);
                var infoProgressTask = ctx.AddTask("[green]Analysing Media File[/]", true, 2);
                var framesProgressTask = ctx.AddTask("[green]Extracting Frames[/]");

                CheckArguments(extractAllArguments, checkArgumentsProgressTask);
                finalFrameCount = GetFileInfo(extractAllArguments, infoProgressTask);

                framesProgressTask.MaxValue(finalFrameCount == 0 ? 1 : finalFrameCount);

                AnsiConsole.MarkupLineInterpolated($"Extracting {finalFrameCount} frames from {extractAllArguments.InputFile} to {extractAllArguments.OutputFolder}.");

                await ExtractFrames(extractAllArguments, framesProgressTask);
            });

        stopwatch.Stop();

        AnsiConsole.MarkupLineInterpolated($"Finished extracting {finalFrameCount} frames in {Math.Round(stopwatch.ElapsedMilliseconds * 0.001, 2)} seconds.");
    }

    private static void CheckArguments(ExtractAllCommand.ExtractAllArguments extractAllArguments, ProgressTask progress)
    {
        if (!File.Exists(extractAllArguments.InputFile))
            throw new MessageOnlyException($"[red]Input file {extractAllArguments.InputFile} does not exist...[/]");

        progress.Increment(1);

        Directory.CreateDirectory(extractAllArguments.OutputFolder);
        progress.Increment(1);


        if (extractAllArguments.DropRatio is < 0 or > 1)
            throw new MessageOnlyException($"[red]Drop Ratio must be between 0 and 1. {extractAllArguments.DropRatio} is invalid...[/]");

        progress.Complete();
    }

    private static int GetFileInfo(ExtractAllCommand.ExtractAllArguments extractAllArguments, ProgressTask progress)
    {
        // Determine video info required to calculate frame count for progress.
        var file = MediaFile.Open(extractAllArguments.InputFile);
        file.PrintFileInfo();
        progress.Increment(1);

        var actualFrameCount = file.Video.Info.NumberOfFrames ?? 0;
        var finalFrameCount = MathF.Round(actualFrameCount * (1 - extractAllArguments.DropRatio));
        progress.Complete();

        return (int)finalFrameCount;
    }


    private Task ExtractFrames(ExtractAllCommand.ExtractAllArguments extractAllArguments, ProgressTask progress)
    {
        var file = MediaFile.Open(extractAllArguments.InputFile);

        ulong frameIndex = 0;
        ulong completed = 0;
        foreach (var frame in file.GetFrames(extractAllArguments.DropRatio))
        {
            var imageName = $"{Path.GetFileNameWithoutExtension(extractAllArguments.InputFile)}_{frameIndex + 1}.{extractAllArguments.OutputFormat}";
            var output = Path.Join(extractAllArguments.OutputFolder, imageName);

            async void SaveFrame()
            {
                await frame.SaveAsync(output);
                progress.Increment(1);
                completed++;
            }

            _ = Task.Run(SaveFrame);
            frameIndex++;
        }

        ulong lastFrameCount = 0;
        // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
        while (frameIndex > completed)
        {
            var frameCount = frameIndex - completed;
            if (frameCount != lastFrameCount)
                progress.Description($"[red] waiting on {frameCount} frame(s) to save[/]");

            lastFrameCount = frameCount;
        }
        progress.Description("[green]Extracting Frames[/]").Complete();

        return Task.CompletedTask;
    }
}
