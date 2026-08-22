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

    [TestMethod]
    public async Task SystemScanAndDeepAnalysisEvidenceCoexistInOneSessionDocument()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        var systemSnapshot = new SystemSnapshot
        {
            OperatingSystem = "Test Windows",
            ProcessorName = "Test Processor",
            TotalMemoryGb = 32,
            AvailableMemoryGb = 16,
            SystemDriveFreeGb = 100,
            ScanTime = DateTime.Now
        };
        var deepAnalysis = new ResourceAnalysisResult
        {
            CpuUsagePercent = 10,
            MemoryUsedPercent = 50,
            UsedMemoryGb = 16,
            AvailableMemoryGb = 16,
            TotalMemoryGb = 32,
            ProcessCount = 100,
            CpuStatus = "NORMAL",
            MemoryStatus = "NORMAL",
            ProcessStatus = "NORMAL",
            OverallPressure = "LOW",
            AnalysisTime = DateTime.Now,
            TopProcesses =
            {
                new ResourceProcessInfo
                {
                    ProcessId = 123,
                    Name = "Test Process",
                    CpuPercent = 1,
                    MemoryMb = 200,
                    PressureScore = 10,
                    PressureLevel = "MINIMAL",
                    PrimaryResource = "BALANCED"
                }
            }
        };

        var writer = new EvidenceService(new JsonEvidenceRepository(temp.Path));
        EvidenceCollectionResult systemEvidence =
            new SystemScanEvidenceAdapter().Collect(systemSnapshot, sessionId);
        EvidenceCollectionResult deepEvidence =
            new DeepAnalysisEvidenceAdapter().Collect(deepAnalysis, sessionId);

        EvidenceCollectionResult systemSave = await writer.AddRangeAsync(systemEvidence.Evidence);
        EvidenceCollectionResult deepSave = await writer.AddRangeAsync(deepEvidence.Evidence);

        var freshReader = new JsonEvidenceRepository(temp.Path);
        IReadOnlyList<EvidenceRecord> reloaded = await freshReader.GetBySessionAsync(sessionId);

        Assert.IsTrue(systemSave.Success);
        Assert.IsTrue(deepSave.Success);
        Assert.HasCount(11, reloaded);
        Assert.HasCount(6, reloaded.Where(record => record.Source == EvidenceSource.SystemScan));
        Assert.HasCount(5, reloaded.Where(record => record.Source == EvidenceSource.DeepAnalysis));
        Assert.IsTrue(reloaded.All(record => record.SessionId == sessionId));
        Assert.IsTrue(File.Exists(System.IO.Path.Combine(temp.Path, sessionId + ".json")));
        Assert.HasCount(1, Directory.GetFiles(temp.Path, "*.json"));
    }
}
