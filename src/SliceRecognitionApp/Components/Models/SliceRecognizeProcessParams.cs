namespace SliceRecognitionApp.Components.Models;

public record SliceRecognizeProcessParams
{
    public int? Iterations { get; init; }
    public double? SlopeFactor { get; init; }
    public double? HugFactor { get; init; }
    public int? Seed { get; init; }
}

public class SliceRecognizeProcessException : Exception
{
    public SliceRecognizeProcessException(string message, string? processErrorOutput = null)
        : base(message)
    {
        ProcessErrorOutput = processErrorOutput;
    }

    public string? ProcessErrorOutput { get; }
}