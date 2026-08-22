using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ForgeCare.App.Models;

public sealed class StartupIntelligenceResult
{
    public StartupIntelligenceResult(
        IEnumerable<StartupIntelligenceEntry> entries,
        IEnumerable<string>? warnings = null,
        IEnumerable<string>? errors = null)
    {
        Entries = new ReadOnlyCollection<StartupIntelligenceEntry>(new List<StartupIntelligenceEntry>(entries));
        Warnings = new ReadOnlyCollection<string>(new List<string>(warnings ?? []));
        Errors = new ReadOnlyCollection<string>(new List<string>(errors ?? []));
    }

    public IReadOnlyList<StartupIntelligenceEntry> Entries { get; }

    public IReadOnlyList<string> Warnings { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool Success => Errors.Count == 0;

    public bool PartialSuccess => Entries.Count > 0 &&
                                  (Warnings.Count > 0 || Errors.Count > 0);
}
