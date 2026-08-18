using System.Collections.Generic;
using System.Linq;

namespace ForgeCare.App.Models;

public class ForgePlanResult
{
    public List<ForgePlanItem> Items { get; set; } =
        new();

    public int TotalCount =>
        Items.Count;

    public int LowRiskCount =>
        Items.Count(item =>
            item.Risk == "LOW");

    public int MediumRiskCount =>
        Items.Count(item =>
            item.Risk == "MEDIUM");

    public int HighRiskCount =>
        Items.Count(item =>
            item.Risk == "HIGH");

    public int ExecutableCount =>
        Items.Count(item =>
            item.CanExecute);

    public int SelectedCount =>
        Items.Count(item =>
            item.IsSelected);

    public int TotalPriority =>
        Items.Sum(item =>
            item.Priority);
}
