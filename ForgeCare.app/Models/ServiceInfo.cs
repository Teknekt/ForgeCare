namespace ForgeCare.App.Models;

public class ServiceInfo
{
    public string Name { get; set; } =
        string.Empty;

    public string DisplayName { get; set; } =
        string.Empty;

    public string Status { get; set; } =
        string.Empty;

    public string StartupType { get; set; } =
        string.Empty;

    public string Category { get; set; } =
        string.Empty;

    public string Recommendation { get; set; } =
        string.Empty;

    public string RiskLevel { get; set; } =
        string.Empty;

    public string Reason { get; set; } =
        string.Empty;

    public string ImagePath { get; set; } =
        string.Empty;

    public string Account { get; set; } =
        string.Empty;

    public bool IsRunning =>
        Status == "RUNNING";
}