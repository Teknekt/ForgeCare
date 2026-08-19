using System.Text.Json;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class StartupIntelligenceEvidenceVerticalSliceTests
{
    [TestMethod]
    public async Task StartupEvidencePersistsReloadsAndCoexistsInSchemaOne()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        DateTime startupTime = new(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);
        EvidenceCollectionResult startup = StartupEvidenceTestFactory.Adapter().Collect(
            new StartupIntelligenceResult(
            [
                StartupEvidenceTestFactory.Entry(name: "OneDrive"),
                StartupEvidenceTestFactory.Entry(
                    name: "Legacy Tool",
                    resolvedPath: @"C:\Program Files\Legacy\Missing.exe",
                    fileStatus: StartupFileInspectionStatus.Missing,
                    fileExists: false,
                    signature: StartupSignatureStatus.FileMissing,
                    classification: StartupClassification.Broken)
            ]),
            sessionId,
            startupTime);

        EvidenceRecord systemScan = Record(sessionId, startupTime.AddMinutes(-2), EvidenceSource.SystemScan, "startup-entries");
        EvidenceRecord deepAnalysis = Record(sessionId, startupTime.AddMinutes(-1), EvidenceSource.DeepAnalysis, "process-count");
        var service = new EvidenceService(new JsonEvidenceRepository(temp.Path));

        EvidenceCollectionResult persistedExisting = await service.AddRangeAsync([systemScan, deepAnalysis]);
        EvidenceCollectionResult persistedStartup = await service.AddRangeAsync(startup.Evidence);

        Assert.IsTrue(persistedExisting.Success);
        Assert.IsTrue(persistedStartup.Success);

        IReadOnlyList<EvidenceRecord> reloaded = await new JsonEvidenceRepository(temp.Path)
            .GetBySessionAsync(sessionId);
        string json = await File.ReadAllTextAsync(Path.Combine(temp.Path, sessionId + ".json"));
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.HasCount(4, reloaded);
        Assert.AreEqual(1, document.RootElement.GetProperty("SchemaVersion").GetInt32());
        Assert.AreEqual(sessionId, document.RootElement.GetProperty("SessionId").GetString());
        CollectionAssert.AreEquivalent(
            new[] { EvidenceSource.SystemScan, EvidenceSource.DeepAnalysis, EvidenceSource.StartupIntelligence },
            reloaded.Select(record => record.Source).Distinct().ToArray());
        Assert.IsTrue(startup.Evidence.All(original => reloaded.Any(saved => saved.Id == original.Id)));
        Assert.IsTrue(reloaded.All(record => record.SessionId == sessionId));
        Assert.IsTrue(reloaded.All(record => record.TimestampUtc.Kind == DateTimeKind.Utc));
        Assert.AreEqual(startupTime, reloaded[0].TimestampUtc);
        Assert.AreEqual(@"%LOCALAPPDATA%\Vendor\App.exe",
            reloaded.First(record => record.Subject == "startup-entry:onedrive")
                .Metadata["normalizedExecutablePath"]);
    }

    [TestMethod]
    public async Task ExistingSchemaOneSourcesStillLoadUnchanged()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        EvidenceRecord system = Record(sessionId, DateTime.UtcNow, EvidenceSource.SystemScan, "system");
        EvidenceRecord deep = Record(sessionId, DateTime.UtcNow.AddSeconds(-1), EvidenceSource.DeepAnalysis, "deep");
        var repository = new JsonEvidenceRepository(temp.Path);

        await repository.AddRangeAsync([system, deep]);
        IReadOnlyList<EvidenceRecord> reloaded = await new JsonEvidenceRepository(temp.Path).GetBySessionAsync(sessionId);

        Assert.HasCount(2, reloaded);
        Assert.IsTrue(reloaded.Any(record => record.Id == system.Id && record.Source == EvidenceSource.SystemScan));
        Assert.IsTrue(reloaded.Any(record => record.Id == deep.Id && record.Source == EvidenceSource.DeepAnalysis));
    }

    [TestMethod]
    public async Task EvidenceInspectionAcceptsStartupIntelligenceInSchemaOne()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        EvidenceCollectionResult startup = StartupEvidenceTestFactory.Adapter().Collect(
            new StartupIntelligenceResult([StartupEvidenceTestFactory.Entry()]),
            sessionId,
            DateTime.UtcNow);
        await new JsonEvidenceRepository(temp.Path).AddRangeAsync(startup.Evidence);

        EvidenceHealthResult health = new EvidenceInspectionService(temp.Path).Inspect();

        Assert.AreEqual(1, health.ValidDocumentCount);
        Assert.AreEqual(1, health.TotalRecordCount);
        Assert.IsFalse(health.HasErrors);
        Assert.IsFalse(health.HasWarnings);
    }

    private static EvidenceRecord Record(
        string sessionId,
        DateTime timestamp,
        EvidenceSource source,
        string subject) =>
        new()
        {
            SessionId = sessionId,
            TimestampUtc = timestamp,
            Category = EvidenceCategory.System,
            Source = source,
            Subject = subject,
            Observation = "Existing Evidence remained present.",
            Severity = EvidenceSeverity.Informational,
            Confidence = EvidenceConfidence.High,
            Collector = nameof(StartupIntelligenceEvidenceVerticalSliceTests)
        };
}
