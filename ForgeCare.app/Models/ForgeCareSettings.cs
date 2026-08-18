namespace ForgeCare.App.Models;

public sealed class ForgeCareSettings
{
    public string TechnicianName { get; set; } = "";
    public string CompanyName { get; set; } = "Mindforge Studio";
    public string DefaultCustomerName { get; set; } = "";
    public string DefaultDeviceLabel { get; set; } = "";
    public bool AutoFillReportDetails { get; set; } = true;
    public bool ConfirmBeforeRecoveryActions { get; set; } = true;
}
