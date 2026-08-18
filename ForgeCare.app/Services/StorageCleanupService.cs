using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ForgeCare.App.Models;
using Microsoft.VisualBasic.FileIO;

namespace ForgeCare.App.Services;

public class StorageCleanupService
{
    private readonly StorageCleanupSafetyService _safetyService;

    public StorageCleanupService()
    {
        _safetyService =
            new StorageCleanupSafetyService();
    }

    public List<StorageCleanupCandidate> BuildCandidates(
        IEnumerable<StorageFileFinding> findings)
    {
        return findings
            .Select(CreateCandidate)
            .OrderBy(candidate =>
                CleanupClassRank(candidate.CleanupClass))
            .ThenByDescending(candidate =>
                candidate.SizeBytes)
            .ToList();
    }

    public Task<StorageCleanupResult> SimulateAsync(
        IEnumerable<StorageCleanupCandidate> candidates)
    {
        var selected =
            candidates
                .Where(candidate =>
                    candidate.IsSelected &&
                    candidate.CanSelect)
                .ToList();

        return Task.Run(
            () => Process(
                selected,
                execute: false));
    }

    public Task<StorageCleanupResult> MoveToRecycleBinAsync(
        IEnumerable<StorageCleanupCandidate> candidates)
    {
        var selected =
            candidates
                .Where(candidate =>
                    candidate.IsSelected &&
                    candidate.CanSelect)
                .ToList();

        return Task.Run(
            () => Process(
                selected,
                execute: true));
    }

    private StorageCleanupResult Process(
        IReadOnlyCollection<StorageCleanupCandidate> selected,
        bool execute)
    {
        var result =
            new StorageCleanupResult
            {
                IsDryRun = !execute,
                RequestedFiles = selected.Count
            };

        foreach (var candidate in selected)
        {
            ValidateAndProcess(
                candidate,
                result,
                execute);
        }

        return result;
    }

    private void ValidateAndProcess(
        StorageCleanupCandidate candidate,
        StorageCleanupResult result,
        bool execute)
    {
        try
        {
            if (!_safetyService.IsFileAllowed(
                    candidate.FullPath,
                    out string reason))
            {
                candidate.Status = "BLOCKED";
                candidate.StatusReason = reason;

                result.BlockedFiles++;
                result.Items.Add(candidate);
                return;
            }

            var info =
                new FileInfo(
                    candidate.FullPath);

            // The file must still match the item the user reviewed.
            // If it changed after the Storage scan, force a rescan
            // rather than acting on stale metadata.
            if (info.Length !=
                candidate.SizeBytes)
            {
                candidate.Status = "BLOCKED";
                candidate.StatusReason =
                    "File size changed since Storage scan. Run the scan again.";

                result.BlockedFiles++;
                result.Items.Add(candidate);
                return;
            }

            using (var stream =
                   new FileStream(
                       info.FullName,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.None))
            {
                // Lock/readability check only.
            }

            if (!execute)
            {
                candidate.Status = "VALIDATED";
                candidate.StatusReason =
                    "Passed path, reparse-point, metadata and lock checks. No changes made.";

                result.ValidatedFiles++;
                result.ValidatedBytes +=
                    info.Length;

                result.Items.Add(candidate);
                return;
            }

            // Revalidate immediately before the destructive action.
            if (!_safetyService.IsFileAllowed(
                    info.FullName,
                    out string finalReason))
            {
                candidate.Status = "BLOCKED";
                candidate.StatusReason =
                    $"Final safety check failed: {finalReason}";

                result.BlockedFiles++;
                result.Items.Add(candidate);
                return;
            }

            // Deliberately no File.Delete fallback.
            // If Windows cannot recycle the file, ForgeCare stops
            // and reports the error instead of permanently deleting it.
            FileSystem.DeleteFile(
                info.FullName,
                UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin,
                UICancelOption.ThrowException);

            if (File.Exists(
                    info.FullName))
            {
                candidate.Status = "ERROR";
                candidate.StatusReason =
                    "Recycle operation returned but the file still exists.";

                result.ErrorCount++;
                result.Items.Add(candidate);
                return;
            }

            candidate.Status = "RECYCLED";
            candidate.StatusReason =
                "Moved to the Windows Recycle Bin.";

            result.RecycledFiles++;
            result.RecycledBytes +=
                candidate.SizeBytes;

            result.Items.Add(candidate);
        }
        catch (IOException)
        {
            candidate.Status = "SKIPPED";
            candidate.StatusReason =
                "File is in use, locked or changed during validation.";

            result.SkippedFiles++;
            result.Items.Add(candidate);
        }
        catch (UnauthorizedAccessException)
        {
            candidate.Status = "BLOCKED";
            candidate.StatusReason =
                "Access denied. ForgeCare did not elevate permissions.";

            result.BlockedFiles++;
            result.Items.Add(candidate);
        }
        catch (OperationCanceledException)
        {
            candidate.Status = "SKIPPED";
            candidate.StatusReason =
                "Recycle operation was cancelled.";

            result.SkippedFiles++;
            result.Items.Add(candidate);
        }
        catch (Exception ex)
        {
            candidate.Status = "ERROR";
            candidate.StatusReason =
                ex.Message;

            result.ErrorCount++;
            result.Items.Add(candidate);
        }
    }

    private static StorageCleanupCandidate CreateCandidate(
        StorageFileFinding finding)
    {
        string cleanupClass =
            Classify(
                finding);

        string recommendation =
            cleanupClass switch
            {
                "SAFE CLEANUP" =>
                    "Good cleanup candidate, but still requires explicit selection.",

                "REVIEW FIRST" =>
                    "Review that you no longer need this file before recycling it.",

                _ =>
                    "Personal/user data: manual review required before any action."
            };

        return new StorageCleanupCandidate
        {
            Name =
                finding.Name,

            FullPath =
                finding.FullPath,

            Location =
                finding.Location,

            Category =
                finding.Category,

            CleanupClass =
                cleanupClass,

            Recommendation =
                recommendation,

            SizeBytes =
                finding.SizeBytes,

            LastWriteTime =
                finding.LastWriteTime,

            CanSelect =
                true,

            IsSelected =
                false,

            Status =
                "READY",

            StatusReason =
                "Not selected. No changes have been made."
        };
    }

    private static string Classify(
        StorageFileFinding finding)
    {
        if (finding.Location.Equals(
                "User Temp",
                StringComparison.OrdinalIgnoreCase) ||
            finding.Location.Equals(
                "Local App Cache",
                StringComparison.OrdinalIgnoreCase))
        {
            return "SAFE CLEANUP";
        }

        if (finding.Location.Equals(
                "Downloads",
                StringComparison.OrdinalIgnoreCase) &&
            finding.Category is
                "INSTALLER" or
                "ARCHIVE" or
                "DISK IMAGE")
        {
            return "REVIEW FIRST";
        }

        if (finding.Category ==
            "DISK IMAGE")
        {
            return "REVIEW FIRST";
        }

        return "MANUAL REVIEW";
    }

    private static int CleanupClassRank(
        string cleanupClass)
    {
        return cleanupClass switch
        {
            "SAFE CLEANUP" => 0,
            "REVIEW FIRST" => 1,
            "MANUAL REVIEW" => 2,
            _ => 3
        };
    }
}
