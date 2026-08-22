using System.Collections.Generic;

namespace ForgeCare.App.Models;

public sealed class EvidenceCollectionResult
{
    public List<EvidenceRecord> Evidence { get; set; } = new();

    public List<string> Warnings { get; set; } = new();

    public List<string> Errors { get; set; } = new();

    public bool Success => Errors.Count == 0;

    public bool PartialSuccess => Evidence.Count > 0 &&
                                  (Warnings.Count > 0 || Errors.Count > 0);
}
