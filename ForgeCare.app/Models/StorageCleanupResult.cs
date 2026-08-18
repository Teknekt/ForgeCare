using System.Collections.Generic;

namespace ForgeCare.App.Models;

public class StorageCleanupResult
{
    public bool IsDryRun { get; set; }

    public int RequestedFiles { get; set; }

    public int ValidatedFiles { get; set; }

    public long ValidatedBytes { get; set; }

    public int RecycledFiles { get; set; }

    public long RecycledBytes { get; set; }

    public int BlockedFiles { get; set; }

    public int SkippedFiles { get; set; }

    public int ErrorCount { get; set; }

    public List<StorageCleanupCandidate> Items { get; } =
        new();

    public string DisplayValidatedSize =>
        FormatBytes(ValidatedBytes);

    public string DisplayRecycledSize =>
        FormatBytes(RecycledBytes);

    private static string FormatBytes(long bytes)
    {
        double gb = bytes / 1024d / 1024d / 1024d;
        if (gb >= 1)
            return $"{gb:0.00} GB";

        double mb = bytes / 1024d / 1024d;
        if (mb >= 1)
            return $"{mb:0.0} MB";

        return $"{bytes / 1024d:0.0} KB";
    }
}
