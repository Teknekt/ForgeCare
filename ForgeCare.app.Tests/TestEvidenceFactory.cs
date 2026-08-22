using ForgeCare.App.Models;

namespace ForgeCare.App.Tests;

internal static class TestEvidenceFactory
{
    public static EvidenceRecord Create(
        string? sessionId = null,
        DateTime? timestampUtc = null,
        EvidenceCategory category = EvidenceCategory.System,
        string? correlationKey = "system:test")
    {
        return new EvidenceRecord
        {
            SessionId = sessionId ?? Guid.NewGuid().ToString("N"),
            TimestampUtc = timestampUtc ?? DateTime.UtcNow,
            Category = category,
            Source = EvidenceSource.Manual,
            Subject = "test-subject",
            Observation = "A test observation was recorded.",
            Severity = EvidenceSeverity.Informational,
            Confidence = EvidenceConfidence.Unknown,
            Collector = nameof(TestEvidenceFactory),
            CorrelationKey = correlationKey
        };
    }
}
