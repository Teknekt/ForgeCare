using System.IO.Compression;
using System.Text;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class BetaFieldTestEvidenceTests
{
    [TestMethod]
    public void NewFieldTestIncludesEvidencePersistenceRestartStep()
    {
        using var temp = new TemporaryDirectory();
        var service = new BetaFieldTestService(temp.Path);

        BetaFieldTestSession session = service.StartNew("Test technician");

        Assert.AreEqual("Not collected", session.ComputerName);

        BetaFieldTestStep evidence = session.Steps.Single(step => step.Id == "evidence");
        StringAssert.Contains(evidence.Title, "Evidence");
        StringAssert.Contains(evidence.Detail, "System Scan");
        StringAssert.Contains(evidence.Detail, "Deep Analysis");
        StringAssert.Contains(evidence.Detail, "restart ForgeCare");
    }

    [TestMethod]
    public async Task IssuePackageProjectsLegacyCrashDiagnosticsWithoutMutatingSource()
    {
        using var temp = new TemporaryDirectory();
        string dataRoot = Path.Combine(temp.Path, "Data");
        string diagnosticsRoot = Path.Combine(temp.Path, "Diagnostics");
        Directory.CreateDirectory(diagnosticsRoot);
        string crashPath = Path.Combine(diagnosticsRoot, "crash.log");
        byte[] sourceBytes = Encoding.UTF8.GetBytes(
            "============================================================\n" +
            "ForgeCare exception · 2026-08-30 14:32:10\n" +
            "Version: 1.1.0-beta.1\n" +
            "Context: WPF DispatcherUnhandledException\n" +
            "Machine: SECRET-WORKSTATION\n" +
            "User: Alice\n" +
            "OS: Microsoft Windows 11\n" +
            ".NET: .NET 10.0.0\n\n" +
            "System.IO.IOException: customer-specific failure at C:\\Users\\Alice\\Documents\\CustomerA\\case.txt\n" +
            "   at ForgeCare.App.MainWindow.Export() in C:\\dev\\Mindforge\\ForgeCare\\MainWindow.xaml.cs:line 42\n" +
            "   at D:\\Clients\\SecretProject\\Support.Run()\n" +
            "Bearer SUPER_SECRET_TOKEN\n" +
            "API_KEY_TEST_VALUE\n" +
            "sk-test-secret\n");
        await File.WriteAllBytesAsync(crashPath, sourceBytes);

        string zipPath = Path.Combine(temp.Path, "issue.zip");
        var service = new BetaFieldTestService(dataRoot, crashPath);
        var diagnostics = new BetaDiagnosticsService(
            dataRoot,
            diagnosticsRoot,
            crashPath);

        service.ExportIssuePackage(CreateIssue(), zipPath, diagnostics);

        using ZipArchive archive = ZipFile.OpenRead(zipPath);
        ZipArchiveEntry? crashEntry = archive.GetEntry("crash.log");
        Assert.IsNotNull(crashEntry);
        string projected = await ReadEntryAsync(crashEntry);
        StringAssert.Contains(projected, "Timestamp: 2026-08-30 14:32:10");
        StringAssert.Contains(projected, "Version: 1.1.0-beta.1");
        StringAssert.Contains(projected, "Context: WPF DispatcherUnhandledException");
        StringAssert.Contains(projected, "Failure type: System.IO.IOException");
        CrashLogBundleProjectorTests.AssertSensitiveValuesAbsent(projected);
        CollectionAssert.AreEqual(sourceBytes, await File.ReadAllBytesAsync(crashPath));
    }

    [TestMethod]
    public async Task IssuePackageProjectionFailureFailsClosedWithoutRawFallback()
    {
        using var temp = new TemporaryDirectory();
        string dataRoot = Path.Combine(temp.Path, "Data");
        string diagnosticsRoot = Path.Combine(temp.Path, "Diagnostics");
        Directory.CreateDirectory(diagnosticsRoot);
        string crashPath = Path.Combine(diagnosticsRoot, "crash.log");
        byte[] sourceBytes = [0xC3, 0x28, 0xFF, 0xFE];
        await File.WriteAllBytesAsync(crashPath, sourceBytes);

        string zipPath = Path.Combine(temp.Path, "issue.zip");
        var service = new BetaFieldTestService(dataRoot, crashPath);
        var diagnostics = new BetaDiagnosticsService(
            dataRoot,
            diagnosticsRoot,
            crashPath);

        service.ExportIssuePackage(CreateIssue(), zipPath, diagnostics);

        using ZipArchive archive = ZipFile.OpenRead(zipPath);
        Assert.IsNull(archive.GetEntry("crash.log"));
        ZipArchiveEntry? warningEntry = archive.GetEntry("issue-package-warnings.txt");
        Assert.IsNotNull(warningEntry);
        Assert.AreEqual(
            "Crash diagnostics unavailable: SanitizationFailed" + Environment.NewLine,
            await ReadEntryAsync(warningEntry));
        CollectionAssert.AreEqual(sourceBytes, await File.ReadAllBytesAsync(crashPath));
    }

    private static BetaIssueReport CreateIssue() =>
        new()
        {
            IssueId = "FCI-PRIVACY-TEST",
            BuildVersion = "1.1.0-beta.1",
            Area = "Support export",
            Severity = "Medium",
            Description = "Harmless test issue",
            ReproductionSteps = "Export the Issue Package",
            ExpectedResult = "Privacy-safe diagnostics",
            ActualResult = "Test fixture"
        };

    private static async Task<string> ReadEntryAsync(ZipArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open());
        return await reader.ReadToEndAsync();
    }
}
