using System.IO.Compression;
namespace SkyRemoval;

public static class ModelSetup
{
    private const string url = "https://github.com/OpenDroneMap/SkyRemoval/releases/download/v1.0.6/model.zip";
    private static async Task<bool> DownloadAndExtractModel(Uri modelUri, string modelFolder)
    {
        try
        {
            var httpClient = new HttpClient();
            await using var memoryStream = new MemoryStream();
            await using var stream = await httpClient.GetStreamAsync(modelUri);
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0; // Reset position to the beginning of the stream

            // Extract directly from the MemoryStream
            using var zip = new ZipArchive(memoryStream);

            zip.ExtractToDirectory(modelFolder);
        }
        catch (Exception e)
        {
            throw new Exception("Failed to download and extract model", e);

        }

        return true;
    }

    internal static async Task<string?> SetupModel(string modelUrl = url)
    {
        const string modelFolder = "model";

        if (!Directory.Exists(modelFolder))
            Directory.CreateDirectory(modelFolder);


        foreach (var file in Directory.EnumerateFiles(modelFolder))
        {
            File.Delete(file);
        }

        var status = await DownloadAndExtractModel(new Uri(modelUrl), modelFolder);

        if (!status)
        {
            throw new Exception("Failed to download and extract model");
        }

        // Look for the ONNX file
        return Directory.GetFiles(modelFolder, "*.onnx").FirstOrDefault();

    }
}
