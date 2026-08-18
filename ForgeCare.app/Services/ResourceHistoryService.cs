using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public class ResourceHistoryService
{
    private const int MaxSamples = 30;

    private readonly string _historyDirectory;
    private readonly string _historyFile;

    private readonly JsonSerializerOptions _jsonOptions =
        new()
        {
            WriteIndented = true
        };

    public ResourceHistoryService()
    {
        _historyDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "ForgeCare",
                "History");

        _historyFile =
            Path.Combine(
                _historyDirectory,
                "resource-history.json");
    }

    public async Task<ResourceBaselineResult> RecordAsync(
        ResourceAnalysisResult analysis)
    {
        var samples =
            await LoadSamplesAsync();

        samples.Add(
            CreateSample(
                analysis));

        if (samples.Count > MaxSamples)
        {
            samples =
                samples
                    .OrderByDescending(
                        sample =>
                            sample.CapturedAt)
                    .Take(MaxSamples)
                    .OrderBy(
                        sample =>
                            sample.CapturedAt)
                    .ToList();
        }

        await SaveSamplesAsync(
            samples);

        return BuildBaseline(
            samples);
    }

    public async Task<ResourceBaselineResult> GetBaselineAsync()
    {
        var samples =
            await LoadSamplesAsync();

        return BuildBaseline(
            samples);
    }

    public Task ClearAsync()
    {
        return Task.Run(
            () =>
            {
                if (File.Exists(
                        _historyFile))
                {
                    File.Delete(
                        _historyFile);
                }
            });
    }

    private static ResourceHistorySample CreateSample(
        ResourceAnalysisResult analysis)
    {
        return new ResourceHistorySample
        {
            CapturedAt =
                analysis.AnalysisTime,

            CpuUsagePercent =
                analysis.CpuUsagePercent,

            MemoryUsedPercent =
                analysis.MemoryUsedPercent,

            ProcessCount =
                analysis.ProcessCount,

            OverallPressure =
                analysis.OverallPressure,

            Processes =
                analysis.TopProcesses
                    .Select(
                        process =>
                            new ResourceHistoryProcessSample
                            {
                                Name =
                                    process.Name,

                                CpuPercent =
                                    process.CpuPercent,

                                MemoryMb =
                                    process.MemoryMb,

                                PressureScore =
                                    process.PressureScore
                            })
                    .ToList()
        };
    }

    private async Task<List<ResourceHistorySample>> LoadSamplesAsync()
    {
        try
        {
            if (!File.Exists(
                    _historyFile))
            {
                return new List<ResourceHistorySample>();
            }

            string json =
                await File.ReadAllTextAsync(
                    _historyFile);

            if (string.IsNullOrWhiteSpace(
                    json))
            {
                return new List<ResourceHistorySample>();
            }

            return JsonSerializer.Deserialize<
                       List<ResourceHistorySample>>(
                           json,
                           _jsonOptions)
                   ?? new List<ResourceHistorySample>();
        }
        catch
        {
            // Corrupt or unreadable history should never prevent
            // ForgeCare from performing a live analysis.
            return new List<ResourceHistorySample>();
        }
    }

    private async Task SaveSamplesAsync(
        List<ResourceHistorySample> samples)
    {
        Directory.CreateDirectory(
            _historyDirectory);

        string json =
            JsonSerializer.Serialize(
                samples,
                _jsonOptions);

        await File.WriteAllTextAsync(
            _historyFile,
            json);
    }

    private static ResourceBaselineResult BuildBaseline(
        List<ResourceHistorySample> samples)
    {
        if (samples.Count == 0)
        {
            return new ResourceBaselineResult();
        }

        var ordered =
            samples
                .OrderBy(
                    sample =>
                        sample.CapturedAt)
                .ToList();

        ResourceHistorySample latest =
            ordered[^1];

        double averageCpu =
            ordered.Average(
                sample =>
                    sample.CpuUsagePercent);

        double averageMemory =
            ordered.Average(
                sample =>
                    sample.MemoryUsedPercent);

        var result =
            new ResourceBaselineResult
            {
                SampleCount =
                    ordered.Count,

                FirstSampleAt =
                    ordered[0].CapturedAt,

                LastSampleAt =
                    latest.CapturedAt,

                AverageCpuPercent =
                    Math.Round(
                        averageCpu,
                        1),

                AverageMemoryUsedPercent =
                    Math.Round(
                        averageMemory,
                        1),

                LatestCpuPercent =
                    latest.CpuUsagePercent,

                LatestMemoryUsedPercent =
                    latest.MemoryUsedPercent,

                CpuTrend =
                    GetTrend(
                        latest.CpuUsagePercent,
                        averageCpu,
                        ordered.Count),

                MemoryTrend =
                    GetTrend(
                        latest.MemoryUsedPercent,
                        averageMemory,
                        ordered.Count),

                BaselineState =
                    GetBaselineState(
                        ordered.Count),

                Confidence =
                    GetConfidence(
                        ordered.Count)
            };

        result.PersistentProcesses =
            BuildProcessBaselines(
                ordered);

        return result;
    }

    private static List<ProcessBaselineInfo> BuildProcessBaselines(
        List<ResourceHistorySample> samples)
    {
        int sampleCount =
            samples.Count;

        if (sampleCount < 2)
        {
            return new List<ProcessBaselineInfo>();
        }

        return samples
            .SelectMany(
                sample =>
                    sample.Processes
                        .GroupBy(
                            process =>
                                process.Name,
                            StringComparer.OrdinalIgnoreCase)
                        .Select(
                            group =>
                                group
                                    .OrderByDescending(
                                        process =>
                                            process.PressureScore)
                                    .First()))
            .GroupBy(
                process =>
                    process.Name,
                StringComparer.OrdinalIgnoreCase)
            .Select(
                group =>
                {
                    int seenCount =
                        group.Count();

                    double occurrence =
                        seenCount /
                        (double)sampleCount;

                    double averageCpu =
                        group.Average(
                            process =>
                                process.CpuPercent);

                    double averageMemory =
                        group.Average(
                            process =>
                                process.MemoryMb);

                    int averagePressure =
                        (int)Math.Round(
                            group.Average(
                                process =>
                                    process.PressureScore));

                    int peakPressure =
                        group.Max(
                            process =>
                                process.PressureScore);

                    return new ProcessBaselineInfo
                    {
                        Name =
                            group.Key,

                        SeenCount =
                            seenCount,

                        SampleCount =
                            sampleCount,

                        AverageCpuPercent =
                            Math.Round(
                                averageCpu,
                                1),

                        AverageMemoryMb =
                            Math.Round(
                                averageMemory,
                                0),

                        AveragePressureScore =
                            averagePressure,

                        PeakPressureScore =
                            peakPressure,

                        Pattern =
                            GetProcessPattern(
                                occurrence,
                                averagePressure),

                        Confidence =
                            GetConfidence(
                                sampleCount)
                    };
                })
            .Where(
                process =>
                    process.SeenCount >= 2)
            .OrderByDescending(
                process =>
                    process.SeenCount)
            .ThenByDescending(
                process =>
                    process.AveragePressureScore)
            .Take(12)
            .ToList();
    }

    private static string GetTrend(
        double latest,
        double average,
        int sampleCount)
    {
        if (sampleCount < 3)
        {
            return "LEARNING";
        }

        double difference =
            latest - average;

        if (difference >= 15)
        {
            return "ABOVE BASELINE";
        }

        if (difference <= -15)
        {
            return "BELOW BASELINE";
        }

        return "NEAR BASELINE";
    }

    private static string GetBaselineState(
        int sampleCount)
    {
        return sampleCount switch
        {
            >= 10 => "ESTABLISHED",
            >= 5 => "FORMING",
            _ => "LEARNING"
        };
    }

    private static string GetConfidence(
        int sampleCount)
    {
        return sampleCount switch
        {
            >= 10 => "HIGH",
            >= 5 => "MEDIUM",
            _ => "LOW"
        };
    }

    private static string GetProcessPattern(
        double occurrence,
        int averagePressure)
    {
        if (occurrence >= 0.70 &&
            averagePressure >= 45)
        {
            return "PERSISTENT PRESSURE";
        }

        if (occurrence >= 0.70)
        {
            return "CONSISTENT";
        }

        if (averagePressure >= 45)
        {
            return "RECURRING SPIKE";
        }

        return "RECURRING";
    }
}