using System.Reflection;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class StartupSignatureInspectorTests
{
    [TestMethod]
    [DataRow(0, StartupSignatureStatus.Valid)]
    [DataRow(unchecked((int)0x800B0100), StartupSignatureStatus.NotSigned)]
    [DataRow(unchecked((int)0x80096010), StartupSignatureStatus.HashMismatch)]
    [DataRow(unchecked((int)0x800B0109), StartupSignatureStatus.Untrusted)]
    [DataRow(unchecked((int)0x800B0111), StartupSignatureStatus.Untrusted)]
    [DataRow(unchecked((int)0x80096004), StartupSignatureStatus.Invalid)]
    [DataRow(unchecked((int)0x80096001), StartupSignatureStatus.InspectionFailure)]
    public void NativeStatusMappingPreservesSignatureSemantics(
        int nativeStatus,
        StartupSignatureStatus expected)
    {
        MethodInfo map = typeof(WinVerifyTrustStartupSignatureInspector)
            .GetMethod("MapNativeStatus", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new AssertFailedException("Native status mapper was not found.");

        var actual = (StartupSignatureStatus)map.Invoke(null, [nativeStatus])!;

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public async Task MissingAndUnsupportedFilesAreDistinct()
    {
        using var temp = new TemporaryDirectory();
        var inspector = new WinVerifyTrustStartupSignatureInspector();

        StartupSignatureInfo missing = await inspector.InspectAsync(Path.Combine(temp.Path, "missing.exe"));
        string unsupportedPath = Path.Combine(temp.Path, "sample.txt");
        await File.WriteAllTextAsync(unsupportedPath, "test");
        StartupSignatureInfo unsupported = await inspector.InspectAsync(unsupportedPath);

        Assert.AreEqual(StartupSignatureStatus.FileMissing, missing.Status);
        Assert.AreEqual(StartupSignatureStatus.Unsupported, unsupported.Status);
        Assert.AreNotEqual(StartupSignatureStatus.NotSigned, missing.Status);
        Assert.AreNotEqual(StartupSignatureStatus.NotSigned, unsupported.Status);
    }

    [TestMethod]
    public void AllRequiredSemanticStatesRemainDistinct()
    {
        StartupSignatureStatus[] values =
        [
            StartupSignatureStatus.Valid,
            StartupSignatureStatus.NotSigned,
            StartupSignatureStatus.HashMismatch,
            StartupSignatureStatus.Untrusted,
            StartupSignatureStatus.Invalid,
            StartupSignatureStatus.InspectionFailure,
            StartupSignatureStatus.FileMissing,
            StartupSignatureStatus.NotChecked,
            StartupSignatureStatus.Unsupported
        ];

        Assert.AreEqual(values.Length, values.Distinct().Count());
    }
}
