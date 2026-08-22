using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

internal static class ProcessEvidenceTestFactory
{
    public static ProcessEvidencePathProjector PathProjector() => new(
        userProfile: @"C:\Users\Alice",
        localApplicationData: @"C:\Users\Alice\AppData\Local",
        applicationData: @"C:\Users\Alice\AppData\Roaming",
        programFiles: @"C:\Program Files",
        programFilesX86: @"C:\Program Files (x86)",
        programData: @"C:\ProgramData",
        windows: @"C:\Windows");

    public static ProcessApplicationGroup Group(
        string name = "Vendor App",
        string? path = @"C:\Users\Alice\AppData\Local\Vendor\App.exe",
        ProcessIdentityStrength strength = ProcessIdentityStrength.Strong,
        ProcessIdentityClassification classification = ProcessIdentityClassification.Verified,
        EvidenceConfidence confidence = EvidenceConfidence.High,
        string pressureLevel = "HIGH",
        double pressure = 70,
        double totalCpu = 25,
        double totalMemory = 600,
        int memberCount = 2,
        ProcessSignatureStatus signature = ProcessSignatureStatus.Valid,
        string? company = "Vendor Company",
        string? product = "Vendor Product",
        string transientIdentity = "transient-identity")
    {
        var inspection = strength == ProcessIdentityStrength.Strong
            ? new ProcessExecutableInspection(
                path!, ProcessFileInspectionStatus.Available, true, Path.GetFileName(path), ".exe",
                "Vendor Description", product, company, "1.2.3", "1.2", "App.exe",
                signature, "Certificate Signer")
            : null;
        ProcessIntelligenceEntry[] members = Enumerable.Range(1, memberCount)
            .Select(index =>
            {
                ProcessInstanceObservation observation = ProcessIntelligenceTestFactory.Observation(
                    index, name, path, totalCpu / memberCount, totalMemory / memberCount,
                    pressure, pressureLevel);
                return new ProcessIntelligenceEntry(
                    observation, strength, transientIdentity, path, inspection,
                    classification, confidence, "Transient rationale must not be copied.");
            })
            .ToArray();

        return new ProcessApplicationGroup(
            transientIdentity, name, strength, path, members, totalCpu, totalMemory,
            totalCpu / memberCount, totalMemory / memberCount, pressure, pressureLevel,
            classification, confidence, inspection, "Transient rationale must not be copied.");
    }

    public static ProcessIntelligenceResult Result(params ProcessApplicationGroup[] groups) =>
        new(groups.SelectMany(group => group.Members), groups);

    public static ProcessIntelligenceEvidenceAdapter Adapter() =>
        new(PathProjector(), new ProcessEvidenceCorrelationKeyBuilder());

    public static EvidenceRecord ExistingRecord(string sessionId, EvidenceSource source, DateTime timestamp) =>
        new()
        {
            SessionId = sessionId,
            TimestampUtc = timestamp,
            Category = EvidenceCategory.System,
            Source = source,
            Subject = "existing-record",
            Observation = "Existing Evidence remained present.",
            Severity = EvidenceSeverity.Informational,
            Confidence = EvidenceConfidence.High,
            Collector = nameof(ProcessEvidenceTestFactory)
        };
}
