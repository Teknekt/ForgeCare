using System.Text.Json;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ProcessIntelligenceEvidenceVerticalSliceTests
{
    [TestMethod]
    public async Task AggregateEvidencePersistsReloadsWithStableIdentityInSchemaOne()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        DateTime timestamp = new(2026, 8, 22, 15, 30, 0, DateTimeKind.Utc);
        ProcessApplicationGroup strong = ProcessEvidenceTestFactory.Group(totalCpu: 135, totalMemory: 1800, memberCount: 4);
        ProcessApplicationGroup provisional = ProcessEvidenceTestFactory.Group(
            name: "helper", path: null, strength: ProcessIdentityStrength.Provisional,
            classification: ProcessIdentityClassification.Unknown, pressureLevel: "LOW",
            transientIdentity: "provisional");
        EvidenceCollectionResult collection = ProcessEvidenceTestFactory.Adapter().Collect(
            ProcessEvidenceTestFactory.Result(strong, provisional), sessionId, timestamp);
        var service = new EvidenceService(new JsonEvidenceRepository(temp.Path));

        EvidenceCollectionResult persisted = await service.AddRangeAsync(collection.Evidence);
        var freshService = new EvidenceService(new JsonEvidenceRepository(temp.Path));
        EvidenceCollectionResult reloadResult = await freshService.GetBySessionAsync(sessionId);
        IReadOnlyList<EvidenceRecord> reloaded = reloadResult.Evidence;
        string json = await File.ReadAllTextAsync(Path.Combine(temp.Path, sessionId + ".json"));
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.IsTrue(persisted.Success);
        Assert.IsTrue(reloadResult.Success);
        Assert.HasCount(2, reloaded);
        Assert.AreEqual(1, document.RootElement.GetProperty("SchemaVersion").GetInt32());
        Assert.IsTrue(json.Contains("\"Source\": \"ProcessIntelligence\"", StringComparison.Ordinal));
        Assert.IsTrue(collection.Evidence.All(original => reloaded.Any(saved => saved.Id == original.Id)));
        Assert.IsTrue(reloaded.All(record => record.SessionId == sessionId && record.TimestampUtc == timestamp));
        EvidenceRecord savedStrong = reloaded.Single(record => record.Metadata["identityStrength"] == "Strong");
        Assert.AreEqual(1800d, savedStrong.Value);
        Assert.AreEqual("135", savedStrong.Metadata["totalCpuPercent"]);
        Assert.AreEqual("Verified", savedStrong.Metadata["classification"]);
        Assert.AreEqual(@"%LOCALAPPDATA%\Vendor\App.exe", savedStrong.Metadata["normalizedExecutablePath"]);
        Assert.IsNotNull(savedStrong.CorrelationKey);
        Assert.IsFalse(json.Contains(@"C:\Users\Alice", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("Alice", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task AllFourEvidenceSourcesCoexistInOneSchemaOneDocument()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        DateTime timestamp = DateTime.UtcNow;
        var service = new EvidenceService(new JsonEvidenceRepository(temp.Path));
        EvidenceRecord[] existing =
        [
            ProcessEvidenceTestFactory.ExistingRecord(sessionId, EvidenceSource.SystemScan, timestamp.AddSeconds(-3)),
            ProcessEvidenceTestFactory.ExistingRecord(sessionId, EvidenceSource.DeepAnalysis, timestamp.AddSeconds(-2)),
            ProcessEvidenceTestFactory.ExistingRecord(sessionId, EvidenceSource.StartupIntelligence, timestamp.AddSeconds(-1))
        ];
        EvidenceCollectionResult process = ProcessEvidenceTestFactory.Adapter().Collect(
            ProcessEvidenceTestFactory.Result(ProcessEvidenceTestFactory.Group()), sessionId, timestamp);

        await service.AddRangeAsync(existing);
        await service.AddRangeAsync(process.Evidence);
        IReadOnlyList<EvidenceRecord> records = await new JsonEvidenceRepository(temp.Path).GetBySessionAsync(sessionId);

        CollectionAssert.AreEquivalent(
            new[] { EvidenceSource.SystemScan, EvidenceSource.DeepAnalysis, EvidenceSource.StartupIntelligence, EvidenceSource.ProcessIntelligence },
            records.Select(record => record.Source).Distinct().ToArray());
        Assert.HasCount(4, records);
        EvidenceHealthResult health = new EvidenceInspectionService(temp.Path).Inspect();
        Assert.AreEqual(1, health.ValidDocumentCount);
        Assert.AreEqual(4, health.TotalRecordCount);
        Assert.IsFalse(health.HasErrors);
    }

    [TestMethod]
    public async Task ExistingSchemaOneSourceCombinationsStillReload()
    {
        EvidenceSource[][] combinations =
        [
            [EvidenceSource.SystemScan],
            [EvidenceSource.SystemScan, EvidenceSource.DeepAnalysis],
            [EvidenceSource.StartupIntelligence],
            [EvidenceSource.ProcessIntelligence]
        ];
        foreach (EvidenceSource[] sources in combinations)
        {
            using var temp = new TemporaryDirectory();
            string sessionId = Guid.NewGuid().ToString("N");
            var repository = new JsonEvidenceRepository(temp.Path);
            await repository.AddRangeAsync(sources.Select(source =>
                ProcessEvidenceTestFactory.ExistingRecord(sessionId, source, DateTime.UtcNow)).ToArray());
            IReadOnlyList<EvidenceRecord> records = await new JsonEvidenceRepository(temp.Path).GetBySessionAsync(sessionId);
            CollectionAssert.AreEquivalent(sources, records.Select(record => record.Source).ToArray());
        }
    }
}
