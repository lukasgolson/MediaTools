using System.Diagnostics;
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





        var i = 0;
        var file = MediaFile.Open(arguments.InputFile);

        var frameCount = file.Video.Info.NumberOfFrames.GetValueOrDefault(0);


        Console.WriteLine($"Starting extraction. File has {frameCount} frames.");

        var stopWatch = new Stopwatch();
        stopWatch.Start();

        while (file.Video.TryGetNextFrame(out var imageData))
        {


            var imageName = $"{i}.png";
            var output = Path.Join(arguments.OutputFolder, imageName);

            imageData.ToBitmap().Save(output);


            var x = i;
            if (x == 0)
                x = 1;

            var elapsed = stopWatch.ElapsedMilliseconds;
            var avgTime = elapsed / x;
            var remainingFrames = frameCount - x;
            var timeRemaining = remainingFrames * avgTime * 0.001;


            var message = $"Frame {i + 1} of {frameCount}. {elapsed * 0.001:0.00} of {timeRemaining:0} seconds";

            Console.Write("\r{0}", message);




            i++;
        }


        stopWatch.Stop();


        return Task.CompletedTask;
    }
}
