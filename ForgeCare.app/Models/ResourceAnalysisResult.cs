using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace ForgeCare.App.Models;

public class ResourceAnalysisResult
{
    private IReadOnlyList<ProcessInstanceObservation> _processObservations =
        Array.Empty<ProcessInstanceObservation>();

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

    [JsonIgnore]
    public IReadOnlyList<ProcessInstanceObservation> ProcessObservations
    {
        get => _processObservations;
        set => _processObservations = new ReadOnlyCollection<ProcessInstanceObservation>(
            new List<ProcessInstanceObservation>(value ?? Array.Empty<ProcessInstanceObservation>()));
    }

    public List<ResourceInsight> Insights { get; set; } =
        new();
}
