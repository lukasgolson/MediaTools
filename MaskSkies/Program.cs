// See https://aka.ms/new-console-template for more information
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkyRemoval;
Console.WriteLine("Setting up model...");

var model = new Model();

await model.Setup();

var output = await model.Run(Image.Load<Rgba32>("input.png"));
output.Save("output.jpg");
