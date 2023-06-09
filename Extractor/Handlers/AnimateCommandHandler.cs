using System.Numerics;
using Extractor.Commands;
using SixLabors.ImageSharp.Formats.Gif;
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
                var checkArgumentsProgressTask = ctx.AddTask("[green]Validating Command Arguments[/]", true, 2);
                var loadImageProgressTask = ctx.AddTask("[green]Loading Media File[/]", true, 2);
                var framesProgressTask = ctx.AddTask("[green]Animating Frames[/]");
                var RenderingProgressTask = ctx.AddTask("[green]Rendering Gif[/]");
                var savingProgressTask = ctx.AddTask("[green]Saving Gif[/]");


                var image = await LoadImageFile(arguments.InputFile, loadImageProgressTask);
                loadImageProgressTask.Complete();


                var frameCount = (int)(arguments.fps * arguments.duration);

                var images = await GenerateRotatedFrames(image, frameCount, framesProgressTask);
                framesProgressTask.Complete();


                var gif = await RenderGif(images, arguments.fps, RenderingProgressTask);
                RenderingProgressTask.Complete();


                await SaveGif(gif, arguments.OutputFile, savingProgressTask);

                savingProgressTask.Complete();
            });


    }



    public async Task<Image> LoadImageFile(string path, ProgressTask progressTask)
    {
        progressTask.MaxValue = 2;
        progressTask.Description = "[green]Loading Media File[/]";


        var image = await Image.LoadAsync(path);
        progressTask.Increment(1);

        var height = image.Height;
        var width = image.Width;

        var aspectRatio = width / height;

        int newWidth;
        int newHeight;

        if (width > height)
        {
            newWidth = 128;
            newHeight = 128 / aspectRatio;
        }
        else
        {
            newHeight = 128;
            newWidth = 128 * aspectRatio;
        }



        image.Mutate(x => x.Resize(newWidth, newHeight));

        var newImage = new Image<Rgba32>(128, 128);
        newImage.Mutate(x => x.BackgroundColor(Color.Transparent));
        newImage.Mutate(x => x.DrawImage(image, new Point((128 - newWidth) / 2, (128 - newHeight) / 2), 1f));

        progressTask.Increment(1);

        return newImage;
    }

    public Task<List<Image>> GenerateRotatedFrames(Image image, int framecount, ProgressTask progressTask)
    {
        if (framecount == 0)
            throw new InvalidOperationException("Framecount must be greater than 0");

        progressTask.MaxValue = framecount;


        var images = new List<Image>();

        for (var i = 0; i < framecount; i++)
        {
            var baseImage = new Image<Rgba32>(image.Width, image.Height);



            var rotationAngle = 360 / framecount * i;


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




        return Task.FromResult(images);
    }


    public async Task<Image> RenderGif(List<Image> images, int framesPerSecond, ProgressTask progressTask)
    {
        progressTask.MaxValue = images.Count;

        var gif = images[0];


        var frameDelay = 1 / framesPerSecond * 100;


        var gifMetaData = gif.Metadata.GetGifMetadata();
        gifMetaData.RepeatCount = 0;
        gifMetaData.ColorTableMode = GifColorTableMode.Global;


        var metadata = gif.Frames.RootFrame.Metadata.GetGifMetadata();
        metadata.FrameDelay = frameDelay;
        metadata.DisposalMethod = GifDisposalMethod.RestoreToBackground;
        metadata.ColorTableMode = GifColorTableMode.Global;

        for (var index = 1; index < images.Count; index++)
        {
            var image = images[index];
            metadata = image.Frames.RootFrame.Metadata.GetGifMetadata();
            metadata.FrameDelay = frameDelay;
            metadata.DisposalMethod = GifDisposalMethod.RestoreToBackground;

            gif.Frames.AddFrame(image.Frames.RootFrame);

            progressTask.Increment(1);
        }


        return gif;
    }

    private async Task SaveGif(Image gif, string argumentsOutputFile, ProgressTask savingProgressTask)
    {
        savingProgressTask.IsIndeterminate = true;
        await gif.SaveAsGifAsync(argumentsOutputFile);
    }
}
