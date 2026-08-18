using System;

namespace ForgeCare.App.Models;

public sealed class ReleaseIdentity
{
    public string Version { get; set; } = "unknown";
    public string Channel { get; set; } = "beta";
    public string InstallMode { get; set; } = "PORTABLE";
    public string ExecutablePath { get; set; } = string.Empty;
    public string InstallDirectory { get; set; } = string.Empty;
    public string DataDirectory { get; set; } = string.Empty;
    public string UpdatePolicy { get; set; } = "MANUAL BETA";
    public string ReleaseFingerprint { get; set; } = "UNAVAILABLE";
    public bool IsInstalled { get; set; }
    public bool IsPerUserInstall { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.Now;
}
