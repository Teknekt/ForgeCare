using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ForgeCare.App.Models;

public sealed class ForgePlanAttentionItem
{
    public ForgePlanAttentionItem(
        string id,
        string attentionId,
        string ruleId,
        string title,
        string summary,
        string categoryDisplay,
        string priorityDisplay,
        TechnicianAttentionPriority priority,
        string confidenceDisplay,
        EvidenceConfidence confidence,
        string rationale,
        string suggestedInvestigation,
        string supportingEvidenceSummary,
        IEnumerable<Guid> evidenceIds,
        DateTime latestEvidenceTimestampUtc,
        string timestampDisplay,
        IEnumerable<string> sourceDisplays)
    {
        if (latestEvidenceTimestampUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Forge Plan attention timestamp must be UTC.", nameof(latestEvidenceTimestampUtc));

        Id = Required(id, nameof(id));
        AttentionId = Required(attentionId, nameof(attentionId));
        RuleId = Required(ruleId, nameof(ruleId));
        Title = Required(title, nameof(title));
        Summary = Required(summary, nameof(summary));
        CategoryDisplay = Required(categoryDisplay, nameof(categoryDisplay));
        PriorityDisplay = Required(priorityDisplay, nameof(priorityDisplay));
        Priority = priority;
        ConfidenceDisplay = Required(confidenceDisplay, nameof(confidenceDisplay));
        Confidence = confidence;
        Rationale = Required(rationale, nameof(rationale));
        SuggestedInvestigation = Required(suggestedInvestigation, nameof(suggestedInvestigation));
        SupportingEvidenceSummary = Required(supportingEvidenceSummary, nameof(supportingEvidenceSummary));
        EvidenceIds = new ReadOnlyCollection<Guid>(
            (evidenceIds ?? throw new ArgumentNullException(nameof(evidenceIds))).Distinct().ToList());
        LatestEvidenceTimestampUtc = latestEvidenceTimestampUtc;
        TimestampDisplay = Required(timestampDisplay, nameof(timestampDisplay));
        SourceDisplays = new ReadOnlyCollection<string>(
            (sourceDisplays ?? throw new ArgumentNullException(nameof(sourceDisplays)))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList());

        if (EvidenceIds.Count == 0 || EvidenceIds.Any(value => value == Guid.Empty))
            throw new ArgumentException("Supporting Evidence IDs must be non-empty.", nameof(evidenceIds));
    }

    public string Id { get; }
    public string AttentionId { get; }
    public string RuleId { get; }
    public string Title { get; }
    public string Summary { get; }
    public string CategoryDisplay { get; }
    public string PriorityDisplay { get; }
    public TechnicianAttentionPriority Priority { get; }
    public string ConfidenceDisplay { get; }
    public EvidenceConfidence Confidence { get; }
    public string Rationale { get; }
    public string SuggestedInvestigation { get; }
    public string SupportingEvidenceSummary { get; }
    public int SupportingEvidenceCount => EvidenceIds.Count;
    public IReadOnlyList<Guid> EvidenceIds { get; }
    public DateTime LatestEvidenceTimestampUtc { get; }
    public string TimestampDisplay { get; }
    public bool HasMultipleEvidenceSources => SourceDisplays.Count > 1;
    public IReadOnlyList<string> SourceDisplays { get; }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value must not be empty.", parameterName);
        return value.Trim();
    }
}
