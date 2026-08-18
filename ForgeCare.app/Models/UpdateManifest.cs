using System;

namespace ForgeCare.App.Models;

public sealed class UpdateManifest
{
    public int SchemaVersion { get; set; } = 1;
    public string Product { get; set; } = "ForgeCare";
    public string Edition { get; set; } = "Technician Edition";
    public string Publisher { get; set; } = "Mindforge Studio";
    public string Version { get; set; } = string.Empty;
    public string NumericVersion { get; set; } = string.Empty;
    public string Channel { get; set; } = "beta";
    public DateTime? PublishedAt { get; set; }
    public string UpdateMode { get; set; } = "manual-beta";
    public string AppId { get; set; } = string.Empty;
    public UpdateArtifact? Portable { get; set; }
    public UpdateArtifact? Installer { get; set; }
}

public sealed class UpdateArtifact
{
    public string File { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public bool UpgradeInPlace { get; set; }
}

public sealed class UpdateCheckResult
{
    public string CurrentVersion { get; set; } = string.Empty;
    public string AvailableVersion { get; set; } = string.Empty;
    public string State { get; set; } = "NO MANIFEST";
    public string Detail { get; set; } = string.Empty;
    public bool UpdateAvailable { get; set; }
    public UpdateManifest? Manifest { get; set; }
}
