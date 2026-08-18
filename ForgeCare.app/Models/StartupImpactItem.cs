namespace ForgeCare.App.Models;

public class StartupImpactItem
{
    public string Name { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Command { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string ImpactLevel { get; set; } = string.Empty;

    public int ImpactScore { get; set; }

    public string Recommendation { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string Confidence { get; set; } = string.Empty;
}