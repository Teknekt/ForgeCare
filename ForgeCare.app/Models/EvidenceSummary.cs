using System.Collections.Generic;

namespace ForgeCare.App.Models;

public sealed class EvidenceSummary
{
    public int TotalCount { get; set; }

    public int InformationalCount { get; set; }

    public int LowCount { get; set; }

    public int MediumCount { get; set; }

    public int HighCount { get; set; }

    public int CriticalCount { get; set; }

    public int UnknownCount { get; set; }

    public Dictionary<EvidenceCategory, int> Categories { get; set; } = new();
}
