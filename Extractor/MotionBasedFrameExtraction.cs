using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Extractor.Extensions;
using FFMediaToolkit.Graphics;
namespace Extractor;

public static class MotionBasedFrameExtraction
{
    // Calculates the amount of frame overlap between two frames using EmGU CV
    public static double CalculateFrameMovement(ImageData previousFrame, ImageData currentFrame)
    {
        using var frame1 = previousFrame.ToEmguBitmap().Convert<Gray, byte>();
        using var frame2 = currentFrame.ToEmguBitmap().Convert<Gray, byte>();

        using var flow = new Mat();

        CvInvoke.CalcOpticalFlowFarneback(frame1, frame2, flow, 0.5, 3, 15, 3, 5, 1.1, 0);


        using var flowX = new Mat(frame1.Size, DepthType.Cv32F, 1);
        using var flowY = new Mat(frame1.Size, DepthType.Cv32F, 1);
        CvInvoke.Split(flow, new VectorOfMat(flowX, flowY));


        using var magnitude = new Mat();
        using var angle = new Mat();
        CvInvoke.CartToPolar(flowX, flowY, magnitude, angle);

        var totalMagnitude = CvInvoke.Sum(magnitude).V0;

        // Calculate maximum possible total movement (diagonal length * number of pixels)
        var maxTotalMagnitude = Math.Sqrt(Math.Pow(frame1.Rows, 2) + Math.Pow(frame1.Cols, 2)) * frame1.Rows * frame1.Cols;

        // Calculate the total movement as a percentage of the total possible movement
        var percentMovement = totalMagnitude / maxTotalMagnitude;

        return Math.Round(percentMovement, 3);
    }


    // Theory adapted from https://stackoverflow.com/a/20198278
    public static double CalculateImageSharpness(ImageData frame)
    {
        using var greyscaleImage = frame.ToEmguBitmap().Convert<Gray, byte>();

        using var cannyFilteredImage = greyscaleImage.Canny(255, 175);

        var imageEdgeCount = cannyFilteredImage.CountNonzero()[0];

        double imagePixelCount = cannyFilteredImage.Cols * cannyFilteredImage.Rows;
        var sharpness = imageEdgeCount / imagePixelCount * 100;

        return sharpness;
    }
}
