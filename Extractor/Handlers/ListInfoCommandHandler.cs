using Extractor.Commands;
using FFMediaToolkit.Decoding;
using Spectre.Console;
using TreeBasedCli;
namespace Extractor.Handlers;

public class ListInfoCommandHandler : ILeafCommandHandler<ListInformationCommand.Arguments>
{
    public Task HandleAsync(ListInformationCommand.Arguments arguments, LeafCommand executedCommand)
    {
        var file = MediaFile.Open(arguments.InputFile);
        var info = file.Video.Info;

        AnsiConsole.WriteLine(arguments.InputFile);

        AnsiConsole.WriteLine(Resources.Resources.InfoLine1, info.NumberOfFrames, info.FrameSize, info.IsVariableFrameRate, info.AvgFrameRate);
        AnsiConsole.WriteLine(Resources.Resources.InfoLine2, info.Rotation, info.IsInterlaced, info.PixelFormat, info.Duration, info.CodecName);


        AnsiConsole.Write(Resources.Resources.MetadataHeader);
        foreach (var pair in info.Metadata)
        {
            AnsiConsole.Write(Resources.Resources.MetadataPair, pair.Key, pair.Value);
            AnsiConsole.Write(@" ");
        }




        return Task.CompletedTask;
    }
}
