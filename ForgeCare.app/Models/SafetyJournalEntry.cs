using System;

namespace ForgeCare.App.Models;

public class SafetyJournalEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Recovery { get; set; } = "NONE";
    public string Detail { get; set; } = string.Empty;
    public bool IsReversible { get; set; }

    public string DisplayTime => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    public string RecoveryLabel => IsReversible ? Recovery : "NOT REVERSIBLE";
}
