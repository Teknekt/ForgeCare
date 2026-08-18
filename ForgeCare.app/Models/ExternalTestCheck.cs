namespace ForgeCare.App.Models;

public sealed class ExternalTestCheck
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "INFO";
    public string Detail { get; set; } = string.Empty;

    public bool IsPass =>
        Status == "PASS";
}
