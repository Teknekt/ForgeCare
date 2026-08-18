using System;
using System.Collections.Generic;
using System.Linq;

namespace ForgeCare.App.Models;

public class ServiceAnalysisResult
{
    public List<ServiceInfo> Services { get; set; } =
        new();

    public List<ServiceInsight> Insights { get; set; } =
        new();

    public DateTime AnalysisTime { get; set; }

    public int TotalCount =>
        Services.Count;

    public int RunningCount =>
        Services.Count(service =>
            service.Status == "RUNNING");

    public int StoppedCount =>
        Services.Count(service =>
            service.Status == "STOPPED");

    public int AutomaticCount =>
        Services.Count(service =>
            service.StartupType.StartsWith(
                "Automatic",
                System.StringComparison.OrdinalIgnoreCase));

    public int ManualCount =>
        Services.Count(service =>
            service.StartupType == "Manual");

    public int DisabledCount =>
        Services.Count(service =>
            service.StartupType == "Disabled");

    public int CriticalCount =>
        Services.Count(service =>
            service.Category == "SYSTEM CRITICAL" ||
            service.Category == "SECURITY");

    public int ReviewCount =>
        Services.Count(service =>
            service.Recommendation == "REVIEW IF UNUSED" ||
            service.Recommendation == "REVIEW CAREFULLY" ||
            service.Recommendation == "POTENTIALLY OPTIONAL");
}