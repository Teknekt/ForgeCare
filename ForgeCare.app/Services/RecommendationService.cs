using System.Collections.Generic;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public class RecommendationService
{
    public List<Recommendation> Generate(
        SystemSnapshot snapshot,
        HealthResult health)
    {
        var recommendations =
            new List<Recommendation>();


        // =========================
        // STORAGE
        // =========================

        if (health.StorageFreePercent < 10)
        {
            recommendations.Add(
                new Recommendation
                {
                    Title = "CRITICALLY LOW DISK SPACE",

                    Description =
                        $"Only {health.StorageFreePercent:0.0}% " +
                        "of the system drive is free. " +
                        "Freeing disk space is strongly recommended.",

                    Severity =
                        RecommendationSeverity.Critical
                });
        }
        else if (health.StorageFreePercent < 15)
        {
            recommendations.Add(
                new Recommendation
                {
                    Title = "FREE UP DISK SPACE",

                    Description =
                        $"Your system drive only has " +
                        $"{health.StorageFreePercent:0.0}% free space remaining. " +
                        "Consider cleaning temporary files or unused data.",

                    Severity =
                        RecommendationSeverity.Attention
                });
        }
        else if (health.StorageFreePercent < 25)
        {
            recommendations.Add(
                new Recommendation
                {
                    Title = "STORAGE HEADROOM IS GETTING LOW",

                    Description =
                        $"{health.StorageFreePercent:0.0}% of the system drive " +
                        "is currently available.",

                    Severity =
                        RecommendationSeverity.Info
                });
        }
        else
        {
            recommendations.Add(
                new Recommendation
                {
                    Title = "STORAGE LOOKS GOOD",

                    Description =
                        $"{health.StorageFreePercent:0.0}% of the system drive " +
                        "is available.",

                    Severity =
                        RecommendationSeverity.Healthy
                });
        }


        // =========================
        // STARTUP
        // =========================

        if (health.StartupCount > 25)
        {
            recommendations.Add(
                new Recommendation
                {
                    Title = "HEAVY STARTUP LOAD",

                    Description =
                        $"{health.StartupCount} startup items were detected. " +
                        "Reviewing unnecessary programs may improve startup time.",

                    Severity =
                        RecommendationSeverity.Critical
                });
        }
        else if (health.StartupCount > 15)
        {
            recommendations.Add(
                new Recommendation
                {
                    Title = "REVIEW STARTUP ITEMS",

                    Description =
                        $"{health.StartupCount} startup items were detected. " +
                        "Some may not need to launch automatically with Windows.",

                    Severity =
                        RecommendationSeverity.Attention
                });
        }
        else if (health.StartupCount > 8)
        {
            recommendations.Add(
                new Recommendation
                {
                    Title = "STARTUP LOAD IS MODERATE",

                    Description =
                        $"{health.StartupCount} startup items were detected.",

                    Severity =
                        RecommendationSeverity.Info
                });
        }
        else
        {
            recommendations.Add(
                new Recommendation
                {
                    Title = "STARTUP LOOKS CLEAN",

                    Description =
                        $"Only {health.StartupCount} startup items were detected.",

                    Severity =
                        RecommendationSeverity.Healthy
                });
        }


        // =========================
        // MEMORY
        // =========================

        if (health.MemoryAvailablePercent < 8)
        {
            recommendations.Add(
                new Recommendation
                {
                    Title = "MEMORY PRESSURE DETECTED",

                    Description =
                        $"Only {health.MemoryAvailablePercent:0.0}% " +
                        "of physical memory is currently available.",

                    Severity =
                        RecommendationSeverity.Critical
                });
        }
        else if (health.MemoryAvailablePercent < 15)
        {
            recommendations.Add(
                new Recommendation
                {
                    Title = "HIGH MEMORY USAGE",

                    Description =
                        $"{health.MemoryAvailablePercent:0.0}% " +
                        "of physical memory is currently available.",

                    Severity =
                        RecommendationSeverity.Attention
                });
        }
        else if (health.MemoryAvailablePercent < 40)
        {
            recommendations.Add(
                new Recommendation
                {
                    Title = "MEMORY USAGE IS NORMAL",

                    Description =
                        $"{health.MemoryAvailablePercent:0.0}% " +
                        "of physical memory is currently available.",

                    Severity =
                        RecommendationSeverity.Info
                });
        }
        else
        {
            recommendations.Add(
                new Recommendation
                {
                    Title = "MEMORY LOOKS GOOD",

                    Description =
                        $"{health.MemoryAvailablePercent:0.0}% " +
                        "of physical memory is currently available.",

                    Severity =
                        RecommendationSeverity.Healthy
                });
        }


        // =========================
        // GENERAL
        // =========================

        recommendations.Add(
            new Recommendation
            {
                Title = "KEEP YOUR SYSTEM UPDATED",

                Description =
                    "Keeping Windows, drivers and applications updated " +
                    "helps maintain security and stability.",

                Severity =
                    RecommendationSeverity.Info
            });


        return recommendations;
    }
}