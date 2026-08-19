using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class StartupClassificationPolicy
{
    public (StartupClassification Classification, EvidenceConfidence Confidence, string Rationale) Classify(
        StartupCommandResolution command,
        StartupFileInspection file,
        StartupSignatureInfo signature)
    {
        if (command.Status == StartupCommandResolutionStatus.LauncherMediated)
        {
            return (
                StartupClassification.Unverified,
                EvidenceConfidence.High,
                $"Startup command uses {command.LauncherName ?? "a recognized program"} as a launcher. " +
                "ForgeCare did not resolve the payload executable during this inspection.");
        }

        if (command.Status == StartupCommandResolutionStatus.ShortcutNotResolved)
        {
            return (
                StartupClassification.Unverified,
                EvidenceConfidence.High,
                "The Startup-folder entry is a shortcut. ForgeCare did not resolve the shortcut target during Phase B.");
        }

        if (!command.HasConfidentDirectPath)
        {
            EvidenceConfidence confidence = command.Status switch
            {
                StartupCommandResolutionStatus.Empty or
                StartupCommandResolutionStatus.Malformed => EvidenceConfidence.High,
                StartupCommandResolutionStatus.Ambiguous => EvidenceConfidence.Medium,
                _ => EvidenceConfidence.Unknown
            };

            return (
                StartupClassification.Unknown,
                confidence,
                command.Rationale);
        }

        if (file.Status == StartupFileInspectionStatus.Missing)
        {
            return (
                StartupClassification.Broken,
                EvidenceConfidence.High,
                "The configured direct executable path was resolved, but the target file was not present.");
        }

        if (file.Status != StartupFileInspectionStatus.Available)
        {
            return (
                StartupClassification.Unverified,
                file.Status == StartupFileInspectionStatus.Unsupported
                    ? EvidenceConfidence.High
                    : EvidenceConfidence.Low,
                $"The startup target was resolved, but file inspection reported {file.Status}. " +
                "ForgeCare could not establish complete target provenance.");
        }

        return signature.Status switch
        {
            StartupSignatureStatus.Valid => (
                StartupClassification.Verified,
                EvidenceConfidence.High,
                "Startup target was resolved and exists. Windows reported a valid Authenticode signature under local/offline trust evaluation."),

            StartupSignatureStatus.NotSigned => (
                StartupClassification.Known,
                EvidenceConfidence.High,
                "Startup target was resolved and exists, but it is not Authenticode-signed. Unsigned status alone does not indicate malicious behavior."),

            StartupSignatureStatus.HashMismatch => (
                StartupClassification.Unverified,
                EvidenceConfidence.High,
                "Startup target exists, but Windows reported that its Authenticode signature digest did not match."),

            StartupSignatureStatus.Untrusted => (
                StartupClassification.Unverified,
                EvidenceConfidence.High,
                "Startup target exists, but Windows did not trust its Authenticode signature under local/offline evaluation."),

            StartupSignatureStatus.Invalid => (
                StartupClassification.Unverified,
                EvidenceConfidence.High,
                "Startup target exists, but Windows reported an invalid Authenticode signature state."),

            StartupSignatureStatus.Unsupported => (
                StartupClassification.Unverified,
                EvidenceConfidence.High,
                "Startup target exists, but its file type is unsupported for Authenticode inspection."),

            _ => (
                StartupClassification.Unknown,
                EvidenceConfidence.Low,
                "Startup target exists, but ForgeCare could not determine its Authenticode status. Inspection failure was not treated as unsigned.")
        };
    }
}
