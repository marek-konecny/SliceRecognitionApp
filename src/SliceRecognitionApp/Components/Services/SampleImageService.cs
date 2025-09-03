namespace SliceRecognitionApp.Components.Services;

using Microsoft.AspNetCore.Hosting;

public record SampleImage(string LowResUrl, string FullResUrl);

public class SampleImageService
{
    private readonly IWebHostEnvironment _env;

    public SampleImageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public IEnumerable<SampleImage> GetSampleImages()
    {
        var folder = Path.Combine(_env.WebRootPath, "sampleimgs");
        var lowresFolder = Path.Combine(folder, "lowres");

        if (!Directory.Exists(folder))
            yield break;

        foreach (var file in Directory.EnumerateFiles(folder))
        {
            var fileName = Path.GetFileName(file);
            var lowresPath = Path.Combine(lowresFolder, fileName);

            yield return new SampleImage(
                LowResUrl: File.Exists(lowresPath) ? $"sampleimgs/lowres/{fileName}" : $"sampleimgs/{fileName}",
                FullResUrl: $"sampleimgs/{fileName}"
            );
        }
    }
}
