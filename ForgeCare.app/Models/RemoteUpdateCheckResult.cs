using System;

namespace ForgeCare.App.Models;

public sealed class RemoteUpdateCheckResult
{
    public string State { get; set; } = "NOT CHECKED";
    public string CurrentVersion { get; set; } = string.Empty;
    public string AvailableVersion { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string ManifestUrl { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; } = DateTime.Now;
    public bool UpdateAvailable { get; set; }
    public bool UsedCachedResult { get; set; }

    // Sprint 14C: verified-download metadata exposed only after
    // a valid remote ForgeCare manifest has been accepted.
    public string InstallerFile { get; set; } = string.Empty;
    public string InstallerSha256 { get; set; } = string.Empty;
}
