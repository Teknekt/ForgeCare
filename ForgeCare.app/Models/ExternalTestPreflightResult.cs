using System.Collections.Generic;
using System.Linq;

namespace ForgeCare.App.Models;

public sealed class ExternalTestPreflightResult
{
    public List<ExternalTestCheck> Checks { get; set; } = new();

    public int Passed =>
        Checks.Count(x => x.Status == "PASS");

    public int Warnings =>
        Checks.Count(x => x.Status == "WARN");

    public int Failed =>
        Checks.Count(x => x.Status == "FAIL");

    public bool IsReady =>
        Failed == 0;

    public string State =>
        Failed > 0
            ? "BLOCKED"
            : Warnings > 0
                ? "READY WITH WARNINGS"
                : "READY";
}
