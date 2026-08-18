namespace ForgeCare.App.Models;

public class ForgeWorkflowSummary
{
    public string Stage { get; set; } = "READY";
    public string CurrentTitle { get; set; } = "Start a Forge session";
    public string CurrentRoute { get; set; } = "WORKFLOW";
    public double ProgressPercent { get; set; }
    public bool CanContinue { get; set; }
    public bool IsComplete { get; set; }
    public string StageStrip { get; set; } =
        "○ SCAN   ○ ANALYZE   ○ PLAN   ○ FORGE   ○ VERIFY   ○ REPORT";
}
