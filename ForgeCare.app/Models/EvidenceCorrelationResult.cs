using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ForgeCare.App.Models;

public sealed class EvidenceCorrelationResult
{
    public EvidenceCorrelationResult(
        IEnumerable<TechnicianAttentionItem>? items = null,
        IEnumerable<string>? warnings = null,
        IEnumerable<string>? errors = null)
    {
        Items = Copy(items ?? Array.Empty<TechnicianAttentionItem>());
        Warnings = CopyMessages(warnings);
        Errors = CopyMessages(errors);
    }

    public IReadOnlyList<TechnicianAttentionItem> Items { get; }
    public IReadOnlyList<string> Warnings { get; }
    public IReadOnlyList<string> Errors { get; }
    public bool Success => Errors.Count == 0;
    public bool PartialSuccess => Errors.Count > 0 && Items.Count > 0;

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToList());

    private static IReadOnlyList<string> CopyMessages(IEnumerable<string>? messages) =>
        new ReadOnlyCollection<string>(
            (messages ?? Array.Empty<string>())
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Select(message => message.Trim())
                .Select(message => message.Length <= 240 ? message : message[..240])
                .Take(32)
                .ToList());
}
