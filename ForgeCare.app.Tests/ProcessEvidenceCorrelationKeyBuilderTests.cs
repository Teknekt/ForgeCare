using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ProcessEvidenceCorrelationKeyBuilderTests
{
    private readonly ProcessEvidenceCorrelationKeyBuilder _builder = new();

    [TestMethod]
    public void StrongKeysAreDeterministicBoundedAndPrivacySafe()
    {
        var group = ProcessEvidenceTestFactory.Group();
        string first = _builder.Build(group, @"%LOCALAPPDATA%\Vendor\App.exe");
        string repeat = _builder.Build(group, @"%localappdata%\vendor\app.exe");
        string other = _builder.Build(group, @"%PROGRAMFILES%\Vendor\App.exe");

        Assert.AreEqual(first, repeat);
        Assert.AreNotEqual(first, other);
        StringAssert.Matches(first, new System.Text.RegularExpressions.Regex("^process-app:[a-f0-9]{16}$"));
        Assert.IsFalse(first.Contains("Alice", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(first.Contains("Vendor", StringComparison.OrdinalIgnoreCase));
        Assert.IsLessThanOrEqualTo(64, first.Length);
    }

    [TestMethod]
    public void ProvisionalKeyUsesSafeNameAndDoesNotClaimApplicationDurability()
    {
        var group = ProcessEvidenceTestFactory.Group(
            name: "Helper Process!", path: null, strength: ForgeCare.App.Models.ProcessIdentityStrength.Provisional,
            classification: ForgeCare.App.Models.ProcessIdentityClassification.Unknown);
        string key = _builder.Build(group, null);

        StringAssert.Matches(key, new System.Text.RegularExpressions.Regex("^process-instance:helper-process:[a-f0-9]{16}$"));
        Assert.IsFalse(key.Contains("session", StringComparison.OrdinalIgnoreCase));
    }
}
