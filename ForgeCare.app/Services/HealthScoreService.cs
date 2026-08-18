using System;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public class HealthScoreService
{
    public HealthResult Calculate(
        SystemSnapshot snapshot)
    {
        double storageFreePercent =
            snapshot.SystemDriveTotalGb > 0
                ? snapshot.SystemDriveFreeGb /
                  snapshot.SystemDriveTotalGb * 100
                : 0;

        double memoryAvailablePercent =
            snapshot.TotalMemoryGb > 0
                ? snapshot.AvailableMemoryGb /
                  snapshot.TotalMemoryGb * 100
                : 0;

        int startupCount =
            snapshot.StartupItems.Count;

        int storageScore =
            CalculateStorageScore(
                storageFreePercent);

        int memoryScore =
            CalculateMemoryScore(
                memoryAvailablePercent);

        int startupScore =
            CalculateStartupScore(
                startupCount);

        // Weighted score:
        //
        // Storage: 40%
        // Startup: 35%
        // Memory headroom: 25%

        int finalScore =
            (int)Math.Round(
                storageScore * 0.40 +
                startupScore * 0.35 +
                memoryScore * 0.25);

        return new HealthResult
        {
            Score = finalScore,

            Rating =
                GetRating(finalScore),

            StorageStatus =
                GetStorageStatus(storageFreePercent),

            MemoryStatus =
                GetMemoryStatus(memoryAvailablePercent),

            StartupStatus =
                GetStartupStatus(startupCount),

            StorageFreePercent =
                storageFreePercent,

            MemoryAvailablePercent =
                memoryAvailablePercent,

            StartupCount =
                startupCount
        };
    }

    private static int CalculateStorageScore(
        double freePercent)
    {
        if (freePercent >= 25)
            return 100;

        if (freePercent >= 15)
            return 85;

        if (freePercent >= 10)
            return 65;

        if (freePercent >= 5)
            return 40;

        return 20;
    }

    private static int CalculateMemoryScore(
        double availablePercent)
    {
        if (availablePercent >= 40)
            return 100;

        if (availablePercent >= 25)
            return 85;

        if (availablePercent >= 15)
            return 65;

        if (availablePercent >= 8)
            return 40;

        return 20;
    }

    private static int CalculateStartupScore(
        int startupCount)
    {
        if (startupCount <= 8)
            return 100;

        if (startupCount <= 15)
            return 85;

        if (startupCount <= 25)
            return 65;

        if (startupCount <= 35)
            return 45;

        return 25;
    }

    private static string GetRating(
        int score)
    {
        return score switch
        {
            >= 90 => "EXCELLENT",
            >= 75 => "GOOD",
            >= 60 => "FAIR",
            >= 40 => "ATTENTION",
            _ => "CRITICAL"
        };
    }

    private static string GetStorageStatus(
        double freePercent)
    {
        return freePercent switch
        {
            >= 25 => "Healthy",
            >= 15 => "Good",
            >= 10 => "Attention",
            _ => "Low space"
        };
    }

    private static string GetMemoryStatus(
        double availablePercent)
    {
        return availablePercent switch
        {
            >= 40 => "Healthy",
            >= 25 => "Good",
            >= 15 => "Attention",
            _ => "High usage"
        };
    }

    private static string GetStartupStatus(
        int startupCount)
    {
        return startupCount switch
        {
            <= 8 => "Healthy",
            <= 15 => "Good",
            <= 25 => "Attention",
            _ => "Heavy"
        };
    }
}