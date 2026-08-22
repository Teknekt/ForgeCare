using System;
using System.Collections.Generic;
using System.Linq;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class EvidenceCorrelationEngine
{
    private const int MaximumMessages = 32;
    private readonly IReadOnlyList<IEvidenceCorrelationRule> _rules;

    public EvidenceCorrelationEngine()
        : this(new IEvidenceCorrelationRule[] { new StartupProcessAttentionRule() })
    {
    }

    public EvidenceCorrelationEngine(IEnumerable<IEvidenceCorrelationRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules
            .Where(rule => rule != null)
            .OrderBy(rule => rule.RuleId, StringComparer.Ordinal)
            .ThenBy(rule => rule.GetType().FullName, StringComparer.Ordinal)
            .ToArray();
    }

    public EvidenceCorrelationResult Correlate(
        IReadOnlyCollection<EvidenceRecord> evidence,
        string sessionId)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        var warnings = new List<string>();
        var errors = new List<string>();
        var records = new List<EvidenceCorrelationRecord>();

        if (!Guid.TryParseExact(sessionId, "N", out _))
        {
            AddBounded(errors, "Evidence correlation requires a report session GUID in N format.");
            return new EvidenceCorrelationResult(errors: errors);
        }

        foreach (EvidenceRecord? record in evidence)
        {
            if (record == null)
            {
                AddBounded(errors, "A null Evidence record was excluded from correlation.");
                continue;
            }

            IReadOnlyList<string> validation;
            try
            {
                validation = record.Validate();
            }
            catch (Exception ex)
            {
                AddBounded(errors,
                    $"Evidence {record.Id} was excluded because validation failed ({ex.GetType().Name}).");
                continue;
            }

            if (validation.Count > 0)
            {
                AddBounded(errors, $"Evidence {record.Id} was excluded because validation failed.");
                continue;
            }

            if (!string.Equals(record.SessionId, sessionId, StringComparison.Ordinal))
            {
                AddBounded(errors, $"Evidence {record.Id} was excluded because its session did not match.");
                continue;
            }

            records.Add(new EvidenceCorrelationRecord(record));
        }

        EvidenceCorrelationRecord[] orderedRecords = records
            .OrderByDescending(record => record.TimestampUtc)
            .ThenBy(record => record.Id)
            .ToArray();
        var context = new EvidenceCorrelationContext(sessionId, orderedRecords);
        var items = new List<TechnicianAttentionItem>();

        foreach (IEvidenceCorrelationRule rule in _rules)
        {
            string ruleId = BoundRuleId(rule.RuleId);
            try
            {
                IReadOnlyList<TechnicianAttentionItem> produced =
                    rule.Evaluate(context) ?? Array.Empty<TechnicianAttentionItem>();
                items.AddRange(produced.Where(item => item != null));
            }
            catch (Exception ex)
            {
                AddBounded(errors,
                    $"Correlation rule '{ruleId}' failed ({ex.GetType().Name}).");
            }
        }

        TechnicianAttentionItem[] finalItems = items
            .GroupBy(DeduplicationKey, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderByDescending(item => item.Priority)
            .ThenByDescending(item => item.LatestEvidenceTimestampUtc)
            .ThenBy(item => item.RuleId, StringComparer.Ordinal)
            .ThenBy(item => item.EntityKey, StringComparer.Ordinal)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();

        return new EvidenceCorrelationResult(finalItems, warnings, errors);
    }

    private static string DeduplicationKey(TechnicianAttentionItem item) =>
        $"{item.RuleId}|{item.EntityKey}|{string.Join(',', item.EvidenceIds.OrderBy(id => id))}";

    private static string BoundRuleId(string? ruleId)
    {
        string value = string.IsNullOrWhiteSpace(ruleId) ? "unknown-rule" : ruleId.Trim();
        return value.Length <= 96 ? value : value[..96];
    }

    private static void AddBounded(ICollection<string> messages, string message)
    {
        if (messages.Count >= MaximumMessages)
            return;
        messages.Add(message.Length <= 240 ? message : message[..240]);
    }
}
