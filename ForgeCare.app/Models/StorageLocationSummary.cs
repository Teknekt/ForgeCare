namespace ForgeCare.App.Models;

public class StorageLocationSummary
{
    public string Name { get; set; } =
        string.Empty;

    public string Path { get; set; } =
        string.Empty;

    public long SizeBytes { get; set; }

    public int FileCount { get; set; }

    public string Status { get; set; } =
        string.Empty;

    public string DisplaySize =>
        FormatBytes(SizeBytes);

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