using Extractor.Handlers;
using TreeBasedCli;
using TreeBasedCli.Exceptions;
namespace Extractor.Commands;

public class MaskSkyCommand : LeafCommand<MaskSkyCommand.MaskSkyArguments, MaskSkyCommand.Parser, MaskSkyCommandHandler>
{
    public MaskSkyCommand() : base(
        "mask_sky",
        new[]
        {
            "Generates sky masks for all images in a folder."
        },
        new[]
        {
            CommandOptions.InputOption, CommandOptions.OutputOption
        })
    {
    }

    public record MaskSkyArguments(string InputPath, string OutputPath, PathType InputType) : IParsedCommandArguments;



    public class Parser : ICommandArgumentParser<MaskSkyArguments>
    {
        public IParseResult<MaskSkyArguments> Parse(CommandArguments arguments)
        {
            var inputPath = arguments.GetArgument(CommandOptions.InputLabel).ExpectedAsSingleValue();
            var outputFolder = arguments.GetArgumentOrNull(CommandOptions.OutputLabel)?.ExpectedAsSingleValue() ?? Path.GetFileNameWithoutExtension(inputPath);


            var inputDirectory = PathType.None;

            if (Directory.Exists(inputPath))
            {
                inputDirectory |= PathType.Directory;
            }

            if (File.Exists(inputPath))
            {
                inputDirectory |= PathType.File;
            }

            if (inputDirectory == PathType.None)
            {
                throw new MessageOnlyException("Input file or directory does not exist.");
            }


            var result = new MaskSkyArguments(
                inputPath,
                outputFolder,
                inputDirectory
            );


            return new SuccessfulParseResult<MaskSkyArguments>(result);
        }
    }
}
