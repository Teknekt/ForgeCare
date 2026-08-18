using System;
using System.Collections.Generic;
using System.Linq;

namespace ForgeCare.App.Models;

public class StorageAnalysisResult
{
    public DateTime AnalysisTime { get; set; }

    public long ScannedBytes { get; set; }

    public int ScannedFiles { get; set; }

    public int SkippedDirectories { get; set; }

    public bool HitSafetyLimit { get; set; }

    public List<StorageLocationSummary> Locations { get; set; } =
        new();

    public List<StorageFileFinding> LargeFiles { get; set; } =
        new();

    public int LargeFileCount =>
        LargeFiles.Count;

    public long LargeFileBytes =>
        LargeFiles.Sum(file => file.SizeBytes);

    public string DisplayScannedSize =>
        FormatBytes(ScannedBytes);

    public string DisplayLargeFileSize =>
        FormatBytes(LargeFileBytes);

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