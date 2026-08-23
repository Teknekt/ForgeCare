namespace ForgeCare.App.Models;

public enum StartupVerificationTargetKind
{
    HkcuRun,
    UserStartupFolder,
    Unsupported
}

public sealed class StartupVerificationTargetIdentity
{
    public StartupVerificationTargetIdentity(
        StartupVerificationTargetKind kind,
        string displayName,
        string sourceLocator,
        string locatorId,
        string targetId,
        string? configurationFingerprint)
    {
        Kind = kind;
        DisplayName = Required(displayName, nameof(displayName), 120);
        SourceLocator = Required(sourceLocator, nameof(sourceLocator), 48);
        LocatorId = Required(locatorId, nameof(locatorId), 64);
        TargetId = Required(targetId, nameof(targetId), 64);
        ConfigurationFingerprint = Fingerprint(configurationFingerprint);
    }

    public StartupVerificationTargetKind Kind { get; }
    public string DisplayName { get; }
    public string SourceLocator { get; }
    public string LocatorId { get; }
    public string TargetId { get; }
    public string? ConfigurationFingerprint { get; }
    public bool IsSupported => Kind is StartupVerificationTargetKind.HkcuRun or
        StartupVerificationTargetKind.UserStartupFolder;

    private static string Required(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value must not be empty.", parameterName);

        string normalized = value.Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }

    private static string? Fingerprint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string normalized = value.Trim();
        if (normalized.Length != 64 ||
            normalized.Any(character => !char.IsAsciiHexDigit(character) || char.IsUpper(character)))
        {
            throw new ArgumentException("Configuration fingerprint must be a lowercase SHA-256 value.", nameof(value));
        }

        return normalized;
    }
}
