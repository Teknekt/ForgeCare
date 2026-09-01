using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class DiagnosticPrivacyTests
{
    private static readonly string[] SensitiveValues =
    {
        @"C:\Users\Alice\SecretCustomer\failure.txt",
        @"C:\Work\Customer-Acquisition\private.json",
        "user@example.com",
        "Bearer abcdef123456",
        "sk-test-secret-value",
        @"HKCU\Software\Customer\Token=abcdef"
    };

    [TestMethod]
    public void CrashEntryContainsOnlyBoundedDiagnosticIdentity()
    {
        string combined = string.Join(" | ", SensitiveValues);
        var exception = new InvalidOperationException(combined);

        string entry = PrivacySafeDiagnosticFormatter.FormatCrashEntry(
            exception,
            combined,
            new DateTime(2026, 8, 28, 12, 30, 0, DateTimeKind.Utc));

        StringAssert.Contains(entry, "UTC");
        StringAssert.Contains(entry, "InvalidOperationException");
        StringAssert.Contains(entry, "Context: Unspecified subsystem");
        AssertSensitiveValuesAbsent(entry);
        Assert.IsFalse(entry.Contains(exception.ToString(), StringComparison.Ordinal));
    }

    [TestMethod]
    public void SupportFailureDoesNotExposePathOrExceptionMessage()
    {
        var exception = new UnauthorizedAccessException(string.Join(" | ", SensitiveValues));

        string warning = PrivacySafeDiagnosticFormatter.FormatSupportFailure(
            "Safety",
            "CopyFailed",
            exception);

        Assert.AreEqual(
            "Category: Safety | Status: CopyFailed | Reason: UnauthorizedAccessException",
            warning);
        AssertSensitiveValuesAbsent(warning);
    }

    private static void AssertSensitiveValuesAbsent(string output)
    {
        foreach (string sensitive in SensitiveValues)
            Assert.IsFalse(output.Contains(sensitive, StringComparison.OrdinalIgnoreCase), sensitive);
    }
}
