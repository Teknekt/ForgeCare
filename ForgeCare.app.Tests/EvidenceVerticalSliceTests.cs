using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class EvidenceVerticalSliceTests
{
    [TestMethod]
    public async Task SystemSnapshotCanBePersistedReloadedAndQueriedThroughFreshServices()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        var snapshot = new SystemSnapshot
        {
            OperatingSystem = "Test Windows",
            ProcessorName = "Test Processor",
            TotalMemoryGb = 32.0,
            AvailableMemoryGb = 12.5,
            SystemDriveFreeGb = 200.0,
            ScanTime = DateTime.Now,
            StartupItems = { new StartupItem(), new StartupItem() }
        };

        EvidenceCollectionResult collected =
            new SystemScanEvidenceAdapter().Collect(snapshot, sessionId);
        var writerService = new EvidenceService(
            new JsonEvidenceRepository(temp.Path));

        EvidenceCollectionResult saved = await writerService.AddRangeAsync(collected.Evidence);

        var freshReaderService = new EvidenceService(
            new JsonEvidenceRepository(temp.Path));
        EvidenceCollectionResult reloaded = await freshReaderService.GetBySessionAsync(sessionId);
        EvidenceCollectionResult memory = await freshReaderService.GetByCategoryAsync(
            EvidenceCategory.Memory);

        Assert.IsTrue(collected.Success);
        Assert.IsTrue(saved.Success);
        Assert.HasCount(6, saved.Evidence);
        Assert.IsTrue(reloaded.Success);
        Assert.HasCount(6, reloaded.Evidence);
        Assert.HasCount(2, memory.Evidence);
        CollectionAssert.AreEquivalent(
            saved.Evidence.Select(record => record.Id).ToList(),
            reloaded.Evidence.Select(record => record.Id).ToList());
    }
}
