using System;
using System.Collections.Generic;

namespace ForgeCare.App.Models;

public class ResourceHistorySample
{
    public DateTime CapturedAt { get; set; }

    public double CpuUsagePercent { get; set; }

    public double MemoryUsedPercent { get; set; }

    public int ProcessCount { get; set; }

    public string OverallPressure { get; set; } =
        string.Empty;

    public List<ResourceHistoryProcessSample> Processes { get; set; } =
        new();
}

public class ResourceHistoryProcessSample
{
    public string Name { get; set; } =
        string.Empty;

    public double CpuPercent { get; set; }

    public double MemoryMb { get; set; }

    public int PressureScore { get; set; }
}