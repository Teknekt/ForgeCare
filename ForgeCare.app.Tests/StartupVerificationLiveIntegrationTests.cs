using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public class StartupVerificationLiveIntegrationTests
{
    [TestMethod]
    public void LaterCompletedScanPersistsVerifiedDisableAndClearsPendingAcrossRestart()
    {
        using var temp = new TempDirectory();
        var report = new ForgeReportService(temp.Path);
        StartupActionReceipt receipt = DisableReceipt(report.Snapshot().SessionId);
        report.RecordStartupActionReceipt(receipt);

        var reopenedBeforeScan = new ForgeReportService(temp.Path);

        StartupVerificationLiveResult result = new StartupVerificationLiveService(reopenedBeforeScan)
            .VerifyAfterSystemScan(Scan(StartupScanSourceStatus.Completed));

        Assert.AreEqual(1, result.PersistedCount);
        Assert.AreEqual(StartupVerificationStatus.Verified,
            reopenedBeforeScan.GetStartupVerificationResults().Single().Status);
        Assert.IsEmpty(new ForgeReportService(temp.Path).GetPendingStartupActionReceipts());
    }

    [TestMethod]
    public void FailedSourcePersistsInconclusiveAndLeavesReceiptPending()
    {
        using var temp = new TempDirectory();
        var report = new ForgeReportService(temp.Path);
        report.RecordStartupActionReceipt(DisableReceipt(report.Snapshot().SessionId));

        StartupVerificationLiveResult result = new StartupVerificationLiveService(report)
            .VerifyAfterSystemScan(Scan(StartupScanSourceStatus.Failed));

        Assert.AreEqual(1, result.PersistedCount);
        Assert.AreEqual(StartupVerificationStatus.Inconclusive,
            report.GetStartupVerificationResults().Single().Status);
        Assert.HasCount(1, report.GetPendingStartupActionReceipts());
    }

    [TestMethod]
    public void CompletedScanWithPresentTargetRecordsExpectedOutcomeNotObserved()
    {
        using var temp = new TempDirectory();
        var report = new ForgeReportService(temp.Path);
        StartupActionReceipt receipt = DisableReceipt(report.Snapshot().SessionId);
        report.RecordStartupActionReceipt(receipt);
        StartupVerificationTargetIdentity target = receipt.Items.Single().TargetIdentity;

        new StartupVerificationLiveService(report).VerifyAfterSystemScan(
            Scan(StartupScanSourceStatus.Completed,
                new StartupItem { Name = target.DisplayName, Command = "agent.exe", Source = "Current User Registry" }));

        Assert.AreEqual(StartupVerificationStatus.ExpectedOutcomeNotObserved,
            report.GetStartupVerificationResults().Single().Status);
        Assert.IsEmpty(report.GetPendingStartupActionReceipts());
    }

    [TestMethod]
    public void LaterCompletedScanPersistsVerifiedRestoreWithMatchingConfiguration()
    {
        using var temp = new TempDirectory();
        var report = new ForgeReportService(temp.Path);
        string session = report.Snapshot().SessionId;
        const string configuredValue = "agent.exe --restored";
        StartupVerificationTargetIdentity target =
            StartupVerificationTestFactory.Hkcu("Agent", configuredValue);
        var receipt = new StartupActionReceipt(
            Guid.NewGuid().ToString("N"),
            session,
            StartupVerificationOperation.Restore,
            StartupVerificationTestFactory.ExecutedAt,
            new[] { Item(target, StartupVerificationOperation.Restore) });
        report.RecordStartupActionReceipt(receipt);

        new StartupVerificationLiveService(report).VerifyAfterSystemScan(
            Scan(StartupScanSourceStatus.Completed,
                new StartupItem { Name = "Agent", Command = configuredValue, Source = "Current User Registry" }));

        Assert.AreEqual(StartupVerificationStatus.Verified,
            report.GetStartupVerificationResults().Single().Status);
    }

    [TestMethod]
    public void ProjectionFailureIsContainedAndDoesNotAlterPersistedReceipt()
    {
        using var temp = new TempDirectory();
        var report = new ForgeReportService(temp.Path);
        report.RecordStartupActionReceipt(DisableReceipt(report.Snapshot().SessionId));
        var invalid = new SystemSnapshot
        {
            ScanTime = StartupVerificationTestFactory.ObservedAt,
            StartupSourceResults = new List<StartupScanSourceResult> { null! }
        };

        var service = new StartupVerificationLiveService(
            report,
            new StartupObservationSnapshotBuilder(),
            new StartupVerificationEvaluator(),
            () => StartupVerificationTestFactory.EvaluatedAt,
            (_, _) => { });
        StartupVerificationLiveResult result = service.VerifyAfterSystemScan(invalid);

        Assert.AreEqual(1, result.FailureCount);
        Assert.IsEmpty(report.GetStartupVerificationResults());
        Assert.HasCount(1, report.GetPendingStartupActionReceipts());
    }

    [TestMethod]
    public void RestoreSupersedesOnlyMatchingOlderDisableTarget()
    {
        using var temp = new TempDirectory();
        var report = new ForgeReportService(temp.Path);
        string session = report.Snapshot().SessionId;
        StartupVerificationTargetIdentity first = StartupVerificationTestFactory.Hkcu("First", "first.exe");
        StartupVerificationTargetIdentity second = StartupVerificationTestFactory.Hkcu("Second", "second.exe");
        var disable = new StartupActionReceipt(Guid.NewGuid().ToString("N"), session,
            StartupVerificationOperation.Disable, StartupVerificationTestFactory.ExecutedAt,
            new[]
            {
                Item(first, StartupVerificationOperation.Disable),
                Item(second, StartupVerificationOperation.Disable)
            });
        var restore = new StartupActionReceipt(Guid.NewGuid().ToString("N"), session,
            StartupVerificationOperation.Restore, StartupVerificationTestFactory.ExecutedAt.AddMinutes(1),
            new[] { Item(first, StartupVerificationOperation.Restore) });
        report.RecordStartupActionReceipt(disable);
        report.RecordStartupActionReceipt(restore);

        IReadOnlyList<StartupActionReceipt> pending = report.GetPendingStartupActionReceipts();

        Assert.HasCount(2, pending);
        Assert.AreEqual(second.TargetId, pending.Single(value => value.Operation == StartupVerificationOperation.Disable).Items.Single().TargetIdentity.TargetId);
        Assert.AreEqual(first.TargetId, pending.Single(value => value.Operation == StartupVerificationOperation.Restore).Items.Single().TargetIdentity.TargetId);
    }

    private static StartupActionReceipt DisableReceipt(string sessionId)
    {
        StartupVerificationTargetIdentity target = StartupVerificationTestFactory.Hkcu("Agent", "agent.exe");
        return new StartupActionReceipt(Guid.NewGuid().ToString("N"), sessionId,
            StartupVerificationOperation.Disable, StartupVerificationTestFactory.ExecutedAt,
            new[] { Item(target, StartupVerificationOperation.Disable) });
    }

    private static StartupActionReceiptItem Item(
        StartupVerificationTargetIdentity target,
        StartupVerificationOperation operation) =>
        new(target, StartupExecutionOutcome.Executed,
            operation == StartupVerificationOperation.Disable
                ? new StartupExpectedState(StartupExpectedStateKind.Absent)
                : new StartupExpectedState(StartupExpectedStateKind.PresentWithMatchingConfiguration, target.ConfigurationFingerprint));

    private static SystemSnapshot Scan(StartupScanSourceStatus status, params StartupItem[] items) =>
        new()
        {
            ScanTime = StartupVerificationTestFactory.ObservedAt,
            StartupSourceResults = new List<StartupScanSourceResult>
            {
                new(StartupScanSourceKind.CurrentUserRegistry, status, items),
                new(StartupScanSourceKind.UserStartupFolder, StartupScanSourceStatus.Completed, Array.Empty<StartupItem>())
            }
        };

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "ForgeCare-StartupVerification-" + Guid.NewGuid().ToString("N"));
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
