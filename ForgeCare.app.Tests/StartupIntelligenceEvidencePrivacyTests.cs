using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class StartupIntelligenceEvidencePrivacyTests
{
    [TestMethod]
    public async Task SensitiveRawCommandArgumentsAndUserPathNeverPersist()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        const string secret = "SUPER_SECRET_VALUE";
        StartupIntelligenceEntry transientEntry = StartupEvidenceTestFactory.Entry(
            command: "\"C:\\Users\\Alice\\AppData\\Local\\Vendor\\App.exe\" --token=" + secret,
            arguments: "--token=" + secret);

        EvidenceCollectionResult collection = StartupEvidenceTestFactory.Adapter().Collect(
            new StartupIntelligenceResult([transientEntry]),
            sessionId,
            new DateTime(2026, 8, 20, 11, 0, 0, DateTimeKind.Utc));
        await new EvidenceService(new JsonEvidenceRepository(temp.Path))
            .AddRangeAsync(collection.Evidence);

        string json = await File.ReadAllTextAsync(Path.Combine(temp.Path, sessionId + ".json"));
        EvidenceRecord record = (await new JsonEvidenceRepository(temp.Path).GetBySessionAsync(sessionId)).Single();
        string persistedFields = string.Join("|",
            new[] { record.Subject, record.Observation, record.CorrelationKey ?? string.Empty }
                .Concat(record.Metadata.SelectMany(pair => new[] { pair.Key, pair.Value })));

        Assert.IsFalse(persistedFields.Contains(secret, StringComparison.Ordinal));
        Assert.IsFalse(persistedFields.Contains("--token", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(persistedFields.Contains(@"C:\Users\Alice", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains(secret, StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("--token", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains(@"C:\\Users\\Alice", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(@"%LOCALAPPDATA%\Vendor\App.exe", record.Metadata["normalizedExecutablePath"]);

        // The transient intelligence input remains intact; privacy is a projection boundary.
        StringAssert.Contains(transientEntry.CommandResolution.OriginalCommand, secret);
        StringAssert.Contains(transientEntry.CommandResolution.Arguments!, secret);
    }

    [TestMethod]
    public void SourceSerializesAsStringInsideSchemaOneDocument()
    {
        var document = new EvidenceDocument
        {
            SessionId = Guid.NewGuid().ToString("N"),
            Evidence =
            [
                new EvidenceRecord
                {
                    SessionId = Guid.NewGuid().ToString("N"),
                    TimestampUtc = DateTime.UtcNow,
                    Category = EvidenceCategory.Startup,
                    Source = EvidenceSource.StartupIntelligence,
                    Observation = "Startup Evidence test.",
                    Severity = EvidenceSeverity.Informational,
                    Confidence = EvidenceConfidence.High
                }
            ]
        };
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };

        string json = JsonSerializer.Serialize(document, options);

        StringAssert.Contains(json, "\"SchemaVersion\":1");
        StringAssert.Contains(json, "\"Source\":\"StartupIntelligence\"");
    }
}
