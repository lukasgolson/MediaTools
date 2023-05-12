using Extractor.Handlers;
using TreeBasedCli;
namespace Extractor.Commands;

public class ExtractAllCommand : LeafCommand<ExtractAllCommand.Arguments, ExtractAllCommand.Parser, ExtractAllCommandHandler>
{
    private const string InputFileLabel = "--input";
    private const string OutputFolderLabel = "--output";
    private const string OutputImageFormatLabel = "--format";
    private const string OutputFrameDropRatioLabel = "--drop";


    public ExtractAllCommand() : base(
        "extract",
        new[]
        {
            "extracts all frames from the input video file."
        },
        new[]
        {
            new CommandOption(InputFileLabel, new[]
            {
                "Required. The input video file."
            }),
            new CommandOption(OutputFolderLabel, new[]
            {
                "The output path. Default: Name of the input file without extension."
            }),
            new CommandOption(OutputImageFormatLabel, new[]
            {
                "The output image file format (e.g., .png or .jpg). Default: jpg"
            }),
            new CommandOption(OutputFrameDropRatioLabel, new[]
            {
                "The ratio of frames to drop from the output. (e.g., 0 = no drop, 0.5 = half, 1 = all). Default: 0"
            })
        })
    {
    }

    public record Arguments(string InputFile, string OutputFolder, string OutputFormat, float DropRatio) : IParsedCommandArguments;

    public class Parser : ICommandArgumentParser<Arguments>
    {
        public IParseResult<Arguments> Parse(CommandArguments arguments)
        {
            var inputFile = arguments.GetArgument(InputFileLabel).ExpectedAsSinglePathToExistingFile();
            var outputFolder = arguments.GetArgumentOrNull(OutputFolderLabel)?.ExpectedAsSingleValue() ?? Path.GetFileNameWithoutExtension(inputFile);
            var outputFormat = (arguments.GetArgumentOrNull(OutputImageFormatLabel)?.ExpectedAsSingleValue() ?? "jpg").Replace(".", "", StringComparison.InvariantCultureIgnoreCase);
            var dropRatioString = arguments.GetArgumentOrNull(OutputFrameDropRatioLabel)?.ExpectedAsSingleValue();

            float dropRatio;

            if (float.TryParse(dropRatioString, out var parseResult))
                dropRatio = parseResult;
            else
                dropRatio = 0;

            var result = new Arguments(
                inputFile,
                outputFolder,
                outputFormat,
                dropRatio
            );

            return new SuccessfulParseResult<Arguments>(result);
        }
    }
}
