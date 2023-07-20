using System.Numerics;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NumSharp;

namespace SkyRemoval
{
    public class Model
    {
        private const int ModelWidth = 384;
        private const int ModelHeight = 384;

        private string? _modelPath;

        public async Task<bool> Setup()
        {
            _modelPath = await ModelSetup.SetupModel();
            return _modelPath != null;
        }

        public async Task<Image<Rgba32>> Run(Image<Rgba32> image)
        {
            if (_modelPath == null)
            {
                throw new Exception("Model not setup");
            }

            var originalWidth = image.Width;
            var originalHeight = image.Height;

            image.Mutate(x =>
            {
                x.ProcessPixelRowsAsVector4(NormalizePixelRow);
                x.Resize(ModelWidth, ModelHeight);
            });

            var npImg = ImageToNdArray(image);

            npImg = TransposeExpandNdArray(npImg);
            var output = RunOnnxSession(npImg);

            return NdArrayToImage(output);

            //    var finalOutput = ProcessOutput(output, originalWidth, originalHeight);

            //  return finalOutput;
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

            //return npImg;
            return npImg.reshape(1, npImg.shape[0], npImg.shape[1], npImg.shape[2]);
        }

        private NDArray RunOnnxSession(NDArray npImg)
        {
            var session = new InferenceSession(_modelPath);

            var data = npImg.Data<float>().ToArray();
            var tensor = new DenseTensor<float>(data, npImg.shape);

            var inputName = session.InputNames[0];

            var input = NamedOnnxValue.CreateFromTensor(inputName, tensor);
            var onnxOutput = session.Run(new[]
            {
                input
            });
            var outputTensor = onnxOutput.First().AsTensor<float>();
            var flatArray = outputTensor.ToArray();
            var shape = tensor.Dimensions.ToArray();
            return new NDArray(flatArray, shape);
        }

        private static Image<Rgba32> ProcessOutput(NDArray output, int originalWidth, int originalHeight)
        {
            output = output[0, 0].transpose(new[]
            {
                1,
                2,
                0
            });
            var img = ResizeImage(output, originalWidth, originalHeight);
            output = ImageToNdArray(img);
            output = np.array(new[]
            {
                output,
                output,
                output
            }).transpose(new[]
            {
                1,
                2,
                0
            });
            output = np.clip(output, 0.0f, 1.0f);
            return NdArrayToImage(output);
        }

        private static Image<Rgba32> ResizeImage(NDArray output, int originalWidth, int originalHeight)
        {
            using var img = NdArrayToImage(output);
            img.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(originalWidth, originalHeight),
                Sampler = KnownResamplers.Lanczos3
            }));

            return img;
        }

        private static NDArray ImageToNdArray(Image<Rgba32> image)
        {
            var height = image.Height;
            var width = image.Width;
            var channels = image.PixelType.BitsPerPixel / 8;
            var npImg = new NDArray(typeof(float), new Shape(height, width, channels));

            for (var i = 0; i < height; i++)
            {
                for (var j = 0; j < width; j++)
                {
                    var pixel = image[i, j];
                    npImg[i, j, 0] = pixel.R / 255f;
                    npImg[i, j, 1] = pixel.G / 255f;
                    npImg[i, j, 2] = pixel.B / 255f;
                    npImg[i, j, 3] = pixel.A / 255f;
                }
            }

            return npImg;
        }

        private static Image<Rgba32> NdArrayToImage(NDArray ndArray)
        {
            var height = ndArray.shape[0];
            var width = ndArray.shape[1];
            var img = new Image<Rgba32>(Configuration.Default, width, height);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    img[x, y] = new Rgba32(
                        (byte)(ndArray[y, x, 0] * 255),
                        (byte)(ndArray[y, x, 1] * 255),
                        (byte)(ndArray[y, x, 2] * 255),
                        (byte)(ndArray[y, x, 3] * 255)
                    );
                }
            }

            return img;
        }
    }
}
