using System.Collections.Generic;
using System.Linq;

namespace ForgeCare.App.Models;

public class OptimizationResult
{
    public List<OptimizationRecommendation> Recommendations { get; set; } =
        new();

    public int RecommendationCount =>
        Recommendations.Count;

    public int TotalEstimatedImpact =>
        Recommendations.Sum(x => x.EstimatedImpact);

    public string ImpactRating
    {
        get
        {
            return TotalEstimatedImpact switch
            {
                >= 70 => "HIGH",
                >= 35 => "MODERATE",
                > 0 => "LOW",
                _ => "NONE"
            };
        }
    }
}