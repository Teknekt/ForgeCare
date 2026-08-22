using System;
using System.Collections.Generic;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class StartupIntelligenceEvidenceAdapter
{
    private const int MaximumMetadataValueLength = 512;

    private readonly StartupEvidencePathNormalizer _pathNormalizer;
    private readonly StartupEvidenceCorrelationKeyBuilder _correlationKeyBuilder;

    public StartupIntelligenceEvidenceAdapter(
        StartupEvidencePathNormalizer? pathNormalizer = null,
        StartupEvidenceCorrelationKeyBuilder? correlationKeyBuilder = null)
    {
        _pathNormalizer = pathNormalizer ?? new StartupEvidencePathNormalizer();
        _correlationKeyBuilder = correlationKeyBuilder ?? new StartupEvidenceCorrelationKeyBuilder();
    }

    public EvidenceCollectionResult Collect(
        StartupIntelligenceResult intelligence,
        string sessionId,
        DateTime timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(intelligence);

        var result = new EvidenceCollectionResult();
        result.Warnings.AddRange(intelligence.Warnings);
        result.Errors.AddRange(intelligence.Errors);

        if (!Guid.TryParseExact(sessionId, "N", out _))
        {
            result.Errors.Add("Startup Intelligence Evidence requires a report session GUID in N format.");
            return result;
        }

        if (timestampUtc.Kind != DateTimeKind.Utc)
        {
            result.Errors.Add("Startup Intelligence Evidence requires an explicit UTC timestamp.");
            return result;
        }

        foreach (StartupIntelligenceEntry? entry in intelligence.Entries)
        {
            if (entry == null)
            {
                result.Errors.Add("A null Startup Intelligence entry could not be translated to Evidence.");
                continue;
            }

            try
            {
                EvidenceRecord record = CreateRecord(entry, sessionId, timestampUtc);
                IReadOnlyList<string> validationErrors = record.Validate();
                if (validationErrors.Count > 0)
                {
                    result.Errors.AddRange(validationErrors);
                    continue;
                }

                result.Evidence.Add(record);
            }
            catch (Exception ex)
            {
                result.Errors.Add(
                    $"Startup entry '{SafeDisplayName(entry.Name)}' could not be translated: {ex.GetType().Name}.");
            }
        }

        return result;
    }

    private EvidenceRecord CreateRecord(
        StartupIntelligenceEntry entry,
        string sessionId,
        DateTime timestampUtc)
    {
        string? persistedPath = _pathNormalizer.Normalize(
            entry.FileInspection.NormalizedPath ?? entry.CommandResolution.ResolvedPath);
        string observation = BuildObservation(entry);

        var metadata = new Dictionary<string, string>();
        Add(metadata, "entryName", entry.Name);
        Add(metadata, "startupSource", entry.SourceKind.ToString());
        Add(metadata, "resolutionStatus", entry.CommandResolution.Status.ToString());
        Add(metadata, "normalizedExecutablePath", persistedPath);
        Add(metadata, "fileExists", entry.FileInspection.Exists?.ToString().ToLowerInvariant());
        Add(metadata, "fileDescription", entry.FileInspection.FileDescription);
        Add(metadata, "productName", entry.FileInspection.ProductName);
        Add(metadata, "companyName", entry.FileInspection.CompanyName);
        Add(metadata, "fileVersion", entry.FileInspection.FileVersion);
        Add(metadata, "productVersion", entry.FileInspection.ProductVersion);
        Add(metadata, "signatureStatus", entry.Signature.Status.ToString());
        Add(metadata, "signerName", entry.Signature.SignerName);
        Add(metadata, "classification", entry.Classification.ToString());
        Add(metadata, "classificationRationale", observation);
        Add(metadata, "launcherName", entry.CommandResolution.LauncherName);
        Add(metadata, "originalFilename", entry.FileInspection.OriginalFilename);

        return new EvidenceRecord
        {
            SessionId = sessionId,
            TimestampUtc = timestampUtc,
            Category = EvidenceCategory.Startup,
            Source = EvidenceSource.StartupIntelligence,
            Subject = "startup-entry:" +
                      StartupEvidenceCorrelationKeyBuilder.NormalizeSubjectName(entry.Name),
            Observation = observation,
            Severity = MapSeverity(entry.Classification),
            Confidence = entry.Confidence,
            Collector = nameof(StartupIntelligenceEvidenceAdapter),
            Metadata = metadata,
            CorrelationKey = _correlationKeyBuilder.Build(entry, persistedPath)
        };
    }

    private static string BuildObservation(StartupIntelligenceEntry entry)
    {
        string name = SafeDisplayName(entry.Name);
        if (entry.CommandResolution.Status == StartupCommandResolutionStatus.LauncherMediated)
        {
            return $"Startup entry '{name}' uses a launcher-mediated startup command. " +
                   "The launched payload was not resolved by Startup Intelligence.";
        }

        if (entry.CommandResolution.Status == StartupCommandResolutionStatus.ShortcutNotResolved)
        {
            return $"Startup entry '{name}' is a Startup-folder shortcut whose target was not resolved by Startup Intelligence.";
        }

        return entry.Classification switch
        {
            StartupClassification.Verified =>
                $"Startup entry '{name}' resolved to an existing executable with a valid locally evaluated Authenticode signature.",
            StartupClassification.Known =>
                $"Startup entry '{name}' resolved to an existing executable without an Authenticode signature. Unsigned status alone does not indicate malicious behavior.",
            StartupClassification.Broken =>
                $"Startup entry '{name}' resolved to a target that was not present at inspection time.",
            StartupClassification.Suspicious =>
                $"Startup entry '{name}' was classified as suspicious from completed Startup Intelligence observations.",
            StartupClassification.Unverified when entry.Signature.Status == StartupSignatureStatus.HashMismatch =>
                $"Startup entry '{name}' resolved to an existing target whose Authenticode signature digest did not match.",
            StartupClassification.Unverified when entry.Signature.Status == StartupSignatureStatus.Untrusted =>
                $"Startup entry '{name}' resolved to an existing target whose Authenticode signature was not trusted by local/offline Windows evaluation.",
            StartupClassification.Unverified when entry.Signature.Status == StartupSignatureStatus.InspectionFailure =>
                $"Startup entry '{name}' resolved to an existing target, but its Authenticode status could not be determined.",
            StartupClassification.Unverified =>
                $"Startup entry '{name}' could not be fully verified from the completed Startup Intelligence observations.",
            _ =>
                $"Startup entry '{name}' could not be resolved confidently from the configured startup information."
        };
    }

    private static EvidenceSeverity MapSeverity(StartupClassification classification) =>
        classification switch
        {
            StartupClassification.Verified => EvidenceSeverity.Informational,
            StartupClassification.Known => EvidenceSeverity.Informational,
            StartupClassification.Unverified => EvidenceSeverity.Low,
            StartupClassification.Broken => EvidenceSeverity.Medium,
            StartupClassification.Suspicious => EvidenceSeverity.High,
            _ => EvidenceSeverity.Unknown
        };

    private static void Add(
        IDictionary<string, string> metadata,
        string key,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        string bounded = value.Trim();
        metadata[key] = bounded.Length <= MaximumMetadataValueLength
            ? bounded
            : bounded[..MaximumMetadataValueLength];
    }

    private static string SafeDisplayName(string? name)
    {
        string value = string.IsNullOrWhiteSpace(name) ? "Unnamed startup entry" : name.Trim();
        value = value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        return value.Length <= 120 ? value : value[..120];
    }
}
