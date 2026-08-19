using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class DeepAnalysisEvidenceAdapter :
    IEvidenceCollector<ResourceAnalysisResult>
{
    public EvidenceCollectionResult Collect(
        ResourceAnalysisResult result,
        string sessionId)
    {
        ArgumentNullException.ThrowIfNull(result);

        var collection = new EvidenceCollectionResult();
        DateTime timestampUtc =
            result.AnalysisTime.ToUniversalTime();

        AddGlobalEvidence(
            collection,
            CreateRecord(
                sessionId,
                timestampUtc,
                EvidenceCategory.Cpu,
                "cpu-pressure",
                $"CPU utilization was {Format(result.CpuUsagePercent)}% during the Deep Analysis sampling window.",
                result.CpuUsagePercent,
                "%",
                MapSeverity(result.CpuStatus),
                "cpu:pressure",
                new Dictionary<string, string>
                {
                    ["cpuStatus"] = result.CpuStatus
                }),
            result.CpuUsagePercent,
            "CPU utilization");

        AddGlobalEvidence(
            collection,
            CreateRecord(
                sessionId,
                timestampUtc,
                EvidenceCategory.Memory,
                "memory-pressure",
                $"{Format(result.MemoryUsedPercent)}% of physical memory was in use during Deep Analysis.",
                result.MemoryUsedPercent,
                "%",
                MapSeverity(result.MemoryStatus),
                "memory:pressure",
                new Dictionary<string, string>
                {
                    ["usedMemoryGb"] = Format(result.UsedMemoryGb),
                    ["availableMemoryGb"] = Format(result.AvailableMemoryGb),
                    ["memoryStatus"] = result.MemoryStatus
                }),
            result.MemoryUsedPercent,
            "Memory utilization");

        AddGlobalEvidence(
            collection,
            CreateRecord(
                sessionId,
                timestampUtc,
                EvidenceCategory.Process,
                "process-count",
                $"{result.ProcessCount} processes were observed during Deep Analysis.",
                result.ProcessCount,
                "processes",
                MapSeverity(result.ProcessStatus),
                "process:count",
                new Dictionary<string, string>
                {
                    ["processStatus"] = result.ProcessStatus
                }),
            result.ProcessCount,
            "Process count");

        if (string.IsNullOrWhiteSpace(result.OverallPressure))
        {
            collection.Warnings.Add(
                "Deep Analysis did not provide an overall resource-pressure state.");
        }
        else
        {
            Add(
                collection,
                CreateRecord(
                    sessionId,
                    timestampUtc,
                    EvidenceCategory.System,
                    "overall-resource-pressure",
                    $"Deep Analysis classified overall resource pressure as {result.OverallPressure}.",
                    null,
                    null,
                    MapSeverity(result.OverallPressure),
                    "system:resource-pressure",
                    new Dictionary<string, string>
                    {
                        ["overallPressure"] = result.OverallPressure
                    }));
        }

        IReadOnlyList<ResourceProcessInfo> topProcesses =
            result.TopProcesses ?? new List<ResourceProcessInfo>();

        for (int index = 0; index < topProcesses.Count; index++)
        {
            ResourceProcessInfo? process = topProcesses[index];

            try
            {
                EvidenceRecord processRecord = CreateProcessRecord(
                    process,
                    sessionId,
                    timestampUtc);

                Add(collection, processRecord);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                collection.Errors.Add(
                    $"Top process observation {index + 1} was skipped: {ex.Message}");
            }
        }

        return collection;
    }

    public static EvidenceSeverity MapSeverity(string? status)
    {
        return status?.Trim().ToUpperInvariant() switch
        {
            "NORMAL" or "MINIMAL" => EvidenceSeverity.Informational,
            "LOW" => EvidenceSeverity.Low,
            "MODERATE" or "MEDIUM" or "ELEVATED" => EvidenceSeverity.Medium,
            "HIGH" => EvidenceSeverity.High,
            "CRITICAL" => EvidenceSeverity.Critical,
            _ => EvidenceSeverity.Unknown
        };
    }

    private static EvidenceRecord CreateProcessRecord(
        ResourceProcessInfo? process,
        string sessionId,
        DateTime timestampUtc)
    {
        if (process == null)
            throw new ArgumentException("The process entry was null.");

        if (process.ProcessId < 0)
            throw new ArgumentException("The process ID was invalid.");

        if (!IsFiniteNonNegative(process.CpuPercent) ||
            !IsFiniteNonNegative(process.MemoryMb))
        {
            throw new ArgumentException(
                "CPU or working-set memory was not a finite non-negative value.");
        }

        string displayName =
            string.IsNullOrWhiteSpace(process.Name)
                ? $"PID {process.ProcessId}"
                : process.Name.Trim();

        string normalizedName = NormalizeProcessName(displayName);
        string primaryResource =
            process.PrimaryResource?.Trim().ToUpperInvariant() ?? string.Empty;

        (double value, string unit) = primaryResource switch
        {
            "CPU" => (process.CpuPercent, "%"),
            "MEMORY" => (process.MemoryMb, "MB"),
            _ => (process.PressureScore, "score")
        };

        return CreateRecord(
            sessionId,
            timestampUtc,
            EvidenceCategory.Process,
            $"process:{normalizedName}",
            $"Process '{displayName}' (PID {process.ProcessId}) used approximately " +
            $"{Format(process.CpuPercent)}% CPU and {Format(process.MemoryMb)} MB of working-set memory during the Deep Analysis sample.",
            value,
            unit,
            MapSeverity(process.PressureLevel),
            $"process:{normalizedName}:{process.ProcessId}",
            new Dictionary<string, string>
            {
                ["processName"] = displayName,
                ["processId"] = process.ProcessId.ToString(CultureInfo.InvariantCulture),
                ["cpuPercent"] = Format(process.CpuPercent),
                ["memoryMb"] = Format(process.MemoryMb),
                ["pressureScore"] = process.PressureScore.ToString(CultureInfo.InvariantCulture),
                ["pressureLevel"] = process.PressureLevel ?? string.Empty,
                ["primaryResource"] = process.PrimaryResource ?? string.Empty
            });
    }

    private static EvidenceRecord CreateRecord(
        string sessionId,
        DateTime timestampUtc,
        EvidenceCategory category,
        string subject,
        string observation,
        double? value,
        string? unit,
        EvidenceSeverity severity,
        string correlationKey,
        Dictionary<string, string> metadata)
    {
        return new EvidenceRecord
        {
            SessionId = sessionId,
            TimestampUtc = timestampUtc,
            Category = category,
            Source = EvidenceSource.DeepAnalysis,
            Subject = subject,
            Observation = observation,
            Value = value,
            Unit = unit,
            Severity = severity,
            Confidence = EvidenceConfidence.High,
            Collector = nameof(DeepAnalysisEvidenceAdapter),
            Metadata = metadata,
            CorrelationKey = correlationKey
        };
    }

    private static void AddGlobalEvidence(
        EvidenceCollectionResult collection,
        EvidenceRecord record,
        double value,
        string valueName)
    {
        if (!IsFiniteNonNegative(value))
        {
            collection.Errors.Add(
                $"{valueName} was not a finite non-negative value.");
            return;
        }

        Add(collection, record);
    }

    private static void Add(
        EvidenceCollectionResult collection,
        EvidenceRecord record)
    {
        IReadOnlyList<string> errors = record.Validate();
        if (errors.Count == 0)
        {
            collection.Evidence.Add(record);
            return;
        }

        collection.Errors.AddRange(errors);
    }

    private static bool IsFiniteNonNegative(double value) =>
        double.IsFinite(value) && value >= 0;

    private static string Format(double value) =>
        value.ToString("0.0", CultureInfo.InvariantCulture);

    private static string NormalizeProcessName(string value)
    {
        char[] normalized = value
            .Trim()
            .ToLowerInvariant()
            .Select(character =>
                char.IsLetterOrDigit(character)
                    ? character
                    : '-')
            .ToArray();

        string result = string.Join(
            '-',
            new string(normalized)
                .Split('-', StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(result)
            ? "pid"
            : result;
    }
}
