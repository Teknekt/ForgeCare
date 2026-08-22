using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ForgeCare.App.Models;

public enum TechnicianAttentionPriority
{
    Informational,
    Low,
    Medium,
    High
}

public sealed class TechnicianAttentionItem
{
    public TechnicianAttentionItem(
        string id,
        string sessionId,
        string ruleId,
        string title,
        string summary,
        EvidenceCategory category,
        TechnicianAttentionPriority priority,
        EvidenceConfidence confidence,
        string rationale,
        string suggestedInvestigation,
        string entityKey,
        IEnumerable<Guid> evidenceIds,
        IEnumerable<string> correlationKeys,
        DateTime latestEvidenceTimestampUtc)
    {
        if (latestEvidenceTimestampUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Attention item timestamp must be UTC.", nameof(latestEvidenceTimestampUtc));
        if (!Enum.IsDefined(category))
            throw new ArgumentOutOfRangeException(nameof(category));
        if (!Enum.IsDefined(priority))
            throw new ArgumentOutOfRangeException(nameof(priority));
        if (!Enum.IsDefined(confidence))
            throw new ArgumentOutOfRangeException(nameof(confidence));

        Id = BoundRequired(id, nameof(id), 96);
        SessionId = BoundRequired(sessionId, nameof(sessionId), 64);
        RuleId = BoundRequired(ruleId, nameof(ruleId), 96);
        Title = BoundRequired(title, nameof(title), 180);
        Summary = BoundRequired(summary, nameof(summary), 400);
        Category = category;
        Priority = priority;
        Confidence = confidence;
        Rationale = BoundRequired(rationale, nameof(rationale), 900);
        SuggestedInvestigation = BoundRequired(suggestedInvestigation, nameof(suggestedInvestigation), 400);
        EntityKey = BoundRequired(entityKey, nameof(entityKey), 80);
        EvidenceIds = new ReadOnlyCollection<Guid>(
            (evidenceIds ?? throw new ArgumentNullException(nameof(evidenceIds)))
                .Distinct()
                .ToList());
        CorrelationKeys = new ReadOnlyCollection<string>(
            (correlationKeys ?? throw new ArgumentNullException(nameof(correlationKeys)))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList());
        LatestEvidenceTimestampUtc = latestEvidenceTimestampUtc;

        if (EvidenceIds.Count == 0)
            throw new ArgumentException("Attention item requires supporting Evidence IDs.", nameof(evidenceIds));
        if (EvidenceIds.Any(id => id == Guid.Empty))
            throw new ArgumentException("Supporting Evidence IDs must not be empty.", nameof(evidenceIds));
    }

    public string Id { get; }
    public string SessionId { get; }
    public string RuleId { get; }
    public string Title { get; }
    public string Summary { get; }
    public EvidenceCategory Category { get; }
    public TechnicianAttentionPriority Priority { get; }
    public EvidenceConfidence Confidence { get; }
    public string Rationale { get; }
    public string SuggestedInvestigation { get; }
    public string EntityKey { get; }
    public IReadOnlyList<Guid> EvidenceIds { get; }
    public IReadOnlyList<string> CorrelationKeys { get; }
    public DateTime LatestEvidenceTimestampUtc { get; }

    private static string BoundRequired(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value must not be empty.", parameterName);

        string normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : normalized[..maximumLength];
    }
}
