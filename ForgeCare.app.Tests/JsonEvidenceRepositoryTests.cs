using System.Text.Json;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class JsonEvidenceRepositoryTests
{
    [TestMethod]
    public async Task MissingSessionFileReturnsEmptyCollection()
    {
        using var temp = new TemporaryDirectory();
        var repository = new JsonEvidenceRepository(temp.Path);

        IReadOnlyList<EvidenceRecord> records = await repository.GetBySessionAsync(
            Guid.NewGuid().ToString("N"));

        Assert.IsEmpty(records);
    }

    [TestMethod]
    public async Task SaveReloadAndQueriesPreserveRecordsAndStableIds()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        EvidenceRecord first = TestEvidenceFactory.Create(
            sessionId,
            new DateTime(2026, 8, 19, 10, 0, 0, DateTimeKind.Utc),
            EvidenceCategory.Memory,
            "memory:available");
        EvidenceRecord second = TestEvidenceFactory.Create(
            sessionId,
            new DateTime(2026, 8, 19, 11, 0, 0, DateTimeKind.Utc),
            EvidenceCategory.Storage,
            "drive:system");

        var writer = new JsonEvidenceRepository(temp.Path);
        await writer.AddAsync(first);
        await writer.AddRangeAsync(new[] { second });

        var reader = new JsonEvidenceRepository(temp.Path);
        IReadOnlyList<EvidenceRecord> session = await reader.GetBySessionAsync(sessionId);
        EvidenceRecord? byId = await reader.GetByIdAsync(first.Id);
        IReadOnlyList<EvidenceRecord> byCategory = await reader.GetByCategoryAsync(EvidenceCategory.Memory);
        IReadOnlyList<EvidenceRecord> byCorrelation = await reader.GetByCorrelationKeyAsync("DRIVE:SYSTEM");

        Assert.HasCount(2, session);
        Assert.AreEqual(second.Id, session[0].Id);
        Assert.AreEqual(first.Id, session[1].Id);
        Assert.AreEqual(first.Id, byId?.Id);
        Assert.HasCount(1, byCategory);
        Assert.AreEqual(first.Id, byCategory[0].Id);
        Assert.HasCount(1, byCorrelation);
        Assert.AreEqual(second.Id, byCorrelation[0].Id);
    }

    [TestMethod]
    public async Task BatchAdditionPersistsMultipleRecordsAndUsesStableTieOrdering()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        DateTime timestamp = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);
        EvidenceRecord highId = TestEvidenceFactory.Create(sessionId, timestamp);
        EvidenceRecord lowId = TestEvidenceFactory.Create(sessionId, timestamp);
        highId.Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        lowId.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var repository = new JsonEvidenceRepository(temp.Path);
        await repository.AddRangeAsync(new[] { highId, lowId });

        IReadOnlyList<EvidenceRecord> records = await repository.GetBySessionAsync(sessionId);

        Assert.HasCount(2, records);
        Assert.AreEqual(lowId.Id, records[0].Id);
        Assert.AreEqual(highId.Id, records[1].Id);
    }

    [TestMethod]
    public async Task PersistedDocumentUsesSchemaOneAndStringEnums()
    {
        using var temp = new TemporaryDirectory();
        EvidenceRecord record = TestEvidenceFactory.Create(category: EvidenceCategory.Memory);
        record.Source = EvidenceSource.SystemScan;

        var repository = new JsonEvidenceRepository(temp.Path);
        await repository.AddAsync(record);

        string path = System.IO.Path.Combine(temp.Path, record.SessionId + ".json");
        string json = await File.ReadAllTextAsync(path);
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.AreEqual(1, document.RootElement.GetProperty("SchemaVersion").GetInt32());
        JsonElement evidence = document.RootElement.GetProperty("Evidence")[0];
        Assert.AreEqual("Memory", evidence.GetProperty("Category").GetString());
        Assert.AreEqual("SystemScan", evidence.GetProperty("Source").GetString());
        StringAssert.Contains(json, Environment.NewLine);
    }

    [TestMethod]
    public async Task MalformedJsonIsNotOverwritten()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        string path = System.IO.Path.Combine(temp.Path, sessionId + ".json");
        const string original = "{ this is not valid json";
        await File.WriteAllTextAsync(path, original);
        var repository = new JsonEvidenceRepository(temp.Path);

        await Assert.ThrowsExactlyAsync<MalformedEvidenceDocumentException>(
            () => repository.AddAsync(TestEvidenceFactory.Create(sessionId)));

        Assert.AreEqual(original, await File.ReadAllTextAsync(path));
    }

    [TestMethod]
    public async Task UnsupportedSchemaIsNotOverwritten()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        string path = System.IO.Path.Combine(temp.Path, sessionId + ".json");
        string original = $$"""
            {
              "SchemaVersion": 999,
              "SessionId": "{{sessionId}}",
              "Evidence": []
            }
            """;
        await File.WriteAllTextAsync(path, original);
        var repository = new JsonEvidenceRepository(temp.Path);

        await Assert.ThrowsExactlyAsync<UnsupportedEvidenceSchemaException>(
            () => repository.AddAsync(TestEvidenceFactory.Create(sessionId)));

        Assert.AreEqual(original, await File.ReadAllTextAsync(path));
    }

    [TestMethod]
    public async Task RepeatedWritesAtomicallyReplaceDestinationWithoutLeavingTempFile()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        var repository = new JsonEvidenceRepository(temp.Path);

        await repository.AddAsync(TestEvidenceFactory.Create(sessionId));
        await repository.AddAsync(TestEvidenceFactory.Create(sessionId));

        string path = System.IO.Path.Combine(temp.Path, sessionId + ".json");
        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        Assert.AreEqual(2, document.RootElement.GetProperty("Evidence").GetArrayLength());
        Assert.IsFalse(File.Exists(path + ".tmp"));
    }

    [TestMethod]
    public async Task InvalidSessionIdCannotBecomeAFileName()
    {
        using var temp = new TemporaryDirectory();
        var repository = new JsonEvidenceRepository(temp.Path);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => repository.GetBySessionAsync("..\\outside"));
    }
}
