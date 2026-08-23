namespace ForgeCare.App.Models;

public enum StartupVerificationStatus
{
    Verified,
    ExpectedOutcomeNotObserved,
    Inconclusive,
    NotEligible
}

public sealed class StartupVerificationResult
{
    public StartupVerificationResult(
        string receiptId,
        string targetId,
        StartupVerificationStatus status,
        DateTime evaluatedAtUtc,
        DateTime observationTimestampUtc,
        string rationale,
        string expectedStateSummary,
        string sourceLocator)
    {
        if (evaluatedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Evaluation timestamp must be UTC.", nameof(evaluatedAtUtc));
        if (observationTimestampUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Observation timestamp must be UTC.", nameof(observationTimestampUtc));

        ReceiptId = Required(receiptId, nameof(receiptId), 64);
        TargetId = Required(targetId, nameof(targetId), 64);
        Status = status;
        EvaluatedAtUtc = evaluatedAtUtc;
        ObservationTimestampUtc = observationTimestampUtc;
        Rationale = Required(rationale, nameof(rationale), 400);
        ExpectedStateSummary = Required(expectedStateSummary, nameof(expectedStateSummary), 240);
        SourceLocator = Required(sourceLocator, nameof(sourceLocator), 48);
    }

    public string ReceiptId { get; }
    public string TargetId { get; }
    public StartupVerificationStatus Status { get; }
    public DateTime EvaluatedAtUtc { get; }
    public DateTime ObservationTimestampUtc { get; }
    public string Rationale { get; }
    public string ExpectedStateSummary { get; }
    public string SourceLocator { get; }

    private static string Required(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value must not be empty.", parameterName);
        string normalized = value.Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }
}
