using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class StartupEvidencePathNormalizerTests
{
    private readonly StartupEvidencePathNormalizer _normalizer = StartupEvidenceTestFactory.Normalizer();

    [TestMethod]
    [DataRow(@"C:\Users\Alice\AppData\Local\Vendor\App.exe", @"%LOCALAPPDATA%\Vendor\App.exe")]
    [DataRow(@"C:\Users\Alice\AppData\Roaming\Vendor\App.exe", @"%APPDATA%\Vendor\App.exe")]
    [DataRow(@"C:\Users\Alice\Tools\App.exe", @"%USERPROFILE%\Tools\App.exe")]
    [DataRow(@"C:\Program Files\Vendor\App.exe", @"%PROGRAMFILES%\Vendor\App.exe")]
    [DataRow(@"C:\Program Files (x86)\Vendor\App.exe", @"%PROGRAMFILES(X86)%\Vendor\App.exe")]
    [DataRow(@"C:\ProgramData\Vendor\App.exe", @"%PROGRAMDATA%\Vendor\App.exe")]
    [DataRow(@"C:\Windows\System32\App.exe", @"%WINDIR%\System32\App.exe")]
    [DataRow(@"D:\Portable\App.exe", @"D:\Portable\App.exe")]
    public void NormalizesRepresentativeWindowsPaths(string input, string expected)
    {
        Assert.AreEqual(expected, _normalizer.Normalize(input));
    }

    [TestMethod]
    public void MostSpecificRootWinsCaseInsensitively()
    {
        string? result = _normalizer.Normalize(@"c:\users\alice\appdata\LOCAL\Vendor\App.exe");

        Assert.AreEqual(@"%LOCALAPPDATA%\Vendor\App.exe", result);
        Assert.IsFalse(result!.Contains("Alice", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.Contains("%USERPROFILE%", StringComparison.Ordinal));
    }

    [TestMethod]
    public void UnknownUserProfileIsRedacted()
    {
        string? result = _normalizer.Normalize(@"C:\Users\Bob\Private\Agent.exe");

        Assert.AreEqual(@"<redacted>\Agent.exe", result);
        Assert.IsFalse(result!.Contains("Bob", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NullEmptyRelativeAndInvalidInputsAreSafeAndDeterministic()
    {
        Assert.IsNull(_normalizer.Normalize(null));
        Assert.IsNull(_normalizer.Normalize("  "));
        Assert.AreEqual(@"<redacted>\Agent.exe", _normalizer.Normalize(@"relative\Agent.exe"));
        Assert.AreEqual(_normalizer.Normalize("bad\0Agent.exe"), _normalizer.Normalize("bad\0Agent.exe"));
    }
}
