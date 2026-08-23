using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public class StartupVerificationTargetIdentityTests
{
    [TestMethod]
    public void HkcuIdentity_IsDeterministicAndCaseInsensitive()
    {
        var builder = new StartupVerificationIdentityBuilder();
        var first = builder.BuildHkcuRun("Example Agent", @"C:\Program Files\Example\Agent.exe");
        var second = builder.BuildHkcuRun("example agent", @"c:/program files/example/agent.EXE");

        Assert.AreEqual(first.LocatorId, second.LocatorId);
        Assert.AreEqual(first.TargetId, second.TargetId);
        Assert.AreEqual(first.ConfigurationFingerprint, second.ConfigurationFingerprint);
        Assert.AreEqual("hkcu-run", first.SourceLocator);
    }

    [TestMethod]
    public void StartupFolderIdentity_UsesOnlyBoundedFilenameForDisplay()
    {
        var identity = new StartupVerificationIdentityBuilder().BuildUserStartupFolder(
            @"C:\Users\Alice\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\Agent.lnk",
            @"C:\Users\Alice\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\Agent.lnk");

        Assert.AreEqual("Agent.lnk", identity.DisplayName);
        Assert.AreEqual("user-startup", identity.SourceLocator);
        Assert.IsFalse(Serialize(identity).Contains("Alice", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(Serialize(identity).Contains(@"C:\Users", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void SourceNameAndFingerprintAffectTheAppropriateIdentityLevels()
    {
        var builder = new StartupVerificationIdentityBuilder();
        var original = builder.BuildHkcuRun("Agent", "first-value");
        var changed = builder.BuildHkcuRun("Agent", "second-value");
        var renamed = builder.BuildHkcuRun("Other Agent", "first-value");
        var folder = builder.BuildUserStartupFolder("Agent", "first-value");

        Assert.AreEqual(original.LocatorId, changed.LocatorId);
        Assert.AreNotEqual(original.TargetId, changed.TargetId);
        Assert.AreNotEqual(original.LocatorId, renamed.LocatorId);
        Assert.AreNotEqual(original.LocatorId, folder.LocatorId);
    }

    [TestMethod]
    public void ConfigurationFingerprint_DoesNotRetainSensitiveInput()
    {
        const string raw = @"C:\Users\Alice\agent.exe --token=SUPER_SECRET_VALUE API_KEY_TEST_VALUE";
        string fingerprint = new StartupVerificationIdentityBuilder().BuildConfigurationFingerprint(raw);

        Assert.AreEqual(64, fingerprint.Length);
        Assert.IsTrue(fingerprint.All(character => char.IsAsciiHexDigit(character) && !char.IsUpper(character)));
        Assert.IsFalse(fingerprint.Contains("Alice", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(fingerprint.Contains("SECRET", StringComparison.OrdinalIgnoreCase));
    }

    private static string Serialize(StartupVerificationTargetIdentity identity) =>
        string.Join('|', identity.DisplayName, identity.SourceLocator, identity.LocatorId,
            identity.TargetId, identity.ConfigurationFingerprint);
}
