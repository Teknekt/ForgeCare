using System;

namespace ForgeCare.App.Models;

public class ForgeReportArchiveEntry
{
    public string ReportKind { get; set; } =
        "Legacy";

    public DateTime ExportedAt { get; set; }

    public string JobId { get; set; } =
        string.Empty;

    public string CustomerName { get; set; } =
        string.Empty;

    public string DeviceLabel { get; set; } =
        string.Empty;

    public string ComputerName { get; set; } =
        string.Empty;

    public string FilePath { get; set; } =
        string.Empty;

    public string RecoveredStorage { get; set; } =
        string.Empty;

    public int ActionCount { get; set; }

    public string DisplayTime =>
        ExportedAt.ToString("yyyy-MM-dd HH:mm");

    public string DisplayReportKind =>
        string.Equals(ReportKind, "Professional", StringComparison.OrdinalIgnoreCase)
            ? "PROFESSIONAL"
            : "LEGACY";
}
