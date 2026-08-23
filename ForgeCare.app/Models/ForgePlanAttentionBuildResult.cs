using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ForgeCare.App.Models;

public enum ForgePlanAttentionBuildState
{
    Ready,
    Empty,
    PartialSuccess,
    Failed
}

public sealed class ForgePlanAttentionBuildResult
{
    public ForgePlanAttentionBuildResult(
        ForgePlanAttentionBuildState state,
        IEnumerable<ForgePlanAttentionItem>? items = null,
        int correlationErrorCount = 0)
    {
        State = state;
        Items = new ReadOnlyCollection<ForgePlanAttentionItem>(
            (items ?? Array.Empty<ForgePlanAttentionItem>()).ToList());
        CorrelationErrorCount = Math.Max(0, correlationErrorCount);
    }

    public ForgePlanAttentionBuildState State { get; }
    public IReadOnlyList<ForgePlanAttentionItem> Items { get; }
    public int CorrelationErrorCount { get; }
}
