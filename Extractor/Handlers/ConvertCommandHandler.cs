using Extractor.Commands;
using Spectre.Console;
using TreeBasedCli;
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

                var progressTask = ctx.AddTask("[green]Converting Images[/]");


                await ConvertImages(arguments, progressTask);

            });

    }
    private async Task ConvertImages(ConvertCommand.ConvertCommandArguments arguments, ProgressTask progressTask)
    {

    }
}
