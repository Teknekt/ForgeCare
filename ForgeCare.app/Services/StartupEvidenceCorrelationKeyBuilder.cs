using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed partial class StartupEvidenceCorrelationKeyBuilder
{
    private const int MaximumNameTokenLength = 32;
    private const int HashLength = 16;

    public string Build(
        StartupIntelligenceEntry entry,
        string? normalizedExecutablePath)
    {
        ArgumentNullException.ThrowIfNull(entry);

        string source = SourceToken(entry.SourceKind);
        string name = NormalizeToken(entry.Name, "entry", MaximumNameTokenLength);
        string identity = string.IsNullOrWhiteSpace(normalizedExecutablePath)
            ? $"{source}|{name}|{entry.CommandResolution.Status}"
            : $"{source}|{name}|{normalizedExecutablePath.ToLowerInvariant()}";

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        string hash = Convert.ToHexStringLower(digest)[..HashLength];
        return $"startup:{source}:{name}:{hash}";
    }

    internal static string NormalizeSubjectName(string? value) =>
        NormalizeToken(value, "entry", MaximumNameTokenLength);

    private static string SourceToken(StartupSourceKind source) =>
        source switch
        {
            StartupSourceKind.CurrentUserRegistry => "hkcu-run",
            StartupSourceKind.LocalMachineRegistry => "hklm-run",
            StartupSourceKind.UserStartupFolder => "user-startup",
            StartupSourceKind.CommonStartupFolder => "common-startup",
            _ => "unknown"
        };

    private static string NormalizeToken(
        string? value,
        string fallback,
        int maximumLength)
    {
        string token = UnsafeCharactersRegex()
            .Replace((value ?? string.Empty).Trim().ToLowerInvariant(), "-")
            .Trim('-');
        token = RepeatedHyphensRegex().Replace(token, "-");
        if (string.IsNullOrEmpty(token))
            token = fallback;
        return token.Length <= maximumLength ? token : token[..maximumLength].TrimEnd('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex UnsafeCharactersRegex();

    [GeneratedRegex("-+")]
    private static partial Regex RepeatedHyphensRegex();
}
