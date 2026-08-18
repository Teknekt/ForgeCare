using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ForgeCare.App.Services;

public class CleanupSafetyService
{
    private readonly List<string> _allowedRoots;

    public CleanupSafetyService()
    {
        _allowedRoots =
            BuildAllowedRoots()
                .Select(NormalizePath)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    public bool IsPathAllowed(
        string path,
        out string reason)
    {
        reason = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            reason = "Empty path.";
            return false;
        }

        string fullPath;

        try
        {
            fullPath =
                NormalizePath(path);
        }
        catch
        {
            reason =
                "Path could not be normalized.";

            return false;
        }

        if (IsDangerousRoot(fullPath))
        {
            reason =
                "Protected system location.";

            return false;
        }

        bool insideAllowedRoot =
            _allowedRoots.Any(
                root =>
                    IsInsideRoot(
                        fullPath,
                        root));

        if (!insideAllowedRoot)
        {
            reason =
                "Path is outside ForgeCare cleanup allowlist.";

            return false;
        }

        if (ContainsReparsePoint(fullPath))
        {
            reason =
                "Reparse point / junction detected.";

            return false;
        }

        reason =
            "Allowed cleanup path.";

        return true;
    }

    private static IEnumerable<string>
        BuildAllowedRoots()
    {
        string userTemp =
            Path.GetTempPath();

        if (!string.IsNullOrWhiteSpace(userTemp))
        {
            yield return userTemp;
        }

        string windowsPath =
            Environment.GetFolderPath(
                Environment.SpecialFolder.Windows);

        if (!string.IsNullOrWhiteSpace(windowsPath))
        {
            yield return
                Path.Combine(
                    windowsPath,
                    "Temp");
        }
    }

    private static bool IsInsideRoot(
        string path,
        string root)
    {
        if (path.Equals(
                root,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string rootWithSeparator =
            root.EndsWith(
                Path.DirectorySeparatorChar)
                ? root
                : root +
                  Path.DirectorySeparatorChar;

        return path.StartsWith(
            rootWithSeparator,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDangerousRoot(
        string path)
    {
        string windows =
            NormalizePath(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.Windows));

        string system =
            NormalizePath(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.System));

        string programFiles =
            NormalizePath(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFiles));

        string programFilesX86 =
            NormalizePath(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFilesX86));

        string userProfile =
            NormalizePath(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile));

        string systemDrive =
            Path.GetPathRoot(windows) ?? string.Empty;

        var protectedPaths =
            new[]
            {
                windows,
                system,
                programFiles,
                programFilesX86,
                userProfile,
                NormalizePath(systemDrive)
            };

        return protectedPaths
            .Where(p =>
                !string.IsNullOrWhiteSpace(p))
            .Any(p =>
                path.Equals(
                    p,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsReparsePoint(
        string path)
    {
        try
        {
            var info =
                new FileInfo(path);

            if (info.Exists &&
                info.Attributes.HasFlag(
                    FileAttributes.ReparsePoint))
            {
                return true;
            }

            string? current =
                Directory.Exists(path)
                    ? path
                    : Path.GetDirectoryName(path);

            while (!string.IsNullOrWhiteSpace(current))
            {
                var directory =
                    new DirectoryInfo(current);

                if (directory.Exists &&
                    directory.Attributes.HasFlag(
                        FileAttributes.ReparsePoint))
                {
                    return true;
                }

                current =
                    directory.Parent?.FullName;
            }

            return false;
        }
        catch
        {
            // If ForgeCare cannot safely establish
            // what the path is, deny it.
            return true;
        }
    }

    private static string NormalizePath(
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string fullPath =
            Path.GetFullPath(path);

        string root =
            Path.GetPathRoot(fullPath) ??
            string.Empty;

        if (fullPath.Equals(
                root,
                StringComparison.OrdinalIgnoreCase))
        {
            return fullPath;
        }

        return fullPath.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
    }
}