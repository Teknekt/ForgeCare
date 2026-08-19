using System.Text.RegularExpressions;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class StartupEvidenceCorrelationKeyBuilderTests
{
    private readonly StartupEvidenceCorrelationKeyBuilder _builder = new();

    [TestMethod]
    public void KeyIsDeterministicBoundedLowercaseAndSafe()
    {
        StartupIntelligenceEntry entry = StartupEvidenceTestFactory.Entry(name: "OneDrive Helper");
        const string path = @"%LOCALAPPDATA%\Vendor\App.exe";

        string first = _builder.Build(entry, path);
        string second = _builder.Build(entry, path);

        Assert.AreEqual(first, second);
        Assert.IsLessThanOrEqualTo(80, first.Length);
        Assert.AreEqual(first.ToLowerInvariant(), first);
        Assert.IsTrue(Regex.IsMatch(first, "^[a-z0-9:-]+$"));
        Assert.IsFalse(first.Contains("Alice", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(first.Contains("AppData", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void MeaningfulIdentityAndSourceChangesChangeKey()
    {
        StartupIntelligenceEntry user = StartupEvidenceTestFactory.Entry();
        StartupIntelligenceEntry machine = StartupEvidenceTestFactory.Entry(
            source: StartupSourceKind.LocalMachineRegistry);

        string original = _builder.Build(user, @"%LOCALAPPDATA%\Vendor\App.exe");

        Assert.AreNotEqual(original, _builder.Build(user, @"%LOCALAPPDATA%\Vendor\Other.exe"));
        Assert.AreNotEqual(original, _builder.Build(machine, @"%LOCALAPPDATA%\Vendor\App.exe"));
    }

    [TestMethod]
    public void SessionTimestampRawCommandAndArgumentsCannotAffectKey()
    {
        StartupIntelligenceEntry first = StartupEvidenceTestFactory.Entry(
            command: @"C:\Users\Alice\App.exe --token=FIRST",
            arguments: "--token=FIRST");
        StartupIntelligenceEntry second = StartupEvidenceTestFactory.Entry(
            command: @"C:\Users\Alice\App.exe --token=SECOND",
            arguments: "--token=SECOND");

        string firstKey = _builder.Build(first, @"%LOCALAPPDATA%\Vendor\App.exe");
        string secondKey = _builder.Build(second, @"%LOCALAPPDATA%\Vendor\App.exe");

        Assert.AreEqual(firstKey, secondKey);
        Assert.IsFalse(firstKey.Contains("token", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(firstKey.Contains("FIRST", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void UnresolvedEntryReceivesDeterministicKey()
    {
        StartupIntelligenceEntry entry = StartupEvidenceTestFactory.Entry(
            resolvedPath: null,
            resolution: StartupCommandResolutionStatus.Ambiguous,
            fileStatus: StartupFileInspectionStatus.NotChecked,
            fileExists: null,
            signature: StartupSignatureStatus.NotChecked,
            classification: StartupClassification.Unknown);

        string key = _builder.Build(entry, null);

        Assert.AreEqual(key, _builder.Build(entry, null));
        StringAssert.StartsWith(key, "startup:hkcu-run:example:");
    }
}
