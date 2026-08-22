using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeCare.App.Models;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ProcessIntelligenceEvidencePrivacyTests
{
    [TestMethod]
    [DataRow(@"C:\Users\Alice\AppData\Local\Vendor\App.exe")]
    [DataRow(@"D:\Users\Alice\PrivateTools\App.exe")]
    [DataRow(@"C:\Users\Alice\Documents\SecretProject\App.exe")]
    public void SerializedEvidenceRedactsSensitiveExecutablePaths(string transientPath)
    {
        ProcessApplicationGroup group = ProcessEvidenceTestFactory.Group(path: transientPath);
        EvidenceRecord record = ProcessEvidenceTestFactory.Adapter().Collect(
            ProcessEvidenceTestFactory.Result(group), Guid.NewGuid().ToString("N"), DateTime.UtcNow).Evidence.Single();
        string json = JsonSerializer.Serialize(record, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });

        Assert.IsFalse(json.Contains(@"C:\Users\Alice", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains(@"D:\Users\Alice", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("Alice", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("SecretProject", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("PrivateTools", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TransientRationaleAndInputMessagesCannotLeakSecrets()
    {
        ProcessApplicationGroup group = ProcessEvidenceTestFactory.Group(name: "App");
        var intelligence = new ProcessIntelligenceResult(
            group.Members, [group], ["SUPER_SECRET_VALUE"], ["API_KEY_TEST_VALUE"]);
        EvidenceCollectionResult collection = ProcessEvidenceTestFactory.Adapter().Collect(
            intelligence, Guid.NewGuid().ToString("N"), DateTime.UtcNow);
        string serialized = JsonSerializer.Serialize(collection.Evidence);
        string messages = string.Join(' ', collection.Warnings.Concat(collection.Errors));

        Assert.IsFalse(serialized.Contains("Transient rationale", StringComparison.Ordinal));
        Assert.IsFalse((serialized + messages).Contains("SUPER_SECRET_VALUE", StringComparison.Ordinal));
        Assert.IsFalse((serialized + messages).Contains("API_KEY_TEST_VALUE", StringComparison.Ordinal));
    }

    [TestMethod]
    public void EvidenceMetadataHasNoCommandAccountOrArbitraryFields()
    {
        EvidenceRecord record = ProcessEvidenceTestFactory.Adapter().Collect(
            ProcessEvidenceTestFactory.Result(ProcessEvidenceTestFactory.Group()),
            Guid.NewGuid().ToString("N"), DateTime.UtcNow).Evidence.Single();
        string[] forbidden = ["command", "argument", "username", "owner", "sid", "account"];

        foreach (string key in record.Metadata.Keys)
            Assert.IsFalse(forbidden.Any(token => key.Contains(token, StringComparison.OrdinalIgnoreCase)));
    }
}
