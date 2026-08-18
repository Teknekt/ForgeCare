namespace ForgeCare.App.Models;

public enum OptimizationSeverity
{
    Info,
    Recommended,
    Important
}

public class OptimizationRecommendation
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public OptimizationSeverity Severity { get; set; }

    public int EstimatedImpact { get; set; }
}