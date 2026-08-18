namespace ForgeCare.App.Models;

public sealed class InstallerHandoffResult
{
    public bool Success { get; set; }
    public string State { get; set; } = "NOT READY";
    public string Detail { get; set; } = string.Empty;
    public string InstallerPath { get; set; } = string.Empty;
}
