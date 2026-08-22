using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ForgeCare.App.Services;

public sealed partial class ProcessEvidencePathProjector
{
    private const int HashLength = 12;
    private const int MaximumOutputLength = 260;
    private readonly IReadOnlyList<PathRoot> _roots;

    public ProcessEvidencePathProjector(
        string? userProfile = null,
        string? localApplicationData = null,
        string? applicationData = null,
        string? programFiles = null,
        string? programFilesX86 = null,
        string? programData = null,
        string? windows = null)
    {
        userProfile ??= Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        localApplicationData ??= Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        applicationData ??= Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        programFiles ??= Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        programFilesX86 ??= Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        programData ??= Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        windows ??= Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        _roots = new[]
            {
                new PathRoot("%LOCALAPPDATA%", localApplicationData, true),
                new PathRoot("%APPDATA%", applicationData, true),
                new PathRoot("%USERPROFILE%", userProfile, false),
                new PathRoot("%PROGRAMFILES%", programFiles, true),
                new PathRoot("%PROGRAMFILES(X86)%", programFilesX86, true),
                new PathRoot("%PROGRAMDATA%", programData, true),
                new PathRoot("%WINDIR%", windows, true)
            }
            .Where(root => !string.IsNullOrWhiteSpace(root.Path))
            .Select(root => root with { Path = NormalizeSeparators(root.Path!) })
            .OrderByDescending(root => root.Path!.Length)
            .ToArray();
    }

    public string? Project(string? canonicalPath)
    {
        if (string.IsNullOrWhiteSpace(canonicalPath)) return null;
        string candidate = NormalizeSeparators(canonicalPath.Trim().Trim('"'));
        if (candidate.IndexOf('\0') >= 0) return RedactedFileName(candidate, "<redacted>");

        foreach (PathRoot root in _roots)
        {
            if (!IsWithin(candidate, root.Path!)) continue;
            string suffix = candidate[root.Path!.Length..].TrimStart('\\');
            if (!root.PreserveSuffix)
                return RedactedFileName(candidate, root.Token);
            return Bound(string.IsNullOrEmpty(suffix) ? root.Token : $"{root.Token}\\{suffix}");
        }

        if (UserProfilePathRegex().IsMatch(candidate))
            return RedactedFileName(candidate, "<redacted>");

        if (!LooksLikeAbsoluteWindowsPath(candidate))
            return RedactedFileName(candidate, "<redacted>");

        string hash = ShortHash(candidate.ToLowerInvariant());
        return Bound($"<custom-root>\\{hash}\\{SafeFileName(candidate)}");
    }

    private static bool IsWithin(string path, string root)
    {
        string normalizedRoot = root.TrimEnd('\\');
        return path.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(normalizedRoot + "\\", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSeparators(string value) => value.Replace('/', '\\').TrimEnd('\\');
    private static bool LooksLikeAbsoluteWindowsPath(string value) =>
        value.Length >= 3 && char.IsAsciiLetter(value[0]) && value[1] == ':' && value[2] == '\\';

    private static string RedactedFileName(string value, string prefix) => Bound($"{prefix}\\{SafeFileName(value)}");
    private static string SafeFileName(string value)
    {
        try
        {
            string fileName = Path.GetFileName(value.Replace('\\', Path.DirectorySeparatorChar));
            return string.IsNullOrWhiteSpace(fileName) ? "<unknown-file>" : Bound(fileName, 120);
        }
        catch
        {
            return "<unknown-file>";
        }
    }

    private static string ShortHash(string value)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(digest)[..HashLength];
    }

    private static string Bound(string value, int maximum = MaximumOutputLength) =>
        value.Length <= maximum ? value : value[..maximum];

    [GeneratedRegex(@"^[A-Za-z]:\\Users\\[^\\]+(?:\\|$)", RegexOptions.IgnoreCase)]
    private static partial Regex UserProfilePathRegex();

    private sealed record PathRoot(string Token, string? Path, bool PreserveSuffix);
}
