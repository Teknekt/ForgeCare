using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public class OptimizationService
{
    public OptimizationResult Analyze(
        SystemSnapshot snapshot,
        HealthResult health)
    {
        var result =
            new OptimizationResult();

        AnalyzeStartup(
            snapshot,
            health,
            result);

        AnalyzeStorage(
            health,
            result);

        AnalyzeMemory(
            health,
            result);

        AddGeneralRecommendations(
            result);

        return result;
    }

    private static void AnalyzeStartup(
        SystemSnapshot snapshot,
        HealthResult health,
        OptimizationResult result)
    {
        if (health.StartupCount > 25)
        {
            result.Recommendations.Add(
                new OptimizationRecommendation
                {
                    Title = "Reduce startup load",

                    Description =
                        $"{health.StartupCount} startup items were detected. " +
                        "Reviewing non-essential startup applications may " +
                        "reduce boot time and background resource usage.",

                    Category =
                        "STARTUP",

                    Severity =
                        OptimizationSeverity.Important,

                    EstimatedImpact =
                        35
                });

            return;
        }

        if (health.StartupCount > 15)
        {
            result.Recommendations.Add(
                new OptimizationRecommendation
                {
                    Title = "Review startup applications",

                    Description =
                        $"{health.StartupCount} startup items are configured. " +
                        "Some applications may not need to start with Windows.",

                    Category =
                        "STARTUP",

                    Severity =
                        OptimizationSeverity.Recommended,

                    EstimatedImpact =
                        22
                });
        }
        else
        {
            result.Recommendations.Add(
                new OptimizationRecommendation
                {
                    Title = "Startup configuration looks healthy",

                    Description =
                        $"{health.StartupCount} startup items were detected.",

                    Category =
                        "STARTUP",

                    Severity =
                        OptimizationSeverity.Info,

                    EstimatedImpact =
                        0
                });
        }
    }

    private static void AnalyzeStorage(
        HealthResult health,
        OptimizationResult result)
    {
        if (health.StorageFreePercent < 10)
        {
            result.Recommendations.Add(
                new OptimizationRecommendation
                {
                    Title = "Recover system drive space",

                    Description =
                        $"Only {health.StorageFreePercent:0.0}% free space remains " +
                        "on the system drive. Low storage headroom can affect " +
                        "updates, caching and overall system responsiveness.",

                    Category =
                        "STORAGE",

                    Severity =
                        OptimizationSeverity.Important,

                    EstimatedImpact =
                        30
                });

            return;
        }

        if (health.StorageFreePercent < 20)
        {
            result.Recommendations.Add(
                new OptimizationRecommendation
                {
                    Title = "Increase storage headroom",

                    Description =
                        $"{health.StorageFreePercent:0.0}% of the system drive " +
                        "is currently free. Creating additional headroom is recommended.",

                    Category =
                        "STORAGE",

                    Severity =
                        OptimizationSeverity.Recommended,

                    EstimatedImpact =
                        18
                });
        }
        else
        {
            result.Recommendations.Add(
                new OptimizationRecommendation
                {
                    Title = "Storage headroom looks good",

                    Description =
                        $"{health.StorageFreePercent:0.0}% free space is available.",

                    Category =
                        "STORAGE",

                    Severity =
                        OptimizationSeverity.Info,

                    EstimatedImpact =
                        0
                });
        }
    }

    private static void AnalyzeMemory(
        HealthResult health,
        OptimizationResult result)
    {
        if (health.MemoryAvailablePercent < 10)
        {
            result.Recommendations.Add(
                new OptimizationRecommendation
                {
                    Title = "High memory pressure detected",

                    Description =
                        $"Only {health.MemoryAvailablePercent:0.0}% of physical memory " +
                        "is currently available. Background applications should be reviewed.",

                    Category =
                        "MEMORY",

                    Severity =
                        OptimizationSeverity.Important,

                    EstimatedImpact =
                        25
                });

            return;
        }

        if (health.MemoryAvailablePercent < 25)
        {
            result.Recommendations.Add(
                new OptimizationRecommendation
                {
                    Title = "Review background memory usage",

                    Description =
                        $"{health.MemoryAvailablePercent:0.0}% of memory is currently available.",

                    Category =
                        "MEMORY",

                    Severity =
                        OptimizationSeverity.Recommended,

                    EstimatedImpact =
                        12
                });
        }
        else
        {
            result.Recommendations.Add(
                new OptimizationRecommendation
                {
                    Title = "Memory headroom looks healthy",

                    Description =
                        $"{health.MemoryAvailablePercent:0.0}% of physical memory is available.",

                    Category =
                        "MEMORY",

                    Severity =
                        OptimizationSeverity.Info,

                    EstimatedImpact =
                        0
                });
        }
    }

    private static void AddGeneralRecommendations(
        OptimizationResult result)
    {
        result.Recommendations.Add(
            new OptimizationRecommendation
            {
                Title = "Keep Windows and drivers current",

                Description =
                    "Current Windows updates and vendor drivers can improve " +
                    "stability, compatibility and security.",

                Category =
                    "SYSTEM",

                Severity =
                    OptimizationSeverity.Info,

                EstimatedImpact =
                    5
            });
    }
}