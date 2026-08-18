using System;
using System.Collections.Generic;

namespace ForgeCare.App.Models;

public class ResourceBaselineResult
{
    public int SampleCount { get; set; }

    public DateTime? FirstSampleAt { get; set; }

    public DateTime? LastSampleAt { get; set; }

    public double AverageCpuPercent { get; set; }

    public double AverageMemoryUsedPercent { get; set; }

    public double LatestCpuPercent { get; set; }

    public double LatestMemoryUsedPercent { get; set; }

    public string CpuTrend { get; set; } =
        "LEARNING";

    public string MemoryTrend { get; set; } =
        "LEARNING";

    public string BaselineState { get; set; } =
        "LEARNING";

    public string Confidence { get; set; } =
        "LOW";

    public List<ProcessBaselineInfo> PersistentProcesses { get; set; } =
        new();
}