using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public class ResourceAnalyzerService
{
    private const int SampleDelayMs = 900;

    public async Task<ResourceAnalysisResult> AnalyzeAsync(
        SystemSnapshot snapshot)
    {
        var firstSample =
            CaptureCpuTimes();

        var stopwatch =
            Stopwatch.StartNew();

        await Task.Delay(
            SampleDelayMs);

        stopwatch.Stop();

        var secondProcesses =
            Process.GetProcesses();

        var processResults =
            new List<ResourceProcessInfo>();

        double totalCpu =
            0;

        foreach (var process in secondProcesses)
        {
            try
            {
                TimeSpan currentCpu =
                    process.TotalProcessorTime;

                firstSample.TryGetValue(
                    process.Id,
                    out TimeSpan previousCpu);

                double cpuPercent =
                    CalculateCpuPercent(
                        previousCpu,
                        currentCpu,
                        stopwatch.Elapsed.TotalMilliseconds);

                double memoryMb =
                    process.WorkingSet64 /
                    1024d /
                    1024d;

                double memoryPercent =
                    snapshot.TotalMemoryGb > 0
                        ? memoryMb /
                          (snapshot.TotalMemoryGb * 1024d) *
                          100
                        : 0;

                int pressureScore =
                    CalculatePressureScore(
                        cpuPercent,
                        memoryPercent,
                        memoryMb);

                string primaryResource =
                    cpuPercent >= 10 &&
                    cpuPercent >= memoryPercent
                        ? "CPU"
                        : memoryMb >= 500
                            ? "MEMORY"
                            : "BALANCED";

                processResults.Add(
                    new ResourceProcessInfo
                    {
                        ProcessId =
                            process.Id,

                        Name =
                            string.IsNullOrWhiteSpace(
                                process.ProcessName)
                                ? $"PID {process.Id}"
                                : process.ProcessName,

                        CpuPercent =
                            Math.Round(
                                cpuPercent,
                                1),

                        MemoryMb =
                            Math.Round(
                                memoryMb,
                                0),

                        MemoryPercent =
                            Math.Round(
                                memoryPercent,
                                1),

                        PressureScore =
                            pressureScore,

                        PressureLevel =
                            GetPressureLevel(
                                pressureScore),

                        PrimaryResource =
                            primaryResource
                    });

                totalCpu +=
                    cpuPercent;
            }
            catch
            {
                // Processes can exit or deny access between samples.
                // Deep Analysis should continue rather than fail.
            }
            finally
            {
                process.Dispose();
            }
        }

        totalCpu =
            Math.Clamp(
                totalCpu,
                0,
                100);

        double memoryUsedPercent =
            snapshot.TotalMemoryGb > 0
                ? (snapshot.TotalMemoryGb -
                   snapshot.AvailableMemoryGb) /
                  snapshot.TotalMemoryGb *
                  100
                : 0;

        double usedMemoryGb =
            Math.Max(
                0,
                snapshot.TotalMemoryGb -
                snapshot.AvailableMemoryGb);

        int highCpuCount =
            processResults.Count(
                item =>
                    item.CpuPercent >= 10);

        int highMemoryCount =
            processResults.Count(
                item =>
                    item.MemoryMb >= 500);

        var topProcesses =
            processResults
                .OrderByDescending(
                    item =>
                        item.PressureScore)
                .ThenByDescending(
                    item =>
                        item.CpuPercent)
                .ThenByDescending(
                    item =>
                        item.MemoryMb)
                .Take(14)
                .ToList();

        var result =
            new ResourceAnalysisResult
            {
                CpuUsagePercent =
                    Math.Round(
                        totalCpu,
                        1),

                MemoryUsedPercent =
                    Math.Round(
                        memoryUsedPercent,
                        1),

                UsedMemoryGb =
                    Math.Round(
                        usedMemoryGb,
                        1),

                AvailableMemoryGb =
                    snapshot.AvailableMemoryGb,

                TotalMemoryGb =
                    snapshot.TotalMemoryGb,

                ProcessCount =
                    processResults.Count,

                HighCpuProcessCount =
                    highCpuCount,

                HighMemoryProcessCount =
                    highMemoryCount,

                CpuStatus =
                    GetCpuStatus(
                        totalCpu),

                MemoryStatus =
                    GetMemoryStatus(
                        memoryUsedPercent),

                ProcessStatus =
                    GetProcessStatus(
                        processResults.Count),

                OverallPressure =
                    GetOverallPressure(
                        totalCpu,
                        memoryUsedPercent,
                        highCpuCount,
                        highMemoryCount),

                AnalysisTime =
                    DateTime.Now,

                TopProcesses =
                    topProcesses
            };

        result.Insights =
            BuildInsights(
                result);

        return result;
    }

    private static Dictionary<int, TimeSpan>
        CaptureCpuTimes()
    {
        var values =
            new Dictionary<int, TimeSpan>();

        foreach (var process in
                 Process.GetProcesses())
        {
            try
            {
                values[process.Id] =
                    process.TotalProcessorTime;
            }
            catch
            {
                // Ignore inaccessible/exited processes.
            }
            finally
            {
                process.Dispose();
            }
        }

        return values;
    }

    private static double CalculateCpuPercent(
        TimeSpan previous,
        TimeSpan current,
        double elapsedMilliseconds)
    {
        if (elapsedMilliseconds <= 0)
        {
            return 0;
        }

        double deltaMilliseconds =
            Math.Max(
                0,
                (current - previous)
                .TotalMilliseconds);

        double cpu =
            deltaMilliseconds /
            elapsedMilliseconds /
            Environment.ProcessorCount *
            100;

        return Math.Clamp(
            cpu,
            0,
            100);
    }

    private static int CalculatePressureScore(
        double cpuPercent,
        double memoryPercent,
        double memoryMb)
    {
        double cpuScore =
            Math.Min(
                100,
                cpuPercent * 4);

        double memoryScore =
            Math.Min(
                100,
                memoryPercent * 12);

        // A process can be meaningful even on a machine with lots
        // of RAM, so absolute working-set size contributes as well.
        double absoluteMemoryScore =
            memoryMb switch
            {
                >= 3000 => 100,
                >= 2000 => 85,
                >= 1000 => 70,
                >= 500 => 50,
                >= 250 => 30,
                _ => 10
            };

        double score =
            cpuScore * 0.55 +
            memoryScore * 0.20 +
            absoluteMemoryScore * 0.25;

        return (int)Math.Round(
            Math.Clamp(
                score,
                0,
                100));
    }

    private static string GetPressureLevel(
        int score)
    {
        return score switch
        {
            >= 70 => "HIGH",
            >= 45 => "MODERATE",
            >= 20 => "LOW",
            _ => "MINIMAL"
        };
    }

    private static string GetCpuStatus(
        double cpu)
    {
        return cpu switch
        {
            >= 85 => "CRITICAL",
            >= 65 => "HIGH",
            >= 35 => "MODERATE",
            _ => "NORMAL"
        };
    }

    private static string GetMemoryStatus(
        double used)
    {
        return used switch
        {
            >= 92 => "CRITICAL",
            >= 80 => "HIGH",
            >= 65 => "MODERATE",
            _ => "NORMAL"
        };
    }

    private static string GetProcessStatus(
        int count)
    {
        return count switch
        {
            >= 350 => "HIGH",
            >= 250 => "ELEVATED",
            _ => "NORMAL"
        };
    }

    private static string GetOverallPressure(
        double cpu,
        double memoryUsed,
        int highCpuCount,
        int highMemoryCount)
    {
        int score =
            0;

        if (cpu >= 85) score += 4;
        else if (cpu >= 65) score += 3;
        else if (cpu >= 35) score += 1;

        if (memoryUsed >= 92) score += 4;
        else if (memoryUsed >= 80) score += 3;
        else if (memoryUsed >= 65) score += 1;

        if (highCpuCount >= 4) score += 2;
        else if (highCpuCount >= 1) score += 1;

        if (highMemoryCount >= 8) score += 2;
        else if (highMemoryCount >= 3) score += 1;

        return score switch
        {
            >= 8 => "CRITICAL",
            >= 5 => "HIGH",
            >= 3 => "MODERATE",
            _ => "LOW"
        };
    }

    private static List<ResourceInsight>
        BuildInsights(
            ResourceAnalysisResult result)
    {
        var insights =
            new List<ResourceInsight>();

        if (result.CpuUsagePercent >= 65)
        {
            insights.Add(
                new ResourceInsight
                {
                    Title =
                        "CPU pressure detected",

                    Description =
                        $"Observed process CPU usage was approximately " +
                        $"{result.CpuUsagePercent:0.0}% during the sample window. " +
                        $"{result.HighCpuProcessCount} process(es) crossed the high-CPU threshold.",

                    Severity =
                        result.CpuUsagePercent >= 85
                            ? "CRITICAL"
                            : "ATTENTION"
                });
        }
        else
        {
            insights.Add(
                new ResourceInsight
                {
                    Title =
                        "CPU pressure currently looks normal",

                    Description =
                        $"Observed process CPU usage was approximately " +
                        $"{result.CpuUsagePercent:0.0}% during the sample window.",

                    Severity =
                        "HEALTHY"
                });
        }

        if (result.MemoryUsedPercent >= 80)
        {
            insights.Add(
                new ResourceInsight
                {
                    Title =
                        "Memory pressure is elevated",

                    Description =
                        $"{result.MemoryUsedPercent:0.0}% of physical memory is currently in use. " +
                        $"{result.HighMemoryProcessCount} process(es) are using at least 500 MB.",

                    Severity =
                        result.MemoryUsedPercent >= 92
                            ? "CRITICAL"
                            : "ATTENTION"
                });
        }
        else
        {
            insights.Add(
                new ResourceInsight
                {
                    Title =
                        "Memory headroom remains available",

                    Description =
                        $"{result.AvailableMemoryGb:0.0} GB of " +
                        $"{result.TotalMemoryGb:0.0} GB physical memory is currently available.",

                    Severity =
                        "HEALTHY"
                });
        }

        ResourceProcessInfo? top =
            result.TopProcesses
                .FirstOrDefault();

        if (top != null)
        {
            insights.Add(
                new ResourceInsight
                {
                    Title =
                        $"{top.Name} is the top observed resource consumer",

                    Description =
                        $"{top.Name} used about {top.DisplayCpu} CPU and " +
                        $"{top.DisplayMemory} working-set memory during this sample. " +
                        $"ForgeCare classified its current pressure as {top.PressureLevel}.",

                    Severity =
                        top.PressureLevel == "HIGH"
                            ? "ATTENTION"
                            : "INFO"
                });
        }

        insights.Add(
            new ResourceInsight
            {
                Title =
                    "Snapshot, not a permanent diagnosis",

                Description =
                    "Resource usage changes constantly. ForgeCare samples the current system state " +
                    "and should compare repeated runs before recommending invasive actions.",

                Severity =
                    "INFO"
            });

        return insights;
    }
}