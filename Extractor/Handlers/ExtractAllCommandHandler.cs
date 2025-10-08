using System.Diagnostics;
using ExifLibrary;
using Extractor.Commands;
using Extractor.Structs;
using FFMediaToolkit.Decoding;
using SkyRemoval;
using Spectre.Console;
using TreeBasedCli;
using TreeBasedCli.Exceptions;

namespace Extractor.Handlers;

public class ExtractAllCommandHandler : ILeafCommandHandler<ExtractAllCommand.ExtractAllArguments>
{
    private SkyRemovalModel? _skyRemovalModel = null;

    public async Task HandleAsync(ExtractAllCommand.ExtractAllArguments extractAllArguments,
        LeafCommand executedCommand)
    {
        var stopwatch = Stopwatch.StartNew();


        var finalFrameCount = 0;

        await AnsiConsole.Progress()
            .AutoClear(false) // Do not remove the task list when done
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(),
                new ElapsedTimeColumn(), new RemainingTimeColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var checkArgumentsProgressTask = ctx.AddTask("[green]Validating Command Arguments[/]", true, 2);
                var setupDependenciesProgressTask = ctx.AddTask("[green]Setting up Dependencies[/]", true, 2);
                var infoProgressTask = ctx.AddTask("[green]Analysing Media File[/]", true, 2);
                var framesProgressTask = ctx.AddTask("[green]Extracting Frames[/]");


                ProgressTask? extractGeospatialTask = null;
                if (extractAllArguments.ExtractGeoSpatial)
                {
                    extractGeospatialTask = ctx.AddTask("[green]Extracting Geospatial Information[/]");
                }


                ProgressTask? maskProgressTask = null;
                if (extractAllArguments.ImageMaskGeneration != ExtractAllCommand.ImageMaskGeneration.None)
                {
                    maskProgressTask = ctx.AddTask("[green]Generating Masks[/]");
                }

                await CheckArguments(extractAllArguments, checkArgumentsProgressTask);

                await SetupDependencies(extractAllArguments, setupDependenciesProgressTask);


                finalFrameCount = GetFrameCount(extractAllArguments, infoProgressTask);

                framesProgressTask.MaxValue(finalFrameCount == 0 ? 1 : finalFrameCount);
                maskProgressTask?.MaxValue(finalFrameCount == 0 ? 1 : finalFrameCount);

                AnsiConsole.MarkupLineInterpolated(
                    $"Extracting {finalFrameCount} frames from {extractAllArguments.InputFile} to {extractAllArguments.FramesOutputFolder}.");



                Dictionary<int, DjiTelemetryData>? djiTelemetryDatas = null;
                if (extractAllArguments.ExtractGeoSpatial)
                {
                    djiTelemetryDatas = (await ExtractCoordinates(extractAllArguments, extractGeospatialTask))
                        .ToDictionary(x => x.FrameCount, x => x);

                }
           
                
                
                await ExtractFrames(extractAllArguments, djiTelemetryDatas, framesProgressTask, maskProgressTask);
            });

        stopwatch.Stop();

        AnsiConsole.MarkupLineInterpolated(
            $"Finished extracting {finalFrameCount} frames in {Math.Round(stopwatch.ElapsedMilliseconds * 0.001, 2)} seconds.");
    }

    private async Task<List<DjiTelemetryData>> ExtractCoordinates(
        ExtractAllCommand.ExtractAllArguments extractAllArguments,
        ProgressTask? extractGeospatialTask)
    {
        extractGeospatialTask.StartTask();
        var geospatialInfoPath = Path.ChangeExtension(extractAllArguments.InputFile, ".srt");

        var fileContents = await File.ReadAllTextAsync(geospatialInfoPath);

        var records = SrtParser.Parse(fileContents);
        
        extractGeospatialTask.StopTask();
       

        return records;
    }

    private static Task CheckArguments(ExtractAllCommand.ExtractAllArguments extractAllArguments, ProgressTask progress)
    {
        if (!File.Exists(extractAllArguments.InputFile))
            throw new MessageOnlyException($"[red]Input file {extractAllArguments.InputFile} does not exist...[/]");

        progress.Increment(1);

        Directory.CreateDirectory(extractAllArguments.FramesOutputFolder);

        if (extractAllArguments.ImageMaskGeneration != ExtractAllCommand.ImageMaskGeneration.None)
        {
            Directory.CreateDirectory(extractAllArguments.MasksOutputFolder);
        }

        progress.Increment(1);


        if (extractAllArguments.DropRatio is < 0 or > 1)
            throw new MessageOnlyException(
                $"[red]Drop Ratio must be between 0 and 1. {extractAllArguments.DropRatio} is invalid...[/]");


        progress.Complete();

        return Task.CompletedTask;
    }

    private async Task SetupDependencies(ExtractAllCommand.ExtractAllArguments extractAllArguments,
        ProgressTask progress)
    {
        var skyRemoval = extractAllArguments.ImageMaskGeneration.HasFlag(ExtractAllCommand.ImageMaskGeneration.Sky);

        if (skyRemoval)
        {
            progress.IncrementMaxValue();
        }


        if (skyRemoval)
        {
            _skyRemovalModel = await SkyRemovalModel.CreateAsync();
            progress.Increment(1);
        }

        progress.Complete();
    }

    private static int GetFrameCount(ExtractAllCommand.ExtractAllArguments extractAllArguments, ProgressTask progress)
    {
        // Determine video info required to calculate frame count for progress.
        var file = MediaFile.Open(extractAllArguments.InputFile);
        file.PrintFileInfo();
        progress.Increment(1);

        var actualFrameCount = file.Video.Info.NumberOfFrames ?? 0;
        var finalFrameCount = MathF.Round(actualFrameCount * (1 - extractAllArguments.DropRatio));


        progress.Complete();

        return (int)finalFrameCount;
    }


    private async Task ExtractFrames(ExtractAllCommand.ExtractAllArguments extractAllArguments,
        Dictionary<int, DjiTelemetryData>? djiTelemetryDatas, ProgressTask progress,
        ProgressTask? maskProgress)
    {
        var file = MediaFile.Open(extractAllArguments.InputFile);
        var tasks = new List<Task>();

        var skyMask = extractAllArguments.ImageMaskGeneration.HasFlag(ExtractAllCommand.ImageMaskGeneration.Sky);

        ulong frameIndex = 0;
        foreach (var frame in file.GetFrames(extractAllArguments.DropRatio))
        {
            var imageName = $"{Path.GetFileNameWithoutExtension(extractAllArguments.InputFile)}_{frameIndex + 1}";

            tasks.Add(Task.Run(async () =>
            {
                var index_id = frameIndex;
                
                var frameOutput = Path.Join(extractAllArguments.FramesOutputFolder,
                    $"{imageName}.{extractAllArguments.OutputFormat}");

                await frame.SaveAsync(frameOutput);
                
                var geospatialData = djiTelemetryDatas.GetValueOrDefault((int) index_id + 1);

                
                if (geospatialData != null)
                {
                 
                    var t = await ImageFile.FromFileAsync(frameOutput);
                
                    t?.Properties.Set(ExifTag.GPSAltitude, geospatialData.AbsoluteAltitude );
                    
                
                    await t.SaveAsync(frameOutput);   
                }
                

                
                
                
                progress.Increment(1);
            }));

            tasks.Add(Task.Run(async () =>
            {
                if (skyMask && _skyRemovalModel != null)
                {
                    var maskOutput = Path.Join(extractAllArguments.MasksOutputFolder,
                        $"{imageName}_mask.{extractAllArguments.OutputFormat}");

                    var mask = _skyRemovalModel.Run(frame.CloneAs<Rgb24>());

                    await mask.SaveAsync(maskOutput).ConfigureAwait(false);

                    maskProgress?.Increment(1);
                }
            }));

            frameIndex++;
        }

        await Task.WhenAll(tasks);

        progress.Description("[green]Extracting Frames[/]").Complete();
    }
}