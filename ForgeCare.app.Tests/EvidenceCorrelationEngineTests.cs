using ForgeCare.App.Models;
using ForgeCare.App.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForgeCare.app.Tests;

[TestClass]
public sealed class EvidenceCorrelationEngineTests
{
    [TestMethod]
    public void EmptyEvidenceSucceedsWithNoItems()
    {
        EvidenceCorrelationResult result = new EvidenceCorrelationEngine().Correlate(
            Array.Empty<EvidenceRecord>(), EvidenceCorrelationTestFactory.SessionId);
        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.PartialSuccess);
        Assert.IsEmpty(result.Items);
    }

    [TestMethod]
    public void InvalidAndMismatchedRecordsAreExcludedWhileValidCorrelationSurvives()
    {
        EvidenceRecord invalid = EvidenceCorrelationTestFactory.Startup();
        invalid.TimestampUtc = DateTime.Now;
        EvidenceRecord mismatch = EvidenceCorrelationTestFactory.Process(
            sessionId: "22222222222222222222222222222222");

        EvidenceCorrelationResult result = new EvidenceCorrelationEngine().Correlate(
        [
            invalid,
            mismatch,
            EvidenceCorrelationTestFactory.Startup(correlationKey: "valid:startup"),
            EvidenceCorrelationTestFactory.Process(correlationKey: "valid:process")
        ], EvidenceCorrelationTestFactory.SessionId);

        Assert.HasCount(1, result.Items);
        Assert.HasCount(2, result.Errors);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.PartialSuccess);
    }

    [TestMethod]
    public void InvalidSessionReturnsSafeFailure()
    {
        EvidenceCorrelationResult result = new EvidenceCorrelationEngine().Correlate([], "not-a-session");
        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.PartialSuccess);
        Assert.HasCount(1, result.Errors);
    }

    [TestMethod]
    public void InputEvidenceAndMetadataAreNotMutatedOrRetained()
    {
        EvidenceRecord startup = EvidenceCorrelationTestFactory.Startup();
        EvidenceRecord process = EvidenceCorrelationTestFactory.Process();
        var mutatingRule = new SnapshotMutationProbeRule();
        var engine = new EvidenceCorrelationEngine([mutatingRule]);

        engine.Correlate([startup, process], EvidenceCorrelationTestFactory.SessionId);
        startup.Metadata["normalizedExecutablePath"] = "%PROGRAMFILES%\\Changed\\changed.exe";

        Assert.AreEqual("%PROGRAMFILES%\\Acme\\agent.exe", mutatingRule.SnapshotPath);
        Assert.AreEqual("%PROGRAMFILES%\\Changed\\changed.exe", startup.Metadata["normalizedExecutablePath"]);
        Assert.AreEqual(EvidenceSeverity.High, process.Severity);
    }

    [TestMethod]
    public void LatestHighAndLowObservationsControlCurrentResult()
    {
        DateTime old = EvidenceCorrelationTestFactory.Time;
        DateTime current = old.AddMinutes(1);
        EvidenceRecord startup = EvidenceCorrelationTestFactory.Startup(timestamp: old);

        EvidenceCorrelationResult suppressed = new EvidenceCorrelationEngine().Correlate(
        [
            startup,
            EvidenceCorrelationTestFactory.Process(timestamp: old, severity: EvidenceSeverity.High),
            EvidenceCorrelationTestFactory.Process(timestamp: current, severity: EvidenceSeverity.Low)
        ], EvidenceCorrelationTestFactory.SessionId);
        Assert.IsEmpty(suppressed.Items);

        EvidenceCorrelationResult emitted = new EvidenceCorrelationEngine().Correlate(
        [
            startup,
            EvidenceCorrelationTestFactory.Process(timestamp: old, severity: EvidenceSeverity.Low),
            EvidenceCorrelationTestFactory.Process(timestamp: current, severity: EvidenceSeverity.High)
        ], EvidenceCorrelationTestFactory.SessionId);
        Assert.HasCount(1, emitted.Items);
        Assert.AreEqual(current, emitted.Items[0].LatestEvidenceTimestampUtc);
    }

    [TestMethod]
    public void LatestStartupPerKeyCanSuppressOlderMatch()
    {
        EvidenceCorrelationResult result = new EvidenceCorrelationEngine().Correlate(
        [
            EvidenceCorrelationTestFactory.Startup(
                "%PROGRAMFILES%\\Acme\\agent.exe", timestamp: EvidenceCorrelationTestFactory.Time),
            EvidenceCorrelationTestFactory.Startup(
                "%PROGRAMFILES%\\Other\\agent.exe", timestamp: EvidenceCorrelationTestFactory.Time.AddMinutes(2)),
            EvidenceCorrelationTestFactory.Process("%PROGRAMFILES%\\Acme\\agent.exe")
        ], EvidenceCorrelationTestFactory.SessionId);
        Assert.IsEmpty(result.Items);
    }

    [TestMethod]
    public void EqualTimestampUsesAscendingEvidenceIdTieBreak()
    {
        DateTime time = EvidenceCorrelationTestFactory.Time;
        Guid smaller = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid larger = Guid.Parse("00000000-0000-0000-0000-000000000002");
        EvidenceRecord selectedHigh = EvidenceCorrelationTestFactory.Process(
            timestamp: time, severity: EvidenceSeverity.High, id: smaller);
        EvidenceRecord ignoredLow = EvidenceCorrelationTestFactory.Process(
            timestamp: time, severity: EvidenceSeverity.Low, id: larger);

        TechnicianAttentionItem item = new EvidenceCorrelationEngine().Correlate(
            [ignoredLow, EvidenceCorrelationTestFactory.Startup(timestamp: time), selectedHigh],
            EvidenceCorrelationTestFactory.SessionId).Items.Single();
        CollectionAssert.Contains(item.EvidenceIds.ToArray(), smaller);
        CollectionAssert.DoesNotContain(item.EvidenceIds.ToArray(), larger);
    }

    [TestMethod]
    public void InputOrderDoesNotChangeIdsOrOrdering()
    {
        EvidenceRecord[] evidence =
        [
            EvidenceCorrelationTestFactory.Startup("%PROGRAMFILES%\\One\\app.exe", "startup:one"),
            EvidenceCorrelationTestFactory.Process("%PROGRAMFILES%\\One\\app.exe", "process:one"),
            EvidenceCorrelationTestFactory.Startup("%PROGRAMFILES%\\Two\\app.exe", "startup:two"),
            EvidenceCorrelationTestFactory.Process("%PROGRAMFILES%\\Two\\app.exe", "process:two", severity: EvidenceSeverity.Medium)
        ];
        var engine = new EvidenceCorrelationEngine();

        string[] first = engine.Correlate(evidence, EvidenceCorrelationTestFactory.SessionId).Items.Select(item => item.Id).ToArray();
        string[] second = engine.Correlate(evidence.Reverse().ToArray(), EvidenceCorrelationTestFactory.SessionId).Items.Select(item => item.Id).ToArray();
        CollectionAssert.AreEqual(first, second);
    }

    [TestMethod]
    public void ThrowingRuleIsIsolatedAndSuccessfulRuleSurvives()
    {
        var engine = new EvidenceCorrelationEngine([new SuccessfulRule(), new ThrowingRule()]);
        EvidenceCorrelationResult result = engine.Correlate([], EvidenceCorrelationTestFactory.SessionId);

        Assert.HasCount(1, result.Items);
        Assert.HasCount(1, result.Errors);
        StringAssert.Contains(result.Errors[0], ThrowingRule.Id);
        StringAssert.Contains(result.Errors[0], nameof(InvalidOperationException));
        Assert.IsFalse(result.Errors[0].Contains("sensitive", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(result.PartialSuccess);
    }

    [TestMethod]
    public void DuplicateRuleOutputIsSuppressedAndOrderingIsDeterministic()
    {
        TechnicianAttentionItem duplicate = EvidenceCorrelationTestFactory.Attention();
        TechnicianAttentionItem newerMedium = EvidenceCorrelationTestFactory.Attention(
            "attention:0000000000000002", TechnicianAttentionPriority.Medium,
            EvidenceCorrelationTestFactory.Time.AddMinutes(1), "another-rule-v1", "app:0000000000000002",
            Guid.Parse("00000000-0000-0000-0000-000000000002"));
        var engine = new EvidenceCorrelationEngine(
            [new StaticRule("one", [duplicate, newerMedium]), new StaticRule("two", [duplicate])]);

        EvidenceCorrelationResult result = engine.Correlate([], EvidenceCorrelationTestFactory.SessionId);
        Assert.HasCount(2, result.Items);
        Assert.AreEqual(newerMedium.Id, result.Items[0].Id);
        Assert.AreEqual(duplicate.Id, result.Items[1].Id);
    }

    [TestMethod]
    public void AllFourEvidenceSourcesCoexistWithoutChangingCorrelation()
    {
        EvidenceRecord startup = EvidenceCorrelationTestFactory.Startup();
        EvidenceRecord process = EvidenceCorrelationTestFactory.Process();
        var engine = new EvidenceCorrelationEngine();
        TechnicianAttentionItem baseline = engine.Correlate([startup, process], EvidenceCorrelationTestFactory.SessionId).Items.Single();
        TechnicianAttentionItem combined = engine.Correlate(
        [
            EvidenceCorrelationTestFactory.Other(EvidenceSource.SystemScan, 10),
            startup,
            EvidenceCorrelationTestFactory.Other(EvidenceSource.DeepAnalysis, 11),
            process
        ], EvidenceCorrelationTestFactory.SessionId).Items.Single();

        Assert.AreEqual(baseline.Id, combined.Id);
        Assert.AreEqual(baseline.Priority, combined.Priority);
        Assert.AreEqual(baseline.Confidence, combined.Confidence);
    }

    [TestMethod]
    [DataRow(100, 500)]
    [DataRow(200, 1000)]
    public void ScaleSnapshotsRemainDeterministic(int entityCount, int totalRecords)
    {
        var records = new List<EvidenceRecord>(totalRecords);
        for (int index = 0; index < entityCount; index++)
        {
            string path = $"%PROGRAMFILES%\\App{index}\\app.exe";
            records.Add(EvidenceCorrelationTestFactory.Startup(path, $"startup:{index}", EvidenceCorrelationTestFactory.Time));
            records.Add(EvidenceCorrelationTestFactory.Startup(path, $"startup:{index}", EvidenceCorrelationTestFactory.Time.AddMinutes(1)));
            records.Add(EvidenceCorrelationTestFactory.Process(path, $"process:{index}", EvidenceCorrelationTestFactory.Time, EvidenceSeverity.Low));
            records.Add(EvidenceCorrelationTestFactory.Process(path, $"process:{index}", EvidenceCorrelationTestFactory.Time.AddMinutes(1), EvidenceSeverity.High));
            records.Add(EvidenceCorrelationTestFactory.Other(index % 2 == 0 ? EvidenceSource.SystemScan : EvidenceSource.DeepAnalysis, index + 1000));
        }
        Assert.HasCount(totalRecords, records);
        var engine = new EvidenceCorrelationEngine();

        EvidenceCorrelationResult first = engine.Correlate(records, EvidenceCorrelationTestFactory.SessionId);
        EvidenceCorrelationResult second = engine.Correlate(records.AsEnumerable().Reverse().ToArray(), EvidenceCorrelationTestFactory.SessionId);

        Assert.HasCount(entityCount, first.Items);
        CollectionAssert.AreEqual(first.Items.Select(item => item.Id).ToArray(), second.Items.Select(item => item.Id).ToArray());
    }

    private sealed class SnapshotMutationProbeRule : IEvidenceCorrelationRule
    {
        public string RuleId => "snapshot-probe-v1";
        public string? SnapshotPath { get; private set; }
        public IReadOnlyList<TechnicianAttentionItem> Evaluate(EvidenceCorrelationContext context)
        {
            SnapshotPath = context.Records.First().Metadata["normalizedExecutablePath"];
            Assert.ThrowsExactly<NotSupportedException>(() =>
                ((IDictionary<string, string>)context.Records.First().Metadata).Add("new", "value"));
            return [];
        }
    }

    private sealed class SuccessfulRule : IEvidenceCorrelationRule
    {
        public string RuleId => "successful-rule-v1";
        public IReadOnlyList<TechnicianAttentionItem> Evaluate(EvidenceCorrelationContext context) =>
            [EvidenceCorrelationTestFactory.Attention(ruleId: RuleId)];
    }

    private sealed class ThrowingRule : IEvidenceCorrelationRule
    {
        public const string Id = "throwing-rule-v1";
        public string RuleId => Id;
        public IReadOnlyList<TechnicianAttentionItem> Evaluate(EvidenceCorrelationContext context) =>
            throw new InvalidOperationException("sensitive arbitrary message");
    }

    private sealed class StaticRule(string id, IReadOnlyList<TechnicianAttentionItem> items) : IEvidenceCorrelationRule
    {
        public string RuleId => id;
        public IReadOnlyList<TechnicianAttentionItem> Evaluate(EvidenceCorrelationContext context) => items;
    }
}
