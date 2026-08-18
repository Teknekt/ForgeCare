namespace ForgeCare.App.Models;

public class ForgeReportMetadata
{
    public string JobId { get; set; } =
        string.Empty;

    public string CustomerName { get; set; } =
        string.Empty;

    public string DeviceLabel { get; set; } =
        string.Empty;

    public string TechnicianName { get; set; } =
        string.Empty;

    public string CompanyName { get; set; } =
        "Mindforge Studio";

    public string ServiceSummary { get; set; } =
        string.Empty;

    public string TechnicianNotes { get; set; } =
        string.Empty;
}
