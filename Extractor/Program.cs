using System.Diagnostics;
using System.Reflection;
using Extractor.Commands;
using FFMediaToolkit;
using Spectre.Console;
using TreeBasedCli;
using TreeBasedCli.Exceptions;
namespace Extractor;

public static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            FFmpegLoader.FFmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFmpeg");


            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            var name = fvi.ProductName;
            var version = fvi.FileVersion; // or fvi.ProductVersion

            var settings = new ArgumentHandlerSettings
            (
                name ?? string.Empty,
                version ?? string.Empty,
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
            AnsiConsole.Status();
            AnsiConsole.WriteException(ex,
                ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes |
                ExceptionFormats.ShortenMethods | ExceptionFormats.ShowLinks);
            throw;
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
}
