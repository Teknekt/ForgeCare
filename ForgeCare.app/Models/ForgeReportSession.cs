using System;
using System.Collections.Generic;
using System.Linq;

namespace ForgeCare.App.Models;

public class ForgeReportSession
{
    public string SessionId { get; set; } =
        Guid.NewGuid().ToString("N");

    public DateTime StartedAt { get; set; } =
        DateTime.Now;

    public DateTime UpdatedAt { get; set; } =
        DateTime.Now;

    public string ComputerName { get; set; } =
        string.Empty;

    public string OperatingSystem { get; set; } =
        string.Empty;

    public string ProcessorName { get; set; } =
        string.Empty;

    public ForgeReportMetadata Metadata { get; set; } =
        new();

    public List<ForgeReportCheckpoint> Checkpoints { get; set; } =
        new();

    public List<ForgeReportAction> Actions { get; set; } =
        new();

    public ForgeReportCheckpoint? Before =>
        Checkpoints.FirstOrDefault();

    public ForgeReportCheckpoint? Current =>
        Checkpoints.LastOrDefault();

    public int ActionCount =>
        Actions.Count;

    public int SuccessfulActionCount =>
        Actions.Count(action =>
            action.IsSuccess);

    public long TotalRecoveredBytes { get; set; }

    public int StartupEntriesDisabled { get; set; }

    public int StartupEntriesRestored { get; set; }

    public int DeepAnalysisRuns { get; set; }

    public int ServiceAnalysisRuns { get; set; }

    public int StorageAnalysisRuns { get; set; }

    public int DuplicateScanRuns { get; set; }

    public int LastServiceReviewCount { get; set; }

    public int LastLargeFileCount { get; set; }

    public int LastDuplicateGroupCount { get; set; }

    public long LastDuplicateReclaimableBytes { get; set; }

    public string DisplayRecovered =>
        FormatBytes(TotalRecoveredBytes);

    public string DisplayDuplicateOpportunity =>
        FormatBytes(LastDuplicateReclaimableBytes);

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
            return $"{bytes / 1024d / 1024d / 1024d:0.00} GB";

        if (bytes >= 1024L * 1024)
            return $"{bytes / 1024d / 1024d:0.0} MB";

        if (bytes >= 1024)
            return $"{bytes / 1024d:0.0} KB";

        return $"{bytes} B";
    }
}
