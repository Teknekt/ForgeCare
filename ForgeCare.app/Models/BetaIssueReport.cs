using System;

namespace ForgeCare.App.Models;

public sealed class BetaIssueReport
{
    public string IssueId { get; set; } = $"FCI-{DateTime.Now:yyyyMMdd-HHmmss}";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string BuildVersion { get; set; } = string.Empty;
    public string ComputerName { get; set; } = Environment.MachineName;
    public string Area { get; set; } = "General";
    public string Severity { get; set; } = "Medium";
    public string Description { get; set; } = string.Empty;
    public string ReproductionSteps { get; set; } = string.Empty;
    public string ExpectedResult { get; set; } = string.Empty;
    public string ActualResult { get; set; } = string.Empty;
}
