using ForgeCare.App.Models;
using ForgeCare.App.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForgeCare.app.Tests;

[TestClass]
public sealed class ProcessSystemCpuAttentionRuleTests
{
    [TestMethod]
    public void SameAnalysisElevatedCpuProducesFactualTraceableAttention()
    {
        DateTime time = EvidenceCorrelationTestFactory.Time;
        EvidenceRecord process = Process(time, EvidenceSeverity.High, EvidenceConfidence.Medium, 18.4);
        EvidenceRecord cpu = Cpu(time, EvidenceSeverity.High, EvidenceConfidence.High, 72.1);

        TechnicianAttentionItem item = Engine().Correlate(
            [cpu, process], EvidenceCorrelationTestFactory.SessionId).Items.Single();

        Assert.AreEqual(ProcessSystemCpuAttentionRule.StableRuleId, item.RuleId);
        Assert.AreEqual(EvidenceCategory.Cpu, item.Category);
        Assert.AreEqual(TechnicianAttentionPriority.Medium, item.Priority);
        Assert.AreEqual(EvidenceConfidence.Medium, item.Confidence);
        Assert.AreEqual(time, item.LatestEvidenceTimestampUtc);
        CollectionAssert.AreEquivalent(new[] { process.Id, cpu.Id }, item.EvidenceIds.ToArray());
        CollectionAssert.AreEquivalent(new[] { process.CorrelationKey!, cpu.CorrelationKey! }, item.CorrelationKeys.ToArray());
        StringAssert.Contains(item.Rationale, "18.4 % aggregate CPU");
        StringAssert.Contains(item.Rationale, "72.1 % overall CPU");
        StringAssert.Contains(item.Rationale, "HIGH pressure");
        StringAssert.Contains(item.SuggestedInvestigation, "Review");
        Assert.IsFalse(item.Rationale.Contains("caused", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(item.SuggestedInvestigation.Contains("kill", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void SameTimestampIsRequiredWithoutTolerance()
    {
        DateTime time = EvidenceCorrelationTestFactory.Time;
        Assert.HasCount(1, Engine().Correlate(
            [Process(time), Cpu(time)], EvidenceCorrelationTestFactory.SessionId).Items);
        Assert.IsEmpty(Engine().Correlate(
            [Process(time), Cpu(time.AddTicks(1))], EvidenceCorrelationTestFactory.SessionId).Items);
    }

    [TestMethod]
    public void CurrentLatestProcessStateControlsHistoricalOutput()
    {
        DateTime first = EvidenceCorrelationTestFactory.Time;
        DateTime second = first.AddMinutes(1);

        EvidenceCorrelationResult suppressed = Engine().Correlate(
        [
            Process(first, EvidenceSeverity.High, cpu: 20), Cpu(first, EvidenceSeverity.High),
            Process(second, EvidenceSeverity.Low, cpu: 2), Cpu(second, EvidenceSeverity.Low)
        ], EvidenceCorrelationTestFactory.SessionId);
        Assert.IsEmpty(suppressed.Items);

        EvidenceCorrelationResult emitted = Engine().Correlate(
        [
            Process(first, EvidenceSeverity.Low, cpu: 2), Cpu(first, EvidenceSeverity.Low),
            Process(second, EvidenceSeverity.High, cpu: 20), Cpu(second, EvidenceSeverity.High)
        ], EvidenceCorrelationTestFactory.SessionId);
        Assert.HasCount(1, emitted.Items);
        Assert.AreEqual(second, emitted.Items[0].LatestEvidenceTimestampUtc);
    }

    [TestMethod]
    public void MissingEitherRequiredSourceProducesNoAttention()
    {
        Assert.IsEmpty(Engine().Correlate([Process(EvidenceCorrelationTestFactory.Time)], EvidenceCorrelationTestFactory.SessionId).Items);
        Assert.IsEmpty(Engine().Correlate([Cpu(EvidenceCorrelationTestFactory.Time)], EvidenceCorrelationTestFactory.SessionId).Items);
    }

    [TestMethod]
    public void ProvisionalOrIneligibleProcessIdentityProducesNoAttention()
    {
        DateTime time = EvidenceCorrelationTestFactory.Time;
        EvidenceRecord provisional = Process(time);
        provisional.Metadata["identityStrength"] = "Provisional";
        EvidenceRecord redacted = Process(time);
        redacted.Metadata["normalizedExecutablePath"] = "<redacted>\\agent.exe";

        Assert.IsEmpty(Engine().Correlate([provisional, Cpu(time)], EvidenceCorrelationTestFactory.SessionId).Items);
        Assert.IsEmpty(Engine().Correlate([redacted, Cpu(time)], EvidenceCorrelationTestFactory.SessionId).Items);
    }

    [TestMethod]
    [DataRow(EvidenceSeverity.Informational)]
    [DataRow(EvidenceSeverity.Low)]
    [DataRow(EvidenceSeverity.Unknown)]
    public void NonElevatedGlobalCpuProducesNoAttention(EvidenceSeverity severity)
    {
        DateTime time = EvidenceCorrelationTestFactory.Time;
        Assert.IsEmpty(Engine().Correlate(
            [Process(time), Cpu(time, severity)], EvidenceCorrelationTestFactory.SessionId).Items);
    }

    [TestMethod]
    [DataRow(EvidenceSeverity.Medium, TechnicianAttentionPriority.Low)]
    [DataRow(EvidenceSeverity.High, TechnicianAttentionPriority.Medium)]
    [DataRow(EvidenceSeverity.Critical, TechnicianAttentionPriority.Medium)]
    public void GlobalSeverityMapsToConservativePriority(
        EvidenceSeverity severity,
        TechnicianAttentionPriority expected)
    {
        DateTime time = EvidenceCorrelationTestFactory.Time;
        TechnicianAttentionItem item = Engine().Correlate(
            [Process(time), Cpu(time, severity)], EvidenceCorrelationTestFactory.SessionId).Items.Single();
        Assert.AreEqual(expected, item.Priority);
    }

    [TestMethod]
    public void MissingInvalidOrZeroProcessCpuProducesNoAttention()
    {
        DateTime time = EvidenceCorrelationTestFactory.Time;
        EvidenceRecord missing = Process(time);
        missing.Metadata.Remove("totalCpuPercent");
        EvidenceRecord invalid = Process(time);
        invalid.Metadata["totalCpuPercent"] = "not-a-number";
        EvidenceRecord zero = Process(time);
        zero.Metadata["totalCpuPercent"] = "0";

        foreach (EvidenceRecord process in new[] { missing, invalid, zero })
            Assert.IsEmpty(Engine().Correlate([process, Cpu(time)], EvidenceCorrelationTestFactory.SessionId).Items);
    }

    [TestMethod]
    public void FreshInvocationIsDeterministicAndNewEvidenceChangesId()
    {
        DateTime time = EvidenceCorrelationTestFactory.Time;
        EvidenceRecord process = Process(time);
        EvidenceRecord cpu = Cpu(time);
        var engine = Engine();
        string first = engine.Correlate([process, cpu], EvidenceCorrelationTestFactory.SessionId).Items.Single().Id;
        string second = engine.Correlate([cpu, process], EvidenceCorrelationTestFactory.SessionId).Items.Single().Id;
        Assert.AreEqual(first, second);

        EvidenceRecord newerProcess = Process(time.AddMinutes(1));
        EvidenceRecord newerCpu = Cpu(time.AddMinutes(1));
        string newer = engine.Correlate([process, cpu, newerProcess, newerCpu], EvidenceCorrelationTestFactory.SessionId).Items.Single().Id;
        Assert.AreNotEqual(first, newer);
    }

    [TestMethod]
    public void BothRulesProduceDistinctItemsForSameApplication()
    {
        DateTime time = EvidenceCorrelationTestFactory.Time;
        EvidenceRecord startup = EvidenceCorrelationTestFactory.Startup(timestamp: time);
        EvidenceRecord process = Process(time);
        EvidenceRecord cpu = Cpu(time);
        var engine = new EvidenceCorrelationEngine(
            [new StartupProcessAttentionRule(), new ProcessSystemCpuAttentionRule()]);

        EvidenceCorrelationResult result = engine.Correlate(
            [EvidenceCorrelationTestFactory.Other(EvidenceSource.SystemScan, 20), startup, cpu, process],
            EvidenceCorrelationTestFactory.SessionId);

        Assert.HasCount(2, result.Items);
        Assert.HasCount(2, result.Items.Select(item => item.RuleId).Distinct().ToArray());
        Assert.HasCount(2, result.Items.Select(item => item.Id).Distinct().ToArray());
        Assert.AreEqual(result.Items[0].EntityKey, result.Items[1].EntityKey);
        Assert.AreNotEqual(
            string.Join(',', result.Items[0].EvidenceIds),
            string.Join(',', result.Items[1].EvidenceIds));
    }

    private static EvidenceCorrelationEngine Engine() =>
        new([new ProcessSystemCpuAttentionRule()]);

    private static EvidenceRecord Process(
        DateTime timestamp,
        EvidenceSeverity severity = EvidenceSeverity.High,
        EvidenceConfidence confidence = EvidenceConfidence.High,
        double cpu = 18.4)
    {
        EvidenceRecord record = EvidenceCorrelationTestFactory.Process(
            timestamp: timestamp,
            severity: severity,
            confidence: confidence);
        record.Metadata["totalCpuPercent"] = cpu.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return record;
    }

    private static EvidenceRecord Cpu(
        DateTime timestamp,
        EvidenceSeverity severity = EvidenceSeverity.High,
        EvidenceConfidence confidence = EvidenceConfidence.High,
        double value = 72.1) =>
        new()
        {
            SessionId = EvidenceCorrelationTestFactory.SessionId,
            TimestampUtc = timestamp,
            Category = EvidenceCategory.Cpu,
            Source = EvidenceSource.DeepAnalysis,
            Subject = "cpu-pressure",
            Observation = "CPU utilization was observed during Deep Analysis.",
            Value = value,
            Unit = "%",
            Severity = severity,
            Confidence = confidence,
            Collector = "DeepAnalysisEvidenceAdapter",
            CorrelationKey = "cpu:pressure",
            Metadata = new Dictionary<string, string> { ["cpuStatus"] = severity.ToString().ToUpperInvariant() }
        };
}
