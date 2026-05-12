using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ExifLibrary;
using Extractor.Algorithms;
using Extractor.Commands;
using Extractor.Extensions;
using Extractor.Structs;
using Spectre.Console;
using Spectre.Console.Advanced;
using TreeBasedCli;
using ColorSpace = Extractor.enums.ColorSpace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;

namespace Extractor.Handlers;

public class
    NormalizeLuminanceCommandHandler : ILeafCommandHandler<NormalizeLuminanceCommand.NormalizeLuminanceArguments>
{
    public async Task HandleAsync(NormalizeLuminanceCommand.NormalizeLuminanceArguments arguments,
        LeafCommand executedCommand)
    {
        var inputDirectory = arguments.InputDir;
        var outputDirectory = arguments.OutputDir;
        bool globalAverage = arguments.GlobalAverage;
        int windowSize = arguments.windowSize;
        int maxConcurrentTasks = arguments.MaxConcurrentTasks;

        if (!Directory.Exists(inputDirectory))
        {
            AnsiConsole.Console.MarkupLine($"[red]Error: Input directory {inputDirectory} does not exist.[/]");
            return;
        }

        double gamma = arguments.Gamma;
        double clipLimit = arguments.ClipLimit;
        int kernel = arguments.KernelSize;
        double headroom = arguments.Headroom;
        ColorSpace colorSpace = arguments.colorSpace;
        bool useCie = colorSpace == ColorSpace.LAB;
        bool optimalGamma = arguments.optimalGammaCorrection;

        Directory.CreateDirectory(outputDirectory);

        var files = Constants.SupportedExtensions
            .SelectMany(ext => Directory.GetFiles(inputDirectory, ext, SearchOption.AllDirectories)).ToArray();

        var luminanceValues = await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                using var semaphore = new SemaphoreSlim(maxConcurrentTasks);
                var task = ctx.AddTask(
                    $"[yellow]Extracting luminance values from images in {colorSpace.ToString()} color space...[/]",
                    maxValue: files.Length);

                var luminanceTasks = files.Select(async file =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var luminance = await Task.Run(() =>
                            new ImageLuminanceRecord(file, CalculateAverageImageLuminance(file, useCie)));
                        task.Increment(1);
                        return luminance;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToArray();

                return await Task.WhenAll(luminanceTasks);
            });

        var luminanceValuesList = luminanceValues.Where(record => record.Luminance.Max > 0).ToList();
        luminanceValuesList.Sort((a, b) => new NaturalSortComparer().Compare(a.FilePath, b.FilePath));

        var averageLuminance = new LuminanceValues();

        if (globalAverage)
        {
            var avgVariance = luminanceValuesList.Average(record => Math.Pow(record.Luminance.StdDev, 2));
            var avgStdDev = Math.Sqrt(avgVariance);

            averageLuminance = new LuminanceValues(
                luminanceValuesList.Average(record => record.Luminance.Min),
                luminanceValuesList.Average(record => record.Luminance.Max),
                luminanceValuesList.Average(record => record.Luminance.Mean),
                avgStdDev
            );
        }
        else
        {
            var rollingAverages = CalculateRollingAverage(luminanceValuesList, windowSize);

            for (var i = 0; i < luminanceValuesList.Count; i++)
            {
                luminanceValuesList[i] = new ImageLuminanceRecord(luminanceValuesList[i].FilePath, rollingAverages[i]);
            }
        }

        GC.Collect();

        await AnsiConsole.Progress().StartAsync(async ctx =>
        {
            var task = ctx.AddTask("[yellow]Normalizing luminance and saving to disk...[/]", maxValue: files.Length);
            const int batchSize = 50;
            var tasks = new List<Task>();
            using var semaphore = new SemaphoreSlim(maxConcurrentTasks);

            for (var i = 0; i < luminanceValuesList.Count; i += batchSize)
            {
                var batch = luminanceValuesList.Skip(i).Take(batchSize).ToList();

                tasks.Add(Task.Run(async () =>
                {
                    foreach (var record in batch)
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            var outputFilePath = Path.Combine(outputDirectory, Path.GetFileName(record.FilePath));

                            await NormalizeImageExposure(
                                record.FilePath,
                                outputFilePath,
                                globalAverage ? averageLuminance : record.Luminance,
                                useCie,
                                optimalGamma,
                                gamma,
                                clipLimit,
                                kernel,
                                headroom
                            );

                            await TransferExifTagsAsync(record.FilePath, outputFilePath);
                            task.Increment(1);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
        });
    }

    private static LuminanceValues CalculateAverageImageLuminance(string imagePath, bool useCie)
    {
        try
        {
            Mat image = CvInvoke.Imread(imagePath, ImreadModes.Color | ImreadModes.AnyDepth);
            Mat luminanceChannel;

            if (useCie)
            {
                Mat labImage = new Mat();
                CvInvoke.CvtColor(image, labImage, ColorConversion.Bgr2Lab);
                Mat[] labChannels = labImage.Split();
                luminanceChannel = labChannels[0];
                labImage.Dispose();
            }
            else
            {
                Mat hsvImage = new Mat();
                CvInvoke.CvtColor(image, hsvImage, ColorConversion.Bgr2Hsv);
                Mat[] hsvChannels = hsvImage.Split();
                luminanceChannel = hsvChannels[2];
                hsvImage.Dispose();
            }

            var luminance = LuminanceValues.GetCurrentLuminance(luminanceChannel);

            image.Dispose();
            luminanceChannel.Dispose();

            return luminance;
        }
        catch (Exception ex)
        {
            AnsiConsole.Console.WriteAnsi(
                $"[red]Warning: Error processing image at {imagePath}: {ex.Message}. Skipping this image.[/]");
            return new LuminanceValues();
        }
    }

    private static async Task<bool> NormalizeImageExposure(string imagePath, string outputFilePath,
        LuminanceValues targetLuminance, bool useLab, bool useOptimalGamma, double gamma = 0.5, double clipLimit = 10,
        int kernel = 8, double headroom = 0.2)
    {
        using Mat image = CvInvoke.Imread(imagePath, ImreadModes.Color | ImreadModes.AnyDepth);
        if (image.IsEmpty) return false;

        DepthType originalDepth = image.Depth;
        double maxPixelValue = (originalDepth == DepthType.Cv16U) ? 65535.0 : 255.0;

        using Mat processedImage = useOptimalGamma ? OGGCPEAlgorithm.Apply(image) : image.Clone();

        using Mat colorSpaceImage = new Mat();

        if (useLab)
            CvInvoke.CvtColor(processedImage, colorSpaceImage, ColorConversion.Bgr2Lab);
        else
            CvInvoke.CvtColor(processedImage, colorSpaceImage, ColorConversion.Bgr2Hsv);

        using VectorOfMat channels = new VectorOfMat();
        CvInvoke.Split(colorSpaceImage, channels);

        int lumIndex = useLab ? 0 : 2;
        using Mat luminanceChannel = channels[lumIndex].Clone(); // Isolate the channel we want to modify

        if (!useOptimalGamma)
        {
            using Mat gammaCorrected = ApplyGammaCorrection(luminanceChannel, gamma, originalDepth);
            gammaCorrected.CopyTo(luminanceChannel);
        }

        var tileGridSize = new System.Drawing.Size(kernel, kernel);
        CvInvoke.CLAHE(luminanceChannel, clipLimit, tileGridSize, luminanceChannel);

        var currentLuminance = LuminanceValues.GetCurrentLuminance(luminanceChannel);
        const double epsilon = 1e-6;

        // 1. Calculate our Alpha (scale) and Beta (shift)
        double scale = targetLuminance.StdDev / (currentLuminance.StdDev + epsilon);
        
        scale = Math.Clamp(scale, 0.5, 1.5);
        
        double shift = targetLuminance.Mean - (currentLuminance.Mean * scale);
    
        using Mat floatLum = new Mat();
        luminanceChannel.ConvertTo(floatLum, DepthType.Cv32F, scale, shift);

        CvInvoke.Normalize(floatLum, floatLum, 0, maxPixelValue, NormType.MinMax);
    
        floatLum.ConvertTo(luminanceChannel, originalDepth);
        
        using VectorOfMat mergedChannels = new VectorOfMat();
        if (useLab)
        {
            mergedChannels.Push(luminanceChannel);
            mergedChannels.Push(channels[1]); // a
            mergedChannels.Push(channels[2]); // b
        }
        else
        {
            mergedChannels.Push(channels[0]); // H
            mergedChannels.Push(channels[1]); // S
            mergedChannels.Push(luminanceChannel); // V
        }

        CvInvoke.Merge(mergedChannels, colorSpaceImage);

        if (useLab)
            CvInvoke.CvtColor(colorSpaceImage, processedImage, ColorConversion.Lab2Bgr);
        else
            CvInvoke.CvtColor(colorSpaceImage, processedImage, ColorConversion.Hsv2Bgr);

        CvInvoke.Imwrite(outputFilePath, processedImage);

        return true;
    }

    private static Mat ApplyGammaCorrection(Mat srcFloat, double gamma, DepthType originalDepth)
    {
        double maxPixelValue = (originalDepth == DepthType.Cv16U) ? 65535.0 : 255.0;

        Mat floatMat = new Mat();
        srcFloat.ConvertTo(floatMat, DepthType.Cv32F);

        floatMat /= maxPixelValue;
        CvInvoke.Pow(floatMat, gamma, floatMat);
        floatMat *= maxPixelValue;

        Mat result = new Mat();
        floatMat.ConvertTo(result, originalDepth);

        floatMat.Dispose();

        return result;
    }

    private static LuminanceValues[] CalculateRollingAverage(IReadOnlyList<ImageLuminanceRecord> luminanceValues,
        int windowSize)
    {
        var n = luminanceValues.Count;
        var rollingAverages = new LuminanceValues[n];

        var cumSumMin = new double[n + 1];
        var cumSumMax = new double[n + 1];
        var cumSumMean = new double[n + 1];
        var cumSumVariance = new double[n + 1];

        for (var i = 0; i < n; i++)
        {
            var currentLuminance = luminanceValues[i].Luminance;
            cumSumMin[i + 1] = cumSumMin[i] + currentLuminance.Min;
            cumSumMax[i + 1] = cumSumMax[i] + currentLuminance.Max;
            cumSumMean[i + 1] = cumSumMean[i] + currentLuminance.Mean;
            cumSumVariance[i + 1] = cumSumVariance[i] + Math.Pow(currentLuminance.StdDev, 2);
        }

        var fixedWindowStartIndex = Math.Max(0, n - windowSize);
        var fixedWindowSize = n - fixedWindowStartIndex;

        for (var i = 0; i < n; i++)
        {
            int windowStartIndex, windowEndIndex, effectiveWindowSize;

            if (i + windowSize <= n)
            {
                windowStartIndex = i;
                windowEndIndex = i + windowSize;
                effectiveWindowSize = windowSize;
            }
            else
            {
                windowStartIndex = fixedWindowStartIndex;
                windowEndIndex = n;
                effectiveWindowSize = fixedWindowSize;
            }

            var sumMin = cumSumMin[windowEndIndex] - cumSumMin[windowStartIndex];
            var sumMax = cumSumMax[windowEndIndex] - cumSumMax[windowStartIndex];
            var sumMean = cumSumMean[windowEndIndex] - cumSumMean[windowStartIndex];
            var sumVariance = cumSumVariance[windowEndIndex] - cumSumVariance[windowStartIndex];

            var avgMin = sumMin / effectiveWindowSize;
            var avgMax = sumMax / effectiveWindowSize;
            var avgMean = sumMean / effectiveWindowSize;

            var avgStdDev = Math.Sqrt(sumVariance / effectiveWindowSize);

            rollingAverages[i] = new LuminanceValues(avgMin, avgMax, avgMean, avgStdDev);
        }

        return rollingAverages;
    }


    private async Task TransferExifTagsAsync(string sourceFilePath, string destFilePath)
    {
        await Task.Run(() => 
        {
            try 
            {
                using var sourceImage = new MagickImage(sourceFilePath);
                var profile = sourceImage.GetExifProfile();

                if (profile != null)
                {
                    using var destImage = new MagickImage(destFilePath);
                    destImage.SetProfile(profile);
                
                    var xmpProfile = sourceImage.GetXmpProfile();
                    if (xmpProfile != null)
                    {
                        destImage.SetProfile(xmpProfile);
                    }

                    destImage.Write(destFilePath);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.Console.MarkupLine($"[yellow]Warning: Could not transfer EXIF tags. {ex.Message}[/]");
            }
        });
    }
}

public struct ImageLuminanceRecord
{
    public string FilePath { get; set; }
    public LuminanceValues Luminance { get; set; }

    public ImageLuminanceRecord(string filePath, LuminanceValues luminance)
    {
        FilePath = filePath;
        Luminance = luminance;
    }
}