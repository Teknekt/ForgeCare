using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ForgeCare.App.Services;

namespace ForgeCare.App.Models;

public sealed class EvidenceExplorerItem
{
    private readonly string[] _searchValues;

    public EvidenceExplorerItem(EvidenceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        Id = record.Id;
        SessionId = record.SessionId;
        TimestampUtc = record.TimestampUtc;
        Category = record.Category;
        Source = record.Source;
        RawSubject = record.Subject;
        Title = EvidenceDisplayFormatter.FormatSubject(record.Subject);
        Observation = record.Observation;
        Value = record.Value;
        Unit = record.Unit;
        ValueDisplay = EvidenceDisplayFormatter.FormatValue(record.Value, record.Unit);
        CategoryDisplay = EvidenceDisplayFormatter.FormatCategory(record.Category);
        SourceDisplay = EvidenceDisplayFormatter.FormatSource(record.Source);
        Severity = record.Severity;
        SeverityDisplay = EvidenceDisplayFormatter.FormatSeverity(record.Severity);
        Confidence = record.Confidence;
        ConfidenceDisplay = EvidenceDisplayFormatter.FormatConfidence(record.Confidence);
        TimestampDisplay = EvidenceDisplayFormatter.FormatTimestamp(record.TimestampUtc);
        Collector = record.Collector;
        CorrelationKey = record.CorrelationKey;

        EvidenceExplorerMetadataItem[] metadata = (record.Metadata ?? new Dictionary<string, string>())
            .Select(pair => new EvidenceExplorerMetadataItem(
                pair.Key,
                EvidenceDisplayFormatter.FormatMetadataKey(pair.Key),
                pair.Value))
            .OrderBy(item => item.DisplayKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.RawKey, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Metadata = new ReadOnlyCollection<EvidenceExplorerMetadataItem>(metadata);

        _searchValues = new[]
            {
                RawSubject,
                Title,
                Observation,
                Category.ToString(),
                CategoryDisplay,
                Source.ToString(),
                SourceDisplay,
                CorrelationKey ?? string.Empty,
                Collector
            }
            .Concat(metadata.SelectMany(item => new[] { item.RawKey, item.DisplayKey, item.Value }))
            .ToArray();
    }

    public Guid Id { get; }

    public string SessionId { get; }

    public DateTime TimestampUtc { get; }

    public EvidenceCategory Category { get; }

    public EvidenceSource Source { get; }

    public string Title { get; }

    public string RawSubject { get; }

    public string Observation { get; }

    public double? Value { get; }

    public string? Unit { get; }

    public string? ValueDisplay { get; }

    public string CategoryDisplay { get; }

    public string SourceDisplay { get; }

    public EvidenceSeverity Severity { get; }

    public string SeverityDisplay { get; }

    public EvidenceConfidence Confidence { get; }

    public string ConfidenceDisplay { get; }

    public string TimestampDisplay { get; }

    public string Collector { get; }

    public string? CorrelationKey { get; }

    public IReadOnlyList<EvidenceExplorerMetadataItem> Metadata { get; }

    internal bool Matches(string query) =>
        _searchValues.Any(value => value.Contains(query, StringComparison.OrdinalIgnoreCase));
}
