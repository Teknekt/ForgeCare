using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ForgeCare.App.Services;

public class StorageCleanupSafetyService
{
    private readonly List<string> _allowedRoots;

    public StorageCleanupSafetyService()
    {
        _allowedRoots =
            BuildAllowedRoots()
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(NormalizePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    public bool IsFileAllowed(
        string path,
        out string reason)
    {
        reason = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            reason = "Empty file path.";
            return false;
        }

        string fullPath;

        try
        {
            fullPath = NormalizePath(path);
        }
        catch
        {
            reason = "Path could not be normalized.";
            return false;
        }

        if (!File.Exists(fullPath))
        {
            reason = "File no longer exists.";
            return false;
        }

        if (!_allowedRoots.Any(root =>
                IsInsideRoot(fullPath, root)))
        {
            reason =
                "File is outside ForgeCare's approved user-storage roots.";
            return false;
        }

        if (ContainsReparsePoint(fullPath))
        {
            reason =
                "Reparse point / junction detected in file path.";
            return false;
        }

        string windows =
            NormalizePath(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.Windows));

        string programFiles =
            NormalizePath(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFiles));

        string programFilesX86 =
            NormalizePath(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFilesX86));

        if (IsInsideRoot(fullPath, windows) ||
            IsInsideRoot(fullPath, programFiles) ||
            IsInsideRoot(fullPath, programFilesX86))
        {
            reason = "Protected system/program location.";
            return false;
        }

        reason = "Approved user-storage file.";
        return true;
    }

    private static IEnumerable<string> BuildAllowedRoots()
    {
        string userProfile =
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);

        string localAppData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        yield return Path.Combine(userProfile, "Downloads");

        yield return
            Environment.GetFolderPath(
                Environment.SpecialFolder.DesktopDirectory);

        yield return
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments);

        yield return
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyPictures);

        yield return
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyVideos);

        yield return Path.GetTempPath();

        if (!string.IsNullOrWhiteSpace(localAppData))
            yield return Path.Combine(localAppData, "Temp");
    }

    private static bool IsInsideRoot(
        string path,
        string root)
    {
        if (string.IsNullOrWhiteSpace(root))
            return false;

        string normalizedPath =
            NormalizePath(path);

        string normalizedRoot =
            NormalizePath(root);

        if (normalizedPath.Equals(
                normalizedRoot,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return normalizedPath.StartsWith(
            normalizedRoot +
            Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsReparsePoint(
        string path)
    {
        try
        {
            var file =
                new FileInfo(path);

            if (file.Exists &&
                file.Attributes.HasFlag(
                    FileAttributes.ReparsePoint))
            {
                return true;
            }

            string? current =
                file.DirectoryName;

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
            // Fail closed.
            return true;
        }
    }

    private static string NormalizePath(
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        string fullPath =
            Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(path));

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
