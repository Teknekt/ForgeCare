using ForgeCare.App.Models;

namespace ForgeCare.app.Tests;

internal static class EvidenceCorrelationTestFactory
{
    public static readonly string SessionId = "11111111111111111111111111111111";
    public static readonly DateTime Time = new(2026, 8, 23, 10, 0, 0, DateTimeKind.Utc);

    public static EvidenceRecord Startup(
        string path = "%PROGRAMFILES%\\Acme\\agent.exe",
        string correlationKey = "startup:hklm-run:agent:one",
        DateTime? timestamp = null,
        EvidenceConfidence confidence = EvidenceConfidence.High,
        string name = "Acme Agent",
        Guid? id = null,
        string? sessionId = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            SessionId = sessionId ?? SessionId,
            TimestampUtc = timestamp ?? Time,
            Category = EvidenceCategory.Startup,
            Source = EvidenceSource.StartupIntelligence,
            Subject = "startup-entry:acme-agent",
            Observation = "A configured startup entry was observed.",
            Severity = EvidenceSeverity.Informational,
            Confidence = confidence,
            Collector = "StartupIntelligenceEvidenceAdapter",
            CorrelationKey = correlationKey,
            Metadata = new Dictionary<string, string>
            {
                ["entryName"] = name,
                ["normalizedExecutablePath"] = path,
                ["classification"] = "Verified"
            }
        };

    public static EvidenceRecord Process(
        string path = "%PROGRAMFILES%\\Acme\\agent.exe",
        string correlationKey = "process-app:one",
        DateTime? timestamp = null,
        EvidenceSeverity severity = EvidenceSeverity.High,
        EvidenceConfidence confidence = EvidenceConfidence.High,
        string name = "Acme Agent",
        string identityStrength = "Strong",
        Guid? id = null,
        string? sessionId = null,
        double memoryMb = 259,
        string pressure = "HIGH") =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            SessionId = sessionId ?? SessionId,
            TimestampUtc = timestamp ?? Time.AddMinutes(1),
            Category = EvidenceCategory.Process,
            Source = EvidenceSource.ProcessIntelligence,
            Subject = "process-application:acme-agent",
            Observation = "A running application resource observation was recorded.",
            Value = memoryMb,
            Unit = "MB",
            Severity = severity,
            Confidence = confidence,
            Collector = "ProcessIntelligenceEvidenceAdapter",
            CorrelationKey = correlationKey,
            Metadata = new Dictionary<string, string>
            {
                ["applicationName"] = name,
                ["identityStrength"] = identityStrength,
                ["normalizedExecutablePath"] = path,
                ["instanceCount"] = "2",
                ["totalMemoryMb"] = memoryMb.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["totalCpuPercent"] = "12.5",
                ["pressureLevel"] = pressure,
                ["classification"] = "Verified"
            }
        };

    public static EvidenceRecord Other(EvidenceSource source, int index = 0) =>
        new()
        {
            Id = Guid.Parse($"00000000-0000-0000-0000-{index + 1:000000000000}"),
            SessionId = SessionId,
            TimestampUtc = Time.AddSeconds(index),
            Category = source == EvidenceSource.SystemScan ? EvidenceCategory.System : EvidenceCategory.Memory,
            Source = source,
            Subject = "unrelated",
            Observation = "An unrelated observation was recorded.",
            Severity = EvidenceSeverity.Informational,
            Confidence = EvidenceConfidence.High,
            Collector = "Test",
            CorrelationKey = $"other:{source}:{index}"
        };

    public static TechnicianAttentionItem Attention(
        string id = "attention:0000000000000001",
        TechnicianAttentionPriority priority = TechnicianAttentionPriority.Low,
        DateTime? timestamp = null,
        string ruleId = "fake-rule-v1",
        string entityKey = "app:0000000000000001",
        Guid? evidenceId = null) =>
        new(
            id,
            SessionId,
            ruleId,
            "Review application observations",
            "Application observations deserve technician attention.",
            EvidenceCategory.Application,
            priority,
            EvidenceConfidence.High,
            "Two persisted observations were related.",
            "Review the related observations.",
            entityKey,
            [evidenceId ?? Guid.Parse("00000000-0000-0000-0000-000000000001")],
            ["fake:key"],
            timestamp ?? Time);
}
