using Extractor;
using TreeBasedCli;

var settings = new ArgumentHandlerSettings
(
    name: "IRSS Video Tools",
    version: "0.0.1",
    commandTree: new CommandTree(
        root: CreateCommandTreeRoot())
);


var argumentHandler = new ArgumentHandler(settings);
await argumentHandler.HandleAsync(args);


Command CreateCommandTreeRoot()
{
    Command extractCommand = new ExtractCommand();
    return extractCommand;
}
