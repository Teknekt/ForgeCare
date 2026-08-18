using System;

namespace ForgeCare.App.Models;

public sealed class RemoteUpdateSettings
{
    public string ManifestUrl { get; set; } = string.Empty;
    public string Channel { get; set; } = "stable";
    public DateTime? LastSuccessfulCheck { get; set; }
    public string LastKnownAvailableVersion { get; set; } = string.Empty;
    public string LastKnownState { get; set; } = "NEVER CHECKED";
}
