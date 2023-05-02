using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;
using TreeBasedCli;
namespace Extractor;

public class ExtractCommand : LeafCommand<ExtractCommand.Arguments, ExtractCommand.Parser, ExtractCommand.Handler>
{
    private const string InputFileLabel = "--input";
    private const string OutputFolderLabel = "--output";


    public ExtractCommand() : base(
        label: "extract",
        description: new[]
        {
            "extracts all frames from the input video file."
        },
        options: new[]
        {
            new CommandOption(label: InputFileLabel, description: new[]
            {
                "Required. The input video file."
            }),
            new CommandOption(label: OutputFolderLabel, description: new[]
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
            var inputFile = arguments.GetArgument(InputFileLabel).ExpectedAsSingleValue();
            var outputFolder = arguments.GetArgument(OutputFolderLabel).ExpectedAsSingleValue();

            var result = new Arguments(
               InputFile: inputFile,
               OutputFolder: outputFolder
            );

            return new SuccessfulParseResult<Arguments>(result);
        }
    }
    
    public class Handler : ILeafCommandHandler<Arguments>
    {
        public Task HandleAsync(Arguments arguments, LeafCommand executedCommand)
        {
            Directory.CreateDirectory(arguments.OutputFolder);

            var inputFile = new MediaFile(arguments.InputFile);

            using var engine = new Engine();
            engine.GetMetadata(inputFile);

            var duration = inputFile.Metadata.Duration;
            Console.WriteLine(duration);

            
            int counter = 0;
            var outputFileName = Path.Combine(arguments.OutputFolder, counter.ToString());
            var outputFile = new MediaFile(outputFileName);

            var options = new ConversionOptions
            {
                Seek = TimeSpan.FromSeconds(counter)
            };
            engine.GetThumbnail(inputFile, outputFile, options);
            
            
            return Task.CompletedTask;
        }
    }
    

   
}
