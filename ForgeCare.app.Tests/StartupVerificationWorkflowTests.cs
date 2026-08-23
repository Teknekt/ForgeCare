using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public class StartupVerificationWorkflowTests
{
    [TestMethod]
    public void DiagnosticScansAloneDoNotMasqueradeAsExecutedOrVerifiedWork()
    {
        var session = new ForgeReportSession
        {
            Checkpoints = new List<ForgeReportCheckpoint> { new(), new() },
            Actions = new List<ForgeReportAction>
            {
                new() { Category = "SYSTEM", IsSuccess = true },
                new() { Category = "ANALYSIS", IsSuccess = true }
            }
        };

        List<ForgeWorkflowStep> steps = Build(session);

        Assert.AreEqual("NEXT", steps.Single(step => step.Number == 6).Status);
        Assert.AreEqual("LOCKED", steps.Single(step => step.Number == 7).Status);
    }

    [TestMethod]
    public void ExecutedReceiptCreatesWorkButSecondScanDoesNotCompleteVerification()
    {
        ForgeReportSession session = SessionWithReceipt();
        session.Checkpoints.Add(new ForgeReportCheckpoint());
        session.Checkpoints.Add(new ForgeReportCheckpoint());

        List<ForgeWorkflowStep> steps = Build(session);

        Assert.AreEqual("COMPLETE", steps.Single(step => step.Number == 6).Status);
        Assert.AreEqual("NEXT", steps.Single(step => step.Number == 7).Status);
    }

    [TestMethod]
    [DataRow(StartupVerificationStatus.Verified, "COMPLETE")]
    [DataRow(StartupVerificationStatus.ExpectedOutcomeNotObserved, "COMPLETE")]
    [DataRow(StartupVerificationStatus.Inconclusive, "NEXT")]
    public void VerificationStatusControlsFactualWorkflowCompletion(
        StartupVerificationStatus status,
        string expected)
    {
        ForgeReportSession session = SessionWithReceipt();
        StartupActionReceipt receipt = session.StartupActionReceipts.Single();
        StartupActionReceiptItem item = receipt.Items.Single();
        session.StartupVerificationResults.Add(new StartupVerificationResult(
            receipt.ReceiptId,
            item.TargetIdentity.TargetId,
            status,
            StartupVerificationTestFactory.EvaluatedAt,
            StartupVerificationTestFactory.ObservedAt,
            "A bounded factual startup verification result.",
            item.ExpectedState.Summary,
            item.TargetIdentity.SourceLocator));

        Assert.AreEqual(expected, Build(session).Single(step => step.Number == 7).Status);
    }

    [TestMethod]
    public void ExistingNonStartupCleanupWorkRetainsLegacyCheckpointVerification()
    {
        var session = new ForgeReportSession
        {
            Checkpoints = new List<ForgeReportCheckpoint> { new(), new() },
            Actions = new List<ForgeReportAction>
            {
                new() { Category = "CLEANUP", IsSuccess = true }
            }
        };

        List<ForgeWorkflowStep> steps = Build(session);

        Assert.AreEqual("COMPLETE", steps.Single(step => step.Number == 6).Status);
        Assert.AreEqual("COMPLETE", steps.Single(step => step.Number == 7).Status);
    }

    private static ForgeReportSession SessionWithReceipt()
    {
        string sessionId = StartupVerificationTestFactory.SessionId;
        StartupVerificationTargetIdentity target = StartupVerificationTestFactory.Hkcu();
        var receipt = new StartupActionReceipt(
            StartupVerificationTestFactory.ReceiptId,
            sessionId,
            StartupVerificationOperation.Disable,
            StartupVerificationTestFactory.ExecutedAt,
            new[]
            {
                new StartupActionReceiptItem(
                    target,
                    StartupExecutionOutcome.Executed,
                    new StartupExpectedState(StartupExpectedStateKind.Absent))
            });
        return new ForgeReportSession
        {
            SessionId = sessionId,
            StartupActionReceipts = new List<StartupActionReceipt> { receipt }
        };
    }

    private static List<ForgeWorkflowStep> Build(ForgeReportSession session) =>
        new ForgeWorkflowService().Build(
            hasProfile: true,
            hasDeepAnalysis: true,
            hasServiceAnalysis: true,
            hasStorageAnalysis: true,
            hasOptimizationAnalysis: true,
            hasDuplicateScan: false,
            hasForgePlan: true,
            reportSession: session);
}
