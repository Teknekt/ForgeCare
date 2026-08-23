using System.Text.Json;
using System.Text.Json.Nodes;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public class ForgeReportStartupVerificationPersistenceTests
{
    [TestMethod]
    public void OldSessionWithoutSprint25CollectionsLoadsWithEmptyDefaults()
    {
        using var temp = new TempDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        var legacy = new ForgeReportSession
        {
            SessionId = sessionId,
            Actions = new List<ForgeReportAction>
            {
                new() { Title = "Legacy diagnostic", IsSuccess = true, Timestamp = DateTime.Now }
            },
            Checkpoints = new List<ForgeReportCheckpoint>
            {
                new() { Timestamp = DateTime.Now, StartupCount = 4 }
            }
        };
        JsonObject root = JsonNode.Parse(JsonSerializer.Serialize(legacy))!.AsObject();
        root.Remove("StartupActionReceipts");
        root.Remove("StartupVerificationResults");
        Directory.CreateDirectory(temp.Path);
        File.WriteAllText(System.IO.Path.Combine(temp.Path, "current-session.json"), root.ToJsonString());

        var service = new ForgeReportService(temp.Path);
        ForgeReportSession loaded = service.Snapshot();

        Assert.AreEqual(sessionId, loaded.SessionId);
        Assert.HasCount(1, loaded.Actions);
        Assert.HasCount(1, loaded.Checkpoints);
        Assert.IsEmpty(loaded.StartupActionReceipts);
        Assert.IsEmpty(loaded.StartupVerificationResults);
    }

    [TestMethod]
    public void ReceiptAndVerificationResultRoundTripAcrossServiceRestart()
    {
        using var temp = new TempDirectory();
        var service = new ForgeReportService(temp.Path);
        string sessionId = service.Snapshot().SessionId;
        StartupActionReceipt receipt = Receipt(sessionId);
        Assert.IsTrue(service.RecordStartupActionReceipt(receipt));
        StartupVerificationResult result = Verification(receipt);
        Assert.IsTrue(service.RecordStartupVerificationResult(result));

        var reloaded = new ForgeReportService(temp.Path);
        ForgeReportSession snapshot = reloaded.Snapshot();

        Assert.HasCount(1, snapshot.StartupActionReceipts);
        Assert.HasCount(1, snapshot.StartupVerificationResults);
        Assert.AreEqual(receipt.ReceiptId, snapshot.StartupActionReceipts[0].ReceiptId);
        Assert.AreEqual(result.TargetId, snapshot.StartupVerificationResults[0].TargetId);
        Assert.IsEmpty(reloaded.GetPendingStartupActionReceipts());
    }

    [TestMethod]
    public void MultipleReceiptsPersistAndCallerCannotMutateSnapshotIntoService()
    {
        using var temp = new TempDirectory();
        var service = new ForgeReportService(temp.Path);
        string session = service.Snapshot().SessionId;
        service.RecordStartupActionReceipt(Receipt(session));
        service.RecordStartupActionReceipt(Receipt(session));

        ForgeReportSession detached = service.Snapshot();
        detached.StartupActionReceipts.Clear();

        Assert.HasCount(2, service.GetStartupActionReceipts());
        Assert.HasCount(2, new ForgeReportService(temp.Path).GetStartupActionReceipts());
    }

    [TestMethod]
    public void DuplicateIdsAreIdempotentButConflictsAreRejected()
    {
        using var temp = new TempDirectory();
        var service = new ForgeReportService(temp.Path);
        string session = service.Snapshot().SessionId;
        StartupActionReceipt original = Receipt(session);
        Assert.IsTrue(service.RecordStartupActionReceipt(original));
        Assert.IsFalse(service.RecordStartupActionReceipt(original));
        var conflicting = new StartupActionReceipt(
            original.ReceiptId,
            session,
            StartupVerificationOperation.Restore,
            original.ExecutedAtUtc,
            original.Items);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            service.RecordStartupActionReceipt(conflicting));
        Assert.HasCount(1, service.GetStartupActionReceipts());
    }

    [TestMethod]
    public void VerificationResultDuplicateIdentityIsIdempotentAndConflictRejected()
    {
        using var temp = new TempDirectory();
        var service = new ForgeReportService(temp.Path);
        StartupActionReceipt receipt = Receipt(service.Snapshot().SessionId);
        service.RecordStartupActionReceipt(receipt);
        StartupVerificationResult result = Verification(receipt);
        Assert.IsTrue(service.RecordStartupVerificationResult(result));
        Assert.IsFalse(service.RecordStartupVerificationResult(result));
        var conflicting = new StartupVerificationResult(
            result.ReceiptId, result.TargetId,
            StartupVerificationStatus.ExpectedOutcomeNotObserved,
            result.EvaluatedAtUtc, result.ObservationTimestampUtc,
            "The later startup observation still contained the reviewed startup target.",
            result.ExpectedStateSummary, result.SourceLocator);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            service.RecordStartupVerificationResult(conflicting));
    }

    [TestMethod]
    public void InconclusiveResultDoesNotRemoveReceiptFromPendingReobservation()
    {
        using var temp = new TempDirectory();
        var service = new ForgeReportService(temp.Path);
        StartupActionReceipt receipt = Receipt(service.Snapshot().SessionId);
        service.RecordStartupActionReceipt(receipt);
        StartupVerificationTargetIdentity target = receipt.Items.Single().TargetIdentity;
        var inconclusive = new StartupVerificationResult(
            receipt.ReceiptId,
            target.TargetId,
            StartupVerificationStatus.Inconclusive,
            receipt.ExecutedAtUtc.AddMinutes(2),
            receipt.ExecutedAtUtc.AddMinutes(1),
            "The expected state could not be established because the startup source was not completely observed.",
            receipt.Items.Single().ExpectedState.Summary,
            target.SourceLocator);
        service.RecordStartupVerificationResult(inconclusive);

        Assert.HasCount(1, service.GetPendingStartupActionReceipts());
    }

    [TestMethod]
    public void WrongSessionReceiptIsRejectedWithoutFallbackIdentity()
    {
        using var temp = new TempDirectory();
        var service = new ForgeReportService(temp.Path);
        StartupActionReceipt receipt = Receipt(Guid.NewGuid().ToString("N"));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            service.RecordStartupActionReceipt(receipt));
        Assert.IsEmpty(service.GetStartupActionReceipts());
    }

    [TestMethod]
    public void MalformedOptionalEntriesAreSkippedWithoutDiscardingLegacyState()
    {
        using var temp = new TempDirectory();
        var service = new ForgeReportService(temp.Path);
        string session = service.Snapshot().SessionId;
        StartupActionReceipt valid = Receipt(session);
        service.RecordStartupActionReceipt(valid);
        string path = System.IO.Path.Combine(temp.Path, "current-session.json");
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        JsonArray receipts = root["StartupActionReceipts"]!.AsArray();
        receipts.Add(new JsonObject
        {
            ["ReceiptId"] = Guid.NewGuid().ToString("N"),
            ["SessionId"] = session,
            ["Operation"] = 999,
            ["ExecutedAtUtc"] = DateTime.UtcNow
        });
        root["StartupVerificationResults"] = new JsonArray
        {
            new JsonObject { ["ReceiptId"] = "malformed", ["Status"] = 999 }
        };
        File.WriteAllText(path, root.ToJsonString());

        ForgeReportSession reloaded = new ForgeReportService(temp.Path).Snapshot();

        Assert.HasCount(1, reloaded.StartupActionReceipts);
        Assert.IsEmpty(reloaded.StartupVerificationResults);
        Assert.AreEqual(valid.ReceiptId, reloaded.StartupActionReceipts[0].ReceiptId);
    }

    [TestMethod]
    public void PersistedSessionContainsNoTransientStartupSecrets()
    {
        using var temp = new TempDirectory();
        var service = new ForgeReportService(temp.Path);
        StartupChangeItem item = new()
        {
            Name = "Agent", HandlerType = "REGISTRY_HKCU",
            RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run",
            RegistryValueName = "Agent",
            Command = @"C:\Users\Alice\agent.exe --token=SUPER_SECRET_VALUE API_KEY_TEST_VALUE",
            Status = "DISABLED"
        };
        var result = new StartupChangeResult { DisabledCount = 1 };
        result.Items.Add(item);
        var undo = new StartupUndoRecord
        {
            Id = Guid.NewGuid().ToString("N"), Name = "Agent", HandlerType = "REGISTRY_HKCU",
            RegistryPath = item.RegistryPath, RegistryValueName = item.RegistryValueName,
            RegistryValueData = item.Command,
            DisabledFilePath = @"C:\Users\Alice\private\disabled.lnk"
        };
        StartupActionReceipt receipt = new StartupActionReceiptBuilder()
            .BuildDisable(service.Snapshot().SessionId, DateTime.UtcNow, result, new[] { item }, new[] { undo })
            .Receipt!;
        service.RecordStartupActionReceipt(receipt);

        string persisted = File.ReadAllText(System.IO.Path.Combine(temp.Path, "current-session.json"));
        foreach (string forbidden in new[]
        {
            @"C:\\Users", "Alice", "SUPER_SECRET_VALUE", "API_KEY_TEST_VALUE", "--token",
            "DisabledFilePath", "RegistryValueData", "RegistryPath"
        })
            Assert.IsFalse(persisted.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
    }

    [TestMethod]
    public void ExistingStartupActivityRemainsSeparateFromTypedReceipt()
    {
        using var temp = new TempDirectory();
        var service = new ForgeReportService(temp.Path);
        var change = new StartupChangeResult { DisabledCount = 1 };
        service.RecordStartupChange(change, isRestore: false);
        StartupActionReceipt receipt = Receipt(service.Snapshot().SessionId);
        service.RecordStartupActionReceipt(receipt);

        ForgeReportSession snapshot = service.Snapshot();
        Assert.HasCount(1, snapshot.Actions);
        Assert.AreEqual("STARTUP", snapshot.Actions[0].Category);
        Assert.HasCount(1, snapshot.StartupActionReceipts);
        Assert.AreEqual(1, snapshot.ActionCount);
        Assert.AreEqual(1, snapshot.SuccessfulActionCount);
    }

    private static StartupActionReceipt Receipt(string sessionId)
    {
        StartupVerificationTargetIdentity target = StartupVerificationTestFactory.Hkcu();
        return new StartupActionReceipt(
            Guid.NewGuid().ToString("N"), sessionId, StartupVerificationOperation.Disable,
            StartupVerificationTestFactory.ExecutedAt,
            new[]
            {
                new StartupActionReceiptItem(
                    target, StartupExecutionOutcome.Executed,
                    new StartupExpectedState(StartupExpectedStateKind.Absent), "undo:test")
            });
    }

    private static StartupVerificationResult Verification(StartupActionReceipt receipt)
    {
        StartupVerificationTargetIdentity target = receipt.Items.Single().TargetIdentity;
        var observation = new StartupObservationSnapshot(
            receipt.SessionId, receipt.ExecutedAtUtc.AddMinutes(1),
            new[]
            {
                new StartupSourceObservation(
                    target.Kind, StartupSourceObservationStatus.Completed,
                    Array.Empty<StartupVerificationTargetIdentity>())
            });
        return new StartupVerificationEvaluator()
            .Evaluate(receipt, observation, receipt.ExecutedAtUtc.AddMinutes(2)).Single();
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ForgeCareTests", Guid.NewGuid().ToString("N"));
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
