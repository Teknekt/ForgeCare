using System;

namespace ForgeCare.App.Models;

public class ForgeReportAction
{
    public DateTime Timestamp { get; set; }

    public string Category { get; set; } =
        string.Empty;

    public string Title { get; set; } =
        string.Empty;

    public string Result { get; set; } =
        string.Empty;

    public string Detail { get; set; } =
        string.Empty;

    public string Metric { get; set; } =
        string.Empty;

    public bool IsSuccess { get; set; }

    public string DisplayTime =>
        Timestamp.ToString("HH:mm:ss");
}
