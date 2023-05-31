using TreeBasedCli;
namespace Extractor.Commands;

public static class CommandOptions
{
    public const string InputLabel = "--input";
    public const string OutputLabel = "--output";
    public const string InputFormatLabel = "--informat";
    public const string OutputFormatLabel = "--format";
    public const string OutputFrameDropRatioLabel = "--drop";


    public static readonly CommandOption InputOption = new(InputLabel, new[]
    {
        "Required. The input video file."
    });

    public static readonly CommandOption OutputOption = new(OutputLabel, new[]
    {
        "The output path. Default: Name of the input without extension."
    });

    public static readonly CommandOption InputFormatOption = new(InputFormatLabel, new[]
    {
        "The input image file format (e.g., .png or .jpg). Default: jpg"
    });

    public static readonly CommandOption OutputFormatOption = new(OutputFormatLabel, new[]
    {
        "The output image file format (e.g., .png or .jpg). Default: jpg"
    });
}
