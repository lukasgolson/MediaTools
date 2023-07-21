using System.Numerics;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.XImgproc;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NumSharp;


namespace SkyRemoval
{
    public class SkyRemovalModel
    {
        private const int ModelWidth = 384;
        private const int ModelHeight = 384;

        private static string? _modelPath;

        private readonly InferenceSession _session;

        // Make the constructor private to prevent direct construction calls.
        private SkyRemovalModel(string modelPath)
        {
            _session = new InferenceSession(modelPath);
        }

        private static SkyRemovalModel? _instance;
        private static readonly SemaphoreSlim Semaphore = new(1, 1);

        public static async Task<SkyRemovalModel> CreateAsync()
        {
            if (_instance != null)
                return _instance;

            await Semaphore.WaitAsync();

            try
            {
                if (_instance != null)
                {
                    return _instance;
                }

                if (string.IsNullOrEmpty(_modelPath))
                    _modelPath = await ModelSetup.SetupModel();

                if (string.IsNullOrEmpty(_modelPath))
                {
                    throw new Exception("Model not setup");
                }

                _instance = new SkyRemovalModel(_modelPath);
            }
            finally
            {
                Semaphore.Release();
            }

            return _instance;
        }

        public async Task<Image<Rgb24>> Run(Image<Rgb24> image)
        {
            if (_modelPath == null)
            {
                throw new Exception("Model not setup");
            }

            var originalWidth = image.Width;
            var originalHeight = image.Height;

            var processingImage = image.Clone(x =>
            {
                x.ProcessPixelRowsAsVector4(NormalizePixelRow);
                x.Resize(new ResizeOptions
                {
                    Size = new Size(ModelWidth, ModelHeight),
                    Mode = ResizeMode.Stretch,
                    Sampler = KnownResamplers.Box
                });

            });

            var npImg = TransposeExpandNdArray(ImageToNdArray(processingImage));

            var output = RunOnnxSession(npImg);

            output.Mutate(x =>
            {
                x.ProcessPixelRowsAsVector4(pixelRow => Clip(pixelRow, 0f, 1f));

                x.Resize(new ResizeOptions
                {
                    Size = new Size(originalWidth, originalHeight),
                    Mode = ResizeMode.Stretch,
                    Sampler = KnownResamplers.Lanczos3
                });


            });



            var inputSrc = image.ConvertToEmguCv().Mat;
            var outputSrc = output.ConvertToEmguCv().Mat;

            var dst = new Mat();


            XImgprocInvoke.GuidedFilter(outputSrc, inputSrc, dst, 20, 0.01f);

            output = dst.ToImage<Bgr, Byte>().ConvertToImageSharp();


            output.Mutate(x =>
            {
                x.ProcessPixelRowsAsVector4(pixelRow => Clip(pixelRow, 0f, 1f));
                x.Grayscale();
                x.BinaryThreshold(0.5f);
            });

            return output;
        }
        private static void Clip(Span<Vector4> pixelRow, float min = 0, float max = 1)
        {

            foreach (ref var pixel in pixelRow)
            {
                pixel.X = Math.Clamp(pixel.X, min, max);
                pixel.Y = Math.Clamp(pixel.Y, min, max);
                pixel.Z = Math.Clamp(pixel.Z, min, max);
                pixel.W = Math.Clamp(pixel.W, min, max);
            }
        }

        private static void NormalizePixelRow(Span<Vector4> pixelRow)
        {
            foreach (ref var pixel in pixelRow)
            {
                pixel /= pixel.W;
                pixel /= 255.0f; // Convert from [0,255] to [0,1]
            }
        }



        private static NDArray TransposeExpandNdArray(NDArray npImg)
        {
            npImg = npImg.transpose(new[]
            {
                2,
                0,
                1
            });


            return npImg.reshape(1, npImg.shape[0], npImg.shape[1], npImg.shape[2]);
        }

        private Image<Rgb24> RunOnnxSession(NDArray npImg)
        {
            var tensor = NdArrayToDenseTensor(npImg);
            var inputName = _session.InputNames[0];

            var onnxOutput = _session.Run(new[]
            {
                NamedOnnxValue.CreateFromTensor(inputName, tensor)
            });

            var outputTensor = onnxOutput.First().AsTensor<float>();
            var batchSize = outputTensor.Dimensions[0];
            var channels = outputTensor.Dimensions[1];
            var height = outputTensor.Dimensions[2];
            var width = outputTensor.Dimensions[3];

            var img = new Image<Rgb24>(Configuration.Default, width, height);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pixelValue = (byte)(outputTensor[0, 0, y, x] * 255); // Using outputTensor instead of ndArray
                    img[x, y] = new Rgb24(pixelValue, pixelValue, pixelValue);
                }
            }

            return img;
        }


        private static DenseTensor<float> NdArrayToDenseTensor(NDArray npImg)
        {
            var data = npImg.Data<float>().ToArray();
            var tensor = new DenseTensor<float>(data, npImg.shape);
            return tensor;
        }



        private static NDArray ImageToNdArray(Image<Rgb24> image)
        {
            var height = image.Height;
            var width = image.Width;
            var npImg = new NDArray(typeof(float), new Shape(height, width, 3));

            for (var i = 0; i < height; i++)
            {
                for (var j = 0; j < width; j++)
                {
                    var pixel = image[j, i]; // Access image pixels in (width, height) order
                    npImg[i, j, 0] = pixel.R / 255f;
                    npImg[i, j, 1] = pixel.G / 255f;
                    npImg[i, j, 2] = pixel.B / 255f;
                }
            }

            return npImg;
        }


        private static Image<Rgb24> NdArrayToImage(NDArray ndArray)
        {
            if (ndArray.ndim != 3 || ndArray.shape[2] < 3)
            {
                throw new ArgumentException("Invalid NDArray shape. Expecting a 3D array with at least 3 channels in the third dimension.");
            }

            var height = ndArray.shape[0];
            var width = ndArray.shape[1];
            var img = new Image<Rgb24>(Configuration.Default, width, height);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    img[x, y] = new Rgb24(
                        (byte)(ndArray[y, x, 0] * 255),
                        (byte)(ndArray[y, x, 1] * 255),
                        (byte)(ndArray[y, x, 2] * 255));
                }
            }

            return img;
        }
    }
}
