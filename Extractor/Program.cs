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
                "IRSS Media Tools",
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
        Command convertCommand = new ConvertCommand();

        var branchCommand = new BranchCommandBuilder("")
            .WithDesription(new[]
            {
                "IRSS Media Tools is a command-line tool for extracting, converting, and working with media files.", "Unless otherwise stated, all commands will use the current working directory as the input/output folder."
            })
            .WithChildCommand(extractCommand)
            .WithChildCommand(infoCommand)
            .WithChildCommand(convertCommand)
            .WithChildCommand(new AnimateCommand())
            .Build();


        return branchCommand;
    }

    private static void Cleanup()
    {
        AnsiConsole.Reset();
        AnsiConsole.Cursor.Show(true);
    }
}
