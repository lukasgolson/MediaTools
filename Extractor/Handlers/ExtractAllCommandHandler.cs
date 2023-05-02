using Extractor.Commands;
using FFMediaToolkit.Decoding;
using TreeBasedCli;
namespace Extractor.Handlers;

public class ExtractAllCommandHandler : ILeafCommandHandler<ExtractAllCommand.Arguments>
{
    public Task HandleAsync(ExtractAllCommand.Arguments arguments, LeafCommand executedCommand)
    {
        if (!Path.Exists(arguments.InputFile))
        {
            Console.WriteLine("File does not exist...");
            return Task.CompletedTask;
        }

        Directory.CreateDirectory(arguments.OutputFolder);


        Console.WriteLine("Starting extraction....");


        var i = 0;
        var file = MediaFile.Open(arguments.InputFile);
        while (file.Video.TryGetNextFrame(out var imageData))
        {
            var imageName = $"{i}.png";
            var output = Path.Join(arguments.OutputFolder, imageName);

            imageData.ToBitmap().Save(output);

            i++;
        }




        return Task.CompletedTask;
    }
}
