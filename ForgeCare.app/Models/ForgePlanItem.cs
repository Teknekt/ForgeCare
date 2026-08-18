using System;

namespace ForgeCare.App.Models;

public class ForgePlanItem
{
    public string Id { get; set; } =
        Guid.NewGuid().ToString("N");

    public string Category { get; set; } =
        string.Empty;

    public string Title { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public string Risk { get; set; } =
        "LOW";

    public string Value { get; set; } =
        string.Empty;

    public string Route { get; set; } =
        string.Empty;

    public int Priority { get; set; }

    public bool IsSelected { get; set; }

    public bool CanExecute { get; set; }

    public string ActionLabel { get; set; } =
        "REVIEW";

    public string RiskLabel =>
        $"{Risk} RISK";
}
