namespace ForgeCare.App.Models;

public enum RecommendationSeverity
{
    Info,
    Healthy,
    Attention,
    Critical
}

public class Recommendation
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public RecommendationSeverity Severity { get; set; }
}