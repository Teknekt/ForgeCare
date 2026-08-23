using System.Text.Json;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public class StartupActionReceiptBuilderTests
{
    [TestMethod]
    public void Disable_HkcuSuccessBuildsEligiblePrivacySafeReceipt()
    {
        StartupChangeItem item = HkcuItem("DISABLED");
        StartupChangeResult result = Result(item, disabled: 1);
        StartupUndoRecord undo = HkcuUndo();

        StartupActionReceipt receipt = new StartupActionReceiptBuilder()
            .BuildDisable(Session(), Utc(), result, new[] { item }, new[] { undo })
            .Receipt!;

        StartupActionReceiptItem receiptItem = receipt.Items.Single();
        Assert.AreEqual(StartupVerificationOperation.Disable, receipt.Operation);
        Assert.AreEqual(StartupExecutionOutcome.Executed, receiptItem.ExecutionOutcome);
        Assert.AreEqual(StartupExpectedStateKind.Absent, receiptItem.ExpectedState.Kind);
        Assert.IsTrue(receiptItem.IsVerificationEligible);
        Assert.AreEqual("undo:" + undo.Id, receiptItem.RecoveryReference);
        AssertPrivacy(JsonSerializer.Serialize(receipt));
    }

    [TestMethod]
    public void Disable_StartupFolderUsesBoundedFilenameAndNoFullPath()
    {
        StartupChangeItem item = FolderItem("DISABLED");
        StartupUndoRecord undo = FolderUndo();
        StartupChangeResult result = Result(item, disabled: 1);

        StartupActionReceipt receipt = new StartupActionReceiptBuilder()
            .BuildDisable(Session(), Utc(), result, new[] { item }, new[] { undo })
            .Receipt!;

        Assert.AreEqual("Agent.lnk", receipt.Items.Single().TargetIdentity.DisplayName);
        AssertPrivacy(JsonSerializer.Serialize(receipt));
    }

    [TestMethod]
    public void PartialBatchPreservesPerTargetOutcomes()
    {
        StartupChangeItem executed = HkcuItem("DISABLED", "ExecutedAgent");
        StartupChangeItem blocked = HkcuItem("BLOCKED", "BlockedAgent");
        StartupChangeItem skipped = HkcuItem("SKIPPED", "SkippedAgent");
        StartupChangeItem failed = HkcuItem("ERROR", "FailedAgent");
        var result = new StartupChangeResult { DisabledCount = 1, BlockedCount = 1, SkippedCount = 1, ErrorCount = 1 };
        result.Items.AddRange(new[] { executed, blocked, skipped, failed });

        StartupActionReceipt receipt = new StartupActionReceiptBuilder()
            .BuildDisable(Session(), Utc(), result, result.Items, new[] { HkcuUndo("ExecutedAgent") })
            .Receipt!;

        CollectionAssert.AreEqual(
            new[]
            {
                StartupExecutionOutcome.Executed, StartupExecutionOutcome.Blocked,
                StartupExecutionOutcome.Skipped, StartupExecutionOutcome.Failed
            },
            receipt.Items.Select(item => item.ExecutionOutcome).ToArray());
        Assert.HasCount(1, receipt.Items.Where(item => item.IsVerificationEligible));
    }

    [TestMethod]
    public void DryRunNeverProducesEligibleItems()
    {
        StartupChangeItem item = HkcuItem("VALIDATED");
        StartupChangeResult result = Result(item);
        result.IsDryRun = true;

        StartupActionReceipt receipt = new StartupActionReceiptBuilder()
            .BuildDisable(Session(), Utc(), result, new[] { item }, Array.Empty<StartupUndoRecord>())
            .Receipt!;

        Assert.AreEqual(StartupExecutionOutcome.DryRun, receipt.Items.Single().ExecutionOutcome);
        Assert.IsFalse(receipt.Items.Single().IsVerificationEligible);
    }

    [TestMethod]
    public void UnsupportedMachineWideTargetIsRetainedButNotEligible()
    {
        var item = new StartupChangeItem
        {
            Name = "Machine Agent", HandlerType = "UNSUPPORTED_MACHINE_WIDE", Status = "BLOCKED"
        };
        StartupChangeResult result = Result(item, blocked: 1);

        StartupActionReceipt receipt = new StartupActionReceiptBuilder()
            .BuildDisable(Session(), Utc(), result, new[] { item }, Array.Empty<StartupUndoRecord>())
            .Receipt!;

        Assert.AreEqual(StartupVerificationTargetKind.Unsupported, receipt.Items.Single().TargetIdentity.Kind);
        Assert.IsFalse(receipt.Items.Single().IsVerificationEligible);
    }

    [TestMethod]
    public void RestoreSuccessUsesOriginalUndoIdentityAndFingerprint()
    {
        StartupUndoRecord undo = HkcuUndo();
        var restored = new StartupChangeItem { Name = undo.Name, HandlerType = undo.HandlerType, Status = "RESTORED" };
        StartupChangeResult result = Result(restored, restored: 1);

        StartupActionReceipt receipt = new StartupActionReceiptBuilder()
            .BuildRestore(Session(), Utc(), result, new[] { undo })
            .Receipt!;

        StartupActionReceiptItem item = receipt.Items.Single();
        Assert.AreEqual(StartupVerificationOperation.Restore, receipt.Operation);
        Assert.AreEqual(StartupExpectedStateKind.PresentWithMatchingConfiguration, item.ExpectedState.Kind);
        Assert.AreEqual(item.ExpectedState.ExpectedConfigurationFingerprint,
            item.TargetIdentity.ConfigurationFingerprint);
        Assert.AreEqual("undo:" + undo.Id, item.RecoveryReference);
        AssertPrivacy(JsonSerializer.Serialize(receipt));
    }

    [TestMethod]
    public void RestoreFailureIsPersistableButNotEligible()
    {
        StartupUndoRecord undo = FolderUndo();
        var failed = new StartupChangeItem { Name = undo.Name, HandlerType = undo.HandlerType, Status = "ERROR" };
        StartupChangeResult result = Result(failed, errors: 1);

        StartupActionReceipt receipt = new StartupActionReceiptBuilder()
            .BuildRestore(Session(), Utc(), result, new[] { undo })
            .Receipt!;

        Assert.AreEqual(StartupExecutionOutcome.Failed, receipt.Items.Single().ExecutionOutcome);
        Assert.IsFalse(receipt.Items.Single().IsVerificationEligible);
    }

    [TestMethod]
    public void LegacyUndoRecordGetsDeterministicPrivacySafeFallbackReference()
    {
        StartupUndoRecord undo = HkcuUndo();
        undo.Id = string.Empty;
        var item = new StartupChangeItem { Name = undo.Name, HandlerType = undo.HandlerType, Status = "RESTORED" };
        StartupChangeResult result = Result(item, restored: 1);
        var builder = new StartupActionReceiptBuilder();

        string? first = builder.BuildRestore(Session(), Utc(), result, new[] { undo })
            .Receipt!.Items.Single().RecoveryReference;
        string? second = builder.BuildRestore(Session(), Utc(), result, new[] { undo })
            .Receipt!.Items.Single().RecoveryReference;

        Assert.AreEqual(first, second);
        StringAssert.StartsWith(first!, "legacy-undo:");
        AssertPrivacy(first!);
    }

    [TestMethod]
    public void PrePhaseCUndoJsonLoadsAndRemainsRecoverableByBuilder()
    {
        const string legacyJson =
            """
            {
              "Name": "Agent",
              "HandlerType": "REGISTRY_HKCU",
              "RegistryPath": "Software\\Microsoft\\Windows\\CurrentVersion\\Run",
              "RegistryValueName": "Agent",
              "RegistryValueData": "legacy-value",
              "RegistryValueKind": 1,
              "CreatedUtc": "2026-08-23T12:00:00.0000000Z"
            }
            """;
        StartupUndoRecord undo = JsonSerializer.Deserialize<StartupUndoRecord>(legacyJson)!;
        var result = new StartupChangeResult { RestoredCount = 1 };
        result.Items.Add(new StartupChangeItem
        {
            Name = undo.Name, HandlerType = undo.HandlerType, Status = "RESTORED"
        });

        StartupActionReceipt receipt = new StartupActionReceiptBuilder()
            .BuildRestore(Session(), Utc(), result, new[] { undo }).Receipt!;

        Assert.AreEqual(string.Empty, undo.Id);
        Assert.IsTrue(receipt.Items.Single().IsVerificationEligible);
        StringAssert.StartsWith(receipt.Items.Single().RecoveryReference!, "legacy-undo:");
    }

    [TestMethod]
    public void MalformedRestoreTargetDoesNotDiscardOtherValidTargets()
    {
        StartupUndoRecord malformed = HkcuUndo("Malformed");
        malformed.RegistryValueData = string.Empty;
        StartupUndoRecord valid = HkcuUndo("Valid");
        var result = new StartupChangeResult { RestoredCount = 2 };
        result.Items.Add(new StartupChangeItem { Name = "Malformed", HandlerType = "REGISTRY_HKCU", Status = "RESTORED" });
        result.Items.Add(new StartupChangeItem { Name = "Valid", HandlerType = "REGISTRY_HKCU", Status = "RESTORED" });

        StartupActionReceiptBuildResult built = new StartupActionReceiptBuilder()
            .BuildRestore(Session(), Utc(), result, new[] { malformed, valid });

        Assert.IsNotNull(built.Receipt);
        Assert.HasCount(1, built.Receipt.Items);
        Assert.HasCount(1, built.Warnings);
    }

    private static StartupChangeItem HkcuItem(string status, string name = "Agent") => new()
    {
        Name = name,
        HandlerType = "REGISTRY_HKCU",
        RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run",
        RegistryValueName = name,
        Command = @"C:\Users\Alice\agent.exe --token=SUPER_SECRET_VALUE API_KEY_TEST_VALUE",
        Status = status
    };

    private static StartupUndoRecord HkcuUndo(string name = "Agent") => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Name = name,
        HandlerType = "REGISTRY_HKCU",
        RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run",
        RegistryValueName = name,
        RegistryValueData = @"C:\Users\Alice\agent.exe --token=SUPER_SECRET_VALUE API_KEY_TEST_VALUE",
        CreatedUtc = Utc().ToString("O")
    };

    private static StartupChangeItem FolderItem(string status) => new()
    {
        Name = "Agent",
        HandlerType = "STARTUP_FOLDER_USER",
        StartupFilePath = @"C:\Users\Alice\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\Agent.lnk",
        Command = @"C:\Users\Alice\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\Agent.lnk",
        Status = status
    };

    private static StartupUndoRecord FolderUndo() => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Name = "Agent",
        HandlerType = "STARTUP_FOLDER_USER",
        OriginalFilePath = @"C:\Users\Alice\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\Agent.lnk",
        DisabledFilePath = @"C:\Users\Alice\AppData\Local\Mindforge Studio\ForgeCare\StartupState\DisabledStartupFiles\Agent.lnk",
        CreatedUtc = Utc().ToString("O")
    };

    private static StartupChangeResult Result(
        StartupChangeItem item,
        int disabled = 0,
        int restored = 0,
        int blocked = 0,
        int errors = 0)
    {
        var result = new StartupChangeResult
        {
            DisabledCount = disabled, RestoredCount = restored, BlockedCount = blocked, ErrorCount = errors
        };
        result.Items.Add(item);
        return result;
    }

    private static string Session() => Guid.NewGuid().ToString("N");
    private static DateTime Utc() => new(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);

    private static void AssertPrivacy(string serialized)
    {
        foreach (string forbidden in new[]
        {
            @"C:\Users", "Alice", "SUPER_SECRET_VALUE", "API_KEY_TEST_VALUE", "--token",
            "DisabledStartupFiles", @"Software\Microsoft\Windows"
        })
        {
            Assert.IsFalse(serialized.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        }
    }
}
