using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class ProcessSystemCpuAttentionRule : IEvidenceCorrelationRule
{
    public const string StableRuleId = "process-system-cpu-pressure-v1";
    private const int HashLength = 16;
    private static readonly string[] EligibleRoots =
    [
        "%programfiles%\\",
        "%programfiles(x86)%\\",
        "%programdata%\\",
        "%windir%\\",
        "%localappdata%\\",
        "%appdata%\\"
    ];

    public string RuleId => StableRuleId;

    public IReadOnlyList<TechnicianAttentionItem> Evaluate(EvidenceCorrelationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EvidenceCorrelationRecord[] latestProcess = context.Records
            .Where(record => record.Source == EvidenceSource.ProcessIntelligence)
            .Where(record => !string.IsNullOrWhiteSpace(record.CorrelationKey))
            .GroupBy(record => record.CorrelationKey!.Trim(), StringComparer.Ordinal)
            .Select(Latest)
            .Select(record => (Record: record, Identity: EligibleIdentity(record)))
            .Where(value => value.Identity != null)
            .GroupBy(value => value.Identity!, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .Select(value => value.Record)
                .OrderByDescending(record => record.TimestampUtc)
                .ThenBy(record => record.Id)
                .First())
            .ToArray();

        Dictionary<DateTime, EvidenceCorrelationRecord> cpuByTimestamp = context.Records
            .Where(IsGlobalCpuRecord)
            .GroupBy(record => record.TimestampUtc)
            .ToDictionary(group => group.Key, Latest);

        var items = new List<TechnicianAttentionItem>();
        foreach (EvidenceCorrelationRecord process in latestProcess)
        {
            if (!MetadataEquals(process, "identityStrength", "Strong") ||
                process.Severity is not (EvidenceSeverity.Medium or EvidenceSeverity.High or EvidenceSeverity.Critical) ||
                !TryNumber(Metadata(process, "totalCpuPercent"), out double processCpu) ||
                processCpu <= 0 ||
                !cpuByTimestamp.TryGetValue(process.TimestampUtc, out EvidenceCorrelationRecord? cpu) ||
                PriorityFor(cpu.Severity) is not TechnicianAttentionPriority priority ||
                cpu.Value is not double globalCpu || !double.IsFinite(globalCpu) || globalCpu < 0)
                continue;

            string identity = EligibleIdentity(process)!;
            string entityKey = "app:" + Hash(identity);
            EvidenceCorrelationRecord[] supporting = [process, cpu];
            Guid[] evidenceIds = supporting
                .OrderByDescending(record => record.TimestampUtc)
                .ThenBy(record => record.Id)
                .Select(record => record.Id)
                .ToArray();
            string application = SafeApplicationName(process);
            string pressure = cpu.Severity.ToString().ToUpperInvariant();

            items.Add(new TechnicianAttentionItem(
                AttentionId(entityKey, evidenceIds),
                context.SessionId,
                RuleId,
                $"Review {application} CPU and system pressure observations",
                $"{application} was observed with elevated CPU activity during an analysis that also recorded elevated overall CPU pressure.",
                EvidenceCategory.Cpu,
                priority,
                WeakestConfidence(supporting),
                $"The Process Intelligence observation recorded {Number(processCpu)} % aggregate CPU use for {application}. " +
                $"The same Deep Analysis recorded {Number(globalCpu)} % overall CPU utilization with {pressure} pressure.",
                "Review the application's current workload and the related CPU Evidence before considering any change.",
                entityKey,
                evidenceIds,
                supporting
                    .Select(record => record.CorrelationKey)
                    .Where(key => !string.IsNullOrWhiteSpace(key))
                    .Select(key => key!)
                    .ToArray(),
                process.TimestampUtc));
        }

        return items;
    }

    private static EvidenceCorrelationRecord Latest(IEnumerable<EvidenceCorrelationRecord> records) =>
        records.OrderByDescending(record => record.TimestampUtc).ThenBy(record => record.Id).First();

    private static bool IsGlobalCpuRecord(EvidenceCorrelationRecord record) =>
        record.Source == EvidenceSource.DeepAnalysis &&
        record.Category == EvidenceCategory.Cpu &&
        string.Equals(record.Subject?.Trim(), "cpu-pressure", StringComparison.Ordinal) &&
        string.Equals(record.CorrelationKey?.Trim(), "cpu:pressure", StringComparison.Ordinal) &&
        string.Equals(record.Unit?.Trim(), "%", StringComparison.Ordinal) &&
        record.Value is double value && double.IsFinite(value) && value >= 0;

    private static TechnicianAttentionPriority? PriorityFor(EvidenceSeverity severity) =>
        severity switch
        {
            EvidenceSeverity.Medium => TechnicianAttentionPriority.Low,
            EvidenceSeverity.High or EvidenceSeverity.Critical => TechnicianAttentionPriority.Medium,
            _ => null
        };

    private static string? EligibleIdentity(EvidenceCorrelationRecord record)
    {
        if (!record.Metadata.TryGetValue("normalizedExecutablePath", out string? path) ||
            string.IsNullOrWhiteSpace(path))
            return null;

        string candidate = path.Trim().Replace('/', '\\');
        while (candidate.Contains("\\\\", StringComparison.Ordinal))
            candidate = candidate.Replace("\\\\", "\\", StringComparison.Ordinal);
        candidate = candidate.ToLowerInvariant();
        if (!EligibleRoots.Any(root => candidate.StartsWith(root, StringComparison.Ordinal)))
            return null;

        string suffix = candidate[(candidate.IndexOf('\\') + 1)..];
        if (string.IsNullOrWhiteSpace(suffix) ||
            suffix.Split('\\').Any(segment => string.IsNullOrWhiteSpace(segment) || segment is "." or "..") ||
            candidate.IndexOfAny(['\0', '"', '*', '?', '<', '>', '|']) >= 0 ||
            suffix.Contains(':', StringComparison.Ordinal))
            return null;
        return candidate;
    }

    private static string SafeApplicationName(EvidenceCorrelationRecord process)
    {
        string value = Metadata(process, "applicationName") ?? "application";
        value = value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        return value.Length <= 120 ? value : value[..120];
    }

    private static string? Metadata(EvidenceCorrelationRecord record, string key) =>
        record.Metadata.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;

    private static bool MetadataEquals(EvidenceCorrelationRecord record, string key, string expected) =>
        string.Equals(Metadata(record, key), expected, StringComparison.OrdinalIgnoreCase);

    private static bool TryNumber(string? value, out double number) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) &&
        double.IsFinite(number) && number >= 0;

    private static EvidenceConfidence WeakestConfidence(IEnumerable<EvidenceCorrelationRecord> records) =>
        records.Select(record => record.Confidence).Min();

    private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string AttentionId(string entityKey, IEnumerable<Guid> evidenceIds) =>
        "attention:" + Hash($"{StableRuleId}|{entityKey}|{string.Join(',', evidenceIds)}");

    private static string Hash(string value)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(digest)[..HashLength];
    }
}
