using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class StartupClassificationPolicyTests
{
    private readonly StartupClassificationPolicy _policy = new();

    [TestMethod]
    public void DirectExistingValidTargetIsVerifiedWithHighConfidence()
    {
        var decision = Decide(Direct(), Available(), Signature(StartupSignatureStatus.Valid));

        Assert.AreEqual(StartupClassification.Verified, decision.Classification);
        Assert.AreEqual(EvidenceConfidence.High, decision.Confidence);
        StringAssert.Contains(decision.Rationale, "local/offline");
        AssertConservative(decision.Rationale);
    }

    [TestMethod]
    public void DirectExistingUnsignedTargetIsKnownNotSuspicious()
    {
        var decision = Decide(Direct(), Available(), Signature(StartupSignatureStatus.NotSigned));

        Assert.AreEqual(StartupClassification.Known, decision.Classification);
        Assert.AreEqual(EvidenceConfidence.High, decision.Confidence);
        StringAssert.Contains(decision.Rationale, "does not indicate malicious");
    }

    [TestMethod]
    public void MissingDirectTargetIsBroken()
    {
        var decision = Decide(
            Direct(),
            new StartupFileInspection { Status = StartupFileInspectionStatus.Missing, Exists = false },
            Signature(StartupSignatureStatus.FileMissing));

        Assert.AreEqual(StartupClassification.Broken, decision.Classification);
        Assert.AreEqual(EvidenceConfidence.High, decision.Confidence);
        StringAssert.Contains(decision.Rationale, "not present");
    }

    [TestMethod]
    [DataRow(StartupCommandResolutionStatus.LauncherMediated)]
    [DataRow(StartupCommandResolutionStatus.ShortcutNotResolved)]
    public void LauncherAndShortcutAreUnverified(StartupCommandResolutionStatus status)
    {
        StartupCommandResolution command = Resolution(status);
        var decision = Decide(command, StartupFileInspection.NotChecked(), StartupSignatureInfo.NotChecked());

        Assert.AreEqual(StartupClassification.Unverified, decision.Classification);
        Assert.AreEqual(EvidenceConfidence.High, decision.Confidence);
    }

    [TestMethod]
    [DataRow(StartupSignatureStatus.HashMismatch)]
    [DataRow(StartupSignatureStatus.Untrusted)]
    [DataRow(StartupSignatureStatus.Invalid)]
    public void InvalidOrUntrustedSignatureIsUnverified(StartupSignatureStatus status)
    {
        var decision = Decide(Direct(), Available(), Signature(status));

        Assert.AreEqual(StartupClassification.Unverified, decision.Classification);
        Assert.AreNotEqual(StartupClassification.Suspicious, decision.Classification);
    }

    [TestMethod]
    [DataRow(StartupCommandResolutionStatus.Empty, EvidenceConfidence.High)]
    [DataRow(StartupCommandResolutionStatus.Malformed, EvidenceConfidence.High)]
    [DataRow(StartupCommandResolutionStatus.Ambiguous, EvidenceConfidence.Medium)]
    public void UnresolvedIdentityIsUnknown(
        StartupCommandResolutionStatus status,
        EvidenceConfidence confidence)
    {
        var decision = Decide(
            Resolution(status),
            StartupFileInspection.NotChecked(),
            StartupSignatureInfo.NotChecked());

        Assert.AreEqual(StartupClassification.Unknown, decision.Classification);
        Assert.AreEqual(confidence, decision.Confidence);
    }

    [TestMethod]
    public void InspectionFailureIsNeverKnownVerifiedOrSuspicious()
    {
        var decision = Decide(
            Direct(),
            new StartupFileInspection { Status = StartupFileInspectionStatus.InspectionFailure },
            Signature(StartupSignatureStatus.InspectionFailure));

        Assert.AreEqual(StartupClassification.Unverified, decision.Classification);
    }

    [TestMethod]
    public void PhaseBDecisionMatrixNeverEmitsSuspicious()
    {
        foreach (StartupCommandResolutionStatus commandStatus in Enum.GetValues<StartupCommandResolutionStatus>())
        foreach (StartupFileInspectionStatus fileStatus in Enum.GetValues<StartupFileInspectionStatus>())
        foreach (StartupSignatureStatus signatureStatus in Enum.GetValues<StartupSignatureStatus>())
        {
            StartupCommandResolution command = commandStatus is StartupCommandResolutionStatus.DirectExecutable or
                StartupCommandResolutionStatus.DirectFile
                ? Direct(commandStatus)
                : Resolution(commandStatus);

            var decision = Decide(
                command,
                new StartupFileInspection { Status = fileStatus },
                Signature(signatureStatus));

            Assert.AreNotEqual(StartupClassification.Suspicious, decision.Classification);
        }
    }

    private (StartupClassification Classification, EvidenceConfidence Confidence, string Rationale) Decide(
        StartupCommandResolution command,
        StartupFileInspection file,
        StartupSignatureInfo signature) =>
        _policy.Classify(command, file, signature);

    private static StartupCommandResolution Direct(
        StartupCommandResolutionStatus status = StartupCommandResolutionStatus.DirectExecutable) =>
        new()
        {
            OriginalCommand = @"C:\Tools\App.exe",
            Status = status,
            ResolvedPath = @"C:\Tools\App.exe",
            Rationale = "direct"
        };

    private static StartupCommandResolution Resolution(StartupCommandResolutionStatus status) =>
        new()
        {
            OriginalCommand = string.Empty,
            Status = status,
            LauncherName = status == StartupCommandResolutionStatus.LauncherMediated ? "powershell.exe" : null,
            Rationale = "No target identity was established."
        };

    private static StartupFileInspection Available() =>
        new()
        {
            Status = StartupFileInspectionStatus.Available,
            Exists = true
        };

    private static StartupSignatureInfo Signature(StartupSignatureStatus status) =>
        new() { Status = status };

    private static void AssertConservative(string rationale)
    {
        Assert.IsFalse(rationale.Contains("safe", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(rationale.Contains("recommended", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(rationale.Contains("dangerous", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(rationale.Contains("virus", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(rationale.Contains("disable", StringComparison.OrdinalIgnoreCase));
    }
}
