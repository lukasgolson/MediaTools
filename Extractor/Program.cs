using Extractor.Commands;
using FFMediaToolkit;
using TreeBasedCli;
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

        var argumentHandler = new ArgumentHandler(settings);
        await argumentHandler.HandleAsync(args);
    }

    private static Command CreateCommandTreeRoot()
    {
        Command extractCommand = new ExtractAllCommand();

        var branchCommand = new BranchCommandBuilder("").WithDesription(new[]
            {
                "All video extraction commands"
            })
            .WithChildCommand(extractCommand)
            .Build();


        return branchCommand;
    }
}
