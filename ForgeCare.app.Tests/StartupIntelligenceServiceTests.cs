using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class StartupIntelligenceServiceTests
{
    [TestMethod]
    public async Task ZeroEntriesReturnsSuccessfulEmptyResult()
    {
        var file = new FakeFileInspector();
        var signature = new FakeSignatureInspector();

        StartupIntelligenceResult result = await CreateService(file, signature).AnalyzeAsync([]);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.PartialSuccess);
        Assert.IsEmpty(result.Entries);
        Assert.AreEqual(0, file.Calls);
        Assert.AreEqual(0, signature.Calls);
    }

    [TestMethod]
    public async Task DirectEntryFlowsThroughInspectorsAndSnapshotsInput()
    {
        var file = new FakeFileInspector();
        var signature = new FakeSignatureInspector();
        var input = new StartupItem
        {
            Name = "Vendor Agent",
            Command = @"C:\Tools\Agent.exe",
            Source = "Current User Registry"
        };

        StartupIntelligenceResult result = await CreateService(file, signature).AnalyzeAsync([input]);
        input.Name = "MUTATED";
        input.Command = "MUTATED";
        input.Source = "MUTATED";

        StartupIntelligenceEntry entry = result.Entries.Single();
        Assert.AreEqual("Vendor Agent", entry.Name);
        Assert.AreEqual(@"C:\Tools\Agent.exe", entry.CommandResolution.OriginalCommand);
        Assert.AreEqual("Current User Registry", entry.OriginalSource);
        Assert.AreEqual(StartupSourceKind.CurrentUserRegistry, entry.SourceKind);
        Assert.AreEqual(StartupClassification.Verified, entry.Classification);
        Assert.AreEqual(1, file.Calls);
        Assert.AreEqual(1, signature.Calls);
    }

    [TestMethod]
    public async Task LauncherAndShortcutNeverInvokeInspectors()
    {
        var file = new FakeFileInspector();
        var signature = new FakeSignatureInspector();
        StartupItem[] inputs =
        [
            new() { Name = "Script", Command = "powershell.exe -File task.ps1", Source = "Current User Registry" },
            new() { Name = "Link", Command = @"C:\Users\Tech\Startup\App.lnk", Source = "User Startup Folder" }
        ];

        StartupIntelligenceResult result = await CreateService(file, signature).AnalyzeAsync(inputs);

        Assert.AreEqual(0, file.Calls);
        Assert.AreEqual(0, signature.Calls);
        Assert.IsTrue(result.Entries.All(entry => entry.Classification == StartupClassification.Unverified));
    }

    [TestMethod]
    public async Task MissingFileAvoidsSignatureInspection()
    {
        var file = new FakeFileInspector
        {
            Result = new StartupFileInspection
            {
                Status = StartupFileInspectionStatus.Missing,
                Exists = false
            }
        };
        var signature = new FakeSignatureInspector();

        StartupIntelligenceResult result = await CreateService(file, signature).AnalyzeAsync([Direct("Missing")]);

        Assert.AreEqual(StartupClassification.Broken, result.Entries.Single().Classification);
        Assert.AreEqual(1, file.Calls);
        Assert.AreEqual(0, signature.Calls);
        Assert.AreEqual(StartupSignatureStatus.FileMissing, result.Entries.Single().Signature.Status);
    }

    [TestMethod]
    public async Task OneInspectorFailureDoesNotDiscardOtherEntries()
    {
        var file = new FakeFileInspector();
        var signature = new FakeSignatureInspector { ThrowOnFirstCall = true };

        StartupIntelligenceResult result = await CreateService(file, signature).AnalyzeAsync(
        [
            Direct("First", @"C:\Tools\First.exe"),
            Direct("Second", @"C:\Tools\Second.exe")
        ]);

        Assert.HasCount(2, result.Entries);
        Assert.IsTrue(result.PartialSuccess);
        Assert.AreEqual(StartupClassification.Unknown, result.Entries[0].Classification);
        Assert.AreEqual(StartupClassification.Verified, result.Entries[1].Classification);
        Assert.IsNotEmpty(result.Warnings);
    }

    [TestMethod]
    public async Task DuplicateTargetIsInspectedOncePerRun()
    {
        var file = new FakeFileInspector();
        var signature = new FakeSignatureInspector();

        StartupIntelligenceResult result = await CreateService(file, signature).AnalyzeAsync(
        [
            Direct("First"),
            Direct("Second")
        ]);

        Assert.HasCount(2, result.Entries);
        Assert.AreEqual(1, file.Calls);
        Assert.AreEqual(1, signature.Calls);
    }

    [TestMethod]
    public async Task OneHundredEntriesCompleteDeterministically()
    {
        var file = new FakeFileInspector();
        var signature = new FakeSignatureInspector();
        StartupItem[] inputs = Enumerable.Range(0, 100)
            .Select(index => Direct($"Entry {index}", $@"C:\Tools\App{index}.exe"))
            .ToArray();

        StartupIntelligenceResult result = await CreateService(file, signature).AnalyzeAsync(inputs);

        Assert.HasCount(100, result.Entries);
        Assert.AreEqual(100, file.Calls);
        Assert.AreEqual(100, signature.Calls);
        Assert.IsTrue(result.Entries.All(entry => entry.Classification == StartupClassification.Verified));
    }

    [TestMethod]
    public async Task CancellationPropagatesAndStartsNoInspection()
    {
        var file = new FakeFileInspector();
        var signature = new FakeSignatureInspector();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
            CreateService(file, signature).AnalyzeAsync([Direct("Entry")], cancellation.Token));

        Assert.AreEqual(0, file.Calls);
        Assert.AreEqual(0, signature.Calls);
    }

    [TestMethod]
    public async Task NullEntryCreatesBoundedErrorAndPreservesValidEntry()
    {
        var file = new FakeFileInspector();
        var signature = new FakeSignatureInspector();
        StartupItem?[] inputs = [null, Direct("Valid")];

        StartupIntelligenceResult result = await CreateService(file, signature).AnalyzeAsync(inputs!);

        Assert.HasCount(1, result.Entries);
        Assert.HasCount(1, result.Errors);
        Assert.IsTrue(result.PartialSuccess);
    }

    private static StartupIntelligenceService CreateService(
        IStartupFileInspector file,
        IStartupSignatureInspector signature) =>
        new(
            new StartupCommandParser(value => value),
            file,
            signature,
            new StartupClassificationPolicy());

    private static StartupItem Direct(string name, string command = @"C:\Tools\Agent.exe") =>
        new()
        {
            Name = name,
            Command = command,
            Source = "Current User Registry"
        };

    private sealed class FakeFileInspector : IStartupFileInspector
    {
        public int Calls { get; private set; }

        public StartupFileInspection Result { get; set; } = new()
        {
            Status = StartupFileInspectionStatus.Available,
            Exists = true,
            FileName = "Agent.exe",
            CompanyName = "Example Corp"
        };

        public Task<StartupFileInspection> InspectAsync(
            string resolvedPath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeSignatureInspector : IStartupSignatureInspector
    {
        public int Calls { get; private set; }

        public bool ThrowOnFirstCall { get; set; }

        public Task<StartupSignatureInfo> InspectAsync(
            string resolvedPath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            if (ThrowOnFirstCall && Calls == 1)
                throw new IOException("Simulated signature failure.");

            return Task.FromResult(new StartupSignatureInfo
            {
                Status = StartupSignatureStatus.Valid,
                SignerName = "Example Signer"
            });
        }
    }
}
