namespace ForgeCare.App.Models;

public enum StartupFileInspectionStatus
{
    Available,
    Missing,
    AccessDenied,
    Unsupported,
    InspectionFailure,
    NotChecked
}

public sealed class StartupFileInspection
{
    public required StartupFileInspectionStatus Status { get; init; }

    public string? NormalizedPath { get; init; }

    public bool? Exists { get; init; }

    public string? FileName { get; init; }

    public string? Extension { get; init; }

    public string? FileDescription { get; init; }

    public string? ProductName { get; init; }

    public string? CompanyName { get; init; }

    public string? FileVersion { get; init; }

    public string? ProductVersion { get; init; }

    public string? OriginalFilename { get; init; }

    public string? Warning { get; init; }

    public static StartupFileInspection NotChecked(string? warning = null) =>
        new()
        {
            Status = StartupFileInspectionStatus.NotChecked,
            Exists = null,
            Warning = warning
        };
}
