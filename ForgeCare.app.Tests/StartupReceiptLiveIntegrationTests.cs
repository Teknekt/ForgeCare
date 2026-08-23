using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public class StartupReceiptLiveIntegrationTests
{
    [TestMethod]
    public void PersistenceFailureDoesNotAlterSuccessfulMutationResult()
    {
        StartupChangeItem item = Item("DISABLED");
        StartupChangeResult result = Result(item, disabled: 1);
        var logged = new List<string>();
        var integration = new StartupReceiptIntegrationService(
            log: (exception, context) => logged.Add(context + "|" + exception.Message));

        bool recorded = integration.TryRecordDisable(
            Guid.NewGuid().ToString("N"), DateTime.UtcNow,
            result, new[] { item }, new[] { Undo() },
            _ => throw new IOException(@"C:\Users\Alice\private-report-path"));

        Assert.IsFalse(recorded);
        Assert.AreEqual(1, result.DisabledCount);
        Assert.AreEqual("DISABLED", item.Status);
        Assert.HasCount(1, logged);
        Assert.IsFalse(logged[0].Contains("Alice", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(logged[0].Contains(@"C:\Users", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void InvalidSessionIsIsolatedAndNoFallbackIsInvented()
    {
        StartupChangeItem item = Item("DISABLED");
        StartupChangeResult result = Result(item, disabled: 1);
        int persistenceCalls = 0;
        var integration = new StartupReceiptIntegrationService(log: (_, _) => { });

        bool recorded = integration.TryRecordDisable(
            "invalid-session", DateTime.UtcNow,
            result, new[] { item }, new[] { Undo() },
            _ => { persistenceCalls++; return true; });

        Assert.IsFalse(recorded);
        Assert.AreEqual(0, persistenceCalls);
        Assert.AreEqual(1, result.DisabledCount);
    }

    [TestMethod]
    public void SuccessfulIntegrationPersistsExactlyOneConstructedReceipt()
    {
        StartupChangeItem item = Item("DISABLED");
        StartupChangeResult result = Result(item, disabled: 1);
        var persisted = new List<StartupActionReceipt>();
        var integration = new StartupReceiptIntegrationService(log: (_, _) => { });

        bool recorded = integration.TryRecordDisable(
            Guid.NewGuid().ToString("N"), DateTime.UtcNow,
            result, new[] { item }, new[] { Undo() },
            receipt => { persisted.Add(receipt); return true; });

        Assert.IsTrue(recorded);
        Assert.HasCount(1, persisted);
        Assert.AreEqual(StartupExecutionOutcome.Executed,
            persisted.Single().Items.Single().ExecutionOutcome);
    }

    private static StartupChangeItem Item(string status) => new()
    {
        Name = "Agent", HandlerType = "REGISTRY_HKCU",
        RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run",
        RegistryValueName = "Agent", Command = "value", Status = status
    };

    private static StartupUndoRecord Undo() => new()
    {
        Id = Guid.NewGuid().ToString("N"), Name = "Agent", HandlerType = "REGISTRY_HKCU",
        RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run",
        RegistryValueName = "Agent", RegistryValueData = "value"
    };

    private static StartupChangeResult Result(StartupChangeItem item, int disabled)
    {
        var result = new StartupChangeResult { DisabledCount = disabled };
        result.Items.Add(item);
        return result;
    }
}
