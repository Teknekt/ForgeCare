using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ForgeCare.App.Services;

public sealed partial class StartupEvidencePathNormalizer
{
    private readonly IReadOnlyList<PathRoot> _roots;

    public StartupEvidencePathNormalizer(
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
                new PathRoot("%LOCALAPPDATA%", localApplicationData),
                new PathRoot("%APPDATA%", applicationData),
                new PathRoot("%USERPROFILE%", userProfile),
                new PathRoot("%PROGRAMFILES%", programFiles),
                new PathRoot("%PROGRAMFILES(X86)%", programFilesX86),
                new PathRoot("%PROGRAMDATA%", programData),
                new PathRoot("%WINDIR%", windows)
            }
            .Where(root => !string.IsNullOrWhiteSpace(root.Path))
            .Select(root => root with { Path = NormalizeSeparators(root.Path!) })
            .OrderByDescending(root => root.Path!.Length)
            .ToArray();
    }

    public string? Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        string candidate = NormalizeSeparators(path.Trim().Trim('"'));
        if (candidate.IndexOf('\0') >= 0)
            return RedactedFileName(candidate);

        foreach (PathRoot root in _roots)
        {
            if (!IsWithin(candidate, root.Path!))
                continue;

            string suffix = candidate[root.Path!.Length..].TrimStart('\\');
            return string.IsNullOrEmpty(suffix)
                ? root.Token
                : $"{root.Token}\\{suffix}";
        }

        if (UserProfilePathRegex().IsMatch(candidate))
            return RedactedFileName(candidate);

        return LooksLikeAbsoluteWindowsPath(candidate)
            ? candidate
            : RedactedFileName(candidate);
    }

    private static bool IsWithin(string path, string root)
    {
        string normalizedRoot = root.TrimEnd('\\');
        return path.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(normalizedRoot + "\\", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSeparators(string value) =>
        value.Replace('/', '\\').TrimEnd('\\');

    private static bool LooksLikeAbsoluteWindowsPath(string value) =>
        value.Length >= 3 &&
        char.IsAsciiLetter(value[0]) &&
        value[1] == ':' &&
        value[2] == '\\';

    private static string RedactedFileName(string value)
    {
        string sanitized = value.Replace("\0", string.Empty, StringComparison.Ordinal);
        string fileName;
        try
        {
            fileName = Path.GetFileName(sanitized.Replace('\\', Path.DirectorySeparatorChar));
        }
        catch
        {
            fileName = string.Empty;
        }

        return string.IsNullOrWhiteSpace(fileName)
            ? "<redacted>"
            : $"<redacted>\\{fileName}";
    }

    [GeneratedRegex(@"^[A-Za-z]:\\Users\\[^\\]+(?:\\|$)", RegexOptions.IgnoreCase)]
    private static partial Regex UserProfilePathRegex();

    private sealed record PathRoot(string Token, string? Path);
}
