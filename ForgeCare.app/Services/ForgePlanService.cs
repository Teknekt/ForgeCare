using System.Collections.Generic;
using System.Linq;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public class ForgePlanService
{
    public ForgePlanResult Build(
        SystemSnapshot? snapshot,
        HealthResult? health,
        CleanupResult? cleanup,
        StartupImpactResult? startup,
        StorageAnalysisResult? storage,
        DuplicateScanResult? duplicates,
        ServiceAnalysisResult? services,
        OptimizationResult? optimization)
    {
        var items =
            new List<ForgePlanItem>();

        if (cleanup != null &&
            cleanup.TotalFiles > 0)
        {
            items.Add(
                new ForgePlanItem
                {
                    Category = "CLEANUP",
                    Title = "Recover temporary storage",
                    Description =
                        $"{cleanup.TotalFiles:N0} temporary/cache files are available for cleanup. Existing Cleanup Review safety rules remain in control.",
                    Risk = "LOW",
                    Value = cleanup.DisplayTotalSize,
                    Route = "CLEANUP",
                    Priority = 95,
                    CanExecute = true,
                    IsSelected = true,
                    ActionLabel = "OPEN CLEANUP REVIEW"
                });
        }

        if (duplicates != null &&
            duplicates.DuplicateGroupCount > 0)
        {
            items.Add(
                new ForgePlanItem
                {
                    Category = "DUPLICATES",
                    Title = "Review exact duplicate copies",
                    Description =
                        $"{duplicates.DuplicateGroupCount} SHA-256 confirmed duplicate groups can be reviewed. ForgeCare preserves at least one copy per group.",
                    Risk = "LOW",
                    Value = duplicates.DisplayReclaimable,
                    Route = "DUPLICATES",
                    Priority = 90,
                    CanExecute = true,
                    IsSelected = true,
                    ActionLabel = "OPEN DUPLICATE REVIEW"
                });
        }

        if (storage != null &&
            storage.LargeFileCount > 0)
        {
            items.Add(
                new ForgePlanItem
                {
                    Category = "STORAGE",
                    Title = "Review large-file storage",
                    Description =
                        $"{storage.LargeFileCount} large files were found. These are review candidates, not automatic cleanup targets.",
                    Risk = "MEDIUM",
                    Value = storage.DisplayLargeFileSize,
                    Route = "STORAGE",
                    Priority = 75,
                    CanExecute = true,
                    IsSelected = false,
                    ActionLabel = "OPEN LARGE-FILE REVIEW"
                });
        }

        if (startup != null &&
            startup.DisableCandidateCount > 0)
        {
            items.Add(
                new ForgePlanItem
                {
                    Category = "STARTUP",
                    Title = "Reduce startup load",
                    Description =
                        $"{startup.DisableCandidateCount} startup entries are classified as good manual-review candidates. Changes use ForgeCare's reversible startup workflow.",
                    Risk = "MEDIUM",
                    Value = $"{startup.DisableCandidateCount} candidates",
                    Route = "STARTUP",
                    Priority = 82,
                    CanExecute = true,
                    IsSelected = false,
                    ActionLabel = "OPEN STARTUP REVIEW"
                });
        }

        if (services != null &&
            services.ReviewCount > 0)
        {
            items.Add(
                new ForgePlanItem
                {
                    Category = "SERVICES",
                    Title = "Inspect optional service candidates",
                    Description =
                        $"{services.ReviewCount} services were classified for contextual review. Sprint 8B keeps this read-only and does not alter service configuration.",
                    Risk = "HIGH",
                    Value = $"{services.ReviewCount} review",
                    Route = "SERVICES",
                    Priority = 55,
                    CanExecute = false,
                    IsSelected = false,
                    ActionLabel = "READ-ONLY REVIEW"
                });
        }

        if (health != null &&
            health.StorageFreePercent < 15)
        {
            items.Add(
                new ForgePlanItem
                {
                    Category = "SYSTEM",
                    Title = "Restore storage headroom",
                    Description =
                        $"System-drive free space is {health.StorageFreePercent:0.0}%. Prioritize Cleanup, Duplicates and Storage Review before advanced tuning.",
                    Risk = "LOW",
                    Value = $"{health.StorageFreePercent:0.0}% free",
                    Route = "STORAGE",
                    Priority = 100,
                    CanExecute = false,
                    IsSelected = false,
                    ActionLabel = "GUIDANCE"
                });
        }

        if (health != null &&
            health.MemoryAvailablePercent < 20)
        {
            items.Add(
                new ForgePlanItem
                {
                    Category = "MEMORY",
                    Title = "Investigate memory pressure",
                    Description =
                        $"Only {health.MemoryAvailablePercent:0.0}% memory was available during the system profile. Use Deep Analysis before changing configuration.",
                    Risk = "MEDIUM",
                    Value = $"{health.MemoryAvailablePercent:0.0}% available",
                    Route = "ANALYSIS",
                    Priority = 88,
                    CanExecute = false,
                    IsSelected = false,
                    ActionLabel = "RUN / REVIEW ANALYSIS"
                });
        }

        if (optimization != null)
        {
            foreach (var recommendation in
                     optimization.Recommendations
                         .OrderByDescending(x =>
                             x.EstimatedImpact)
                         .Take(3))
            {
                bool alreadyRepresented =
                    items.Any(item =>
                        item.Title.Contains(
                            recommendation.Title,
                            System.StringComparison.OrdinalIgnoreCase));

                if (alreadyRepresented)
                    continue;

                items.Add(
                    new ForgePlanItem
                    {
                        Category =
                            string.IsNullOrWhiteSpace(
                                recommendation.Category)
                                ? "OPTIMIZE"
                                : recommendation.Category.ToUpperInvariant(),

                        Title =
                            recommendation.Title,

                        Description =
                            recommendation.Description,

                        Risk =
                            recommendation.Severity ==
                            OptimizationSeverity.Important
                                ? "MEDIUM"
                                : "LOW",

                        Value =
                            $"Impact {recommendation.EstimatedImpact}",

                        Route =
                            "OPTIMIZE",

                        Priority =
                            60 + recommendation.EstimatedImpact,

                        CanExecute =
                            false,

                        IsSelected =
                            false,

                        ActionLabel =
                            "REVIEW RECOMMENDATION"
                    });
            }
        }

        return new ForgePlanResult
        {
            Items =
                items
                    .OrderByDescending(item =>
                        item.Priority)
                    .ThenBy(item =>
                        item.Risk)
                    .ToList()
        };
    }
}
