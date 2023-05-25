using Extractor.Commands;
using FFMediaToolkit.Decoding;
using TreeBasedCli;
namespace Extractor.Handlers;

public class ListInfoCommandHandler : ILeafCommandHandler<ListInformationCommand.Arguments>
{
    public Task HandleAsync(ListInformationCommand.Arguments arguments, LeafCommand executedCommand)
    {
        var file = MediaFile.Open(arguments.InputFile);
        file.PrintFileInfo();

        return Task.CompletedTask;
    }
}
