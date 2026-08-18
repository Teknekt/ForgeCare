namespace ForgeCare.App.Models;

public sealed class SecureUpdateDownloadResult
{
    public bool Success { get; set; }
    public string State { get; set; } = "NOT STARTED";
    public string Detail { get; set; } = string.Empty;
    public string DownloadPath { get; set; } = string.Empty;
    public string ExpectedSha256 { get; set; } = string.Empty;
    public string ActualSha256 { get; set; } = string.Empty;
}
