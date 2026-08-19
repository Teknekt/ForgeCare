using System;
using System.Collections.Generic;

namespace ForgeCare.App.Models;

public sealed class EvidenceRecord
{
    public const int MaxMetadataEntries = 32;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string SessionId { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; }

    public EvidenceCategory Category { get; set; }

    public EvidenceSource Source { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string Observation { get; set; } = string.Empty;

    public double? Value { get; set; }

    public string? Unit { get; set; }

    public EvidenceSeverity Severity { get; set; }

    public EvidenceConfidence Confidence { get; set; }

    public string Collector { get; set; } = string.Empty;

    public Dictionary<string, string> Metadata { get; set; } = new();

    public string? CorrelationKey { get; set; }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (Id == Guid.Empty)
            errors.Add("Evidence Id must not be empty.");

        if (string.IsNullOrWhiteSpace(SessionId))
            errors.Add("Evidence SessionId must not be empty.");

        if (TimestampUtc.Kind != DateTimeKind.Utc)
            errors.Add("Evidence TimestampUtc must have DateTimeKind.Utc.");

        if (!Enum.IsDefined(Category))
            errors.Add("Evidence Category is not defined.");

        if (!Enum.IsDefined(Source))
            errors.Add("Evidence Source is not defined.");

        if (string.IsNullOrWhiteSpace(Observation))
            errors.Add("Evidence Observation must not be empty.");

        if (!Enum.IsDefined(Severity))
            errors.Add("Evidence Severity is not defined.");

        if (!Enum.IsDefined(Confidence))
            errors.Add("Evidence Confidence is not defined.");

        if (Metadata == null)
            errors.Add("Evidence Metadata must not be null.");
        else if (Metadata.Count > MaxMetadataEntries)
            errors.Add($"Evidence Metadata must contain no more than {MaxMetadataEntries} entries.");

        return errors;
    }
}
