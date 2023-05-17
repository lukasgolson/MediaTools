using Extractor.Commands;
using FFMediaToolkit;
using TreeBasedCli;
using TreeBasedCli.Exceptions;
namespace Extractor;

public static class Program
{
    private static async Task Main(string[] args)
    {
        FFmpegLoader.FFmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFmpeg");


        var settings = new ArgumentHandlerSettings
        (
            "IRSS Video Tools",
            "0.0.1",
            new CommandTree(
                CreateCommandTreeRoot(),
                DependencyInjectionService.Instance)
        );

        try
        {
            var argumentHandler = new ArgumentHandler(settings);
            await argumentHandler.HandleAsync(args);
        }
        catch (MessageOnlyException e)
        {
            Console.WriteLine(e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static Command CreateCommandTreeRoot()
    {
        Command extractCommand = new ExtractAllCommand();
        Command infoCommand = new ListInformationCommand();

        var branchCommand = new BranchCommandBuilder("").WithDesription(new[]
            {
                "All video extraction commands"
            })
            .WithChildCommand(extractCommand)
            .WithChildCommand(infoCommand)
            .Build();


        return branchCommand;
    }
}
