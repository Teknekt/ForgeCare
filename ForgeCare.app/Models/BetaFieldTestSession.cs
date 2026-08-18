using System;
using System.Collections.Generic;

namespace ForgeCare.App.Models;

public sealed class BetaFieldTestSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public string BuildVersion { get; set; } = string.Empty;
    public string ComputerName { get; set; } = Environment.MachineName;
    public string WindowsDescription { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public string TesterName { get; set; } = string.Empty;
    public string OverallStatus { get; set; } = "IN PROGRESS";
    public string Notes { get; set; } = string.Empty;
    public List<BetaFieldTestStep> Steps { get; set; } = new();
    public string DisplayStarted => StartedAt.ToString("yyyy-MM-dd HH:mm");
    public string DisplayCompleted => CompletedAt?.ToString("yyyy-MM-dd HH:mm") ?? "—";
}
