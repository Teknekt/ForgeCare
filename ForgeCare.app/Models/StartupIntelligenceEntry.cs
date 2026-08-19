using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ForgeCare.App.Models;

public sealed class StartupIntelligenceEntry
{
    public required string Name { get; init; }

    public required string OriginalSource { get; init; }

    public required StartupSourceKind SourceKind { get; init; }

    public required StartupCommandResolution CommandResolution { get; init; }

    public required StartupFileInspection FileInspection { get; init; }

    public required StartupSignatureInfo Signature { get; init; }

    public required StartupClassification Classification { get; init; }

    public required EvidenceConfidence Confidence { get; init; }

    public required string Rationale { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } =
        new ReadOnlyCollection<string>(new List<string>());
}
