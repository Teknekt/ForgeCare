using System.IO.Compression;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class BetaDiagnosticsEvidenceTests
{
    [TestMethod]
    public void AbsentEvidenceDirectoryDoesNotFailBundleCreation()
    {
        using var temp = new TemporaryDirectory();
        string dataRoot = Path.Combine(temp.Path, "Data");
        string diagnosticsRoot = Path.Combine(temp.Path, "Diagnostics");
        string zipPath = Path.Combine(temp.Path, "bundle-without-evidence.zip");
        var service = new BetaDiagnosticsService(
            dataRoot,
            diagnosticsRoot,
            Path.Combine(diagnosticsRoot, "crash.log"));

        string result = service.ExportDebugBundle(zipPath);

        Assert.AreEqual(zipPath, result);
        Assert.IsTrue(File.Exists(zipPath));
        using ZipArchive archive = ZipFile.OpenRead(zipPath);
        Assert.IsNotNull(archive.GetEntry("environment.txt"));
        Assert.IsFalse(archive.Entries.Any(entry => entry.FullName.StartsWith("Evidence/")));
    }

    [TestMethod]
    public async Task EvidenceIsIncludedWithoutModifyingSourceFile()
    {
        using var temp = new TemporaryDirectory();
        string dataRoot = Path.Combine(temp.Path, "Data");
        string evidenceRoot = Path.Combine(dataRoot, "Evidence");
        string diagnosticsRoot = Path.Combine(temp.Path, "Diagnostics");
        Directory.CreateDirectory(evidenceRoot);
        string sessionFile = Guid.NewGuid().ToString("N") + ".json";
        string evidencePath = Path.Combine(evidenceRoot, sessionFile);
        const string original = "{\"SchemaVersion\":1,\"Evidence\":[]}";
        await File.WriteAllTextAsync(evidencePath, original);
        DateTime originalWriteTime = File.GetLastWriteTimeUtc(evidencePath);
        string zipPath = Path.Combine(temp.Path, "bundle-with-evidence.zip");
        var service = new BetaDiagnosticsService(
            dataRoot,
            diagnosticsRoot,
            Path.Combine(diagnosticsRoot, "crash.log"));

        service.ExportDebugBundle(zipPath);

        using ZipArchive archive = ZipFile.OpenRead(zipPath);
        ZipArchiveEntry? entry = archive.GetEntry("Evidence/" + sessionFile);
        Assert.IsNotNull(entry);
        using var reader = new StreamReader(entry.Open());
        Assert.AreEqual(original, await reader.ReadToEndAsync());
        Assert.AreEqual(original, await File.ReadAllTextAsync(evidencePath));
        Assert.AreEqual(originalWriteTime, File.GetLastWriteTimeUtc(evidencePath));
    }

    [TestMethod]
    public async Task AuthoritativeSafetyRootIsIncludedAndLegacyMismatchIsIgnored()
    {
        using var temp = new TemporaryDirectory();
        string dataRoot = Path.Combine(temp.Path, "Data");
        string diagnosticsRoot = Path.Combine(temp.Path, "Diagnostics");
        string safetyRoot = Path.Combine(temp.Path, "AuthoritativeSafety");
        string legacySafetyRoot = Path.Combine(dataRoot, "Safety");
        Directory.CreateDirectory(safetyRoot);
        Directory.CreateDirectory(legacySafetyRoot);
        await File.WriteAllTextAsync(Path.Combine(safetyRoot, "action-journal.json"), "authoritative");
        await File.WriteAllTextAsync(Path.Combine(legacySafetyRoot, "wrong-root.json"), "legacy-mismatch");
        string zipPath = Path.Combine(temp.Path, "bundle-with-safety.zip");
        var service = new BetaDiagnosticsService(
            dataRoot,
            diagnosticsRoot,
            Path.Combine(diagnosticsRoot, "crash.log"),
            safetyRoot);

        service.ExportDebugBundle(zipPath);

        using ZipArchive archive = ZipFile.OpenRead(zipPath);
        Assert.IsNotNull(archive.GetEntry("Safety/action-journal.json"));
        Assert.IsNull(archive.GetEntry("Safety/wrong-root.json"));
        Assert.AreEqual("authoritative", await ReadEntryAsync(archive.GetEntry("Safety/action-journal.json")!));
    }

    [TestMethod]
    public async Task LegacyCrashLogIsProjectedWithoutChangingSourceOrOtherBundleContent()
    {
        using var temp = new TemporaryDirectory();
        string dataRoot = Path.Combine(temp.Path, "Data");
        string diagnosticsRoot = Path.Combine(temp.Path, "Diagnostics");
        string safetyRoot = Path.Combine(temp.Path, "Safety");
        string evidenceRoot = Path.Combine(dataRoot, "Evidence");
        string reportsRoot = Path.Combine(dataRoot, "Reports");
        string settingsRoot = Path.Combine(dataRoot, "Settings");
        Directory.CreateDirectory(diagnosticsRoot);
        Directory.CreateDirectory(safetyRoot);
        Directory.CreateDirectory(evidenceRoot);
        Directory.CreateDirectory(reportsRoot);
        Directory.CreateDirectory(settingsRoot);

        string crashPath = Path.Combine(diagnosticsRoot, "crash.log");
        byte[] sourceBytes = System.Text.Encoding.UTF8.GetBytes(
            "============================================================\n" +
            "ForgeCare exception · 2026-08-30 14:32:10\n" +
            "Context: WPF DispatcherUnhandledException\n" +
            "Machine: SECRET-WORKSTATION\n" +
            "User: Alice\n" +
            "System.IO.IOException: customer failure C:\\Users\\Alice\\Documents\\CustomerA\n" +
            " at Method() in C:\\dev\\Mindforge\\ForgeCare\\MainWindow.xaml.cs:line 42\n" +
            "Bearer SUPER_SECRET_TOKEN\n");
        await File.WriteAllBytesAsync(crashPath, sourceBytes);
        await File.WriteAllTextAsync(Path.Combine(safetyRoot, "journal.json"), "safety");
        await File.WriteAllTextAsync(Path.Combine(evidenceRoot, "evidence.json"), "evidence");
        await File.WriteAllTextAsync(Path.Combine(reportsRoot, "report.json"), "report");
        await File.WriteAllTextAsync(Path.Combine(settingsRoot, "settings.json"), "settings");

        string zipPath = Path.Combine(temp.Path, "bundle.zip");
        var service = new BetaDiagnosticsService(dataRoot, diagnosticsRoot, crashPath, safetyRoot);
        service.ExportDebugBundle(zipPath);

        using ZipArchive archive = ZipFile.OpenRead(zipPath);
        string projected = await ReadEntryAsync(archive.GetEntry("crash.log")!);
        StringAssert.Contains(projected, "Context: WPF DispatcherUnhandledException");
        StringAssert.Contains(projected, "Failure type: System.IO.IOException");
        CrashLogBundleProjectorTests.AssertSensitiveValuesAbsent(projected);
        Assert.IsNotNull(archive.GetEntry("Safety/journal.json"));
        Assert.IsNotNull(archive.GetEntry("Evidence/evidence.json"));
        Assert.IsNotNull(archive.GetEntry("Reports/report.json"));
        Assert.IsNotNull(archive.GetEntry("Settings/settings.json"));
        CollectionAssert.AreEqual(sourceBytes, await File.ReadAllBytesAsync(crashPath));
    }

    [TestMethod]
    public async Task InvalidCrashEncodingProducesBoundedWarningWithoutRawFallback()
    {
        using var temp = new TemporaryDirectory();
        string dataRoot = Path.Combine(temp.Path, "Data");
        string diagnosticsRoot = Path.Combine(temp.Path, "Diagnostics");
        Directory.CreateDirectory(diagnosticsRoot);
        string crashPath = Path.Combine(diagnosticsRoot, "crash.log");
        await File.WriteAllBytesAsync(crashPath, new byte[] { 0xC3, 0x28, 0xFF, 0xFE });
        string zipPath = Path.Combine(temp.Path, "bundle.zip");
        var service = new BetaDiagnosticsService(dataRoot, diagnosticsRoot, crashPath);

        service.ExportDebugBundle(zipPath);

        using ZipArchive archive = ZipFile.OpenRead(zipPath);
        Assert.IsNull(archive.GetEntry("crash.log"));
        string warning = await ReadEntryAsync(archive.GetEntry("bundle-copy-warnings.txt")!);
        Assert.AreEqual("Crash diagnostics unavailable: SanitizationFailed" + Environment.NewLine, warning);
    }

    private static async Task<string> ReadEntryAsync(ZipArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open());
        return await reader.ReadToEndAsync();
    }

    [TestMethod]
    public async Task CopyWarningUsesCategoryAndExceptionTypeWithoutSourcePath()
    {
        using var temp = new TemporaryDirectory();
        string dataRoot = Path.Combine(temp.Path, "Data");
        string diagnosticsRoot = Path.Combine(temp.Path, "Diagnostics");
        string safetyRoot = Path.Combine(temp.Path, "Safety", "SecretCustomer");
        Directory.CreateDirectory(safetyRoot);
        string sensitivePath = Path.Combine(safetyRoot, "Bearer-abcdef123456.json");
        await File.WriteAllTextAsync(sensitivePath, "support data");
        await using FileStream lockStream = new(
            sensitivePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.None);
        string zipPath = Path.Combine(temp.Path, "bundle-with-warning.zip");
        var service = new BetaDiagnosticsService(
            dataRoot,
            diagnosticsRoot,
            Path.Combine(diagnosticsRoot, "crash.log"),
            safetyRoot);

        service.ExportDebugBundle(zipPath);

        using ZipArchive archive = ZipFile.OpenRead(zipPath);
        ZipArchiveEntry? warningEntry = archive.GetEntry("bundle-copy-warnings.txt");
        Assert.IsNotNull(warningEntry);
        string warning = await ReadEntryAsync(warningEntry);
        StringAssert.Contains(warning, "Category: Safety | Status: CopyFailed | Reason: IOException");
        Assert.IsFalse(warning.Contains(sensitivePath, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(warning.Contains("Bearer-abcdef123456", StringComparison.OrdinalIgnoreCase));
    }
}
