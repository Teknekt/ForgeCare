using ForgeCare.App.Models;

namespace ForgeCare.App.Tests;

[TestClass]
public class StartupActionReceiptTests
{
    [TestMethod]
    [DataRow(StartupExecutionOutcome.Executed, true)]
    [DataRow(StartupExecutionOutcome.Failed, false)]
    [DataRow(StartupExecutionOutcome.Blocked, false)]
    [DataRow(StartupExecutionOutcome.Skipped, false)]
    [DataRow(StartupExecutionOutcome.DryRun, false)]
    public void Eligibility_DependsOnPerTargetExecutionOutcome(
        StartupExecutionOutcome outcome,
        bool expected)
    {
        StartupActionReceipt receipt = StartupVerificationTestFactory.Receipt(
            StartupVerificationOperation.Disable,
            outcome);

        Assert.AreEqual(expected, receipt.Items.Single().IsVerificationEligible);
    }

    [TestMethod]
    public void UnsupportedTarget_IsNeverEligible()
    {
        var target = new ForgeCare.App.Services.StartupVerificationIdentityBuilder()
            .BuildUnsupported("Machine-wide entry");
        StartupActionReceipt receipt = StartupVerificationTestFactory.Receipt(
            StartupVerificationOperation.Disable,
            StartupExecutionOutcome.Executed,
            target);

        Assert.IsFalse(receipt.Items.Single().IsVerificationEligible);
    }

    [TestMethod]
    public void Receipt_DefensivelyCopiesItemsAndRetainsSupersessionReference()
    {
        var items = new List<StartupActionReceiptItem>();
        StartupVerificationTargetIdentity target = StartupVerificationTestFactory.Hkcu();
        items.Add(new StartupActionReceiptItem(
            target,
            StartupExecutionOutcome.Executed,
            new StartupExpectedState(StartupExpectedStateKind.Absent)));
        string superseded = Guid.Parse("33333333-3333-3333-3333-333333333333").ToString("N");
        var receipt = new StartupActionReceipt(
            StartupVerificationTestFactory.ReceiptId,
            StartupVerificationTestFactory.SessionId,
            StartupVerificationOperation.Disable,
            StartupVerificationTestFactory.ExecutedAt,
            items,
            superseded);

        items.Clear();

        Assert.HasCount(1, receipt.Items);
        Assert.AreEqual(superseded, receipt.SupersedesReceiptId);
    }

    [TestMethod]
    public void RestoreExpectedState_RequiresFingerprint()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new StartupExpectedState(StartupExpectedStateKind.PresentWithMatchingConfiguration));
    }
}
