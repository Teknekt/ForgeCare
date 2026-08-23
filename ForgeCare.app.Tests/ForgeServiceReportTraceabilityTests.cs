using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ForgeServiceReportTraceabilityTests
{
    private readonly ForgeServiceReportBuilder _builder = new();

    [TestMethod]
    [DataRow(StartupExecutionOutcome.Executed, ServiceReportExecutionStatus.Executed, ServiceReportTraceabilityStatus.PendingVerification)]
    [DataRow(StartupExecutionOutcome.Failed, ServiceReportExecutionStatus.Failed, ServiceReportTraceabilityStatus.ExecutionFailed)]
    [DataRow(StartupExecutionOutcome.Blocked, ServiceReportExecutionStatus.Blocked, ServiceReportTraceabilityStatus.NotExecuted)]
    [DataRow(StartupExecutionOutcome.Skipped, ServiceReportExecutionStatus.Skipped, ServiceReportTraceabilityStatus.NotExecuted)]
    [DataRow(StartupExecutionOutcome.DryRun, ServiceReportExecutionStatus.DryRun, ServiceReportTraceabilityStatus.NotExecuted)]
    public void ExecutionOutcomesRemainTypedAndIndependent(
        StartupExecutionOutcome outcome,
        ServiceReportExecutionStatus expectedExecution,
        ServiceReportTraceabilityStatus expectedTraceability)
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        session.StartupActionReceipts.Add(ForgeServiceReportTestFactory.Receipt(session.SessionId, outcome));

        ServiceReportAction action = _builder.Build(session).Actions.Single();

        Assert.AreEqual(expectedExecution, action.ExecutionStatus);
        Assert.AreEqual(expectedTraceability, action.TraceabilityStatus);
    }

    [TestMethod]
    [DataRow(StartupVerificationStatus.Verified, ServiceReportVerificationStatus.Verified, ServiceReportTraceabilityStatus.ExecutedAndVerified)]
    [DataRow(StartupVerificationStatus.ExpectedOutcomeNotObserved, ServiceReportVerificationStatus.NotVerified, ServiceReportTraceabilityStatus.ExecutedNotVerified)]
    [DataRow(StartupVerificationStatus.Inconclusive, ServiceReportVerificationStatus.Inconclusive, ServiceReportTraceabilityStatus.ExecutedInconclusive)]
    public void VerificationStatusesMapWithoutCollapsingToSuccess(
        StartupVerificationStatus status,
        ServiceReportVerificationStatus expectedVerification,
        ServiceReportTraceabilityStatus expectedTraceability)
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        StartupActionReceipt receipt = ForgeServiceReportTestFactory.Receipt(session.SessionId);
        session.StartupActionReceipts.Add(receipt);
        session.StartupVerificationResults.Add(ForgeServiceReportTestFactory.Verification(receipt, status));

        ServiceReportAction action = _builder.Build(session).Actions.Single();

        Assert.AreEqual(expectedVerification, action.VerificationStatus);
        Assert.AreEqual(expectedTraceability, action.TraceabilityStatus);
        if (status == StartupVerificationStatus.ExpectedOutcomeNotObserved)
            Assert.AreEqual("Not Verified", action.Verifications.Single().StatusDisplay);
    }

    [TestMethod]
    public void UnsupportedTargetCanBeReportedAsNotEligibleWithoutFailureClaim()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        StartupVerificationTargetIdentity target = ForgeServiceReportTestFactory.Target(
            kind: StartupVerificationTargetKind.Unsupported);
        StartupActionReceipt receipt = ForgeServiceReportTestFactory.Receipt(session.SessionId, target: target);
        session.StartupActionReceipts.Add(receipt);
        session.StartupVerificationResults.Add(ForgeServiceReportTestFactory.Verification(
            receipt,
            StartupVerificationStatus.NotEligible));

        ServiceReportAction action = _builder.Build(session).Actions.Single();

        Assert.AreEqual(ServiceReportVerificationStatus.NotEligible, action.VerificationStatus);
        Assert.AreEqual(ServiceReportTraceabilityStatus.NotEligible, action.TraceabilityStatus);
        Assert.IsFalse(_builder.Build(session).UnresolvedItems.Any());
    }

    [TestMethod]
    public void LinkageRequiresExactReceiptAndTargetIds()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        StartupActionReceipt receipt = ForgeServiceReportTestFactory.Receipt(session.SessionId);
        session.StartupActionReceipts.Add(receipt);
        session.StartupVerificationResults.Add(ForgeServiceReportTestFactory.Verification(
            receipt,
            targetId: ForgeServiceReportTestFactory.Hash("wrong-target")));
        session.StartupVerificationResults.Add(ForgeServiceReportTestFactory.Verification(
            receipt,
            receiptId: Guid.NewGuid().ToString("N"),
            observedAt: receipt.ExecutedAtUtc.AddMinutes(3)));

        ForgeServiceReportModel report = _builder.Build(session);

        Assert.AreEqual(ServiceReportVerificationStatus.Pending, report.Actions.Single().VerificationStatus);
        Assert.HasCount(2, report.UnresolvedItems.Where(value => value.Kind == ServiceReportUnresolvedKind.OrphanVerification));
    }

    [TestMethod]
    public void InconclusiveObservationLeavesEligibleTargetPendingForFutureProof()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        StartupActionReceipt receipt = ForgeServiceReportTestFactory.Receipt(session.SessionId);
        session.StartupActionReceipts.Add(receipt);
        session.StartupVerificationResults.Add(ForgeServiceReportTestFactory.Verification(
            receipt,
            StartupVerificationStatus.Inconclusive));

        ForgeServiceReportModel report = _builder.Build(session);

        Assert.AreEqual(ServiceReportTraceabilityStatus.ExecutedInconclusive, report.Actions.Single().TraceabilityStatus);
        Assert.IsTrue(report.UnresolvedItems.Any(value => value.Kind == ServiceReportUnresolvedKind.InconclusiveObservation));
    }

    [TestMethod]
    public void LaterOppositeOperationSupersedesMatchingLocatorOnly()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        StartupVerificationTargetIdentity target = ForgeServiceReportTestFactory.Target(locator: "shared");
        StartupActionReceipt disable = ForgeServiceReportTestFactory.Receipt(session.SessionId, target: target);
        StartupActionReceipt restore = ForgeServiceReportTestFactory.Receipt(
            session.SessionId,
            operation: StartupVerificationOperation.Restore,
            target: target,
            executedAt: disable.ExecutedAtUtc.AddMinutes(5),
            supersedesReceiptId: disable.ReceiptId);
        session.StartupActionReceipts.AddRange([restore, disable]);

        ForgeServiceReportModel report = _builder.Build(session);
        ServiceReportAction oldAction = report.Actions.Single(value => value.ReceiptId == disable.ReceiptId);
        ServiceReportAction newAction = report.Actions.Single(value => value.ReceiptId == restore.ReceiptId);

        Assert.AreEqual(ServiceReportTraceabilityStatus.Superseded, oldAction.TraceabilityStatus);
        Assert.AreEqual(restore.ReceiptId, oldAction.SupersededByReceiptId);
        Assert.AreEqual(disable.ReceiptId, newAction.SupersedesReceiptId);
        Assert.IsFalse(report.UnresolvedItems.Any(value => value.ReceiptId == disable.ReceiptId));
    }

    [TestMethod]
    public void BatchTargetsAndReceiptsHaveDeterministicChronologicalOrdering()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        StartupActionReceipt later = ForgeServiceReportTestFactory.Receipt(
            session.SessionId,
            target: ForgeServiceReportTestFactory.Target("Later", "later"),
            executedAt: ForgeServiceReportTestFactory.ExecutedAt.AddMinutes(1));
        StartupActionReceipt earlier = ForgeServiceReportTestFactory.Receipt(
            session.SessionId,
            target: ForgeServiceReportTestFactory.Target("Earlier", "earlier"));
        session.StartupActionReceipts.AddRange([later, earlier]);

        ForgeServiceReportModel report = _builder.Build(session);

        Assert.AreEqual("Earlier", report.Actions[0].TargetDisplayName);
        Assert.AreEqual("Later", report.Actions[1].TargetDisplayName);
    }

    [TestMethod]
    public void DuplicateResultsAreDeterministicAndConflictsProduceWarning()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        StartupActionReceipt receipt = ForgeServiceReportTestFactory.Receipt(session.SessionId);
        StartupVerificationResult verified = ForgeServiceReportTestFactory.Verification(receipt);
        StartupVerificationResult conflict = ForgeServiceReportTestFactory.Verification(
            receipt,
            StartupVerificationStatus.ExpectedOutcomeNotObserved);
        session.StartupActionReceipts.Add(receipt);
        session.StartupVerificationResults.AddRange([conflict, verified, verified]);

        ForgeServiceReportModel report = _builder.Build(session);

        Assert.HasCount(1, report.Actions.Single().Verifications);
        Assert.IsTrue(report.Warnings.Any(value => value.Contains("Conflicting duplicate startup verification", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void PartialBatchPreservesExecutedAndFailedTargetTruth()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        StartupActionReceipt first = ForgeServiceReportTestFactory.Receipt(
            session.SessionId,
            target: ForgeServiceReportTestFactory.Target("Executed agent", "executed"));
        StartupActionReceiptItem failedItem = new(
            ForgeServiceReportTestFactory.Target("Failed agent", "failed"),
            StartupExecutionOutcome.Failed,
            new StartupExpectedState(StartupExpectedStateKind.Absent));
        var batch = new StartupActionReceipt(
            first.ReceiptId,
            first.SessionId,
            first.Operation,
            first.ExecutedAtUtc,
            [first.Items[0], failedItem]);
        session.StartupActionReceipts.Add(batch);

        ForgeServiceReportModel report = _builder.Build(session);

        Assert.HasCount(2, report.Actions);
        Assert.AreEqual(ServiceReportTraceabilityStatus.PendingVerification,
            report.Actions.Single(value => value.TargetDisplayName == "Executed agent").TraceabilityStatus);
        Assert.AreEqual(ServiceReportTraceabilityStatus.ExecutionFailed,
            report.Actions.Single(value => value.TargetDisplayName == "Failed agent").TraceabilityStatus);
    }

    [TestMethod]
    public void LaterTerminalObservationFollowsEarlierInconclusiveObservation()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        StartupActionReceipt receipt = ForgeServiceReportTestFactory.Receipt(session.SessionId);
        session.StartupActionReceipts.Add(receipt);
        session.StartupVerificationResults.Add(ForgeServiceReportTestFactory.Verification(
            receipt,
            StartupVerificationStatus.Inconclusive));
        session.StartupVerificationResults.Add(ForgeServiceReportTestFactory.Verification(
            receipt,
            StartupVerificationStatus.Verified,
            observedAt: receipt.ExecutedAtUtc.AddMinutes(4)));

        ServiceReportAction action = _builder.Build(session).Actions.Single();

        Assert.HasCount(2, action.Verifications);
        Assert.AreEqual(ServiceReportVerificationStatus.Verified, action.VerificationStatus);
        Assert.AreEqual(ServiceReportTraceabilityStatus.ExecutedAndVerified, action.TraceabilityStatus);
    }

    [TestMethod]
    public void RecoveryProjectionExposesPresenceOnly()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        session.StartupActionReceipts.Add(ForgeServiceReportTestFactory.Receipt(
            session.SessionId,
            target: ForgeServiceReportTestFactory.Target("With recovery", "with"),
            recoveryReference: "undo:private-reference"));
        session.StartupActionReceipts.Add(ForgeServiceReportTestFactory.Receipt(
            session.SessionId,
            target: ForgeServiceReportTestFactory.Target("Without recovery", "without"),
            executedAt: ForgeServiceReportTestFactory.ExecutedAt.AddSeconds(1),
            recoveryReference: null));

        ForgeServiceReportModel report = _builder.Build(session);

        Assert.IsTrue(report.Actions.Single(value => value.TargetDisplayName == "With recovery").HasRecoveryReference);
        Assert.IsFalse(report.Actions.Single(value => value.TargetDisplayName == "Without recovery").HasRecoveryReference);
        Assert.IsFalse(typeof(ServiceReportAction).GetProperties().Any(property => property.Name == "RecoveryReference"));
    }

    [TestMethod]
    public void ConstructedScaleUsesOneProjectionPassAndReturnsAllValidRecords()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        for (int index = 0; index < 100; index++)
        {
            StartupActionReceipt receipt = ForgeServiceReportTestFactory.Receipt(
                session.SessionId,
                target: ForgeServiceReportTestFactory.Target("Agent " + index, "locator-" + index),
                executedAt: ForgeServiceReportTestFactory.ExecutedAt.AddSeconds(index));
            session.StartupActionReceipts.Add(receipt);
            session.StartupVerificationResults.Add(ForgeServiceReportTestFactory.Verification(receipt));
        }
        EvidenceRecord[] evidence = Enumerable.Range(0, 1000)
            .Select(index => ForgeServiceReportTestFactory.Evidence(session.SessionId, index))
            .ToArray();

        ForgeServiceReportModel report = _builder.Build(session, evidence);

        Assert.HasCount(100, report.Actions);
        Assert.AreEqual(100, report.Actions.Count(value => value.TraceabilityStatus == ServiceReportTraceabilityStatus.ExecutedAndVerified));
        Assert.HasCount(1000, report.EvidenceReferences);
    }
}
