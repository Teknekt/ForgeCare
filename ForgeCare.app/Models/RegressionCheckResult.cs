namespace ForgeCare.App.Models;

public sealed class RegressionCheckResult
{
    public string Area { get; set; } = string.Empty;
    public string Check { get; set; } = string.Empty;
    public string Status { get; set; } = "PASS";
    public string Detail { get; set; } = string.Empty;
}
