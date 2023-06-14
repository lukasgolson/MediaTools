using System.Numerics;
using Extractor.Commands;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Spectre.Console;
using TreeBasedCli;
using Color = SixLabors.ImageSharp.Color;
namespace Extractor.Handlers;

public class AnimateCommandHandler : ILeafCommandHandler<AnimateCommand.AnimateArguments>
{
    public async Task HandleAsync(AnimateCommand.AnimateArguments arguments, LeafCommand executedCommand)
    {
        await AnsiConsole.Progress()
            .AutoClear(false) // Do not remove the task list when done
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new ElapsedTimeColumn(), new RemainingTimeColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var loadImageProgressTask = ctx.AddTask("[green]Loading Media File[/]", false, 2);
                var framesProgressTask = ctx.AddTask("[green]Animating Frames[/]", false);
                var downscalingProgressTask = ctx.AddTask("[green]Downscaling Frames[/]", false);
                var transparencyProgressTask = ctx.AddTask("[green]Cleaning Transparency[/]", false);
                var renderingProgressTask = ctx.AddTask("[green]Constructing Gif[/]", false);
                var savingProgressTask = ctx.AddTask("[green]Encoding Gif[/]", false);


                var image = await LoadImageFile(arguments.InputFile, loadImageProgressTask);




                var frameCount = (int)(arguments.Fps * arguments.Duration);


                var images = await GenerateRotatedFrames(image, frameCount, framesProgressTask);

                await DownscaleImages(images, KnownResamplers.Robidoux, arguments.length, downscalingProgressTask);

                await ClipTransparencyonImages(images, 0.5f, transparencyProgressTask);


                var gif = await RenderGif(images, arguments.Fps, renderingProgressTask);


                await SaveGif(gif, arguments.OutputFile, savingProgressTask);

                savingProgressTask.Complete();
            });
    }



    private static async Task<Image> LoadImageFile(string path, ProgressTask progressTask)
    {
        progressTask.MaxValue = 2;
        progressTask.StartTask();


        var image = await Image.LoadAsync(path);
        progressTask.Increment(1);

        var newImage = image.CreateSquareImage(Color.Transparent);
        progressTask.Increment(1);

        progressTask.Complete();

        return newImage;
    }


    private static Task<List<Image>> GenerateRotatedFrames(Image image, int frameCount, ProgressTask progressTask)
    {
        if (frameCount == 0)
            throw new InvalidOperationException("FrameCount must be greater than 0");

        progressTask.MaxValue = frameCount;
        progressTask.StartTask();

        var images = new List<Image>();

        for (var i = 0; i < frameCount; i++)
        {
            var baseImage = new Image<Rgba32>(image.Width, image.Height);



            var rotationAngle = 360 / frameCount * i;


            baseImage.Mutate(x => x.BackgroundColor(Color.Transparent));


            var foreground = image.Clone(x =>
            {
                x.Resize((int)(image.Width * 0.75), (int)(image.Height * 0.75));
                x.Rotate(rotationAngle);
            });

            var originalCenterpoint = new Vector2(image.Width, image.Height) / 2;

            var centerpoint = new Vector2(foreground.Width, foreground.Height) / 2;


            var offset = originalCenterpoint - centerpoint;


            baseImage.Mutate(x => x.DrawImage(foreground, new Point((int)offset.X, (int)offset.Y), 1f));

            images.Add(baseImage);
            progressTask.Increment(1);
        }



        progressTask.Complete();
        return Task.FromResult(images);
    }



    private static Task DownscaleImages(List<Image> images, IResampler sampler, int length, ProgressTask progressTask)
    {
        progressTask.MaxValue = images.Count;
        progressTask.StartTask();
        foreach (var image in images)
        {
            image.Mutate(x => x.Resize(length, length, sampler, true));

            progressTask.Increment(1);
        }

        progressTask.Complete();
        return Task.CompletedTask;
    }


    private static Task ClipTransparencyonImages(List<Image> images, float threshold, ProgressTask progressTask)
    {
        progressTask.MaxValue = images.Count;
        progressTask.StartTask();

        foreach (var image in images)
        {
            image.ClipTransparency(threshold);

            progressTask.Increment(1);
        }

        progressTask.Complete();
        return Task.CompletedTask;
    }

    private static Task<Image> RenderGif(IReadOnlyList<Image> images, int framesPerSecond, ProgressTask progressTask)
    {
        progressTask.MaxValue = images.Count;
        progressTask.StartTask();


        var frameDelay = (int)Math.Floor(1f / framesPerSecond * 100);


        var gif = images[0];

        var gifMetaData = gif.Metadata.GetGifMetadata();
        gifMetaData.RepeatCount = 0;


        var metadata = gif.Frames.RootFrame.Metadata.GetGifMetadata();
        metadata.FrameDelay = frameDelay;
        metadata.DisposalMethod = GifDisposalMethod.RestoreToBackground;

        for (var index = 1; index < images.Count; index++)
        {
            var image = images[index];
            metadata = image.Frames.RootFrame.Metadata.GetGifMetadata();
            metadata.FrameDelay = frameDelay;
            metadata.DisposalMethod = GifDisposalMethod.RestoreToBackground;

            gif.Frames.AddFrame(image.Frames.RootFrame);

            progressTask.Increment(1);
        }

        progressTask.Complete();
        return Task.FromResult(gif);
    }

    private static async Task SaveGif(Image gif, string argumentsOutputFile, ProgressTask savingProgressTask)
    {
        savingProgressTask.IsIndeterminate = true;
        await gif.SaveAsGifAsync(argumentsOutputFile, new GifEncoder
        {
            ColorTableMode = GifColorTableMode.Local,
            Quantizer = KnownQuantizers.Wu
        });
    }
}
