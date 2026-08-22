using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ProcessIntelligenceEvidenceAdapterTests
{
    [TestMethod]
    public void AggregateMapsToValidatedProcessEvidence()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        DateTime timestamp = new(2026, 8, 22, 12, 0, 0, DateTimeKind.Utc);
        ProcessApplicationGroup group = ProcessEvidenceTestFactory.Group(totalCpu: 135, totalMemory: 1800, memberCount: 4);

        EvidenceCollectionResult result = ProcessEvidenceTestFactory.Adapter().Collect(
            ProcessEvidenceTestFactory.Result(group), sessionId, timestamp);
        EvidenceRecord record = result.Evidence.Single();

        Assert.IsTrue(result.Success);
        Assert.AreEqual(EvidenceCategory.Process, record.Category);
        Assert.AreEqual(EvidenceSource.ProcessIntelligence, record.Source);
        Assert.AreEqual(nameof(ProcessIntelligenceEvidenceAdapter), record.Collector);
        Assert.AreEqual(sessionId, record.SessionId);
        Assert.AreEqual(timestamp, record.TimestampUtc);
        Assert.AreEqual("process-application:vendor-app", record.Subject);
        Assert.AreEqual(1800d, record.Value);
        Assert.AreEqual("MB", record.Unit);
        Assert.AreEqual("135", record.Metadata["totalCpuPercent"]);
        Assert.AreEqual("4", record.Metadata["instanceCount"]);
        Assert.AreEqual("1,2,3,4", record.Metadata["memberPids"]);
        Assert.IsEmpty(record.Validate());
    }

    [TestMethod]
    public void InvalidSessionAndNonUtcTimestampAreRejected()
    {
        ProcessIntelligenceResult intelligence = ProcessEvidenceTestFactory.Result(ProcessEvidenceTestFactory.Group());
        EvidenceCollectionResult session = ProcessEvidenceTestFactory.Adapter().Collect(intelligence, "bad", DateTime.UtcNow);
        EvidenceCollectionResult time = ProcessEvidenceTestFactory.Adapter().Collect(
            intelligence, Guid.NewGuid().ToString("N"), DateTime.Now);

        Assert.IsEmpty(session.Evidence);
        Assert.IsNotEmpty(session.Errors);
        Assert.IsEmpty(time.Evidence);
        Assert.IsNotEmpty(time.Errors);
    }

    [TestMethod]
    public void ProvisionalGroupProducesUnknownIdentityWithoutPath()
    {
        ProcessApplicationGroup group = ProcessEvidenceTestFactory.Group(
            name: "helper", path: null, strength: ProcessIdentityStrength.Provisional,
            classification: ProcessIdentityClassification.Unknown, pressureLevel: "MINIMAL", memberCount: 1);
        EvidenceRecord record = ProcessEvidenceTestFactory.Adapter().Collect(
            ProcessEvidenceTestFactory.Result(group), Guid.NewGuid().ToString("N"), DateTime.UtcNow).Evidence.Single();

        Assert.AreEqual("Provisional", record.Metadata["identityStrength"]);
        Assert.IsFalse(record.Metadata.ContainsKey("normalizedExecutablePath"));
        StringAssert.StartsWith(record.CorrelationKey!, "process-instance:");
        Assert.AreEqual(EvidenceSeverity.Informational, record.Severity);
        Assert.AreEqual(EvidenceConfidence.High, record.Confidence);
    }

    [TestMethod]
    [DataRow("MINIMAL", EvidenceSeverity.Informational)]
    [DataRow("LOW", EvidenceSeverity.Low)]
    [DataRow("MODERATE", EvidenceSeverity.Medium)]
    [DataRow("HIGH", EvidenceSeverity.High)]
    [DataRow("unexpected", EvidenceSeverity.Unknown)]
    public void SeverityMapsOnlyFromPressure(string pressure, EvidenceSeverity expected)
    {
        Assert.AreEqual(expected, ProcessIntelligenceEvidenceAdapter.MapSeverity(pressure));
    }

    [TestMethod]
    public void ClassificationDoesNotControlSeverityAndConfidenceIsCopied()
    {
        ProcessApplicationGroup verifiedHigh = ProcessEvidenceTestFactory.Group(
            classification: ProcessIdentityClassification.Verified, confidence: EvidenceConfidence.Low, pressureLevel: "HIGH");
        ProcessApplicationGroup unknownMinimal = ProcessEvidenceTestFactory.Group(
            path: null, strength: ProcessIdentityStrength.Provisional,
            classification: ProcessIdentityClassification.Unknown, confidence: EvidenceConfidence.High,
            pressureLevel: "MINIMAL", transientIdentity: "provisional");
        EvidenceCollectionResult result = ProcessEvidenceTestFactory.Adapter().Collect(
            ProcessEvidenceTestFactory.Result(verifiedHigh, unknownMinimal), Guid.NewGuid().ToString("N"), DateTime.UtcNow);

        Assert.AreEqual(EvidenceSeverity.High, result.Evidence[0].Severity);
        Assert.AreEqual(EvidenceConfidence.Low, result.Evidence[0].Confidence);
        Assert.AreEqual(EvidenceSeverity.Informational, result.Evidence[1].Severity);
        Assert.AreEqual(EvidenceConfidence.High, result.Evidence[1].Confidence);
    }

    [TestMethod]
    public void InvalidGroupDoesNotDiscardValidGroup()
    {
        ProcessApplicationGroup invalid = ProcessEvidenceTestFactory.Group(path: null, strength: ProcessIdentityStrength.Strong);
        ProcessApplicationGroup valid = ProcessEvidenceTestFactory.Group(transientIdentity: "valid");
        EvidenceCollectionResult result = ProcessEvidenceTestFactory.Adapter().Collect(
            ProcessEvidenceTestFactory.Result(invalid, valid), Guid.NewGuid().ToString("N"), DateTime.UtcNow);

        Assert.HasCount(1, result.Evidence);
        Assert.IsTrue(result.PartialSuccess);
        Assert.IsNotEmpty(result.Errors);
    }

    [TestMethod]
    public void LongOptionalMetadataIsBoundedAndBudgetRemainsBelowLimit()
    {
        string longValue = new('X', 2000);
        ProcessApplicationGroup group = ProcessEvidenceTestFactory.Group(
            name: longValue, company: longValue, product: longValue);
        EvidenceRecord record = ProcessEvidenceTestFactory.Adapter().Collect(
            ProcessEvidenceTestFactory.Result(group), Guid.NewGuid().ToString("N"), DateTime.UtcNow).Evidence.Single();

        Assert.IsLessThanOrEqualTo(EvidenceRecord.MaxMetadataEntries, record.Metadata.Count);
        Assert.IsTrue(record.Metadata.Values.All(value => value.Length <= 512));
        Assert.IsLessThanOrEqualTo(120, record.Metadata["applicationName"].Length);
    }

    [TestMethod]
    public void MemberPidMetadataUsesSortedBoundedSubsetAndFullCount()
    {
        ProcessApplicationGroup group = ProcessEvidenceTestFactory.Group(memberCount: 40);
        EvidenceRecord record = ProcessEvidenceTestFactory.Adapter().Collect(
            ProcessEvidenceTestFactory.Result(group), Guid.NewGuid().ToString("N"), DateTime.UtcNow).Evidence.Single();

        string[] persistedPids = record.Metadata["memberPids"].Split(',');
        Assert.HasCount(16, persistedPids);
        Assert.AreEqual("1", persistedPids[0]);
        Assert.AreEqual("16", persistedPids[^1]);
        Assert.AreEqual("40", record.Metadata["memberPidCount"]);
    }
}
