using System.Diagnostics;
using Extractor.Commands;
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

        var finalFrameCount = 0;

        await AnsiConsole.Progress()
            .AutoClear(false) // Do not remove the task list when done
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new ElapsedTimeColumn(), new RemainingTimeColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var checkArgumentsProgressTask = ctx.AddTask("[green]Validating Command Arguments[/]", true, 2);
                var infoProgressTask = ctx.AddTask("[green]Analysing Media File[/]", true, 2);
                var framesProgressTask = ctx.AddTask("[green]Extracting Frames[/]");

                CheckArguments(arguments, checkArgumentsProgressTask);
                finalFrameCount = GetFileInfo(arguments, infoProgressTask);

                framesProgressTask.MaxValue(finalFrameCount == 0 ? 1 : finalFrameCount);

                AnsiConsole.MarkupLineInterpolated($"[green]Extracting {finalFrameCount} frames from {arguments.InputFile} to {arguments.OutputFolder}.[/]");

                await ExtractFrames(arguments, framesProgressTask);
            });

        stopwatch.Stop();

        AnsiConsole.MarkupLineInterpolated($"[green]Finished extracting {finalFrameCount} frames in {Math.Round(stopwatch.ElapsedMilliseconds * 0.001, 2)} seconds.[/]");
    }

    private static void CheckArguments(ExtractAllCommand.Arguments arguments, ProgressTask progress)
    {
        if (!File.Exists(arguments.InputFile))
            throw new MessageOnlyException($"[red]Input file {arguments.InputFile} does not exist...[/]");

        progress.Increment(1);

        Directory.CreateDirectory(arguments.OutputFolder);
        progress.Increment(1);


        if (arguments.DropRatio is < 0 or > 1)
            throw new MessageOnlyException($"[red]Drop Ratio must be between 0 and 1. {arguments.DropRatio} is invalid...[/]");

        progress.Complete();
    }

    private static int GetFileInfo(ExtractAllCommand.Arguments arguments, ProgressTask progress)
    {
        // Determine video info required to calculate frame count for progress.
        var file = MediaFile.Open(arguments.InputFile);
        file.PrintFileInfo();
        progress.Increment(1);

        var actualFrameCount = file.Video.Info.NumberOfFrames ?? 0;
        var finalFrameCount = MathF.Round(actualFrameCount * (1 - arguments.DropRatio));
        progress.Complete();

        return (int)finalFrameCount;
    }


    private Task ExtractFrames(ExtractAllCommand.Arguments arguments, ProgressTask progress)
    {
        var file = MediaFile.Open(arguments.InputFile);

        ulong frameIndex = 0;
        ulong completed = 0;
        foreach (var frame in file.GetFrames(arguments.DropRatio))
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
