using Extractor.Handlers;
using TreeBasedCli;
namespace Extractor.Commands;

public class AnimateCommand : LeafCommand<AnimateCommand.AnimateArguments, AnimateCommand.Parser, AnimateCommandHandler>
{
    public AnimateCommand() : base(
        "animate",
        new[]
        {
            "creates an animated gif from the input image file."
        },
        new[]
        {
            CommandOptions.InputOption, CommandOptions.OutputOption
        })
    {
    }

    public record AnimateArguments(string InputFile, string OutputFile, int fps, float duration) : IParsedCommandArguments;

    public class Parser : ICommandArgumentParser<AnimateArguments>
    {
        public IParseResult<AnimateArguments> Parse(CommandArguments arguments)
        {
            var inputFile = arguments.GetArgument(CommandOptions.InputLabel).ExpectedAsSinglePathToExistingFile();
            var outputFile = arguments.GetArgumentOrNull(CommandOptions.OutputLabel)?.ExpectedAsSingleValue() ?? Path.GetFileNameWithoutExtension(inputFile) + ".gif";
            var FPS = arguments.GetArgumentOrNull(CommandOptions.FrameRate)?.ExpectedAsSingleValue() ?? "30";
            var Duration = arguments.GetArgumentOrNull(CommandOptions.Duration)?.ExpectedAsSingleValue() ?? "1";


            var framesPerSecond = 30;
            if (int.TryParse(FPS, out var fpsInt))
                framesPerSecond = fpsInt;

            float duration = 1;
            if (float.TryParse(Duration, out var durationInt))
                duration = durationInt;

            var result = new AnimateArguments(
                inputFile,
                outputFile,
                framesPerSecond,
                duration
            );

            return new SuccessfulParseResult<AnimateArguments>(result);
        }
    }
}
