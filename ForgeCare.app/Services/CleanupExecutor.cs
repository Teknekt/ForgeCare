using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public class CleanupExecutor
{
    private readonly CleanupSafetyService _safetyService;

    public CleanupExecutor()
    {
        _safetyService =
            new CleanupSafetyService();
    }

    // ============================================================
    // DRY RUN
    // ============================================================

    public Task<CleanupExecutionResult> SimulateAsync(
        IEnumerable<CleanupItem> selectedItems)
    {
        var items =
            selectedItems
                .Where(item => item.IsSelected)
                .ToList();

        return Task.Run(
            () => Process(
                items,
                execute: false));
    }

    // ============================================================
    // LIVE EXECUTION
    // ============================================================

    public Task<CleanupExecutionResult> ExecuteAsync(
        IEnumerable<CleanupItem> selectedItems)
    {
        var items =
            selectedItems
                .Where(item => item.IsSelected)
                .ToList();

        return Task.Run(
            () => Process(
                items,
                execute: true));
    }

    // ============================================================
    // PROCESS
    // ============================================================

    private CleanupExecutionResult Process(
        IReadOnlyCollection<CleanupItem> selectedItems,
        bool execute)
    {
        var result =
            new CleanupExecutionResult
            {
                IsDryRun = !execute
            };

        foreach (var item in selectedItems)
        {
            // Recycle Bin intentionally stays disabled.
            if (item.Name.Equals(
                    "Recycle Bin",
                    StringComparison.OrdinalIgnoreCase))
            {
                AddBlockedItem(
                    result,
                    item,
                    "Recycle Bin cleanup is not enabled.");

                continue;
            }

            // Validate the source root.
            if (!_safetyService.IsPathAllowed(
                    item.Path,
                    out string reason))
            {
                AddBlockedItem(
                    result,
                    item,
                    reason);

                continue;
            }

            ProcessDirectory(
                item.Path,
                result,
                execute);
        }

        return result;
    }

    // ============================================================
    // DIRECTORY PROCESSING
    // ============================================================

    private void ProcessDirectory(
        string path,
        CleanupExecutionResult result,
        bool execute)
    {
        if (!_safetyService.IsPathAllowed(
                path,
                out string rootReason))
        {
            result.ErrorCount++;

            result.LogEntries.Add(
                new CleanupExecutionLogEntry
                {
                    Path = path,
                    Status = "BLOCKED",
                    Reason = rootReason
                });

            return;
        }

        ProcessDirectoryRecursive(
            path,
            result,
            execute);
    }

    private void ProcessDirectoryRecursive(
        string path,
        CleanupExecutionResult result,
        bool execute)
    {
        // Every directory is revalidated.
        if (!_safetyService.IsPathAllowed(
                path,
                out string directoryReason))
        {
            result.LogEntries.Add(
                new CleanupExecutionLogEntry
                {
                    Path = path,
                    Status = "BLOCKED",
                    Reason = directoryReason
                });

            return;
        }

        List<string> files;

        try
        {
            files =
                Directory
                    .EnumerateFiles(path)
                    .ToList();
        }
        catch (Exception ex)
        {
            result.ErrorCount++;

            result.LogEntries.Add(
                new CleanupExecutionLogEntry
                {
                    Path = path,
                    Status = "ERROR",
                    Reason = ex.GetType().Name
                });

            return;
        }

        foreach (var file in files)
        {
            ProcessFile(
                file,
                result,
                execute);
        }

        List<string> directories;

        try
        {
            directories =
                Directory
                    .EnumerateDirectories(path)
                    .ToList();
        }
        catch (Exception ex)
        {
            result.ErrorCount++;

            result.LogEntries.Add(
                new CleanupExecutionLogEntry
                {
                    Path = path,
                    Status = "ERROR",
                    Reason = ex.GetType().Name
                });

            return;
        }

        foreach (var directory in directories)
        {
            ProcessDirectoryRecursive(
                directory,
                result,
                execute);
        }

        // IMPORTANT:
        // Sprint 4C.2 does NOT delete directories.
    }

    // ============================================================
    // FILE PROCESSING
    // ============================================================

    private void ProcessFile(
        string path,
        CleanupExecutionResult result,
        bool execute)
    {
        long size = 0;

        // Revalidate every single file.
        if (!_safetyService.IsPathAllowed(
                path,
                out string safetyReason))
        {
            result.SafetyBlockedFiles++;

            result.LogEntries.Add(
                new CleanupExecutionLogEntry
                {
                    Path = path,
                    Status = "BLOCKED",
                    Reason = safetyReason
                });

            return;
        }

        try
        {
            var info =
                new FileInfo(path);

            if (!info.Exists)
            {
                AddSkipped(
                    result,
                    path,
                    0,
                    "File no longer exists.");

                return;
            }

            size =
                info.Length;

            // ----------------------------------------------------
            // LOCK CHECK
            // ----------------------------------------------------

            using (var stream =
                   new FileStream(
                       path,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.None))
            {
                // Successfully opened.
            }

            // ----------------------------------------------------
            // DRY RUN
            // ----------------------------------------------------

            if (!execute)
            {
                result.CleanableBytes +=
                    size;

                result.CleanableFiles++;

                result.LogEntries.Add(
                    new CleanupExecutionLogEntry
                    {
                        Path = path,
                        Status = "CLEANABLE",
                        Reason =
                            "Passed safety and lock checks.",
                        SizeBytes = size
                    });

                return;
            }

            // ----------------------------------------------------
            // LIVE EXECUTION
            // ----------------------------------------------------

            // Final safety validation immediately before delete.
            if (!_safetyService.IsPathAllowed(
                    path,
                    out string finalReason))
            {
                result.SafetyBlockedFiles++;

                result.LogEntries.Add(
                    new CleanupExecutionLogEntry
                    {
                        Path = path,
                        Status = "BLOCKED",
                        Reason =
                            $"Final safety check failed: {finalReason}",
                        SizeBytes = size
                    });

                return;
            }

            File.Delete(path);

            // Verify deletion actually happened.
            if (File.Exists(path))
            {
                AddSkipped(
                    result,
                    path,
                    size,
                    "Delete operation returned but file still exists.");

                return;
            }

            result.ReclaimedBytes +=
                size;

            result.DeletedFiles++;

            result.LogEntries.Add(
                new CleanupExecutionLogEntry
                {
                    Path = path,
                    Status = "DELETED",
                    Reason =
                        "Deleted successfully.",
                    SizeBytes = size
                });
        }
        catch (IOException)
        {
            AddSkipped(
                result,
                path,
                size,
                "File is in use or locked.");
        }
        catch (UnauthorizedAccessException)
        {
            AddSkipped(
                result,
                path,
                size,
                "Access denied.");
        }
        catch (Exception ex)
        {
            result.ErrorCount++;

            AddSkipped(
                result,
                path,
                size,
                ex.GetType().Name);
        }
    }

    // ============================================================
    // RESULT HELPERS
    // ============================================================

    private static void AddBlockedItem(
        CleanupExecutionResult result,
        CleanupItem item,
        string reason)
    {
        result.SafetyBlockedFiles +=
            item.FileCount;

        result.SkippedFiles +=
            item.FileCount;

        result.SkippedBytes +=
            item.SizeBytes;

        result.LogEntries.Add(
            new CleanupExecutionLogEntry
            {
                Path = item.Path,
                Status = "BLOCKED",
                Reason = reason,
                SizeBytes = item.SizeBytes
            });
    }

    private static void AddSkipped(
        CleanupExecutionResult result,
        string path,
        long size,
        string reason)
    {
        result.SkippedBytes +=
            size;

        result.SkippedFiles++;

        result.LogEntries.Add(
            new CleanupExecutionLogEntry
            {
                Path = path,
                Status = "SKIPPED",
                Reason = reason,
                SizeBytes = size
            });
    }
}