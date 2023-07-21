// See https://aka.ms/new-console-template for more information
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkyRemoval;
Console.WriteLine("Setting up model...");

var model = new Model();

await model.Setup();

var path = "input.png";

var extension = Path.GetExtension(path);
var fileName = Path.GetFileNameWithoutExtension(path);

var maskPath = $"{fileName}_mask{extension}";

var output = await model.Run(Image.Load<Rgb24>(path));
output.Save(maskPath);
