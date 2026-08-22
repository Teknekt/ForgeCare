using System;
using System.Collections.Generic;
using System.Linq;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class ForgePlanAttentionPresenter
{
    public IReadOnlyList<ForgePlanAttentionItem> Present(
        IReadOnlyCollection<TechnicianAttentionItem> attentionItems,
        IReadOnlyCollection<EvidenceRecord>? supportingEvidence = null)
    {
        ArgumentNullException.ThrowIfNull(attentionItems);

        IReadOnlyDictionary<Guid, EvidenceSource> sources = (supportingEvidence ?? Array.Empty<EvidenceRecord>())
            .Where(record => record != null && record.Id != Guid.Empty)
            .GroupBy(record => record.Id)
            .ToDictionary(
                group => group.Key,
                group => group.Select(record => record.Source).OrderBy(source => source).First());

        return attentionItems
            .Where(item => item != null)
            .Select(item => Project(item, sources))
            .OrderByDescending(item => item.Priority)
            .ThenByDescending(item => item.LatestEvidenceTimestampUtc)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static ForgePlanAttentionItem Project(
        TechnicianAttentionItem item,
        IReadOnlyDictionary<Guid, EvidenceSource> sources)
    {
        string[] sourceDisplays = item.EvidenceIds
            .Where(sources.ContainsKey)
            .Select(id => EvidenceDisplayFormatter.FormatSource(sources[id]))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        string count = item.EvidenceIds.Count == 1
            ? "1 supporting Evidence record"
            : $"{item.EvidenceIds.Count} supporting Evidence records";
        string summary = sourceDisplays.Length == 0
            ? count
            : $"{count} · {sourceDisplays.Length} source{(sourceDisplays.Length == 1 ? string.Empty : "s")}";

        return new ForgePlanAttentionItem(
            "plan-" + item.Id,
            item.Id,
            item.RuleId,
            string.IsNullOrWhiteSpace(item.Title) ? "Review related Evidence" : item.Title,
            string.IsNullOrWhiteSpace(item.Summary) ? "Related Evidence deserves technician attention." : item.Summary,
            EvidenceDisplayFormatter.FormatCategory(item.Category),
            item.Priority.ToString(),
            item.Priority,
            EvidenceDisplayFormatter.FormatConfidence(item.Confidence),
            item.Confidence,
            item.Rationale,
            item.SuggestedInvestigation,
            summary,
            item.EvidenceIds,
            item.LatestEvidenceTimestampUtc,
            EvidenceDisplayFormatter.FormatTimestamp(item.LatestEvidenceTimestampUtc),
            sourceDisplays);
    }
}
