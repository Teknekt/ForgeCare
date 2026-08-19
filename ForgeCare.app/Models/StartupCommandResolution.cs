namespace ForgeCare.App.Models;

public enum StartupCommandResolutionStatus
{
    DirectExecutable,
    DirectFile,
    LauncherMediated,
    ShortcutNotResolved,
    Empty,
    Malformed,
    Ambiguous,
    Unsupported
}

public sealed class StartupCommandResolution
{
    public required string OriginalCommand { get; init; }

    public required StartupCommandResolutionStatus Status { get; init; }

    public string? ResolvedPath { get; init; }

    public string? Arguments { get; init; }

    public string? LauncherName { get; init; }

    public bool EnvironmentExpansionApplied { get; init; }

    public required string Rationale { get; init; }

    public bool HasConfidentDirectPath =>
        (Status is StartupCommandResolutionStatus.DirectExecutable or
            StartupCommandResolutionStatus.DirectFile) &&
        !string.IsNullOrWhiteSpace(ResolvedPath);
}
