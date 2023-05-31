using Extractor.Handlers;
using TreeBasedCli;
namespace Extractor.Commands;

public class ExtractAllCommand : LeafCommand<ExtractAllCommand.ExtractAllArguments, ExtractAllCommand.Parser, ExtractAllCommandHandler>
{
    public ExtractAllCommand() : base(
        "extract",
        new[]
        {
            "extracts all frames from the input video file."
        },
        new[]
        {
            CommandOptions.InputOption,
            CommandOptions.OutputOption,
            CommandOptions.OutputFormatOption,
            new CommandOption(CommandOptions.OutputFrameDropRatioLabel, new[]
            {
                "The ratio of frames to drop from the output. (e.g., 0 = no drop, 0.5 = half, 1 = all). Default: 0"
            })
        })
    {
    }

    public record ExtractAllArguments(string InputFile, string OutputFolder, string OutputFormat, float DropRatio) : IParsedCommandArguments;

    public class Parser : ICommandArgumentParser<ExtractAllArguments>
    {
        public IParseResult<ExtractAllArguments> Parse(CommandArguments arguments)
        {
            var inputFile = arguments.GetArgument(CommandOptions.InputLabel).ExpectedAsSinglePathToExistingFile();
            var outputFolder = arguments.GetArgumentOrNull(CommandOptions.OutputLabel)?.ExpectedAsSingleValue() ?? Path.GetFileNameWithoutExtension(inputFile);
            var outputFormat = (arguments.GetArgumentOrNull(CommandOptions.OutputFormatLabel)?.ExpectedAsSingleValue() ?? "jpg").ToLowerInvariant().Replace(".", "", StringComparison.InvariantCultureIgnoreCase);
            var dropRatioString = arguments.GetArgumentOrNull(CommandOptions.OutputFrameDropRatioLabel)?.ExpectedAsSingleValue();

            float dropRatio;

            if (float.TryParse(dropRatioString, out var parseResult))
                dropRatio = parseResult;
            else
                dropRatio = 0;

            var result = new ExtractAllArguments(
                inputFile,
                outputFolder,
                outputFormat,
                Math.Abs(dropRatio)
            );

            return new SuccessfulParseResult<ExtractAllArguments>(result);
        }
    }
}
