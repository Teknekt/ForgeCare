using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ForgeCare.App.Models;

public sealed class ProcessIntelligenceResult
{
    public ProcessIntelligenceResult(
        IEnumerable<ProcessIntelligenceEntry> entries,
        IEnumerable<ProcessApplicationGroup> groups,
        IEnumerable<string>? warnings = null,
        IEnumerable<string>? errors = null)
    {
        Entries = new ReadOnlyCollection<ProcessIntelligenceEntry>(new List<ProcessIntelligenceEntry>(entries));
        Groups = new ReadOnlyCollection<ProcessApplicationGroup>(new List<ProcessApplicationGroup>(groups));
        Warnings = new ReadOnlyCollection<string>(new List<string>(warnings ?? []));
        Errors = new ReadOnlyCollection<string>(new List<string>(errors ?? []));
    }

    public IReadOnlyList<ProcessIntelligenceEntry> Entries { get; }
    public IReadOnlyList<ProcessApplicationGroup> Groups { get; }
    public IReadOnlyList<string> Warnings { get; }
    public IReadOnlyList<string> Errors { get; }
    public bool Success => Errors.Count == 0;
    public bool PartialSuccess => Entries.Count > 0 && (Warnings.Count > 0 || Errors.Count > 0);
}
