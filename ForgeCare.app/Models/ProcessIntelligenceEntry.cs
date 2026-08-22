namespace ForgeCare.App.Models;

public sealed record ProcessIntelligenceEntry(
    ProcessInstanceObservation Observation,
    ProcessIdentityStrength IdentityStrength,
    string TransientIdentity,
    string? CanonicalExecutablePath,
    ProcessExecutableInspection? ExecutableInspection,
    ProcessIdentityClassification Classification,
    EvidenceConfidence Confidence,
    string Rationale);
