using System.Collections.Generic;

namespace ForgeCare.App.Models;

public class CleanupExecutionResult
{
    // Simulation
    public long CleanableBytes { get; set; }
    public int CleanableFiles { get; set; }

    // Actual execution
    public long ReclaimedBytes { get; set; }
    public int DeletedFiles { get; set; }

    // Skipped / blocked
    public long SkippedBytes { get; set; }
    public int SkippedFiles { get; set; }

    public int SafetyBlockedFiles { get; set; }
    public int ErrorCount { get; set; }

    public bool IsDryRun { get; set; } = true;

    public List<CleanupExecutionLogEntry> LogEntries { get; } =
        new();

    public string CleanableSize =>
        FormatBytes(CleanableBytes);

    public string ReclaimedSize =>
        FormatBytes(ReclaimedBytes);

    public string SkippedSize =>
        FormatBytes(SkippedBytes);

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
        {
            return
                $"{bytes / 1024d / 1024d / 1024d:0.00} GB";
        }

        if (bytes >= 1024L * 1024)
        {
            return
                $"{bytes / 1024d / 1024d:0.0} MB";
        }

        if (bytes >= 1024)
        {
            return
                $"{bytes / 1024d:0.0} KB";
        }

        return $"{bytes} B";
    }
}