using System.Collections.Generic;

namespace ForgeCare.App.Models;

public class StartupChangeResult
{
    public bool IsDryRun { get; set; }
    public int RequestedCount { get; set; }
    public int ValidatedCount { get; set; }
    public int DisabledCount { get; set; }
    public int RestoredCount { get; set; }
    public int BlockedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }

    public List<StartupChangeItem> Items { get; } = new();
}