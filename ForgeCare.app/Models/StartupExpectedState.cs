namespace ForgeCare.App.Models;

public enum StartupExpectedStateKind
{
    Absent,
    PresentWithMatchingConfiguration
}

public sealed class StartupExpectedState
{
    public StartupExpectedState(
        StartupExpectedStateKind kind,
        string? expectedConfigurationFingerprint = null)
    {
        if (kind == StartupExpectedStateKind.PresentWithMatchingConfiguration &&
            string.IsNullOrWhiteSpace(expectedConfigurationFingerprint))
        {
            throw new ArgumentException(
                "Restore verification requires an expected configuration fingerprint.",
                nameof(expectedConfigurationFingerprint));
        }

        Kind = kind;
        if (string.IsNullOrWhiteSpace(expectedConfigurationFingerprint))
        {
            ExpectedConfigurationFingerprint = null;
        }
        else
        {
            string fingerprint = expectedConfigurationFingerprint.Trim();
            if (fingerprint.Length != 64 ||
                fingerprint.Any(character => !char.IsAsciiHexDigit(character) || char.IsUpper(character)))
            {
                throw new ArgumentException(
                    "Expected configuration fingerprint must be a lowercase SHA-256 value.",
                    nameof(expectedConfigurationFingerprint));
            }

            ExpectedConfigurationFingerprint = fingerprint;
        }
    }

    public StartupExpectedStateKind Kind { get; }
    public string? ExpectedConfigurationFingerprint { get; }
    public string Summary => Kind == StartupExpectedStateKind.Absent
        ? "The reviewed startup target is absent from its configured source."
        : "The reviewed startup target is present with the expected configuration fingerprint.";
}
