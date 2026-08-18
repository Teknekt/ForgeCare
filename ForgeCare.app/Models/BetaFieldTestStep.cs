namespace ForgeCare.App.Models;

public sealed class BetaFieldTestStep
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public string Detail { get; set; } = string.Empty;
}
