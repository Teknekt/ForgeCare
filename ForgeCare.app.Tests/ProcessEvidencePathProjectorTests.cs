using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ProcessEvidencePathProjectorTests
{
    private readonly ProcessEvidencePathProjector _projector = ProcessEvidenceTestFactory.PathProjector();

    [TestMethod]
    public void RecognizedRootsUseLongestCaseInsensitiveToken()
    {
        Assert.AreEqual(@"%LOCALAPPDATA%\Vendor\App.exe",
            _projector.Project(@"c:\users\alice\appdata\local\Vendor\App.exe"));
        Assert.AreEqual(@"%PROGRAMFILES%\Vendor\App.exe",
            _projector.Project(@"C:\Program Files\Vendor\App.exe"));
        Assert.AreEqual(@"%WINDIR%\System32\app.exe",
            _projector.Project(@"C:\Windows\System32\app.exe"));
    }

    [TestMethod]
    public void UserProfileAndCustomUserPathsRedactPrivateSegments()
    {
        string profile = _projector.Project(@"C:\Users\Alice\Documents\SecretProject\App.exe")!;
        string custom = _projector.Project(@"D:\Users\Alice\PrivateTools\App.exe")!;

        Assert.AreEqual(@"%USERPROFILE%\App.exe", profile);
        Assert.AreEqual(@"<redacted>\App.exe", custom);
        Assert.IsFalse((profile + custom).Contains("Alice", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse((profile + custom).Contains("SecretProject", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse((profile + custom).Contains("PrivateTools", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void CustomApplicationPathsUseBoundedHashedRootIdentity()
    {
        string first = _projector.Project(@"D:\Portable\Vendor\App.exe")!;
        string repeat = _projector.Project(@"D:\Portable\Vendor\App.exe")!;
        string other = _projector.Project(@"E:\Portable\Vendor\App.exe")!;

        StringAssert.Matches(first, new System.Text.RegularExpressions.Regex(@"^<custom-root>\\[a-f0-9]{12}\\App\.exe$"));
        Assert.AreEqual(first, repeat);
        Assert.AreNotEqual(first, other);
        Assert.IsLessThanOrEqualTo(260, first.Length);
    }

    [TestMethod]
    public void NullAndMalformedValuesDoNotLeakInput()
    {
        Assert.IsNull(_projector.Project(null));
        Assert.AreEqual(@"<redacted>\not-a-path", _projector.Project("not-a-path"));
    }
}
