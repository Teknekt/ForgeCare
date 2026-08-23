using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ForgeServiceReportHtmlRendererTests
{
    private readonly ForgeServiceReportHtmlRenderer _renderer = new();

    [TestMethod]
    public void RenderProducesSemanticOfflineDocumentInStableSectionOrder()
    {
        string html = _renderer.Render(HtmlReportTestFactory.EmptyModel());

        StringAssert.StartsWith(html, "<!DOCTYPE html>");
        StringAssert.Contains(html, "<html lang=\"en\">");
        StringAssert.Contains(html, "<header>");
        StringAssert.Contains(html, "<main>");
        StringAssert.Contains(html, "<footer>");
        string[] ids =
        [
            "session-summary", "diagnostic-activity", "technician-actions", "verification",
            "unresolved", "evidence-references", "session-checkpoints", "report-data-notes"
        ];
        int previous = -1;
        foreach (string id in ids)
        {
            int current = html.IndexOf($"id=\"{id}\"", StringComparison.Ordinal);
            Assert.IsGreaterThan(previous, current, id);
            previous = current;
        }
        Assert.IsFalse(html.Contains("http://", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("https://", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void SameModelProducesByteForByteDeterministicHtml()
    {
        ForgeServiceReportModel model = HtmlReportTestFactory.PopulatedModel();

        string first = _renderer.Render(model);
        string second = _renderer.Render(model);

        Assert.AreEqual(first, second);
    }

    [TestMethod]
    public void EmptyModelRendersTruthfulNeutralAbsenceStates()
    {
        string html = _renderer.Render(HtmlReportTestFactory.EmptyModel());

        StringAssert.Contains(html, "No diagnostic activity was recorded.");
        StringAssert.Contains(html, "No technician-controlled startup actions were recorded.");
        StringAssert.Contains(html, "No verification results were recorded.");
        StringAssert.Contains(html, "No unresolved verification items were identified.");
        StringAssert.Contains(html, "No Evidence references were supplied.");
        StringAssert.Contains(html, "No session checkpoints were recorded.");
    }

    [TestMethod]
    public void DiagnosticActivityIsSeparateFromTechnicianActions()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        session.Actions.Add(new ForgeReportAction
        {
            Category = "SYSTEM",
            Timestamp = DateTime.Now,
            IsSuccess = true
        });
        string html = _renderer.Render(new ForgeServiceReportBuilder().Build(session));

        StringAssert.Contains(html, "System scan");
        StringAssert.Contains(html, "Diagnostic Activity");
        StringAssert.Contains(html, "No technician-controlled startup actions were recorded.");
        Assert.AreEqual(0, Count(html, "class=\"action-card\""));
    }

    [TestMethod]
    [DataRow(StartupExecutionOutcome.Executed, "Executed")]
    [DataRow(StartupExecutionOutcome.Failed, "Execution Failed")]
    [DataRow(StartupExecutionOutcome.Blocked, "Blocked")]
    [DataRow(StartupExecutionOutcome.Skipped, "Skipped")]
    [DataRow(StartupExecutionOutcome.DryRun, "Dry Run — Not Executed")]
    public void ExecutionStatesRemainExplicit(StartupExecutionOutcome outcome, string display)
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        session.StartupActionReceipts.Add(ForgeServiceReportTestFactory.Receipt(session.SessionId, outcome));

        string html = _renderer.Render(new ForgeServiceReportBuilder().Build(session));

        StringAssert.Contains(html, display);
        StringAssert.Contains(html, "Agent");
        StringAssert.Contains(html, "Expected state");
    }

    [TestMethod]
    [DataRow(StartupVerificationStatus.Verified, "Verified")]
    [DataRow(StartupVerificationStatus.ExpectedOutcomeNotObserved, "Not Verified")]
    [DataRow(StartupVerificationStatus.Inconclusive, "Inconclusive")]
    [DataRow(StartupVerificationStatus.NotEligible, "Not Eligible")]
    public void VerificationStatesRenderIndependently(
        StartupVerificationStatus status,
        string display)
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        StartupVerificationTargetIdentity target = status == StartupVerificationStatus.NotEligible
            ? ForgeServiceReportTestFactory.Target(kind: StartupVerificationTargetKind.Unsupported)
            : ForgeServiceReportTestFactory.Target();
        StartupActionReceipt receipt = ForgeServiceReportTestFactory.Receipt(session.SessionId, target: target);
        session.StartupActionReceipts.Add(receipt);
        session.StartupVerificationResults.Add(ForgeServiceReportTestFactory.Verification(receipt, status));

        string html = _renderer.Render(new ForgeServiceReportBuilder().Build(session));

        StringAssert.Contains(html, display);
    }

    [TestMethod]
    public void PendingVerificationRendersAsAwaitingLaterObservation()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        session.StartupActionReceipts.Add(ForgeServiceReportTestFactory.Receipt(session.SessionId));

        string html = _renderer.Render(new ForgeServiceReportBuilder().Build(session));

        StringAssert.Contains(html, "Pending");
        StringAssert.Contains(html, "Awaiting a later valid observation.");
    }

    [TestMethod]
    public void RecoveryAndSupersessionRenderPresenceWithoutRawIdentifiers()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        StartupVerificationTargetIdentity target = ForgeServiceReportTestFactory.Target(locator: "shared");
        StartupActionReceipt disable = ForgeServiceReportTestFactory.Receipt(session.SessionId, target: target);
        StartupActionReceipt restore = ForgeServiceReportTestFactory.Receipt(
            session.SessionId,
            operation: StartupVerificationOperation.Restore,
            target: target,
            executedAt: disable.ExecutedAtUtc.AddMinutes(2),
            supersedesReceiptId: disable.ReceiptId);
        session.StartupActionReceipts.AddRange([disable, restore]);

        string html = _renderer.Render(new ForgeServiceReportBuilder().Build(session));

        StringAssert.Contains(html, "Recovery reference available");
        StringAssert.Contains(html, "Superseded by a later opposite startup operation.");
        Assert.IsFalse(html.Contains("undo:test", StringComparison.Ordinal));
        Assert.IsFalse(html.Contains(disable.ReceiptId, StringComparison.Ordinal));
        Assert.IsFalse(html.Contains(restore.ReceiptId, StringComparison.Ordinal));
    }

    [TestMethod]
    public void UnresolvedItemsUseNeutralProfessionalLanguage()
    {
        var unresolved = Enum.GetValues<ServiceReportUnresolvedKind>()
            .Select((kind, index) => new ServiceReportUnresolvedItem(
                kind,
                ForgeServiceReportTestFactory.ExecutedAt.AddMinutes(index),
                "Review remains appropriate.",
                null,
                null));
        ForgeServiceReportModel model = HtmlReportTestFactory.Model(unresolvedItems: unresolved);

        string html = _renderer.Render(model);

        StringAssert.Contains(html, "Their presence does not by itself indicate danger.");
        StringAssert.Contains(html, "Execution failed");
        StringAssert.Contains(html, "Pending verification");
        StringAssert.Contains(html, "Incomplete traceability");
    }

    [TestMethod]
    public void EvidenceReferencesAreCompactAndDoNotImplyActionJustification()
    {
        ForgeServiceReportModel model = HtmlReportTestFactory.Model(
            evidenceReferences:
            [
                new ServiceReportEvidenceReference(
                    Guid.NewGuid(), "0123456789", ForgeServiceReportTestFactory.ExecutedAt,
                    EvidenceCategory.Process, "Process", EvidenceSource.ProcessIntelligence,
                    "Process Intelligence", "APPLICATION: EDITOR", EvidenceSeverity.Medium,
                    "MEDIUM", EvidenceConfidence.High, "HIGH")
            ]);

        string html = _renderer.Render(model);

        StringAssert.Contains(html, "0123456789");
        StringAssert.Contains(html, "Process Intelligence");
        StringAssert.Contains(html, "not automatically equivalent to action justification");
        Assert.IsFalse(html.Contains("CorrelationKey", StringComparison.Ordinal));
        Assert.IsFalse(html.Contains("Metadata", StringComparison.Ordinal));
        Assert.IsFalse(html.Contains("Observation body", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CheckpointsRenderInitialAndLatestWithoutCausalityClaim()
    {
        ServiceReportCheckpoint first = HtmlReportTestFactory.Checkpoint(70, 4, DateTime.Today);
        ServiceReportCheckpoint latest = HtmlReportTestFactory.Checkpoint(85, 2, DateTime.Today.AddHours(1));
        string html = _renderer.Render(HtmlReportTestFactory.Model(checkpoints: [first, latest]));

        StringAssert.Contains(html, "Initial recorded state");
        StringAssert.Contains(html, "Latest recorded state");
        StringAssert.Contains(html, "Differences are not attributed to a specific action.");
    }

    [TestMethod]
    public void LegacyPartialReportRendersWarningsWithoutTypedClaims()
    {
        ServiceReportActivity legacy = new(
            DateTime.Today,
            ServiceReportActivityKind.LegacyActivity,
            "STARTUP",
            "Legacy startup activity",
            true);
        ForgeServiceReportModel model = HtmlReportTestFactory.Model(
            activities: [legacy],
            warnings: ["This legacy session does not contain typed Verification 2.0 records."]);

        string html = _renderer.Render(model);

        StringAssert.Contains(html, "Legacy startup activity");
        StringAssert.Contains(html, "This legacy session does not contain typed Verification 2.0 records.");
        StringAssert.Contains(html, "No technician-controlled startup actions were recorded.");
    }

    [TestMethod]
    public void ConstructedScaleRendersAllActionsVerificationsAndEvidence()
    {
        ServiceReportAction[] actions = Enumerable.Range(0, 100)
            .Select(index => HtmlReportTestFactory.Action("Agent " + index, index))
            .ToArray();
        ServiceReportEvidenceReference[] evidence = Enumerable.Range(0, 1000)
            .Select(index => HtmlReportTestFactory.EvidenceReference(index))
            .ToArray();
        ForgeServiceReportModel model = HtmlReportTestFactory.Model(actions: actions, evidenceReferences: evidence);

        string html = _renderer.Render(model);

        Assert.AreEqual(100, Count(html, "class=\"action-card\""));
        Assert.AreEqual(100, Count(html, "Expected state observed."));
        Assert.AreEqual(1001, Count(html, "<tr>"));
    }

    private static int Count(string source, string value) =>
        source.Split(value, StringSplitOptions.None).Length - 1;
}

internal static class HtmlReportTestFactory
{
    internal static ForgeServiceReportModel EmptyModel() => Model();

    internal static ForgeServiceReportModel PopulatedModel() => Model(
        activities: [new ServiceReportActivity(DateTime.Today, ServiceReportActivityKind.DiagnosticActivity, "SYSTEM", "System scan", true)],
        actions: [Action("Agent", 1)],
        evidenceReferences: [EvidenceReference(1)],
        checkpoints: [Checkpoint(75, 3, DateTime.Today)],
        warnings: ["A bounded report data note."]);

    internal static ForgeServiceReportModel Model(
        IEnumerable<ServiceReportActivity>? activities = null,
        IEnumerable<ServiceReportAction>? actions = null,
        IEnumerable<ServiceReportEvidenceReference>? evidenceReferences = null,
        IEnumerable<ServiceReportCheckpoint>? checkpoints = null,
        IEnumerable<ServiceReportUnresolvedItem>? unresolvedItems = null,
        IEnumerable<string>? warnings = null)
    {
        ServiceReportAction[] actionArray = (actions ?? []).ToArray();
        ServiceReportEvidenceReference[] evidenceArray = (evidenceReferences ?? []).ToArray();
        var summary = new ServiceReportSessionSummary(
            Guid.NewGuid().ToString("N"),
            new DateTime(2026, 8, 23, 8, 0, 0),
            new DateTime(2026, 8, 23, 9, 0, 0),
            null,
            (activities ?? []).Count(value => value.Kind == ServiceReportActivityKind.DiagnosticActivity),
            0,
            actionArray.Length,
            evidenceArray.Length);
        return new ForgeServiceReportModel(
            summary,
            activities ?? [],
            actionArray,
            evidenceArray,
            checkpoints ?? [],
            unresolvedItems ?? [],
            warnings ?? []);
    }

    internal static ServiceReportAction Action(string name, int index) => new(
        GuidFromInt(index + 1).ToString("N"),
        ForgeServiceReportTestFactory.Hash("target-" + index),
        StartupVerificationOperation.Disable,
        ForgeServiceReportTestFactory.ExecutedAt.AddSeconds(index),
        name,
        StartupVerificationTargetKind.HkcuRun,
        "Current-user Run entry",
        ServiceReportExecutionStatus.Executed,
        "Executed",
        "The reviewed startup target is absent from its configured source.",
        true,
        true,
        null,
        null,
        ServiceReportVerificationStatus.Verified,
        ServiceReportTraceabilityStatus.ExecutedAndVerified,
        [new ServiceReportVerification(
            ServiceReportVerificationStatus.Verified,
            ForgeServiceReportTestFactory.ExecutedAt.AddMinutes(2),
            ForgeServiceReportTestFactory.ExecutedAt.AddMinutes(1),
            "Verified",
            "Expected state observed.")]);

    internal static ServiceReportEvidenceReference EvidenceReference(int index) => new(
        GuidFromInt(index + 1),
        (index + 1).ToString("D10"),
        ForgeServiceReportTestFactory.ExecutedAt.AddSeconds(index),
        EvidenceCategory.Process,
        "Process",
        EvidenceSource.ProcessIntelligence,
        "Process Intelligence",
        "APPLICATION: AGENT",
        EvidenceSeverity.Medium,
        "MEDIUM",
        EvidenceConfidence.High,
        "HIGH");

    internal static ServiceReportCheckpoint Checkpoint(int health, int startup, DateTime timestamp) => new(
        timestamp,
        health,
        "Good",
        100,
        50,
        16,
        50,
        startup);

    private static Guid GuidFromInt(int value)
    {
        byte[] bytes = new byte[16];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return new Guid(bytes);
    }
}
