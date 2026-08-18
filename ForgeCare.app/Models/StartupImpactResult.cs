using System.Collections.Generic;
using System.Linq;

namespace ForgeCare.App.Models;

public class StartupImpactResult
{
    public List<StartupImpactItem> Items { get; set; } =
        new();

    public int TotalItems =>
        Items.Count;

    public int HighImpactCount =>
        Items.Count(item =>
            item.ImpactLevel == "HIGH");

    public int ReviewCount =>
        Items.Count(item =>
            item.Recommendation == "REVIEW");

    public int DisableCandidateCount =>
        Items.Count(item =>
            item.Recommendation == "GOOD CANDIDATE");

    public int KeepCount =>
        Items.Count(item =>
            item.Recommendation == "KEEP");

    public int AverageImpactScore =>
        Items.Count == 0
            ? 0
            : (int)System.Math.Round(
                Items.Average(item => item.ImpactScore));
}