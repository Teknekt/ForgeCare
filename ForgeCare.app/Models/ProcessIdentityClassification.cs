namespace ForgeCare.App.Models;

public enum ProcessIdentityClassification
{
    Verified,
    Known,
    Unverified,
    Unknown
}

public enum ProcessIdentityStrength
{
    Strong,
    Provisional
}

public sealed record ProcessClassificationDecision(
    ProcessIdentityClassification Classification,
    EvidenceConfidence Confidence,
    string Rationale);
