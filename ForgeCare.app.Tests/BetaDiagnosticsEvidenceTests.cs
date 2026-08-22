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
}
