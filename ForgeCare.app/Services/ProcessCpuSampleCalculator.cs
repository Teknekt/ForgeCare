using System;

namespace ForgeCare.App.Services;

public readonly record struct ProcessCpuSample(
    int ProcessId,
    TimeSpan TotalProcessorTime,
    DateTime? StartTimeUtc);

public static class ProcessCpuSampleCalculator
{
    public static double CalculatePercent(
        ProcessCpuSample? baseline,
        ProcessCpuSample current,
        double elapsedMilliseconds,
        int processorCount)
    {
        if (baseline is not { } previous ||
            previous.ProcessId != current.ProcessId ||
            previous.StartTimeUtc == null ||
            current.StartTimeUtc == null ||
            previous.StartTimeUtc != current.StartTimeUtc ||
            elapsedMilliseconds <= 0 ||
            processorCount <= 0)
        {
            return 0;
        }

        double deltaMilliseconds = Math.Max(
            0,
            (current.TotalProcessorTime - previous.TotalProcessorTime).TotalMilliseconds);
        double cpu = deltaMilliseconds / elapsedMilliseconds / processorCount * 100;
        return Math.Clamp(cpu, 0, 100);
    }
}
