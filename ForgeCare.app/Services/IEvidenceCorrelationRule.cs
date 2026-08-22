using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public interface IEvidenceCorrelationRule
{
    string RuleId { get; }

    IReadOnlyList<TechnicianAttentionItem> Evaluate(
        EvidenceCorrelationContext context);
}

public sealed class EvidenceCorrelationContext
{
    internal EvidenceCorrelationContext(
        string sessionId,
        IReadOnlyList<EvidenceCorrelationRecord> records)
    {
        SessionId = sessionId;
        Records = new ReadOnlyCollection<EvidenceCorrelationRecord>(
            new List<EvidenceCorrelationRecord>(records));
    }

    public string SessionId { get; }
    public IReadOnlyList<EvidenceCorrelationRecord> Records { get; }
}

public sealed class EvidenceCorrelationRecord
{
    internal EvidenceCorrelationRecord(EvidenceRecord source)
    {
        Id = source.Id;
        SessionId = source.SessionId;
        TimestampUtc = source.TimestampUtc;
        Category = source.Category;
        Source = source.Source;
        Subject = source.Subject;
        Observation = source.Observation;
        Value = source.Value;
        Unit = source.Unit;
        Severity = source.Severity;
        Confidence = source.Confidence;
        Collector = source.Collector;
        CorrelationKey = source.CorrelationKey;
        Metadata = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(
                source.Metadata,
                StringComparer.OrdinalIgnoreCase));
    }

    public Guid Id { get; }
    public string SessionId { get; }
    public DateTime TimestampUtc { get; }
    public EvidenceCategory Category { get; }
    public EvidenceSource Source { get; }
    public string Subject { get; }
    public string Observation { get; }
    public double? Value { get; }
    public string? Unit { get; }
    public EvidenceSeverity Severity { get; }
    public EvidenceConfidence Confidence { get; }
    public string Collector { get; }
    public IReadOnlyDictionary<string, string> Metadata { get; }
    public string? CorrelationKey { get; }
}
