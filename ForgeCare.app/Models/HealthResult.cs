namespace ForgeCare.App.Models;

public class HealthResult
{
    public int Score { get; set; }

    public string Rating { get; set; } = string.Empty;

    public string StorageStatus { get; set; } = string.Empty;

    public string MemoryStatus { get; set; } = string.Empty;

    public string StartupStatus { get; set; } = string.Empty;

    public double StorageFreePercent { get; set; }

    public double MemoryAvailablePercent { get; set; }

    public int StartupCount { get; set; }
}