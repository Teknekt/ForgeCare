using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class SystemScanEvidenceAdapterTests
{
    [TestMethod]
    public void CollectProducesRequiredConservativeSystemEvidence()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        var snapshot = new SystemSnapshot
        {
            OperatingSystem = "Test Windows",
            ProcessorName = "Test Processor",
            TotalMemoryGb = 64.0,
            AvailableMemoryGb = 51.0,
            SystemDriveFreeGb = 321.5,
            ScanTime = new DateTime(2026, 8, 19, 14, 30, 0, DateTimeKind.Local),
            StartupItems =
            {
                new StartupItem(),
                new StartupItem(),
                new StartupItem()
            }
        };

        EvidenceCollectionResult result = new SystemScanEvidenceAdapter().Collect(snapshot, sessionId);

        Assert.IsTrue(result.Success);
        Assert.HasCount(6, result.Evidence);
        Assert.IsTrue(result.Evidence.All(record => record.SessionId == sessionId));
        Assert.IsTrue(result.Evidence.All(record => record.TimestampUtc.Kind == DateTimeKind.Utc));
        Assert.IsTrue(result.Evidence.All(record => record.Source == EvidenceSource.SystemScan));

        AssertRecord(result, "operating-system", EvidenceCategory.OperatingSystem, null, "system:os");
        AssertRecord(result, "processor", EvidenceCategory.Cpu, null, "system:processor");
        AssertRecord(result, "physical-memory", EvidenceCategory.Memory, 64.0, "memory:physical");
        AssertRecord(result, "available-memory", EvidenceCategory.Memory, 51.0, "memory:available");
        AssertRecord(result, "system-drive", EvidenceCategory.Storage, 321.5, "drive:system");
        AssertRecord(result, "startup-entries", EvidenceCategory.Startup, 3.0, "startup:count");

        string observations = string.Join(" ", result.Evidence.Select(record => record.Observation));
        Assert.IsFalse(observations.Contains("unnecessary", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(observations.Contains("recommend", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(observations.Contains("should", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(observations.Contains("disable", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(observations.Contains("optimize", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void InvalidSessionProducesPartialCollectionErrorsWithoutThrowing()
    {
        var snapshot = new SystemSnapshot
        {
            ScanTime = DateTime.Now
        };

        EvidenceCollectionResult result = new SystemScanEvidenceAdapter().Collect(snapshot, string.Empty);

        Assert.IsFalse(result.Success);
        Assert.IsEmpty(result.Evidence);
        Assert.HasCount(6, result.Errors);
    }

    private static void AssertRecord(
        EvidenceCollectionResult result,
        string subject,
        EvidenceCategory category,
        double? value,
        string correlationKey)
    {
        EvidenceRecord record = result.Evidence.Single(item => item.Subject == subject);
        Assert.AreEqual(category, record.Category);
        Assert.AreEqual(value, record.Value);
        Assert.AreEqual(correlationKey, record.CorrelationKey);
    }
}
