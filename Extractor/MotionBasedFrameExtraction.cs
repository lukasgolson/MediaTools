using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Extractor.Extensions;
using FFMediaToolkit.Graphics;
namespace Extractor;

public static class MotionBasedFrameExtraction
{
    /// <summary>
    ///     Calculate the variance of the Laplacian of an image.
    ///     Also known as the bluriness or focus measure of an image.
    /// </summary>
    /// <param name="frame">Input image.</param>
    /// <returns>The variance of the Laplacian of the image.</returns>
    private static double VarianceOfLaplacian(IInputArray frame)
    {
        // Create a new matrix to store the Laplacian of the image
        using var lap = new Mat();
        // Apply the Laplacian operator to the image.
        CvInvoke.Laplacian(frame, lap, DepthType.Cv64F);

        // Calculate the mean and standard deviation of the Laplacian image.
        var mean = new MCvScalar();
        var std = new MCvScalar();
        CvInvoke.MeanStdDev(lap, ref mean, ref std);

        // Return the variance of the Laplacian.
        return std.V0 * std.V0;
    }



    /// <summary>
    ///     Calculates the sharpness of an image based on Canny Edge Detection.
    ///     Adapted from: https://stackoverflow.com/a/20198278
    /// </summary>
    /// <param name="frame">Input image.</param>
    /// <returns>Sharpness percentage based on the ratio of edge pixels to total pixels.</returns>
    private static double CalculateCannySharpness(Image<Gray, byte> frame)
    {
        using var image = frame.Canny(255, 175);

        var imageEdgeCount = image.CountNonzero()[0];
        double imagePixelCount = image.Width * image.Height;

        var sharpness = imageEdgeCount / imagePixelCount * 100;
        return sharpness;
    }

    /// <summary>
    ///     Calculates a combined blur measure for an image using both Variance of Laplacian and Canny Edge Detection.
    /// </summary>
    /// <param name="image">Input image.</param>
    /// <returns>
    ///     Combined blur measure, which is a weighted average of normalized laplacian variance and normalized canny
    ///     sharpness.
    /// </returns>
    public static double CalculateCombinedBlurMeasure(Image<Bgr, byte> image)
    {
        // Constants to normalize the measures
        const double maxVarianceOfLaplacian = 1000; // example maximum value

        // Weights for the weighted average
        const double weightLaplacian = 0.5;
        const double weightCanny = 1 - weightLaplacian;

        // Calculate the variance of Laplacian
        var laplacianVariance = VarianceOfLaplacian(image.Mat);
        var normalizedLaplacianVariance = laplacianVariance / maxVarianceOfLaplacian;

        // Convert the image to grayscale
        var grayImage = image.Convert<Gray, byte>();

        // Calculate the Canny Edge Detection Sharpness Measure
        var cannySharpness = CalculateCannySharpness(grayImage);
        var normalizedCannySharpness = cannySharpness / 100;

        // Combine the measures using a weighted average
        var combinedMeasure = normalizedLaplacianVariance * weightLaplacian + normalizedCannySharpness * weightCanny;

        return combinedMeasure;
    }







    /// <summary>
    ///     Calculates the amount of movement between two frames using optical flow.
    /// </summary>
    /// <param name="previousFrame">The first frame to compare.</param>
    /// <param name="currentFrame">The second frame to compare.</param>
    /// <returns>
    ///     A double value representing the percentage of movement between the two frames, rounded to three decimal
    ///     places.
    /// </returns>
    public static double CalculateFrameMovement(ImageData previousFrame, ImageData currentFrame)
    {
        // Convert the images to grayscale
        using var frame1 = previousFrame.ToEmguBitmap().Convert<Gray, byte>();
        using var frame2 = currentFrame.ToEmguBitmap().Convert<Gray, byte>();

        // Create a new Mat to store the optical flow
        using var flow = new Mat();

        // Compute the optical flow using Farneback's method
        CvInvoke.CalcOpticalFlowFarneback(frame1, frame2, flow, 0.5, 3, 15, 3, 5, 1.1, 0);

        // Create new Mats to store the x and y components of the flow
        using var flowX = new Mat(frame1.Size, DepthType.Cv32F, 1);
        using var flowY = new Mat(frame1.Size, DepthType.Cv32F, 1);

        // Split the flow into x and y components
        CvInvoke.Split(flow, new VectorOfMat(flowX, flowY));

        // Create Mats to store the magnitude and angle of the flow
        using var magnitude = new Mat();
        using var angle = new Mat();

        // Convert the flow from Cartesian coordinates to polar coordinates
        CvInvoke.CartToPolar(flowX, flowY, magnitude, angle);

        // Calculate the total magnitude of the flow
        var totalMagnitude = CvInvoke.Sum(magnitude).V0;

        // Calculate the maximum possible total movement (diagonal length * number of pixels)
        var maxTotalMagnitude = Math.Sqrt(Math.Pow(frame1.Rows, 2) + Math.Pow(frame1.Cols, 2)) * frame1.Rows * frame1.Cols;

        // Calculate the total movement as a percentage of the total possible movement
        var percentMovement = totalMagnitude / maxTotalMagnitude;

        // Return the percentage of movement, rounded to 3 decimal places
        return Math.Round(percentMovement, 3);
    }
}
