using System.Diagnostics;
using Extractor.Commands;
using SkyRemoval;
using Spectre.Console;
using TreeBasedCli;
namespace Extractor.Handlers;

public class MaskSkyCommandHandler : ILeafCommandHandler<MaskSkyCommand.MaskSkyArguments>
{
    private SkyRemovalModel? _skyRemovalModel = null;

    public async Task HandleAsync(MaskSkyCommand.MaskSkyArguments arguments, LeafCommand executedCommand)
    {
        var stopwatch = Stopwatch.StartNew();


        var finalMaskCount = 0;

        await AnsiConsole.Progress()
            .AutoClear(false) // Do not remove the task list when done
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new ElapsedTimeColumn(), new RemainingTimeColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var loadingModelProgressTask = ctx.AddTask("[green]Loading Sky Removal Model[/]", true);
                var frameListProgressTask = ctx.AddTask("[green]Loading Frame List[/]", true);
                var maskProgressTask = ctx.AddTask("[green]Generating Masks[/]");


                loadingModelProgressTask.IsIndeterminate = true;
                _skyRemovalModel = await SkyRemovalModel.CreateAsync(arguments.Engine);
                loadingModelProgressTask.Complete();


                Directory.CreateDirectory(arguments.OutputPath);

                frameListProgressTask.IsIndeterminate = true;


                var frameList = GenerateFrameList(arguments.InputPath, arguments.InputType).ToList();

                finalMaskCount = frameList.Count;

                frameListProgressTask.Complete();


                maskProgressTask.MaxValue(frameList.Count);

                foreach (var frame in frameList)
                {
                    var inputImage = await Image.LoadAsync(frame);

                    var inputTensor = _skyRemovalModel.PrepareImage(inputImage);

                    var maskTensor = _skyRemovalModel.RunModel(inputTensor);
                    
                    var maskImage = _skyRemovalModel.ProcessModelResults(inputImage, maskTensor);

                    await maskImage.SaveAsync(Path.Combine(arguments.OutputPath, Path.GetFileNameWithoutExtension(frame) + "_mask" + Path.GetExtension(frame)));
                    
                    maskProgressTask.Increment(1);
                }
            });

        stopwatch.Stop();

        AnsiConsole.MarkupLineInterpolated($"Finished generating {finalMaskCount} masks in {Math.Round(stopwatch.ElapsedMilliseconds * 0.001, 2)} seconds.");
    }

    private static IEnumerable<string> GenerateFrameList(string inputPath, PathType inputType)
    {
        if (inputType.HasFlag(PathType.File))
        {
            yield return inputPath;
        }

        if (inputType.HasFlag(PathType.Directory))
        {
            foreach (var directory in Directory.EnumerateFiles(inputPath, "*", SearchOption.AllDirectories))
            {
                yield return directory;
            }
        }
    }
}
