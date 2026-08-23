using System.Text.Json;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public class StartupObservationSnapshotBuilderTests
{
    [TestMethod]
    public void ProjectsSupportedSourcesWithUtcSessionAndPrivacySafeIdentity()
    {
        var snapshot = Snapshot(
            Registry(StartupScanSourceStatus.Completed,
                new StartupItem { Name = "Agent", Command = @"C:\Users\Alice\agent.exe --token secret", Source = "Current User Registry" }),
            Folder(StartupScanSourceStatus.Completed,
                new StartupItem { Name = "Example", Command = @"C:\Users\Alice\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\Example.lnk", Source = "User Startup Folder" }));

        StartupObservationSnapshot result = new StartupObservationSnapshotBuilder()
            .Build(snapshot, StartupVerificationTestFactory.SessionId);

        Assert.AreEqual(StartupVerificationTestFactory.SessionId, result.SessionId);
        Assert.AreEqual(DateTimeKind.Utc, result.ObservedAtUtc.Kind);
        Assert.HasCount(2, result.Sources);
        Assert.IsTrue(result.Sources.All(source => source.Status == StartupSourceObservationStatus.Completed));
        Assert.IsTrue(result.Sources.SelectMany(source => source.Targets)
            .All(target => target.ConfigurationFingerprint?.Length == 64));
        string json = JsonSerializer.Serialize(result);
        Assert.IsFalse(json.Contains("Alice", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("--token", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("secret", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void MapsEverySupportedSourceCompletenessState()
    {
        foreach (StartupScanSourceStatus status in Enum.GetValues<StartupScanSourceStatus>())
        {
            var snapshot = Snapshot(Registry(status), Folder(status));
            StartupObservationSnapshot result = new StartupObservationSnapshotBuilder()
                .Build(snapshot, StartupVerificationTestFactory.SessionId);
            StartupSourceObservationStatus expected = status switch
            {
                StartupScanSourceStatus.Completed => StartupSourceObservationStatus.Completed,
                StartupScanSourceStatus.Failed => StartupSourceObservationStatus.Failed,
                StartupScanSourceStatus.Unavailable => StartupSourceObservationStatus.Unavailable,
                _ => StartupSourceObservationStatus.Unknown
            };
            Assert.IsTrue(result.Sources.All(source => source.Status == expected));
        }
    }

    [TestMethod]
    [DataRow(StartupScanSourceStatus.Completed, StartupScanSourceStatus.Completed)]
    [DataRow(StartupScanSourceStatus.Failed, StartupScanSourceStatus.Completed)]
    [DataRow(StartupScanSourceStatus.Completed, StartupScanSourceStatus.Unavailable)]
    [DataRow(StartupScanSourceStatus.Failed, StartupScanSourceStatus.Failed)]
    public void PreservesCompletenessIndependentlyPerSupportedSource(
        StartupScanSourceStatus registryStatus,
        StartupScanSourceStatus folderStatus)
    {
        StartupObservationSnapshot result = new StartupObservationSnapshotBuilder()
            .Build(
                Snapshot(Registry(registryStatus), Folder(folderStatus)),
                StartupVerificationTestFactory.SessionId);

        Assert.AreEqual(Map(registryStatus),
            result.Sources.Single(source => source.SourceKind == StartupVerificationTargetKind.HkcuRun).Status);
        Assert.AreEqual(Map(folderStatus),
            result.Sources.Single(source => source.SourceKind == StartupVerificationTargetKind.UserStartupFolder).Status);
    }

    [TestMethod]
    public void MissingSourceIsUnknownAndDuplicateLocatorIsAmbiguous()
    {
        var missing = new SystemSnapshot { ScanTime = StartupVerificationTestFactory.ObservedAt };
        StartupObservationSnapshot missingResult = new StartupObservationSnapshotBuilder()
            .Build(missing, StartupVerificationTestFactory.SessionId);
        Assert.IsTrue(missingResult.Sources.All(source => source.Status == StartupSourceObservationStatus.Unknown));

        StartupItem item = new() { Name = "Agent", Command = "agent.exe", Source = "Current User Registry" };
        var duplicate = Snapshot(Registry(StartupScanSourceStatus.Completed, item, item), Folder(StartupScanSourceStatus.Completed));
        StartupObservationSnapshot duplicateResult = new StartupObservationSnapshotBuilder()
            .Build(duplicate, StartupVerificationTestFactory.SessionId);
        Assert.IsTrue(duplicateResult.Sources.Single(source => source.SourceKind == StartupVerificationTargetKind.HkcuRun).IsAmbiguous);
    }

    [TestMethod]
    public void SameNameAcrossSourcesRemainsTwoSourceSpecificTargets()
    {
        var snapshot = Snapshot(
            Registry(StartupScanSourceStatus.Completed,
                new StartupItem { Name = "Agent", Command = "agent.exe", Source = "Current User Registry" }),
            Folder(StartupScanSourceStatus.Completed,
                new StartupItem { Name = "Agent", Command = "Agent.lnk", Source = "User Startup Folder" }));

        StartupObservationSnapshot result = new StartupObservationSnapshotBuilder()
            .Build(snapshot, StartupVerificationTestFactory.SessionId);

        Assert.HasCount(2, result.Sources);
        Assert.AreNotEqual(result.Sources[0].Targets.Single().LocatorId, result.Sources[1].Targets.Single().LocatorId);
    }

    private static SystemSnapshot Snapshot(params StartupScanSourceResult[] sources) =>
        new() { ScanTime = StartupVerificationTestFactory.ObservedAt, StartupSourceResults = sources.ToList() };

    private static StartupScanSourceResult Registry(StartupScanSourceStatus status, params StartupItem[] items) =>
        new(StartupScanSourceKind.CurrentUserRegistry, status, items);

    private static StartupScanSourceResult Folder(StartupScanSourceStatus status, params StartupItem[] items) =>
        new(StartupScanSourceKind.UserStartupFolder, status, items);

    private static StartupSourceObservationStatus Map(StartupScanSourceStatus status) =>
        status switch
        {
            StartupScanSourceStatus.Completed => StartupSourceObservationStatus.Completed,
            StartupScanSourceStatus.Failed => StartupSourceObservationStatus.Failed,
            StartupScanSourceStatus.Unavailable => StartupSourceObservationStatus.Unavailable,
            _ => StartupSourceObservationStatus.Unknown
        };
}
