namespace ForgeCare.App.Models;

public class CleanupExecutionLogEntry
{
    public string Path { get; set; } =
        string.Empty;

    public string Status { get; set; } =
        string.Empty;

    public string Reason { get; set; } =
        string.Empty;

    public long SizeBytes { get; set; }

    public string DisplaySize =>
        FormatBytes(SizeBytes);

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