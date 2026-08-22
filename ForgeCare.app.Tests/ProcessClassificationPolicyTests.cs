using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ProcessClassificationPolicyTests
{
    private readonly ProcessClassificationPolicy _policy = new();

    [TestMethod]
    [DataRow(ProcessSignatureStatus.Valid, ProcessIdentityClassification.Verified, EvidenceConfidence.High)]
    [DataRow(ProcessSignatureStatus.NotSigned, ProcessIdentityClassification.Known, EvidenceConfidence.High)]
    [DataRow(ProcessSignatureStatus.HashMismatch, ProcessIdentityClassification.Unverified, EvidenceConfidence.High)]
    [DataRow(ProcessSignatureStatus.Untrusted, ProcessIdentityClassification.Unverified, EvidenceConfidence.High)]
    [DataRow(ProcessSignatureStatus.Invalid, ProcessIdentityClassification.Unverified, EvidenceConfidence.High)]
    [DataRow(ProcessSignatureStatus.InspectionFailure, ProcessIdentityClassification.Unknown, EvidenceConfidence.Medium)]
    public void MapsSignatureStateDeterministically(
        ProcessSignatureStatus signature,
        ProcessIdentityClassification classification,
        EvidenceConfidence confidence)
    {
        ProcessExecutableInspection inspection = ProcessIntelligenceTestFactory.Inspection(@"C:\Apps\app.exe", signature);
        ProcessClassificationDecision decision = _policy.Classify(ProcessIdentityStrength.Strong, inspection);

        Assert.AreEqual(classification, decision.Classification);
        Assert.AreEqual(confidence, decision.Confidence);
        Assert.IsFalse(decision.Rationale.Contains("safe", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(decision.Rationale.Contains("danger", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ProvisionalIdentityIsConclusiveUnknown()
    {
        ProcessClassificationDecision result = _policy.Classify(ProcessIdentityStrength.Provisional, null);
        Assert.AreEqual(ProcessIdentityClassification.Unknown, result.Classification);
        Assert.AreEqual(EvidenceConfidence.High, result.Confidence);
    }

    [TestMethod]
    public async Task PressureAndCompanyDoNotChangeClassificationOrSignerIdentity()
    {
        var inspector = new FakeProcessExecutableInspector
        {
            Handler = path => ProcessIntelligenceTestFactory.Inspection(
                path, ProcessSignatureStatus.NotSigned, company: "Company Is Not Signer", signer: null)
        };
        ProcessInstanceObservation low = ProcessIntelligenceTestFactory.Observation(1, cpu: 1, memory: 10, pressure: 1, pressureLevel: "MINIMAL");
        ProcessInstanceObservation high = ProcessIntelligenceTestFactory.Observation(2, cpu: 99, memory: 9999, pressure: 99, pressureLevel: "HIGH");

        ProcessIntelligenceResult result = await new ProcessIntelligenceService(inspector).AnalyzeAsync([low, high]);

        Assert.IsTrue(result.Entries.All(entry => entry.Classification == ProcessIdentityClassification.Known));
        Assert.IsNull(result.Entries[0].ExecutableInspection!.SignerName);
        Assert.AreEqual("Company Is Not Signer", result.Entries[0].ExecutableInspection!.CompanyName);
    }
}
