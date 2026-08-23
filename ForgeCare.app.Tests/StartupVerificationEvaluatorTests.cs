using ForgeCare.App.Models;

namespace ForgeCare.App.Tests;

[TestClass]
public class StartupVerificationEvaluatorTests
{
    [TestMethod]
    public void Disable_AbsenceFromCompletedSource_IsVerified()
    {
        StartupActionReceipt receipt = StartupVerificationTestFactory.Receipt(
            StartupVerificationOperation.Disable, StartupExecutionOutcome.Executed);
        StartupObservationSnapshot observation = StartupVerificationTestFactory.Observation(
            StartupVerificationTargetKind.HkcuRun);

        StartupVerificationResult result = StartupVerificationTestFactory.Evaluate(receipt, observation);

        Assert.AreEqual(StartupVerificationStatus.Verified, result.Status);
        StringAssert.Contains(result.Rationale, "no longer contained");
    }

    [TestMethod]
    public void Disable_TargetStillPresent_IsExpectedOutcomeNotObserved()
    {
        StartupVerificationTargetIdentity target = StartupVerificationTestFactory.Hkcu();
        var receipt = StartupVerificationTestFactory.Receipt(
            StartupVerificationOperation.Disable, StartupExecutionOutcome.Executed, target);
        var observation = StartupVerificationTestFactory.Observation(
            StartupVerificationTargetKind.HkcuRun, targets: new[] { target });

        Assert.AreEqual(
            StartupVerificationStatus.ExpectedOutcomeNotObserved,
            StartupVerificationTestFactory.Evaluate(receipt, observation).Status);
    }

    [TestMethod]
    [DataRow(StartupSourceObservationStatus.Failed)]
    [DataRow(StartupSourceObservationStatus.Unavailable)]
    [DataRow(StartupSourceObservationStatus.Unknown)]
    public void IncompleteSource_IsInconclusive(StartupSourceObservationStatus status)
    {
        var receipt = StartupVerificationTestFactory.Receipt(
            StartupVerificationOperation.Disable, StartupExecutionOutcome.Executed);
        var observation = StartupVerificationTestFactory.Observation(
            StartupVerificationTargetKind.HkcuRun, status);

        Assert.AreEqual(
            StartupVerificationStatus.Inconclusive,
            StartupVerificationTestFactory.Evaluate(receipt, observation).Status);
    }

    [TestMethod]
    public void AmbiguousOrDuplicateObservations_AreInconclusive()
    {
        StartupVerificationTargetIdentity target = StartupVerificationTestFactory.Hkcu();
        var receipt = StartupVerificationTestFactory.Receipt(
            StartupVerificationOperation.Disable, StartupExecutionOutcome.Executed, target);
        var ambiguous = StartupVerificationTestFactory.Observation(
            StartupVerificationTargetKind.HkcuRun, targets: new[] { target }, ambiguous: true);
        var duplicate = StartupVerificationTestFactory.Observation(
            StartupVerificationTargetKind.HkcuRun, targets: new[] { target, target });

        Assert.AreEqual(StartupVerificationStatus.Inconclusive,
            StartupVerificationTestFactory.Evaluate(receipt, ambiguous).Status);
        Assert.AreEqual(StartupVerificationStatus.Inconclusive,
            StartupVerificationTestFactory.Evaluate(receipt, duplicate).Status);
    }

    [TestMethod]
    public void ObservationNotLaterThanExecution_IsInconclusive()
    {
        var receipt = StartupVerificationTestFactory.Receipt(
            StartupVerificationOperation.Disable, StartupExecutionOutcome.Executed);
        var observation = StartupVerificationTestFactory.Observation(
            StartupVerificationTargetKind.HkcuRun,
            observedAt: StartupVerificationTestFactory.ExecutedAt);

        Assert.AreEqual(StartupVerificationStatus.Inconclusive,
            StartupVerificationTestFactory.Evaluate(receipt, observation).Status);
    }

    [TestMethod]
    [DataRow(StartupExecutionOutcome.Failed)]
    [DataRow(StartupExecutionOutcome.Blocked)]
    [DataRow(StartupExecutionOutcome.Skipped)]
    [DataRow(StartupExecutionOutcome.DryRun)]
    public void NonExecutedOutcomes_AreNotEligible(StartupExecutionOutcome outcome)
    {
        var receipt = StartupVerificationTestFactory.Receipt(
            StartupVerificationOperation.Disable, outcome);
        var observation = StartupVerificationTestFactory.Observation(
            StartupVerificationTargetKind.HkcuRun);

        Assert.AreEqual(StartupVerificationStatus.NotEligible,
            StartupVerificationTestFactory.Evaluate(receipt, observation).Status);
    }

    [TestMethod]
    public void DifferentSession_IsNotEligible()
    {
        var receipt = StartupVerificationTestFactory.Receipt(
            StartupVerificationOperation.Disable, StartupExecutionOutcome.Executed);
        string otherSession = Guid.NewGuid().ToString("N");
        var observation = StartupVerificationTestFactory.Observation(
            StartupVerificationTargetKind.HkcuRun, sessionId: otherSession);

        Assert.AreEqual(StartupVerificationStatus.NotEligible,
            StartupVerificationTestFactory.Evaluate(receipt, observation).Status);
    }

    [TestMethod]
    public void Restore_MatchingFingerprint_IsVerified()
    {
        StartupVerificationTargetIdentity target = StartupVerificationTestFactory.UserFile();
        var receipt = StartupVerificationTestFactory.Receipt(
            StartupVerificationOperation.Restore, StartupExecutionOutcome.Executed, target);
        var observation = StartupVerificationTestFactory.Observation(
            StartupVerificationTargetKind.UserStartupFolder, targets: new[] { target });

        Assert.AreEqual(StartupVerificationStatus.Verified,
            StartupVerificationTestFactory.Evaluate(receipt, observation).Status);
    }

    [TestMethod]
    public void Restore_MissingOrConflictingTarget_IsExpectedOutcomeNotObserved()
    {
        StartupVerificationTargetIdentity target = StartupVerificationTestFactory.Hkcu();
        StartupVerificationTargetIdentity changed = StartupVerificationTestFactory.Hkcu(value: "changed-value");
        var receipt = StartupVerificationTestFactory.Receipt(
            StartupVerificationOperation.Restore, StartupExecutionOutcome.Executed, target);
        var missing = StartupVerificationTestFactory.Observation(StartupVerificationTargetKind.HkcuRun);
        var conflicting = StartupVerificationTestFactory.Observation(
            StartupVerificationTargetKind.HkcuRun, targets: new[] { changed });

        Assert.AreEqual(StartupVerificationStatus.ExpectedOutcomeNotObserved,
            StartupVerificationTestFactory.Evaluate(receipt, missing).Status);
        Assert.AreEqual(StartupVerificationStatus.ExpectedOutcomeNotObserved,
            StartupVerificationTestFactory.Evaluate(receipt, conflicting).Status);
    }

    [TestMethod]
    public void SameNameInWrongSource_DoesNotVerifyRestore()
    {
        StartupVerificationTargetIdentity expected = StartupVerificationTestFactory.Hkcu("Agent", "value");
        StartupVerificationTargetIdentity wrongSource = StartupVerificationTestFactory.UserFile("Agent", "value");
        var receipt = StartupVerificationTestFactory.Receipt(
            StartupVerificationOperation.Restore, StartupExecutionOutcome.Executed, expected);
        var observation = StartupVerificationTestFactory.Observation(
            StartupVerificationTargetKind.HkcuRun, targets: Array.Empty<StartupVerificationTargetIdentity>());

        Assert.AreNotEqual(expected.LocatorId, wrongSource.LocatorId);
        Assert.AreEqual(StartupVerificationStatus.ExpectedOutcomeNotObserved,
            StartupVerificationTestFactory.Evaluate(receipt, observation).Status);
    }

    [TestMethod]
    public void SnapshotAndEvaluationResults_DefensivelyCopyCollections()
    {
        var targets = new List<StartupVerificationTargetIdentity> { StartupVerificationTestFactory.Hkcu() };
        var sources = new List<StartupSourceObservation>
        {
            new(StartupVerificationTargetKind.HkcuRun, StartupSourceObservationStatus.Completed, targets)
        };
        var snapshot = new StartupObservationSnapshot(
            StartupVerificationTestFactory.SessionId,
            StartupVerificationTestFactory.ObservedAt,
            sources);
        targets.Clear();
        sources.Clear();

        Assert.HasCount(1, snapshot.Sources);
        Assert.HasCount(1, snapshot.Sources[0].Targets);
    }
}
