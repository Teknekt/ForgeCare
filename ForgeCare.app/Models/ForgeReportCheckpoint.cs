using System;

namespace ForgeCare.App.Models;

public class ForgeReportCheckpoint
{
    public DateTime Timestamp { get; set; }

    public int HealthScore { get; set; }

    public string HealthRating { get; set; } =
        string.Empty;

    public double SystemDriveFreeGb { get; set; }

    public double StorageFreePercent { get; set; }

    public double AvailableMemoryGb { get; set; }

    public double MemoryAvailablePercent { get; set; }

    public int StartupCount { get; set; }
}
