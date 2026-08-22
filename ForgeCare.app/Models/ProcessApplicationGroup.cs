using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ForgeCare.App.Models;

public sealed class ProcessApplicationGroup
{
    public ProcessApplicationGroup(
        string transientGroupIdentity, string displayName, ProcessIdentityStrength identityStrength,
        string? canonicalExecutablePath, IEnumerable<ProcessIntelligenceEntry> members,
        double totalCpuPercent, double totalMemoryMb, double maximumInstanceCpuPercent,
        double maximumInstanceMemoryMb, double maximumPressureScore, string strongestPressureLevel,
        ProcessIdentityClassification classification, EvidenceConfidence confidence,
        ProcessExecutableInspection? executableInspection, string rationale)
    {
        TransientGroupIdentity = transientGroupIdentity;
        DisplayName = displayName;
        IdentityStrength = identityStrength;
        CanonicalExecutablePath = canonicalExecutablePath;
        Members = new ReadOnlyCollection<ProcessIntelligenceEntry>(new List<ProcessIntelligenceEntry>(members));
        TotalCpuPercent = totalCpuPercent;
        TotalMemoryMb = totalMemoryMb;
        MaximumInstanceCpuPercent = maximumInstanceCpuPercent;
        MaximumInstanceMemoryMb = maximumInstanceMemoryMb;
        MaximumPressureScore = maximumPressureScore;
        StrongestPressureLevel = strongestPressureLevel;
        Classification = classification;
        Confidence = confidence;
        ExecutableInspection = executableInspection;
        Rationale = rationale;
    }

    public string TransientGroupIdentity { get; }
    public string DisplayName { get; }
    public ProcessIdentityStrength IdentityStrength { get; }
    public string? CanonicalExecutablePath { get; }
    public IReadOnlyList<ProcessIntelligenceEntry> Members { get; }
    public int MemberCount => Members.Count;
    public double TotalCpuPercent { get; }
    public double TotalMemoryMb { get; }
    public double MaximumInstanceCpuPercent { get; }
    public double MaximumInstanceMemoryMb { get; }
    public double MaximumPressureScore { get; }
    public string StrongestPressureLevel { get; }
    public ProcessIdentityClassification Classification { get; }
    public EvidenceConfidence Confidence { get; }
    public ProcessExecutableInspection? ExecutableInspection { get; }
    public string Rationale { get; }
}
