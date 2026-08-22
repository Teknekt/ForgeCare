using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class StartupProcessAttentionRule : IEvidenceCorrelationRule
{
    public const string StableRuleId = "startup-process-resource-v1";
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

    public IReadOnlyList<TechnicianAttentionItem> Evaluate(
        EvidenceCorrelationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EvidenceCorrelationRecord[] latestStartup = LatestPerCorrelationKey(
            context.Records.Where(record => record.Source == EvidenceSource.StartupIntelligence));
        EvidenceCorrelationRecord[] latestProcess = LatestPerCorrelationKey(
            context.Records.Where(record => record.Source == EvidenceSource.ProcessIntelligence));

        Dictionary<string, List<EvidenceCorrelationRecord>> startupByIdentity = latestStartup
            .Select(record => (Record: record, Identity: EligibleIdentity(record)))
            .Where(value => value.Identity != null)
            .GroupBy(value => value.Identity!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(value => value.Record).ToList(),
                StringComparer.OrdinalIgnoreCase);

        EvidenceCorrelationRecord[] eligibleProcess = latestProcess
            .Where(record => PriorityFor(record.Severity) != null)
            .Where(record => MetadataEquals(record, "identityStrength", "Strong"))
            .Where(record => EligibleIdentity(record) != null)
            .GroupBy(record => EligibleIdentity(record)!, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(record => record.TimestampUtc)
                .ThenBy(record => record.Id)
                .First())
            .ToArray();

        var results = new List<TechnicianAttentionItem>();
        foreach (EvidenceCorrelationRecord process in eligibleProcess)
        {
            string identity = EligibleIdentity(process)!;
            if (!startupByIdentity.TryGetValue(identity, out List<EvidenceCorrelationRecord>? startups))
                continue;

            TechnicianAttentionPriority priority = PriorityFor(process.Severity)!.Value;
            EvidenceCorrelationRecord[] supporting = startups
                .Append(process)
                .OrderByDescending(record => record.TimestampUtc)
                .ThenBy(record => record.Id)
                .ToArray();
            Guid[] evidenceIds = supporting.Select(record => record.Id).Distinct().ToArray();
            string[] correlationKeys = supporting
                .Select(record => record.CorrelationKey)
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key!.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToArray();
            string entityKey = "app:" + Hash(identity);
            string application = SafeApplicationName(process);
            DateTime latestTimestamp = supporting.Max(record => record.TimestampUtc);

            results.Add(new TechnicianAttentionItem(
                AttentionId(entityKey, evidenceIds),
                context.SessionId,
                RuleId,
                $"Review {application} startup and resource observations",
                $"{application} was observed as configured startup behavior and as a running application with elevated resource pressure.",
                EvidenceCategory.Application,
                priority,
                WeakestConfidence(supporting),
                BuildRationale(application, startups.Count, process),
                "Review whether this application needs to launch at sign-in before changing startup behavior.",
                entityKey,
                evidenceIds,
                correlationKeys,
                latestTimestamp));
        }

        return results;
    }

    private static EvidenceCorrelationRecord[] LatestPerCorrelationKey(
        IEnumerable<EvidenceCorrelationRecord> records) =>
        records
            .Where(record => !string.IsNullOrWhiteSpace(record.CorrelationKey))
            .GroupBy(record => record.CorrelationKey!.Trim(), StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(record => record.TimestampUtc)
                .ThenBy(record => record.Id)
                .First())
            .OrderByDescending(record => record.TimestampUtc)
            .ThenBy(record => record.Id)
            .ToArray();

    private static string? EligibleIdentity(EvidenceCorrelationRecord record)
    {
        if (!record.Metadata.TryGetValue("normalizedExecutablePath", out string? value) ||
            string.IsNullOrWhiteSpace(value))
            return null;

        string candidate = value.Trim().Replace('/', '\\');
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

    private static TechnicianAttentionPriority? PriorityFor(EvidenceSeverity severity) =>
        severity switch
        {
            EvidenceSeverity.Medium => TechnicianAttentionPriority.Low,
            EvidenceSeverity.High or EvidenceSeverity.Critical => TechnicianAttentionPriority.Medium,
            _ => null
        };

    private static EvidenceConfidence WeakestConfidence(
        IEnumerable<EvidenceCorrelationRecord> records) =>
        records.Select(record => record.Confidence).Min();

    private static bool MetadataEquals(
        EvidenceCorrelationRecord record,
        string key,
        string expected) =>
        record.Metadata.TryGetValue(key, out string? value) &&
        string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);

    private static string SafeApplicationName(EvidenceCorrelationRecord process)
    {
        string value = process.Metadata.TryGetValue("applicationName", out string? name) &&
                       !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : "application";
        value = value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        return value.Length <= 120 ? value : value[..120];
    }

    private static string BuildRationale(
        string application,
        int startupCount,
        EvidenceCorrelationRecord process)
    {
        string startup = startupCount == 1
            ? $"The latest Startup Intelligence observation shows that {application} is configured to launch at sign-in."
            : $"The latest Startup Intelligence observations include {startupCount} configured startup entries for this executable.";
        string pressure = Metadata(process, "pressureLevel") ?? process.Severity.ToString().ToUpperInvariant();
        string? memory = ValidNumber(Metadata(process, "totalMemoryMb"));
        string processFact = memory == null
            ? $"The latest Process Intelligence observation recorded {pressure} resource pressure."
            : $"The latest Process Intelligence observation recorded {memory} MB of combined working-set memory with {pressure} resource pressure.";
        return $"{startup} {processFact}";
    }

    private static string? Metadata(EvidenceCorrelationRecord record, string key) =>
        record.Metadata.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;

    private static string? ValidNumber(string? value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) ||
            !double.IsFinite(parsed) || parsed < 0)
            return null;
        return parsed.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string AttentionId(string entityKey, IEnumerable<Guid> evidenceIds)
    {
        string identity = $"{StableRuleId}|{entityKey}|{string.Join(',', evidenceIds)}";
        return "attention:" + Hash(identity);
    }

    private static string Hash(string value)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(digest)[..HashLength];
    }
}
