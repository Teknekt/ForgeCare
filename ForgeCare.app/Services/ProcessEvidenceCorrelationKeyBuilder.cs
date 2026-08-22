using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed partial class ProcessEvidenceCorrelationKeyBuilder
{
    private const int HashLength = 16;
    private const int MaximumNameLength = 32;

    public string Build(ProcessApplicationGroup group, string? persistedExecutableIdentity)
    {
        ArgumentNullException.ThrowIfNull(group);
        if (group.IdentityStrength == ProcessIdentityStrength.Strong &&
            !string.IsNullOrWhiteSpace(persistedExecutableIdentity))
        {
            return $"process-app:{Hash(persistedExecutableIdentity.Trim().ToLowerInvariant())}";
        }

        string name = NormalizeName(group.DisplayName);
        string structuralIdentity = $"{name}|{group.TransientGroupIdentity}|{group.MemberCount}";
        return $"process-instance:{name}:{Hash(structuralIdentity)}";
    }

    internal static string NormalizeName(string? value)
    {
        string token = UnsafeCharactersRegex()
            .Replace((value ?? string.Empty).Trim().ToLowerInvariant(), "-")
            .Trim('-');
        token = RepeatedHyphensRegex().Replace(token, "-");
        if (string.IsNullOrEmpty(token)) token = "application";
        return token.Length <= MaximumNameLength ? token : token[..MaximumNameLength].TrimEnd('-');
    }

    private static string Hash(string value)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(digest)[..HashLength];
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex UnsafeCharactersRegex();
    [GeneratedRegex("-+")]
    private static partial Regex RepeatedHyphensRegex();
}
