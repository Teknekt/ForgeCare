using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class EvidenceInspectionServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

    [TestMethod]
    public void MissingDirectoryIsAHealthyLazyState()
    {
        using var temp = new TemporaryDirectory();
        string missing = Path.Combine(temp.Path, "Evidence");

        EvidenceHealthResult result =
            new EvidenceInspectionService(missing).Inspect();

        Assert.IsFalse(result.DirectoryExists);
        Assert.IsFalse(result.HasErrors);
        Assert.IsFalse(result.HasWarnings);
        Assert.AreEqual(0, result.DocumentCount);
        Assert.IsFalse(Directory.Exists(missing));
    }

    [TestMethod]
    public async Task ValidDocumentReportsCountsSchemaAndLatestTimestamp()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        DateTime timestamp = new(2026, 8, 19, 18, 0, 0, DateTimeKind.Utc);
        var repository = new JsonEvidenceRepository(temp.Path);
        await repository.AddAsync(
            TestEvidenceFactory.Create(sessionId, timestamp));

        EvidenceHealthResult result =
            new EvidenceInspectionService(temp.Path).Inspect();

        Assert.IsTrue(result.DirectoryExists);
        Assert.AreEqual(1, result.DocumentCount);
        Assert.AreEqual(1, result.ValidDocumentCount);
        Assert.AreEqual(1, result.TotalRecordCount);
        Assert.AreEqual(timestamp, result.LatestTimestampUtc);
        Assert.IsFalse(result.HasErrors);
        Assert.IsFalse(result.HasWarnings);
    }

    [TestMethod]
    public async Task MalformedDocumentIsReportedAndNotModified()
    {
        using var temp = new TemporaryDirectory();
        string path = Path.Combine(temp.Path, Guid.NewGuid().ToString("N") + ".json");
        const string original = "{ invalid evidence";
        await File.WriteAllTextAsync(path, original);

        EvidenceHealthResult result =
            new EvidenceInspectionService(temp.Path).Inspect();

        Assert.AreEqual(1, result.MalformedDocumentCount);
        Assert.IsTrue(result.HasErrors);
        Assert.AreEqual(original, await File.ReadAllTextAsync(path));
    }

    [TestMethod]
    public async Task UnsupportedSchemaIsWarnedAndNotModified()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        string path = Path.Combine(temp.Path, sessionId + ".json");
        string original = $$"""
            {
              "SchemaVersion": 999,
              "SessionId": "{{sessionId}}",
              "Evidence": []
            }
            """;
        await File.WriteAllTextAsync(path, original);

        EvidenceHealthResult result =
            new EvidenceInspectionService(temp.Path).Inspect();

        Assert.AreEqual(1, result.UnsupportedSchemaCount);
        Assert.IsTrue(result.HasWarnings);
        Assert.IsFalse(result.HasErrors);
        Assert.AreEqual(original, await File.ReadAllTextAsync(path));
    }

    [TestMethod]
    public async Task SessionMismatchIsReported()
    {
        using var temp = new TemporaryDirectory();
        string fileSessionId = Guid.NewGuid().ToString("N");
        string documentSessionId = Guid.NewGuid().ToString("N");
        EvidenceRecord record = TestEvidenceFactory.Create(documentSessionId);
        await WriteDocumentAsync(
            Path.Combine(temp.Path, fileSessionId + ".json"),
            documentSessionId,
            record);

        EvidenceHealthResult result =
            new EvidenceInspectionService(temp.Path).Inspect();

        Assert.AreEqual(1, result.InvalidDocumentCount);
        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("do not match")));
    }

    [TestMethod]
    public async Task NonUtcRecordTimestampIsReported()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        EvidenceRecord record = TestEvidenceFactory.Create(sessionId);
        record.TimestampUtc = DateTime.SpecifyKind(
            new DateTime(2026, 8, 19, 18, 0, 0),
            DateTimeKind.Unspecified);
        await WriteDocumentAsync(
            Path.Combine(temp.Path, sessionId + ".json"),
            sessionId,
            record);

        EvidenceHealthResult result =
            new EvidenceInspectionService(temp.Path).Inspect();

        Assert.AreEqual(1, result.InvalidDocumentCount);
        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("DateTimeKind.Utc")));
    }

    [TestMethod]
    public void DefaultPathUsesForgeCareEvidenceRoot()
    {
        string expectedSuffix = Path.Combine("ForgeCare", "Evidence");

        StringAssert.EndsWith(
            EvidenceInspectionService.DefaultStorageRoot,
            expectedSuffix);
    }

    private static Task WriteDocumentAsync(
        string path,
        string sessionId,
        EvidenceRecord record)
    {
        var document = new EvidenceDocument
        {
            SessionId = sessionId,
            Evidence = new List<EvidenceRecord> { record }
        };

        return File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(document, JsonOptions));
    }
}
