using System.Collections.Generic;

namespace ForgeCare.App.Models;

public sealed class EvidenceDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public string SessionId { get; set; } = string.Empty;

    public List<EvidenceRecord> Evidence { get; set; } = new();
}
