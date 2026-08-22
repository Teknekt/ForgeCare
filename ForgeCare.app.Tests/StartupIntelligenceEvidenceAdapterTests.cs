using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class StartupIntelligenceEvidenceAdapterTests
{
    private static readonly string SessionId = Guid.NewGuid().ToString("N");
    private static readonly DateTime TimestampUtc = new(2026, 8, 20, 10, 30, 0, DateTimeKind.Utc);

    [TestMethod]
    public void VerifiedEntryProducesInformationalPrivacySafeEvidence()
    {
        EvidenceRecord record = CollectOne(StartupEvidenceTestFactory.Entry());

        Assert.AreEqual(EvidenceCategory.Startup, record.Category);
        Assert.AreEqual(EvidenceSource.StartupIntelligence, record.Source);
        Assert.AreEqual(EvidenceSeverity.Informational, record.Severity);
        Assert.AreEqual(EvidenceConfidence.High, record.Confidence);
        Assert.AreEqual(SessionId, record.SessionId);
        Assert.AreEqual(TimestampUtc, record.TimestampUtc);
        Assert.AreEqual(nameof(StartupIntelligenceEvidenceAdapter), record.Collector);
        StringAssert.Contains(record.Observation, "valid locally evaluated Authenticode signature");
        Assert.AreEqual(@"%LOCALAPPDATA%\Vendor\App.exe", record.Metadata["normalizedExecutablePath"]);
        Assert.IsTrue(record.CorrelationKey!.StartsWith("startup:hkcu-run:example:", StringComparison.Ordinal));
        Assert.IsLessThanOrEqualTo(16, record.Metadata.Count);
    }

    [TestMethod]
    public void KnownUnsignedEntryIsFactualAndInformational()
    {
        EvidenceRecord record = CollectOne(StartupEvidenceTestFactory.Entry(
            signature: StartupSignatureStatus.NotSigned,
            classification: StartupClassification.Known));

        Assert.AreEqual(EvidenceSeverity.Informational, record.Severity);
        StringAssert.Contains(record.Observation, "without an Authenticode signature");
        StringAssert.Contains(record.Observation, "does not indicate malicious behavior");
        Assert.IsFalse(record.Observation.Contains("suspicious", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrokenEntryIsMediumAndContainsNoActionRecommendation()
    {
        EvidenceRecord record = CollectOne(StartupEvidenceTestFactory.Entry(
            fileStatus: StartupFileInspectionStatus.Missing,
            fileExists: false,
            signature: StartupSignatureStatus.FileMissing,
            classification: StartupClassification.Broken));

        Assert.AreEqual(EvidenceSeverity.Medium, record.Severity);
        StringAssert.Contains(record.Observation, "not present");
        AssertConservative(record.Observation);
    }

    [TestMethod]
    public void LauncherEntryDoesNotInventPayloadPath()
    {
        EvidenceRecord record = CollectOne(StartupEvidenceTestFactory.Entry(
            command: "powershell.exe -File task.ps1",
            resolvedPath: null,
            resolution: StartupCommandResolutionStatus.LauncherMediated,
            fileStatus: StartupFileInspectionStatus.NotChecked,
            fileExists: null,
            signature: StartupSignatureStatus.NotChecked,
            classification: StartupClassification.Unverified,
            arguments: "-File task.ps1",
            launcher: "powershell.exe"));

        Assert.AreEqual(EvidenceSeverity.Low, record.Severity);
        StringAssert.Contains(record.Observation, "payload was not resolved");
        Assert.AreEqual("powershell.exe", record.Metadata["launcherName"]);
        Assert.IsFalse(record.Metadata.ContainsKey("normalizedExecutablePath"));
        Assert.IsFalse(record.Metadata.Values.Any(value => value.Contains("task.ps1", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void AmbiguousEntryIsUnknownNotBroken()
    {
        EvidenceRecord record = CollectOne(StartupEvidenceTestFactory.Entry(
            resolvedPath: null,
            resolution: StartupCommandResolutionStatus.Ambiguous,
            fileStatus: StartupFileInspectionStatus.NotChecked,
            fileExists: null,
            signature: StartupSignatureStatus.NotChecked,
            classification: StartupClassification.Unknown,
            confidence: EvidenceConfidence.Medium));

        Assert.AreEqual(EvidenceSeverity.Unknown, record.Severity);
        Assert.AreEqual(EvidenceConfidence.Medium, record.Confidence);
        StringAssert.Contains(record.Observation, "could not be resolved confidently");
        Assert.IsFalse(record.Observation.Contains("broken", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [DataRow(StartupSignatureStatus.HashMismatch, "digest did not match")]
    [DataRow(StartupSignatureStatus.Untrusted, "not trusted by local/offline")]
    [DataRow(StartupSignatureStatus.InspectionFailure, "could not be determined")]
    public void UnverifiedSignatureStatesRemainConservative(
        StartupSignatureStatus status,
        string expectedText)
    {
        EvidenceRecord record = CollectOne(StartupEvidenceTestFactory.Entry(
            signature: status,
            classification: StartupClassification.Unverified));

        Assert.AreEqual(EvidenceSeverity.Low, record.Severity);
        StringAssert.Contains(record.Observation, expectedText);
        AssertConservative(record.Observation);
    }

    [TestMethod]
    public void ConstructedSuspiciousInputMapsHighWithoutCreatingDetectionRule()
    {
        EvidenceRecord record = CollectOne(StartupEvidenceTestFactory.Entry(
            classification: StartupClassification.Suspicious));

        Assert.AreEqual(EvidenceSeverity.High, record.Severity);
        Assert.AreEqual("Suspicious", record.Metadata["classification"]);
    }

    [TestMethod]
    public void InvalidSessionOrTimestampProducesNoEvidence()
    {
        var intelligence = new StartupIntelligenceResult([StartupEvidenceTestFactory.Entry()]);
        StartupIntelligenceEvidenceAdapter adapter = StartupEvidenceTestFactory.Adapter();

        EvidenceCollectionResult invalidSession = adapter.Collect(intelligence, "invalid", TimestampUtc);
        EvidenceCollectionResult invalidTimestamp = adapter.Collect(
            intelligence,
            SessionId,
            DateTime.SpecifyKind(TimestampUtc, DateTimeKind.Unspecified));

        Assert.IsEmpty(invalidSession.Evidence);
        Assert.IsNotEmpty(invalidSession.Errors);
        Assert.IsEmpty(invalidTimestamp.Evidence);
        Assert.IsNotEmpty(invalidTimestamp.Errors);
    }

    [TestMethod]
    public void PartialInputAndInvalidEntryPreserveValidEvidence()
    {
        StartupIntelligenceEntry valid = StartupEvidenceTestFactory.Entry();
        var intelligence = new StartupIntelligenceResult(
            new StartupIntelligenceEntry[] { valid, null! },
            warnings: ["Transient collection warning"],
            errors: ["Transient collection error"]);

        EvidenceCollectionResult result = StartupEvidenceTestFactory.Adapter().Collect(
            intelligence,
            SessionId,
            TimestampUtc);

        Assert.HasCount(1, result.Evidence);
        Assert.IsTrue(result.PartialSuccess);
        Assert.IsNotEmpty(result.Warnings);
        Assert.IsGreaterThanOrEqualTo(2, result.Errors.Count);
    }

    private static EvidenceRecord CollectOne(StartupIntelligenceEntry entry)
    {
        EvidenceCollectionResult result = StartupEvidenceTestFactory.Adapter().Collect(
            new StartupIntelligenceResult([entry]),
            SessionId,
            TimestampUtc);

        Assert.IsEmpty(result.Errors);
        return result.Evidence.Single();
    }

    private static void AssertConservative(string value)
    {
        Assert.IsFalse(value.Contains("safe application", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(value.Contains("should disable", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(value.Contains("should remove", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(value.Contains("unnecessary", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(value.Contains("malware", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(value.Contains("virus", StringComparison.OrdinalIgnoreCase));
    }
}
