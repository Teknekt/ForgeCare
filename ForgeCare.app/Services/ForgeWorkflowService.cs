using System.Collections.Generic;
using System.Linq;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public class ForgeWorkflowService
{
    public List<ForgeWorkflowStep> Build(
        bool hasProfile,
        bool hasDeepAnalysis,
        bool hasServiceAnalysis,
        bool hasStorageAnalysis,
        bool hasOptimizationAnalysis,
        bool hasDuplicateScan,
        bool hasForgePlan,
        ForgeReportSession reportSession)
    {
        bool diagnosticsComplete =
            hasDeepAnalysis &&
            hasServiceAnalysis &&
            hasStorageAnalysis;

        bool verified =
            reportSession.Checkpoints.Count >= 2;

        bool hasRecordedWork =
            reportSession.Actions.Count > 1;

        return new List<ForgeWorkflowStep>
        {
            new()
            {
                Number = 1,
                Title = "Capture system baseline",
                Description =
                    "Run System Scan. The first scan in the active report session becomes the BEFORE checkpoint.",
                Status =
                    hasProfile
                        ? "COMPLETE"
                        : "NEXT",
                Route = "DASHBOARD",
                IsRequired = true
            },

            new()
            {
                Number = 2,
                Title = "Run core diagnostics",
                Description =
                    "Run Deep Analysis, Service Intelligence and Storage Deep Scan to establish the current system state.",
                Status =
                    diagnosticsComplete
                        ? "COMPLETE"
                        : hasProfile
                            ? "NEXT"
                            : "LOCKED",
                Route =
                    !hasDeepAnalysis
                        ? "ANALYSIS"
                        : !hasServiceAnalysis
                            ? "SERVICES"
                            : "STORAGE",
                IsRequired = true
            },

            new()
            {
                Number = 3,
                Title = "Analyze optimization opportunities",
                Description =
                    "Build the optimization and startup-impact analysis from the current system profile.",
                Status =
                    hasOptimizationAnalysis
                        ? "COMPLETE"
                        : diagnosticsComplete
                            ? "NEXT"
                            : "LOCKED",
                Route = "OPTIMIZE",
                IsRequired = true
            },

            new()
            {
                Number = 4,
                Title = "Find exact duplicates",
                Description =
                    "Optional storage intelligence. Exact duplicate detection can be slower on large file sets and does not block the service workflow.",
                Status =
                    hasDuplicateScan
                        ? "COMPLETE"
                        : hasStorageAnalysis
                            ? "OPTIONAL"
                            : "LOCKED",
                Route = "STORAGE",
                IsRequired = false
            },

            new()
            {
                Number = 5,
                Title = "Build the Forge Plan",
                Description =
                    "Correlate current findings into one prioritized plan. Existing safety workflows remain authoritative.",
                Status =
                    hasForgePlan
                        ? "COMPLETE"
                        : hasOptimizationAnalysis
                            ? "NEXT"
                            : "LOCKED",
                Route = "FORGE PLAN",
                IsRequired = true
            },

            new()
            {
                Number = 6,
                Title = "Forge selected actions",
                Description =
                    "Open selected Cleanup, Startup, Storage or Duplicate actions one at a time through their existing Review → Dry Run → Confirm flow.",
                Status =
                    hasRecordedWork
                        ? "IN PROGRESS"
                        : hasForgePlan
                            ? "NEXT"
                            : "LOCKED",
                Route = "FORGE PLAN",
                IsRequired = true
            },

            new()
            {
                Number = 7,
                Title = "Verify the result",
                Description =
                    "When the service work is finished, run System Scan again to capture the CURRENT / AFTER checkpoint.",
                Status =
                    verified
                        ? "COMPLETE"
                        : hasRecordedWork
                            ? "NEXT"
                            : "LOCKED",
                Route = "DASHBOARD",
                IsRequired = true
            },

            new()
            {
                Number = 8,
                Title = "Review and export report",
                Description =
                    "Review the recorded activity and Before → Current comparison, then export the customer-facing HTML report.",
                Status =
                    verified
                        ? "READY"
                        : "LOCKED",
                Route = "REPORTS",
                IsRequired = true
            }
        };
    }

    public ForgeWorkflowSummary Summarize(List<ForgeWorkflowStep> steps)
    {
        int required = steps.Count(x => x.IsRequired);
        int complete = steps.Count(x =>
            x.IsRequired && (x.Status == "COMPLETE" || x.Status == "READY"));

        var next = steps.FirstOrDefault(x => x.Status == "NEXT")
            ?? steps.FirstOrDefault(x => x.Status == "READY")
            ?? steps.FirstOrDefault(x => x.Status == "OPTIONAL");

        bool allComplete = required > 0 && complete >= required;
        string stage = ResolveStage(steps, allComplete);

        return new ForgeWorkflowSummary
        {
            Stage = stage,
            CurrentTitle = allComplete ? "Review and deliver Forge Report"
                : next?.Title ?? "Review service workflow",
            CurrentRoute = allComplete ? "REPORTS"
                : next?.Route ?? "WORKFLOW",
            ProgressPercent = required == 0 ? 0 : (double)complete / required * 100d,
            CanContinue = next != null || allComplete,
            IsComplete = allComplete,
            StageStrip = BuildStageStrip(stage, allComplete)
        };
    }

    private static string ResolveStage(List<ForgeWorkflowStep> steps, bool allComplete)
    {
        if (allComplete) return "COMPLETE";
        if (steps.Any(x => x.Number == 7 && x.Status == "COMPLETE")) return "REPORT";
        if (steps.Any(x => x.Number == 6 && x.Status == "IN PROGRESS") ||
            steps.Any(x => x.Number == 5 && x.Status == "COMPLETE")) return "FORGE";
        if (steps.Any(x => x.Number == 2 && x.Status == "COMPLETE")) return "PLAN";
        if (steps.Any(x => x.Number == 1 && x.Status == "COMPLETE")) return "ANALYZE";
        return "SCAN";
    }

    private static string BuildStageStrip(string stage, bool complete)
    {
        if (complete)
            return "✓ SCAN   ✓ ANALYZE   ✓ PLAN   ✓ FORGE   ✓ VERIFY   ● REPORT";

        return stage switch
        {
            "SCAN" => "● SCAN   ○ ANALYZE   ○ PLAN   ○ FORGE   ○ VERIFY   ○ REPORT",
            "ANALYZE" => "✓ SCAN   ● ANALYZE   ○ PLAN   ○ FORGE   ○ VERIFY   ○ REPORT",
            "PLAN" => "✓ SCAN   ✓ ANALYZE   ● PLAN   ○ FORGE   ○ VERIFY   ○ REPORT",
            "FORGE" => "✓ SCAN   ✓ ANALYZE   ✓ PLAN   ● FORGE   ○ VERIFY   ○ REPORT",
            "REPORT" => "✓ SCAN   ✓ ANALYZE   ✓ PLAN   ✓ FORGE   ✓ VERIFY   ● REPORT",
            _ => "○ SCAN   ○ ANALYZE   ○ PLAN   ○ FORGE   ○ VERIFY   ○ REPORT"
        };
    }

}
