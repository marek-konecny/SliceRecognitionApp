namespace SliceRecognitionApp.Components.Services;

using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SliceRecognitionApp.Components.Models;

public class SliceRecognizeProcessService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SliceRecognizeProcessService> _logger;
    private readonly string _executablePath;

    private const string ExecutableName = "slice_localize";

    public SliceRecognizeProcessService(IWebHostEnvironment env, ILogger<SliceRecognizeProcessService> logger)
    {
        _env = env;
        _logger = logger;

        //_executablePath = Path.Combine(_env.ContentRootPath, "Components", "Tools", ExecutableName);
        _executablePath = ExecutableName;
    }

    /// <param name="inputImageBytes">The raw bytes of the input PNG image.</param>
    /// <param name="parameters">Optional parameters for the processing tool.</param>
    /// <returns>A Base64 encoded string of the resulting PNG image.</returns>
    /// <exception cref="FileNotFoundException">If the executable is not found.</exception>
    /// <exception cref="SliceRecognizeProcessException">If the process fails or does not produce an output file.</exception>
    public async Task<string> ProcessImageAsync(byte[] inputImageBytes, SliceRecognizeProcessParams parameters)
    {
        /* if (!File.Exists(_executablePath))
        {
            _logger.LogError("Executable not found: {ExecutablePath}", _executablePath);
            throw new FileNotFoundException($"Spustitelný soubor neexistuje.", _executablePath);
        } */

        // Create unique temporary file paths to handle concurrent requests safely
        var tempId = Guid.NewGuid().ToString("N");
        var tempInputDir = Path.Combine(Path.GetTempPath(), "YourAppTemp");
        Directory.CreateDirectory(tempInputDir); // Ensure the directory exists

        var inputPath = Path.Combine(tempInputDir, $"{tempId}_in.png");
        var outputPath = Path.Combine(tempInputDir, $"{tempId}_out.png");

        try
        {
            // 1. Write the input image from memory to a temporary file
            await File.WriteAllBytesAsync(inputPath, inputImageBytes);

            // 2. Build the command line arguments
            var arguments = BuildArguments(inputPath, outputPath, parameters);

            // 3. Configure and start the process
            var processStartInfo = new ProcessStartInfo
            {
                FileName = _executablePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(_executablePath) // Set working directory
            };

            _logger.LogInformation("Executing command: \"{FileName}\" {Arguments}", processStartInfo.FileName, processStartInfo.Arguments);

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                throw new SliceRecognizeProcessException("Spuštění algoritmu selhalo.");
            }

            // 4. Wait for the process to finish and capture output
            var errorOutput = await process.StandardError.ReadToEndAsync();
            var standardOutput = await process.StandardOutput.ReadToEndAsync(); // Good for debugging
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("\"{FileName}\" failed with exit code {ExitCode}. Error: {ErrorOutput}", processStartInfo.FileName, process.ExitCode, errorOutput);
                throw new SliceRecognizeProcessException($"Algoritmus selhal s chybovým kódem {process.ExitCode}.", errorOutput);
            }

            _logger.LogInformation("\"{FileName}\" stdout:\n{StandardOutput}", processStartInfo.FileName, standardOutput);

            // 5. Read the output file and convert to Base64
            if (!File.Exists(outputPath))
            {
                throw new SliceRecognizeProcessException("Výstupní soubor nenalezen.", errorOutput);
            }

            var outputBytes = await File.ReadAllBytesAsync(outputPath);
            return Convert.ToBase64String(outputBytes);
        }
        finally
        {
            // 6. Clean up temporary files regardless of success or failure
            if (File.Exists(inputPath)) File.Delete(inputPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    private string BuildArguments(string inputPath, string outputPath, SliceRecognizeProcessParams parameters)
    {
        var sb = new StringBuilder();

        // Append optional parameters if they have been provided
        if (parameters.Iterations.HasValue) sb.Append($"--iterations {parameters.Iterations.Value} ");
        if (parameters.SlopeFactor.HasValue) sb.Append($"--slopeFactor {parameters.SlopeFactor.Value} ");
        if (parameters.HugFactor.HasValue) sb.Append($"--hugFactor {parameters.HugFactor.Value} ");
        if (parameters.Seed.HasValue) sb.Append($"--seed {parameters.Seed.Value} ");

        // Append mandatory input path and the output path.
        sb.Append($"\"{inputPath}\" ");
        sb.Append($"\"{outputPath}\"");

        return sb.ToString();
    }
}