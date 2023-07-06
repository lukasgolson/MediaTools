﻿using Extractor.Commands;
using FFMediaToolkit;
using Spectre.Console;
using TreeBasedCli;
using TreeBasedCli.Exceptions;
namespace Extractor
{
    public static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            ValidateIo();

            try
            {
                SetFFmpegPath();
                await HandleArguments(args);
            }
            catch (MessageOnlyException ex)
            {
                DisplayErrorMessage(ex.Message);
                return -1;
            }
            catch (Exception ex)
            {
                DisplayException(ex);
            }
            finally
            {
                Cleanup();
            }

            return 0;
        }

        private static void SetFFmpegPath()
        {
            FFmpegLoader.FFmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFmpeg");
        }

        private static async Task HandleArguments(string[] args)
        {
            var settings = CreateArgumentHandlerSettings();
            var argumentHandler = new ArgumentHandler(settings);
            await argumentHandler.HandleAsync(args);
        }

        private static ArgumentHandlerSettings CreateArgumentHandlerSettings()
        {
            return new ArgumentHandlerSettings(
                "IRSS Media Tools",
                $"{typeof(Program).Assembly.GetName().Version?.ToString()}",
                new CommandTree(
                    CreateCommandTreeRoot(),
                    DependencyInjectionService.Instance));
        }

        private static void ValidateIo()
        {
            CleanupOldLogFiles();

            if (AreStandardStreamsAvailable())
                return;

            RedirectConsoleToLogFile();
        }

        private static bool AreStandardStreamsAvailable()
        {
            return Console.OpenStandardInput(1) != Stream.Null &&
                Console.OpenStandardOutput(1) != Stream.Null &&
                Console.OpenStandardError(1) != Stream.Null;
        }

        private static void RedirectConsoleToLogFile()
        {
            var tempLogFile = CreateTempLogFile();

            var fileStream = new FileStream(tempLogFile, FileMode.OpenOrCreate, FileAccess.Write);
            var streamWriter = new StreamWriter(fileStream);

            Console.SetOut(streamWriter);
            Console.SetError(streamWriter);
        }

        private static string CreateTempLogFile()
        {
            var logDirectoryPath = GetLogDirectoryPath();

            // Create the directory if it doesn't already exist
            Directory.CreateDirectory(logDirectoryPath);

            var fileName = $"{DateTime.Now:yyyyMMdd_HHmmssfff}_{Guid.NewGuid()}.log";
            return Path.Combine(logDirectoryPath, fileName);
        }


        private static void CleanupOldLogFiles()
        {
            var logDirectoryPath = GetLogDirectoryPath();

            if (!Directory.Exists(logDirectoryPath))
                return;
            var oneWeekAgo = DateTime.Now.AddDays(-7);
            DeleteFilesOlderThan(logDirectoryPath, oneWeekAgo);
        }

        private static string GetLogDirectoryPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "IRSSMediaTools");
        }

        private static void DeleteFilesOlderThan(string directoryPath, DateTime date)
        {
            var files = Directory.GetFiles(directoryPath);

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < date)
                    fileInfo.Delete();
            }
        }


        private static void DisplayErrorMessage(string message)
        {
            AnsiConsole.Markup($"[red]{message}[/]");
        }

        private static void DisplayException(Exception ex)
        {
            AnsiConsole.WriteException(ex,
                ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes |
                ExceptionFormats.ShortenMethods | ExceptionFormats.ShowLinks);
        }

        private static Command CreateCommandTreeRoot()
        {
            return new BranchCommandBuilder("")
                .WithDesription(new[]
                {
                    "IRSS Media Tools is a command-line tool for extracting, converting, and working with media files.", "Unless otherwise stated, all commands will use the current working directory as the input/output folder."
                })
                .WithChildCommand(new ExtractAllCommand())
                .WithChildCommand(new ListInformationCommand())
                .WithChildCommand(new ConvertCommand())
                .WithChildCommand(new AnimateCommand())
                .Build();
        }

        private static void Cleanup()
        {
            AnsiConsole.Reset();
            AnsiConsole.Cursor.Show(true);
        }
    }
}
