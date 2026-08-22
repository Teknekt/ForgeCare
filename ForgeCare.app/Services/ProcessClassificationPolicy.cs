using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class ProcessClassificationPolicy
{
    public ProcessClassificationDecision Classify(
        ProcessIdentityStrength identityStrength,
        ProcessExecutableInspection? inspection)
    {
        if (identityStrength == ProcessIdentityStrength.Provisional || inspection == null)
        {
            return new ProcessClassificationDecision(
                ProcessIdentityClassification.Unknown,
                EvidenceConfidence.High,
                "Executable identity was unavailable for this process observation.");
        }

        if (inspection.FileStatus == ProcessFileInspectionStatus.Available &&
            inspection.Exists == true &&
            inspection.SignatureStatus == ProcessSignatureStatus.Valid)
        {
            return new ProcessClassificationDecision(
                ProcessIdentityClassification.Verified,
                EvidenceConfidence.High,
                "Executable identity was resolved and a locally valid Authenticode signature was found.");
        }

        if (inspection.FileStatus == ProcessFileInspectionStatus.Available &&
            inspection.Exists == true &&
            inspection.SignatureStatus == ProcessSignatureStatus.NotSigned)
        {
            return new ProcessClassificationDecision(
                ProcessIdentityClassification.Known,
                EvidenceConfidence.High,
                "Executable identity was resolved and the executable is not Authenticode signed.");
        }

        if (inspection.Exists == true && inspection.SignatureStatus is
            ProcessSignatureStatus.HashMismatch or
            ProcessSignatureStatus.Untrusted or
            ProcessSignatureStatus.Invalid)
        {
            return new ProcessClassificationDecision(
                ProcessIdentityClassification.Unverified,
                EvidenceConfidence.High,
                "Executable identity was resolved, but local Authenticode verification did not produce a valid result.");
        }

        if (inspection.FileStatus is ProcessFileInspectionStatus.Missing or ProcessFileInspectionStatus.Unsupported ||
            inspection.SignatureStatus is ProcessSignatureStatus.FileMissing or ProcessSignatureStatus.Unsupported)
        {
            return new ProcessClassificationDecision(
                ProcessIdentityClassification.Unverified,
                EvidenceConfidence.Medium,
                "Executable identity was resolved, but complete executable provenance was unavailable.");
        }

        return new ProcessClassificationDecision(
            ProcessIdentityClassification.Unknown,
            EvidenceConfidence.Medium,
            "Executable identity was resolved, but inspection did not provide reliable provenance.");
    }
}
