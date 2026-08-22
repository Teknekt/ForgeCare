namespace ForgeCare.App.Models;

public sealed record EvidenceExplorerFacet
{
    public required string DisplayLabel { get; init; }

    public required int Count { get; init; }

    public EvidenceCategory? Category { get; init; }

    public EvidenceSource? Source { get; init; }

    public bool IsAll => Category == null && Source == null;
}
