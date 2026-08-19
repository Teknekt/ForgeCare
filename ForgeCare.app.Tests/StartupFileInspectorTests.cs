using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class StartupFileInspectorTests
{
    [TestMethod]
    public async Task ExistingFileIsInspectedWithoutMutation()
    {
        using var temp = new TemporaryDirectory();
        string path = Path.Combine(temp.Path, "sample.exe");
        byte[] original = [0x4D, 0x5A, 0x00, 0x01];
        await File.WriteAllBytesAsync(path, original);
        DateTime writeTime = File.GetLastWriteTimeUtc(path);

        StartupFileInspection result = await new WindowsStartupFileInspector().InspectAsync(path);

        Assert.AreEqual(StartupFileInspectionStatus.Available, result.Status);
        Assert.IsTrue(result.Exists);
        Assert.AreEqual("sample.exe", result.FileName);
        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(path));
        Assert.AreEqual(writeTime, File.GetLastWriteTimeUtc(path));
    }

    [TestMethod]
    public async Task MissingDirectoryAndUnsupportedFileHaveDistinctStates()
    {
        using var temp = new TemporaryDirectory();
        var inspector = new WindowsStartupFileInspector();

        StartupFileInspection missing = await inspector.InspectAsync(Path.Combine(temp.Path, "missing.exe"));
        StartupFileInspection directory = await inspector.InspectAsync(temp.Path);
        string textPath = Path.Combine(temp.Path, "sample.txt");
        await File.WriteAllTextAsync(textPath, "unchanged");
        StartupFileInspection unsupported = await inspector.InspectAsync(textPath);

        Assert.AreEqual(StartupFileInspectionStatus.Missing, missing.Status);
        Assert.AreEqual(StartupFileInspectionStatus.Unsupported, directory.Status);
        Assert.AreEqual(StartupFileInspectionStatus.Unsupported, unsupported.Status);
        Assert.AreEqual("unchanged", await File.ReadAllTextAsync(textPath));
    }

    [TestMethod]
    public async Task PathIsNormalizedAndMissingMetadataIsNormal()
    {
        using var temp = new TemporaryDirectory();
        string nested = Path.Combine(temp.Path, "folder");
        Directory.CreateDirectory(nested);
        string path = Path.Combine(nested, "..", "plain.exe");
        string actual = Path.Combine(temp.Path, "plain.exe");
        await File.WriteAllBytesAsync(actual, [0x4D, 0x5A]);

        StartupFileInspection result = await new WindowsStartupFileInspector().InspectAsync(path);

        Assert.AreEqual(Path.GetFullPath(actual), result.NormalizedPath);
        Assert.AreEqual(StartupFileInspectionStatus.Available, result.Status);
        Assert.IsNull(result.CompanyName);
    }

    [TestMethod]
    public async Task ManagedAssemblyExposesAvailableVersionMetadata()
    {
        string path = typeof(StartupFileInspectorTests).Assembly.Location;

        StartupFileInspection result = await new WindowsStartupFileInspector().InspectAsync(path);

        Assert.AreEqual(StartupFileInspectionStatus.Available, result.Status);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ProductName));
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FileVersion));
    }

    [TestMethod]
    public async Task MalformedPathBecomesInspectionFailure()
    {
        StartupFileInspection result = await new WindowsStartupFileInspector().InspectAsync("bad\0path.exe");

        Assert.AreEqual(StartupFileInspectionStatus.InspectionFailure, result.Status);
    }
}
