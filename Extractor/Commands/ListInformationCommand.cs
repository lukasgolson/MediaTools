using Extractor.Handlers;
using TreeBasedCli;
namespace Extractor.Commands;

public class ListInformationCommand : LeafCommand<ListInformationCommand.Arguments, ListInformationCommand.Parser, ListInfoCommandHandler>
{
    public ListInformationCommand() : base(
        "info",
        new[]
        {
            "Lists information about the input video file."
        },
        new[]
        {
            new CommandOption(CommandOptions.InputLabel, new[]
            {
                "Required. The input video file."
            })
        })
    {
    }

    public record Arguments(string InputFile) : IParsedCommandArguments;


    public class Parser : ICommandArgumentParser<Arguments>
    {
        public IParseResult<Arguments> Parse(CommandArguments arguments)
        {
            var inputFile = arguments.GetArgument(CommandOptions.InputLabel).ExpectedAsSinglePathToExistingFile();

            return new SuccessfulParseResult<Arguments>(new Arguments(inputFile));
        }
    }
}
