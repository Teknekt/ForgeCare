using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class ProcessIntelligenceEvidenceAdapter
{
    private const int MaximumMetadataValueLength = 512;
    private const int MaximumMessages = 32;
    private const int MaximumMemberPids = 16;
    private readonly ProcessEvidencePathProjector _pathProjector;
    private readonly ProcessEvidenceCorrelationKeyBuilder _correlationBuilder;

    public ProcessIntelligenceEvidenceAdapter(
        ProcessEvidencePathProjector? pathProjector = null,
        ProcessEvidenceCorrelationKeyBuilder? correlationBuilder = null)
    {
        _pathProjector = pathProjector ?? new ProcessEvidencePathProjector();
        _correlationBuilder = correlationBuilder ?? new ProcessEvidenceCorrelationKeyBuilder();
    }

    public EvidenceCollectionResult Collect(
        ProcessIntelligenceResult intelligence,
        string sessionId,
        DateTime timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(intelligence);
        var result = new EvidenceCollectionResult();
        AddInputMessages(result, intelligence);

        if (!Guid.TryParseExact(sessionId, "N", out _))
        {
            result.Errors.Add("Process Intelligence Evidence requires a report session GUID in N format.");
            return result;
        }

        if (timestampUtc.Kind != DateTimeKind.Utc)
        {
            result.Errors.Add("Process Intelligence Evidence requires an explicit UTC timestamp.");
            return result;
        }

        for (int index = 0; index < intelligence.Groups.Count; index++)
        {
            ProcessApplicationGroup? group = intelligence.Groups[index];
            if (group == null)
            {
                AddBounded(result.Errors, "A null Process Intelligence group could not be translated.");
                continue;
            }

            try
            {
                EvidenceRecord record = CreateRecord(group, sessionId, timestampUtc);
                IReadOnlyList<string> validation = record.Validate();
                if (validation.Count > 0)
                {
                    foreach (string error in validation) AddBounded(result.Errors, error);
                    continue;
                }
                result.Evidence.Add(record);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                AddBounded(result.Errors,
                    $"Process application group {index + 1} could not be translated ({ex.GetType().Name}).");
            }
        }

        return result;
    }

    private EvidenceRecord CreateRecord(ProcessApplicationGroup group, string sessionId, DateTime timestampUtc)
    {
        ValidateGroup(group);
        string name = SafeText(group.DisplayName, 120, "Unnamed application");
        string? persistedPath = _pathProjector.Project(group.CanonicalExecutablePath);
        string rationale = BuildSafeRationale(group);
        ProcessExecutableInspection? inspection = group.ExecutableInspection;
        var metadata = new Dictionary<string, string>();

        Add(metadata, "applicationName", name);
        Add(metadata, "identityStrength", group.IdentityStrength.ToString());
        Add(metadata, "normalizedExecutablePath", persistedPath);
        Add(metadata, "executableName", inspection?.FileName);
        Add(metadata, "instanceCount", Integer(group.MemberCount));
        Add(metadata, "totalCpuPercent", Number(group.TotalCpuPercent));
        Add(metadata, "totalMemoryMb", Number(group.TotalMemoryMb));
        Add(metadata, "maximumInstanceCpuPercent", Number(group.MaximumInstanceCpuPercent));
        Add(metadata, "maximumInstanceMemoryMb", Number(group.MaximumInstanceMemoryMb));
        Add(metadata, "maximumPressureScore", Number(group.MaximumPressureScore));
        Add(metadata, "pressureLevel", group.StrongestPressureLevel);
        Add(metadata, "companyName", inspection?.CompanyName);
        Add(metadata, "productName", inspection?.ProductName);
        Add(metadata, "fileDescription", inspection?.FileDescription);
        Add(metadata, "fileVersion", inspection?.FileVersion);
        Add(metadata, "productVersion", inspection?.ProductVersion);
        Add(metadata, "originalFilename", inspection?.OriginalFilename);
        Add(metadata, "signatureStatus", inspection?.SignatureStatus.ToString());
        Add(metadata, "signerName", inspection?.SignerName);
        Add(metadata, "classification", group.Classification.ToString());
        Add(metadata, "classificationRationale", rationale);
        int[] pids = group.Members.Select(member => member.Observation.ProcessId).OrderBy(id => id).ToArray();
        Add(metadata, "memberPids", string.Join(',', pids.Take(MaximumMemberPids)));
        Add(metadata, "memberPidCount", Integer(pids.Length));

        return new EvidenceRecord
        {
            SessionId = sessionId,
            TimestampUtc = timestampUtc,
            Category = EvidenceCategory.Process,
            Source = EvidenceSource.ProcessIntelligence,
            Subject = "process-application:" + ProcessEvidenceCorrelationKeyBuilder.NormalizeName(name),
            Observation = BuildObservation(group, name),
            Value = group.TotalMemoryMb,
            Unit = "MB",
            Severity = MapSeverity(group.StrongestPressureLevel),
            Confidence = group.Confidence,
            Collector = nameof(ProcessIntelligenceEvidenceAdapter),
            Metadata = metadata,
            CorrelationKey = _correlationBuilder.Build(group, persistedPath)
        };
    }

    public static EvidenceSeverity MapSeverity(string? pressureLevel) =>
        pressureLevel?.Trim().ToUpperInvariant() switch
        {
            "MINIMAL" or "NORMAL" => EvidenceSeverity.Informational,
            "LOW" => EvidenceSeverity.Low,
            "MODERATE" or "MEDIUM" or "ELEVATED" => EvidenceSeverity.Medium,
            "HIGH" => EvidenceSeverity.High,
            _ => EvidenceSeverity.Unknown
        };

    private static string BuildObservation(ProcessApplicationGroup group, string name)
    {
        string instances = group.MemberCount == 1 ? "One process instance was" : $"{group.MemberCount} process instances were";
        string identity = group.IdentityStrength == ProcessIdentityStrength.Strong
            ? "grouped under the same executable identity"
            : "observed with provisional application identity because an executable path was unavailable";
        string signature = group.ExecutableInspection?.SignatureStatus switch
        {
            ProcessSignatureStatus.Valid => " The executable carried a valid locally evaluated Authenticode signature.",
            ProcessSignatureStatus.NotSigned => " The executable was not Authenticode signed at inspection time.",
            _ => string.Empty
        };
        return $"{instances} {identity} for '{name}' and used {Number(group.TotalMemoryMb)} MB of combined working-set memory during the completed analysis.{signature}";
    }

    private static string BuildSafeRationale(ProcessApplicationGroup group) => group.Classification switch
    {
        ProcessIdentityClassification.Verified => "Executable identity was established with a locally valid Authenticode result.",
        ProcessIdentityClassification.Known => "Executable identity was established and the executable was not Authenticode signed.",
        ProcessIdentityClassification.Unverified => "Executable identity was established, but local provenance verification was incomplete or not valid.",
        _ => "Executable application identity could not be established beyond the completed process observation."
    };

    private static void ValidateGroup(ProcessApplicationGroup group)
    {
        if (group.Members.Count == 0) throw new ArgumentException("Process application group has no members.");
        if (string.IsNullOrWhiteSpace(group.DisplayName)) throw new ArgumentException("Process application display name is missing.");
        if (group.IdentityStrength == ProcessIdentityStrength.Strong && string.IsNullOrWhiteSpace(group.CanonicalExecutablePath))
            throw new ArgumentException("Strong process identity requires an executable path.");
        double[] values = [group.TotalCpuPercent, group.TotalMemoryMb, group.MaximumInstanceCpuPercent,
            group.MaximumInstanceMemoryMb, group.MaximumPressureScore];
        if (values.Any(value => !double.IsFinite(value) || value < 0))
            throw new ArgumentException("Aggregate process metrics must be finite and non-negative.");
    }

    private static void AddInputMessages(EvidenceCollectionResult target, ProcessIntelligenceResult source)
    {
        if (source.Warnings.Count > 0)
            AddBounded(target.Warnings, $"Process Intelligence completed with {source.Warnings.Count} warning(s).");
        if (source.Errors.Count > 0)
            AddBounded(target.Errors, $"Process Intelligence completed with {source.Errors.Count} error(s).");
    }

    private static void Add(IDictionary<string, string> metadata, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        string bounded = SafeText(value, MaximumMetadataValueLength, string.Empty);
        if (bounded.Length > 0) metadata[key] = bounded;
    }

    private static string SafeText(string? value, int maximum, string fallback)
    {
        string result = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        result = result.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        return result.Length <= maximum ? result : result[..maximum];
    }

    private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    private static string Integer(int value) => value.ToString(CultureInfo.InvariantCulture);
    private static void AddBounded(ICollection<string> messages, string value)
    {
        if (messages.Count < MaximumMessages) messages.Add(SafeText(value, 240, "Process Intelligence Evidence message."));
    }
}
