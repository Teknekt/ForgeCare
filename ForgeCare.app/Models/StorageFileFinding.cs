using System;

namespace ForgeCare.App.Models;

public class StorageFileFinding
{
    public string Name { get; set; } =
        string.Empty;

    public string FullPath { get; set; } =
        string.Empty;

    public string Location { get; set; } =
        string.Empty;

    public string Extension { get; set; } =
        string.Empty;

    public long SizeBytes { get; set; }

    public DateTime LastWriteTime { get; set; }

    public string Category { get; set; } =
        string.Empty;

    public string Recommendation { get; set; } =
        string.Empty;

    public string DisplaySize =>
        FormatBytes(SizeBytes);

    public string DisplayLastWrite =>
        LastWriteTime == DateTime.MinValue
            ? "Unknown"
            : LastWriteTime.ToString("yyyy-MM-dd");

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