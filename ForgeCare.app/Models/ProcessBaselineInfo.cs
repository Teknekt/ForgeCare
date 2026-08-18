namespace ForgeCare.App.Models;

public class ProcessBaselineInfo
{
    public string Name { get; set; } =
        string.Empty;

    public int SeenCount { get; set; }

    public int SampleCount { get; set; }

    public double AverageCpuPercent { get; set; }

    public double AverageMemoryMb { get; set; }

    public int AveragePressureScore { get; set; }

    public int PeakPressureScore { get; set; }

    public string Pattern { get; set; } =
        string.Empty;

    public string Confidence { get; set; } =
        string.Empty;

    public string DisplayOccurrence =>
        $"{SeenCount}/{SampleCount} runs";

    public string DisplayAverageCpu =>
        $"{AverageCpuPercent:0.0}%";

    public string DisplayAverageMemory =>
        AverageMemoryMb >= 1024
            ? $"{AverageMemoryMb / 1024d:0.00} GB"
            : $"{AverageMemoryMb:0} MB";
}