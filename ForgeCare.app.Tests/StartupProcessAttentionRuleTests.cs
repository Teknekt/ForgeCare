using ForgeCare.App.Models;
using ForgeCare.App.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForgeCare.app.Tests;

[TestClass]
public sealed class StartupProcessAttentionRuleTests
{
    [TestMethod]
    public void EligibleExactPathProducesTraceableImmutableAttention()
    {
        EvidenceRecord startup = EvidenceCorrelationTestFactory.Startup(confidence: EvidenceConfidence.Medium);
        EvidenceRecord process = EvidenceCorrelationTestFactory.Process(confidence: EvidenceConfidence.High);

        EvidenceCorrelationResult result = Engine().Correlate([process, startup], EvidenceCorrelationTestFactory.SessionId);
        TechnicianAttentionItem item = result.Items.Single();

        Assert.AreEqual(StartupProcessAttentionRule.StableRuleId, item.RuleId);
        Assert.AreEqual(EvidenceCategory.Application, item.Category);
        Assert.AreEqual(TechnicianAttentionPriority.Medium, item.Priority);
        Assert.AreEqual(EvidenceConfidence.Medium, item.Confidence);
        Assert.AreEqual(process.TimestampUtc, item.LatestEvidenceTimestampUtc);
        CollectionAssert.AreEquivalent(new[] { startup.Id, process.Id }, item.EvidenceIds.ToArray());
        CollectionAssert.AreEquivalent(new[] { startup.CorrelationKey!, process.CorrelationKey! }, item.CorrelationKeys.ToArray());
        StringAssert.StartsWith(item.EntityKey, "app:");
        Assert.AreEqual(20, item.EntityKey.Length);
        StringAssert.StartsWith(item.Id, "attention:");
        Assert.AreEqual(26, item.Id.Length);
        Assert.ThrowsExactly<NotSupportedException>(() =>
            ((IList<Guid>)item.EvidenceIds).Add(Guid.NewGuid()));
        Assert.ThrowsExactly<NotSupportedException>(() =>
            ((IList<string>)item.CorrelationKeys).Add("new:key"));
    }

    [TestMethod]
    public void MatchingIsCaseInsensitiveAndDoesNotRequireMatchingNames()
    {
        EvidenceRecord startup = EvidenceCorrelationTestFactory.Startup(
            "%PROGRAMFILES%/Acme/AGENT.EXE", name: "Startup Alias");
        EvidenceRecord process = EvidenceCorrelationTestFactory.Process(
            "%programfiles%\\acme\\agent.exe", name: "Product Display Name");

        TechnicianAttentionItem item = Engine()
            .Correlate([startup, process], EvidenceCorrelationTestFactory.SessionId).Items.Single();

        StringAssert.Contains(item.Title, "Product Display Name");
        Assert.IsFalse(item.EntityKey.Contains("agent.exe", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void MultipleStartupEntriesProduceOneItemWithAllSupport()
    {
        EvidenceRecord first = EvidenceCorrelationTestFactory.Startup(correlationKey: "startup:first");
        EvidenceRecord second = EvidenceCorrelationTestFactory.Startup(correlationKey: "startup:second");
        EvidenceRecord process = EvidenceCorrelationTestFactory.Process();

        TechnicianAttentionItem item = Engine()
            .Correlate([first, process, second], EvidenceCorrelationTestFactory.SessionId).Items.Single();

        Assert.HasCount(3, item.EvidenceIds);
        Assert.HasCount(3, item.CorrelationKeys);
        StringAssert.Contains(item.Rationale, "2 configured startup entries");
    }

    [TestMethod]
    public void DifferentPathsDoNotCorrelateEvenWhenNamesMatch()
    {
        EvidenceRecord startup = EvidenceCorrelationTestFactory.Startup("%PROGRAMFILES%\\One\\agent.exe");
        EvidenceRecord process = EvidenceCorrelationTestFactory.Process("%PROGRAMFILES%\\Two\\agent.exe");

        Assert.IsEmpty(Engine().Correlate([startup, process], EvidenceCorrelationTestFactory.SessionId).Items);
    }

    [TestMethod]
    public void NameOnlyAndProvisionalIdentityDoNotCorrelate()
    {
        EvidenceRecord startup = EvidenceCorrelationTestFactory.Startup();
        EvidenceRecord missingPath = EvidenceCorrelationTestFactory.Process(path: "", identityStrength: "Strong");
        EvidenceRecord provisional = EvidenceCorrelationTestFactory.Process(identityStrength: "Provisional");

        Assert.IsEmpty(Engine().Correlate([startup, missingPath], EvidenceCorrelationTestFactory.SessionId).Items);
        Assert.IsEmpty(Engine().Correlate([startup, provisional], EvidenceCorrelationTestFactory.SessionId).Items);
    }

    [TestMethod]
    [DataRow("<redacted>\\agent.exe")]
    [DataRow("<custom-root>\\abc\\agent.exe")]
    [DataRow("%USERPROFILE%\\agent.exe")]
    [DataRow("agent.exe")]
    [DataRow("C:\\Apps\\agent.exe")]
    [DataRow("%PROGRAMFILES%\\..\\agent.exe")]
    [DataRow("%UNKNOWN%\\agent.exe")]
    public void IneligibleIdentitiesAreRejected(string path)
    {
        Assert.IsEmpty(Engine().Correlate(
            [EvidenceCorrelationTestFactory.Startup(path), EvidenceCorrelationTestFactory.Process(path)],
            EvidenceCorrelationTestFactory.SessionId).Items);
    }

    [TestMethod]
    [DataRow(EvidenceSeverity.Informational)]
    [DataRow(EvidenceSeverity.Low)]
    [DataRow(EvidenceSeverity.Unknown)]
    public void NonElevatedProcessSeverityProducesNoItem(EvidenceSeverity severity)
    {
        Assert.IsEmpty(Engine().Correlate(
            [EvidenceCorrelationTestFactory.Startup(), EvidenceCorrelationTestFactory.Process(severity: severity)],
            EvidenceCorrelationTestFactory.SessionId).Items);
    }

    [TestMethod]
    [DataRow(EvidenceSeverity.Medium, TechnicianAttentionPriority.Low)]
    [DataRow(EvidenceSeverity.High, TechnicianAttentionPriority.Medium)]
    [DataRow(EvidenceSeverity.Critical, TechnicianAttentionPriority.Medium)]
    public void ElevatedSeverityMapsToSeparateAttentionPriority(
        EvidenceSeverity severity,
        TechnicianAttentionPriority priority)
    {
        TechnicianAttentionItem item = Engine().Correlate(
            [EvidenceCorrelationTestFactory.Startup(), EvidenceCorrelationTestFactory.Process(severity: severity)],
            EvidenceCorrelationTestFactory.SessionId).Items.Single();
        Assert.AreEqual(priority, item.Priority);
    }

    [TestMethod]
    public void WeakestRequiredConfidenceWinsAcrossEverySupportingRecord()
    {
        TechnicianAttentionItem item = Engine().Correlate(
        [
            EvidenceCorrelationTestFactory.Startup(correlationKey: "startup:one", confidence: EvidenceConfidence.High),
            EvidenceCorrelationTestFactory.Startup(correlationKey: "startup:two", confidence: EvidenceConfidence.Low),
            EvidenceCorrelationTestFactory.Process(confidence: EvidenceConfidence.Medium)
        ], EvidenceCorrelationTestFactory.SessionId).Items.Single();

        Assert.AreEqual(EvidenceConfidence.Low, item.Confidence);
    }

    [TestMethod]
    public void WordingIsFactualAdvisoryAndBounded()
    {
        TechnicianAttentionItem item = Engine().Correlate(
            [EvidenceCorrelationTestFactory.Startup(), EvidenceCorrelationTestFactory.Process()],
            EvidenceCorrelationTestFactory.SessionId).Items.Single();
        string allText = $"{item.Title} {item.Summary} {item.Rationale} {item.SuggestedInvestigation}";

        StringAssert.Contains(item.Rationale, "259 MB");
        StringAssert.Contains(item.SuggestedInvestigation, "Review");
        foreach (string forbidden in new[] { "caused", "unnecessary", "unsafe", "Disable", "Kill", "Remove" })
            Assert.IsFalse(allText.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        Assert.IsLessThanOrEqualTo(180, item.Title.Length);
        Assert.IsLessThanOrEqualTo(400, item.Summary.Length);
        Assert.IsLessThanOrEqualTo(900, item.Rationale.Length);
    }

    private static EvidenceCorrelationEngine Engine() =>
        new([new StartupProcessAttentionRule()]);
}
