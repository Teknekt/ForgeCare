namespace ForgeCare.App.Models;

public class ForgeWorkflowStep
{
    public int Number { get; set; }

    public string Title { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public string Status { get; set; } =
        "PENDING";

    public string Route { get; set; } =
        string.Empty;

    public bool IsRequired { get; set; } =
        true;

    public string RequirementLabel =>
        IsRequired
            ? "REQUIRED"
            : "OPTIONAL";
}
