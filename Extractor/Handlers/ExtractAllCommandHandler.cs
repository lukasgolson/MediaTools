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
            Console.WriteLine(Resources.Resources.File_Does_Not_Exist);
            return Task.CompletedTask;
        }

        Directory.CreateDirectory(arguments.OutputFolder);

        var format = arguments.outputFormat;
        if (string.IsNullOrEmpty(format))
            format = ".jpg";

        var fileName = Path.GetFileNameWithoutExtension(arguments.InputFile);

        var file = MediaFile.Open(arguments.InputFile);

        var frameCount = file.Video.Info.NumberOfFrames.GetValueOrDefault(0);


        Console.WriteLine(Resources.Resources.Begin_Extraction, frameCount);

        var stopWatch = new Stopwatch();
        stopWatch.Start();


        var frameIndex = 0;
        while (file.Video.TryGetNextFrame(out var imageData))
        {
            var imageName = $"{fileName}_{frameIndex}{format}";
            var output = Path.Join(arguments.OutputFolder, imageName);

            imageData.ToBitmap().Save(output);

            var x = frameIndex != 0 ? frameIndex : 1;

            var elapsed = stopWatch.ElapsedMilliseconds;
            var avgTime = elapsed / x;
            var remainingFrames = frameCount - x;
            var timeRemaining = remainingFrames * avgTime * 0.001;


            var statusMessage = $"Frame {frameIndex + 1} of {frameCount}. {elapsed * 0.001:0.00} of {timeRemaining:0} seconds";

            Console.Write("\r{0}", statusMessage);

            frameIndex++;
        }


        stopWatch.Stop();


        return Task.CompletedTask;
    }
}
