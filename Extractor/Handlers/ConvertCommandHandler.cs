using Extractor.Commands;
using Spectre.Console;
using TreeBasedCli;
using TreeBasedCli.Exceptions;
namespace Extractor.Handlers;

public class ConvertCommandHandler : ILeafCommandHandler<ConvertCommand.ConvertCommandArguments>
{
    public async Task HandleAsync(ConvertCommand.ConvertCommandArguments arguments, LeafCommand executedCommand)
    {
        await AnsiConsole.Progress()
            .AutoClear(false) // Do not remove the task list when done
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new ElapsedTimeColumn(), new RemainingTimeColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {

                var checkArgumentsProgressTask = ctx.AddTask("[green]Validating Command Arguments[/]", true, 2);
                var buildImageListTask = ctx.AddTask("[green]Building Image List[/]");
                var convertProgressTask = ctx.AddTask("[green]Converting Images[/]");


                CheckArguments(arguments, checkArgumentsProgressTask);

                var images = await BuildImageList(arguments.InputFolder, arguments.InputFormat, buildImageListTask);

                await ConvertImages(images, arguments.OutputFolder, arguments.OutputFormat, convertProgressTask);

            });

    }

    private static void CheckArguments(ConvertCommand.ConvertCommandArguments arguments, ProgressTask progress)
    {
        if (!Directory.Exists(arguments.InputFolder))
            throw new MessageOnlyException($"[red]Input directory {arguments.InputFolder} does not exist...[/]");

        progress.Increment(1);

        Directory.CreateDirectory(arguments.OutputFolder);
        progress.Increment(1);



        progress.Complete();
    }

    private static Task<string[]> BuildImageList(string inputFolder, string inputFormat, ProgressTask progressTask)
    {
        progressTask.IsIndeterminate = true;
        var files = Directory.GetFiles(inputFolder, $"*.{inputFormat}", SearchOption.AllDirectories);

        progressTask.Complete();

        return Task.FromResult(files);
    }

    private static async Task ConvertImages(IReadOnlyCollection<string> files, string outputPath, string outputFormat, ProgressTask progressTask)
    {
        progressTask.MaxValue = files.Count;


        await Parallel.ForEachAsync(files, async (file, cancellationToken) =>
        {
            using var image = await Image.LoadAsync(file, cancellationToken);
            await image.SaveAsync(Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(file)}.{outputFormat}"), cancellationToken);
            progressTask.Increment(1);
        });

        progressTask.Complete();
    }
}
