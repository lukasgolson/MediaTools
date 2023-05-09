using Extractor.Handlers;
using TreeBasedCli;
namespace Extractor.Commands;

public class ExtractAllCommand : LeafCommand<ExtractAllCommand.Arguments, ExtractAllCommand.Parser, ExtractAllCommandHandler>
{
    private const string InputFileLabel = "--input";
    private const string OutputFolderLabel = "--output";
    private const string OutputImageFormat = "--format";
    private const string OutputFrameSkipCount = "--skip";


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
            new CommandOption(OutputImageFormat, new[]
            {
                "The output image file format (e.g., .png or .jpg). Default: .jpg"
            }),
            new CommandOption(OutputFrameSkipCount, new[]
            {
                "The number of frames to skip for each output frame. (e.g., 0, 1, 2). Default: No skip"
            })
        })
    {
    }

    public record Arguments(string InputFile, string OutputFolder, string OutputFormat, int FrameSkipCount) : IParsedCommandArguments;

    public class Parser : ICommandArgumentParser<Arguments>
    {
        public IParseResult<Arguments> Parse(CommandArguments arguments)
        {
            var inputFile = arguments.GetArgument(InputFileLabel).ExpectedAsSinglePathToExistingFile();
            var outputFolder = arguments.GetArgumentOrNull(OutputFolderLabel)?.ExpectedAsSingleValue() ?? Path.GetFileNameWithoutExtension(inputFile);
            var outputFormat = arguments.GetArgumentOrNull(OutputImageFormat)?.ExpectedAsSingleValue() ?? ".jpg";


            var skipCount = arguments.GetArgumentOrNull(OutputFrameSkipCount)?.ExpectedAsSingleInteger() ?? 1;


            var result = new Arguments(
                inputFile,
                outputFolder,
                outputFormat,
                skipCount
            );

            return new SuccessfulParseResult<Arguments>(result);
        }
    }
}
