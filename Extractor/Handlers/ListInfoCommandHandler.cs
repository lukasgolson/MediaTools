using Extractor.Commands;
using FFMediaToolkit.Decoding;
using TreeBasedCli;
namespace Extractor.Handlers;

public class ListInfoCommandHandler : ILeafCommandHandler<ListInformationCommand.Arguments>
{
    public Task HandleAsync(ListInformationCommand.Arguments arguments, LeafCommand executedCommand)
    {
        var file = MediaFile.Open(arguments.InputFile);
        var info = file.Video.Info;


        Console.WriteLine(arguments.InputFile);

        Console.WriteLine(Resources.Resources.InfoLine1, info.NumberOfFrames, info.FrameSize, info.IsVariableFrameRate, info.AvgFrameRate);
        Console.WriteLine(Resources.Resources.InfoLine2, info.Rotation, info.IsInterlaced, info.PixelFormat, info.Duration, info.CodecName);


        Console.Write(Resources.Resources.MetadataHeader);
        foreach (var pair in info.Metadata)
        {
            Console.Write(Resources.Resources.MetadataPair, pair.Key, pair.Value);
            Console.Write(@" ");
        }




        return Task.CompletedTask;
    }
}
