using Extractor.Commands;
using FFMediaToolkit;
using Spectre.Console;
using TreeBasedCli;
using TreeBasedCli.Exceptions;
namespace Extractor;

public static class Program
{
    private static readonly CancellationTokenSource cts = new();
    private static async Task<int> Main(string[] args)
    {
        try
        {
            Console.CancelKeyPress += delegate
            {
                Cleanup();
            };


            // Ensure that FFmpeg is loaded from the correct path.
            FFmpegLoader.FFmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFmpeg");



            var settings = new ArgumentHandlerSettings
            (
                "IRSS Media Tools" ?? string.Empty,
                $"{typeof(Program).Assembly.GetName().Version?.ToString()}",
                new CommandTree(
                    CreateCommandTreeRoot(),
                    DependencyInjectionService.Instance)
            );


            var argumentHandler = new ArgumentHandler(settings);
            await argumentHandler.HandleAsync(args);
        }
        catch (MessageOnlyException ex)
        {
            AnsiConsole.Markup($"[red]{ex.Message}[/]");
            return -1;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex,
                ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes |
                ExceptionFormats.ShortenMethods | ExceptionFormats.ShowLinks);
            throw;
        }
        finally
        {
            Cleanup();
        }

        return 0;
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

    private static void Cleanup()
    {
        AnsiConsole.Reset();
        AnsiConsole.Cursor.Show(true);
    }
}
