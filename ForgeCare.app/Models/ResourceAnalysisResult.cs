using System;
using System.Collections.Generic;

namespace ForgeCare.App.Models;

public class ResourceAnalysisResult
{
    public double CpuUsagePercent { get; set; }

    public double MemoryUsedPercent { get; set; }

    public double UsedMemoryGb { get; set; }

    public double AvailableMemoryGb { get; set; }

    public double TotalMemoryGb { get; set; }

    public int ProcessCount { get; set; }

    public int HighCpuProcessCount { get; set; }

    public int HighMemoryProcessCount { get; set; }

    public string CpuStatus { get; set; } =
        string.Empty;

    public string MemoryStatus { get; set; } =
        string.Empty;

    public string ProcessStatus { get; set; } =
        string.Empty;

    public string OverallPressure { get; set; } =
        string.Empty;

    public DateTime AnalysisTime { get; set; }

    public List<ResourceProcessInfo> TopProcesses { get; set; } =
        new();

    public List<ResourceInsight> Insights { get; set; } =
        new();
}