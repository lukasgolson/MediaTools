using Extractor.Handlers;
using TreeBasedCli;
namespace Extractor.Commands;

public class ExtractAllCommand : LeafCommand<ExtractAllCommand.Arguments, ExtractAllCommand.Parser, ExtractAllCommandHandler>
{
    private const string InputFileLabel = "--input";
    private const string OutputFolderLabel = "--output";
    private const string OutputImageFormat = "--format";


    public ExtractAllCommand() : base(
        "all",
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
                "Required. The output video file."
            }),
            new CommandOption(OutputImageFormat, new[]
            {
                "Required. The output image file format (e.g., .png or .jpg)."
            })
        })
    {
    }

    public record Arguments(string InputFile, string OutputFolder, string outputFormat) : IParsedCommandArguments;

    public class Parser : ICommandArgumentParser<Arguments>
    {
        public IParseResult<Arguments> Parse(CommandArguments arguments)
        {
            var inputFile = arguments.GetArgument(InputFileLabel).ExpectedAsSinglePathToExistingFile();
            var outputFolder = arguments.GetArgument(OutputFolderLabel).ExpectedAsSingleValue();
            var outputFormat = arguments.GetArgument(OutputImageFormat).ExpectedAsSingleValue();

            var result = new Arguments(
                inputFile,
                outputFolder,
                outputFormat
            );

            return new SuccessfulParseResult<Arguments>(result);
        }
    }
}
