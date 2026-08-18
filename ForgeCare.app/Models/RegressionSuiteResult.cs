using System;
using System.Collections.Generic;
using System.Linq;

namespace ForgeCare.App.Models;

public sealed class RegressionSuiteResult
{
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime CompletedAt { get; set; } = DateTime.Now;
    public List<RegressionCheckResult> Checks { get; set; } = new();

    public int Passed => Checks.Count(x => x.Status == "PASS");
    public int Warnings => Checks.Count(x => x.Status == "WARN");
    public int Failed => Checks.Count(x => x.Status == "FAIL");

    public string Overall =>
        Failed > 0
            ? "FAIL"
            : Warnings > 0
                ? "PASS WITH WARNINGS"
                : "PASS";
}
