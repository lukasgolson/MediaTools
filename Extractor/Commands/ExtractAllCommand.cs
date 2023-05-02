using Extractor.Handlers;
using TreeBasedCli;
namespace Extractor.Commands;

public class ExtractAllCommand : LeafCommand<ExtractAllCommand.Arguments, ExtractAllCommand.Parser, ExtractAllCommandHandler>
{
    private const string InputFileLabel = "--input";
    private const string OutputFolderLabel = "--output";


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
            })
        })
    {
    }

    public record Arguments(string InputFile, string OutputFolder) : IParsedCommandArguments;

    public class Parser : ICommandArgumentParser<Arguments>
    {
        public IParseResult<Arguments> Parse(CommandArguments arguments)
        {
            var inputFile = arguments.GetArgument(InputFileLabel).ExpectedAsSinglePathToExistingFile();
            var outputFolder = arguments.GetArgument(OutputFolderLabel).ExpectedAsSingleValue();

            var result = new Arguments(
                inputFile,
                outputFolder
            );

            return new SuccessfulParseResult<Arguments>(result);
        }
    }
}
