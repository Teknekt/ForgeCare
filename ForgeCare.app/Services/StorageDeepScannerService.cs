using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public class StorageDeepScannerService
{
    private const int MaxFilesToInspect =
        120_000;

    private const long LargeFileThreshold =
        500L * 1024L * 1024L;

    private const int MaxLargeFilesReturned =
        40;

    public Task<StorageAnalysisResult> AnalyzeAsync()
    {
        return Task.Run(
            Analyze);
    }

    private StorageAnalysisResult Analyze()
    {
        var result =
            new StorageAnalysisResult
            {
                AnalysisTime =
                    DateTime.Now
            };

        int inspectedFiles =
            0;

        foreach (var target in
                 BuildTargets())
        {
            if (inspectedFiles >=
                MaxFilesToInspect)
            {
                result.HitSafetyLimit =
                    true;

                break;
            }

            var summary =
                ScanTarget(
                    target.Name,
                    target.Path,
                    result,
                    ref inspectedFiles);

            result.Locations.Add(
                summary);
        }

        result.LargeFiles =
            result.LargeFiles
                .OrderByDescending(
                    file =>
                        file.SizeBytes)
                .Take(
                    MaxLargeFilesReturned)
                .ToList();

        return result;
    }

    private static List<(string Name, string Path)>
        BuildTargets()
    {
        string userProfile =
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);

        string localAppData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        string downloads =
            Path.Combine(
                userProfile,
                "Downloads");

        return new List<(string, string)>
        {
            (
                "Downloads",
                downloads
            ),
            (
                "Desktop",
                Environment.GetFolderPath(
                    Environment.SpecialFolder.DesktopDirectory)
            ),
            (
                "Documents",
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments)
            ),
            (
                "Pictures",
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyPictures)
            ),
            (
                "Videos",
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyVideos)
            ),
            (
                "User Temp",
                Path.GetTempPath()
            ),
            (
                "Local App Cache",
                Path.Combine(
                    localAppData,
                    "Temp")
            )
        }
        .Where(
            target =>
                !string.IsNullOrWhiteSpace(
                    target.Item2))
        .GroupBy(
            target =>
                target.Item2,
            StringComparer.OrdinalIgnoreCase)
        .Select(
            group =>
                group.First())
        .ToList();
    }

    private static StorageLocationSummary ScanTarget(
        string name,
        string path,
        StorageAnalysisResult result,
        ref int inspectedFiles)
    {
        var summary =
            new StorageLocationSummary
            {
                Name =
                    name,

                Path =
                    path
            };

        if (!Directory.Exists(
                path))
        {
            summary.Status =
                "NOT FOUND";

            return summary;
        }

        var pending =
            new Stack<string>();

        pending.Push(
            path);

        while (pending.Count > 0)
        {
            if (inspectedFiles >=
                MaxFilesToInspect)
            {
                result.HitSafetyLimit =
                    true;

                summary.Status =
                    "LIMIT REACHED";

                break;
            }

            string current =
                pending.Pop();

            try
            {
                var directoryInfo =
                    new DirectoryInfo(
                        current);

                if ((directoryInfo.Attributes &
                     FileAttributes.ReparsePoint) != 0)
                {
                    result.SkippedDirectories++;
                    continue;
                }

                foreach (var filePath in
                         Directory.EnumerateFiles(
                             current))
                {
                    if (inspectedFiles >=
                        MaxFilesToInspect)
                    {
                        result.HitSafetyLimit =
                            true;

                        break;
                    }

                    inspectedFiles++;

                    try
                    {
                        var file =
                            new FileInfo(
                                filePath);

                        long size =
                            file.Length;

                        summary.SizeBytes +=
                            size;

                        summary.FileCount++;

                        result.ScannedBytes +=
                            size;

                        result.ScannedFiles++;

                        if (size >=
                            LargeFileThreshold)
                        {
                            result.LargeFiles.Add(
                                CreateFinding(
                                    file,
                                    name));
                        }
                    }
                    catch
                    {
                        // File may disappear, deny access, or be in use.
                    }
                }

                foreach (var directory in
                         Directory.EnumerateDirectories(
                             current))
                {
                    try
                    {
                        var childInfo =
                            new DirectoryInfo(
                                directory);

                        if ((childInfo.Attributes &
                             FileAttributes.ReparsePoint) != 0)
                        {
                            result.SkippedDirectories++;
                            continue;
                        }

                        pending.Push(
                            directory);
                    }
                    catch
                    {
                        result.SkippedDirectories++;
                    }
                }
            }
            catch
            {
                result.SkippedDirectories++;
            }
        }

        if (string.IsNullOrWhiteSpace(
                summary.Status))
        {
            summary.Status =
                "SCANNED";
        }

        return summary;
    }

    private static StorageFileFinding CreateFinding(
        FileInfo file,
        string location)
    {
        string extension =
            file.Extension
                .ToLowerInvariant();

        string category =
            extension switch
            {
                ".iso" or
                ".img" or
                ".vhd" or
                ".vhdx" =>
                    "DISK IMAGE",

                ".zip" or
                ".7z" or
                ".rar" or
                ".tar" or
                ".gz" =>
                    "ARCHIVE",

                ".mp4" or
                ".mkv" or
                ".mov" or
                ".avi" or
                ".webm" =>
                    "VIDEO",

                ".exe" or
                ".msi" =>
                    "INSTALLER",

                _ =>
                    "LARGE FILE"
            };

        string recommendation =
            location.Equals(
                "Downloads",
                StringComparison.OrdinalIgnoreCase) &&
            category is "INSTALLER" or "ARCHIVE"
                ? "REVIEW OLD DOWNLOAD"
                : category == "DISK IMAGE"
                    ? "REVIEW IF UNUSED"
                    : "INSPECT MANUALLY";

        return new StorageFileFinding
        {
            Name =
                file.Name,

            FullPath =
                file.FullName,

            Location =
                location,

            Extension =
                extension,

            SizeBytes =
                file.Length,

            LastWriteTime =
                file.LastWriteTime,

            Category =
                category,

            Recommendation =
                recommendation
        };
    }
}
