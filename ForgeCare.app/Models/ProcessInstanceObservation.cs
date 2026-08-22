using System;

namespace ForgeCare.App.Models;

public sealed record ProcessInstanceObservation
{
    public ProcessInstanceObservation(
        int processId,
        string name,
        DateTime? startTimeUtc,
        string? executablePath,
        double cpuPercent,
        double memoryMb,
        double memoryPercent,
        double pressureScore,
        string pressureLevel,
        string primaryResource)
    {
        if (processId < 0) throw new ArgumentOutOfRangeException(nameof(processId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Process name is required.", nameof(name));
        if (startTimeUtc is { Kind: not DateTimeKind.Utc }) throw new ArgumentException("Process start time must be UTC.", nameof(startTimeUtc));
        ValidateNumber(cpuPercent, nameof(cpuPercent));
        ValidateNumber(memoryMb, nameof(memoryMb));
        ValidateNumber(memoryPercent, nameof(memoryPercent));
        ValidateNumber(pressureScore, nameof(pressureScore));
        if (string.IsNullOrWhiteSpace(pressureLevel)) throw new ArgumentException("Pressure level is required.", nameof(pressureLevel));
        if (string.IsNullOrWhiteSpace(primaryResource)) throw new ArgumentException("Primary resource is required.", nameof(primaryResource));

        ProcessId = processId;
        Name = name.Trim();
        StartTimeUtc = startTimeUtc;
        ExecutablePath = string.IsNullOrWhiteSpace(executablePath) ? null : executablePath.Trim();
        CpuPercent = cpuPercent;
        MemoryMb = memoryMb;
        MemoryPercent = memoryPercent;
        PressureScore = pressureScore;
        PressureLevel = pressureLevel.Trim();
        PrimaryResource = primaryResource.Trim();
    }

    public int ProcessId { get; }
    public string Name { get; }
    public DateTime? StartTimeUtc { get; }
    public string? ExecutablePath { get; }
    public double CpuPercent { get; }
    public double MemoryMb { get; }
    public double MemoryPercent { get; }
    public double PressureScore { get; }
    public string PressureLevel { get; }
    public string PrimaryResource { get; }

    private static void ValidateNumber(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0)
            throw new ArgumentOutOfRangeException(parameterName, "Process metrics must be finite and non-negative.");
    }
}
