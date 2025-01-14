using Extractor.Handlers.GDI;
using TreeBasedCli;

namespace Extractor.Commands.GDI;

public class GdiValidateCommand : LeafCommand<GdiValidateCommand.Arguments, GdiValidateCommand.Parser, GDIValidateCommandHandler>
{
    public GdiValidateCommand() : base(
        "validate",
        [
            "Validates an image file for GDI+ compatibility."
        ],
        new[]
        {
            CommandOptions.InputFileOption,
            CommandOptions.StackTraceOption
         
        })
    {
    }
    
    public record Arguments(string InputFile, bool stackTrace) : IParsedCommandArguments;
    
    
    public class Parser : ICommandArgumentParser<Arguments>
    {
        public IParseResult<Arguments> Parse(CommandArguments arguments)
        {
            var inputFile = arguments.GetArgument(CommandOptions.InputLabel).ExpectedAsSinglePathToExistingFile();

            var stackTrace = arguments.ContainsArgument(CommandOptions.StackTraceLabel);
          
            var result = new Arguments(
                inputFile,
                stackTrace
            );

            return new SuccessfulParseResult<Arguments>(result);
        }
    }

    
}