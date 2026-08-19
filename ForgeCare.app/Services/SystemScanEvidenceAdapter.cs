using System;
using System.Collections.Generic;
using System.Globalization;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class SystemScanEvidenceAdapter : IEvidenceCollector<SystemSnapshot>
{
    public EvidenceCollectionResult Collect(
        SystemSnapshot snapshot,
        string sessionId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var result = new EvidenceCollectionResult();
        DateTime timestampUtc = snapshot.ScanTime.ToUniversalTime();

        Add(result, Create(
            sessionId,
            timestampUtc,
            EvidenceCategory.OperatingSystem,
            "operating-system",
            $"Operating system '{snapshot.OperatingSystem}' was identified.",
            null,
            null,
            "system:os",
            new Dictionary<string, string>
            {
                ["operatingSystem"] = snapshot.OperatingSystem
            }));

        Add(result, Create(
            sessionId,
            timestampUtc,
            EvidenceCategory.Cpu,
            "processor",
            $"Processor '{snapshot.ProcessorName}' was identified.",
            null,
            null,
            "system:processor",
            new Dictionary<string, string>
            {
                ["processorName"] = snapshot.ProcessorName
            }));

        Add(result, Create(
            sessionId,
            timestampUtc,
            EvidenceCategory.Memory,
            "physical-memory",
            $"{snapshot.TotalMemoryGb.ToString("0.0", CultureInfo.InvariantCulture)} GB of physical memory was installed.",
            snapshot.TotalMemoryGb,
            "GB",
            "memory:physical"));

        Add(result, Create(
            sessionId,
            timestampUtc,
            EvidenceCategory.Memory,
            "available-memory",
            $"{snapshot.AvailableMemoryGb.ToString("0.0", CultureInfo.InvariantCulture)} GB of physical memory was available when the system scan completed.",
            snapshot.AvailableMemoryGb,
            "GB",
            "memory:available"));

        Add(result, Create(
            sessionId,
            timestampUtc,
            EvidenceCategory.Storage,
            "system-drive",
            $"{snapshot.SystemDriveFreeGb.ToString("0.0", CultureInfo.InvariantCulture)} GB was available on the system drive when the system scan completed.",
            snapshot.SystemDriveFreeGb,
            "GB",
            "drive:system"));

        Add(result, Create(
            sessionId,
            timestampUtc,
            EvidenceCategory.Startup,
            "startup-entries",
            $"{snapshot.StartupItems.Count} startup entries were discovered.",
            snapshot.StartupItems.Count,
            "items",
            "startup:count"));

        return result;
    }

    private static EvidenceRecord Create(
        string sessionId,
        DateTime timestampUtc,
        EvidenceCategory category,
        string subject,
        string observation,
        double? value,
        string? unit,
        string correlationKey,
        Dictionary<string, string>? metadata = null)
    {
        return new EvidenceRecord
        {
            SessionId = sessionId,
            TimestampUtc = timestampUtc,
            Category = category,
            Source = EvidenceSource.SystemScan,
            Subject = subject,
            Observation = observation,
            Value = value,
            Unit = unit,
            Severity = EvidenceSeverity.Informational,
            Confidence = EvidenceConfidence.High,
            Collector = nameof(SystemScanEvidenceAdapter),
            Metadata = metadata ?? new Dictionary<string, string>(),
            CorrelationKey = correlationKey
        };
    }

    private static void Add(
        EvidenceCollectionResult result,
        EvidenceRecord record)
    {
        IReadOnlyList<string> errors = record.Validate();
        if (errors.Count == 0)
        {
            result.Evidence.Add(record);
            return;
        }

        result.Errors.AddRange(errors);
    }
}
