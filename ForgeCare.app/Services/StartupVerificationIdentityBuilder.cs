using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed partial class StartupVerificationIdentityBuilder
{
    private const int IdentifierHashLength = 20;
    private const int MaximumNameLength = 80;

    public StartupVerificationTargetIdentity BuildHkcuRun(
        string registryValueName,
        string? configuredValue)
    {
        string displayName = NormalizeDisplayName(registryValueName);
        string normalizedName = NormalizeIdentityName(registryValueName);
        string? fingerprint = OptionalConfigurationFingerprint(configuredValue);
        return Build(
            StartupVerificationTargetKind.HkcuRun,
            displayName,
            "hkcu-run",
            normalizedName,
            fingerprint);
    }

    public StartupVerificationTargetIdentity BuildUserStartupFolder(
        string fileName,
        string? configuredValue)
    {
        string boundedFileName = FileNameOnly(fileName);
        string displayName = NormalizeDisplayName(boundedFileName);
        string normalizedName = NormalizeIdentityName(boundedFileName);
        string? fingerprint = OptionalConfigurationFingerprint(configuredValue);
        return Build(
            StartupVerificationTargetKind.UserStartupFolder,
            displayName,
            "user-startup",
            normalizedName,
            fingerprint);
    }

    public StartupVerificationTargetIdentity BuildUnsupported(string displayName)
    {
        string safeName = NormalizeDisplayName(displayName);
        string normalizedName = NormalizeIdentityName(displayName);
        return Build(
            StartupVerificationTargetKind.Unsupported,
            safeName,
            "unsupported",
            normalizedName,
            null);
    }

    public string BuildConfigurationFingerprint(string transientConfiguredValue)
    {
        if (string.IsNullOrWhiteSpace(transientConfiguredValue))
            throw new ArgumentException("Configured value must not be empty.", nameof(transientConfiguredValue));

        string normalized = transientConfiguredValue
            .Trim()
            .Replace('/', '\\')
            .ToLowerInvariant();
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private string? OptionalConfigurationFingerprint(string? transientConfiguredValue) =>
        string.IsNullOrWhiteSpace(transientConfiguredValue)
            ? null
            : BuildConfigurationFingerprint(transientConfiguredValue);

    private static StartupVerificationTargetIdentity Build(
        StartupVerificationTargetKind kind,
        string displayName,
        string sourceLocator,
        string normalizedName,
        string? fingerprint)
    {
        string locatorMaterial = $"{sourceLocator}|{normalizedName}";
        string locatorId = "startup-locator:" + ShortHash(locatorMaterial);
        string targetMaterial = $"{locatorMaterial}|{fingerprint ?? "none"}";
        string targetId = "startup-target:" + ShortHash(targetMaterial);
        return new StartupVerificationTargetIdentity(
            kind,
            displayName,
            sourceLocator,
            locatorId,
            targetId,
            fingerprint);
    }

    private static string NormalizeDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Target name must not be empty.", nameof(value));

        string normalized = value.Trim().Replace('\\', ' ').Replace('/', ' ')
            .Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        normalized = RepeatedWhitespaceRegex().Replace(normalized, " ");
        return normalized.Length <= MaximumNameLength ? normalized : normalized[..MaximumNameLength];
    }

    private static string NormalizeIdentityName(string value)
    {
        string normalized = NormalizeDisplayName(value).ToLowerInvariant();
        normalized = UnsafeTokenCharactersRegex().Replace(normalized, "-").Trim('-');
        return string.IsNullOrEmpty(normalized) ? "target" : normalized;
    }

    private static string FileNameOnly(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Startup filename must not be empty.", nameof(value));

        string normalized = value.Trim().Trim('"').Replace('/', '\\');
        int separator = normalized.LastIndexOf('\\');
        string fileName = separator >= 0 ? normalized[(separator + 1)..] : normalized;
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Startup filename could not be resolved.", nameof(value));
        return fileName;
    }

    private static string ShortHash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..IdentifierHashLength];

    [GeneratedRegex(@"\s+")]
    private static partial Regex RepeatedWhitespaceRegex();

    [GeneratedRegex("[^a-z0-9._-]+")]
    private static partial Regex UnsafeTokenCharactersRegex();
}
