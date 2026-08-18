using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class ReleaseIdentityService
{
    public ReleaseIdentity Inspect()
    {
        string executable =
            Environment.ProcessPath
            ?? string.Empty;

        string version =
            Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
            ?? Assembly.GetExecutingAssembly()
                .GetName()
                .Version
                ?.ToString()
            ?? "unknown";

        string localPrograms =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "ForgeCare");

        bool installed =
            !string.IsNullOrWhiteSpace(executable) &&
            executable.StartsWith(
                localPrograms,
                StringComparison.OrdinalIgnoreCase);

        return new ReleaseIdentity
        {
            Version = version.Split('+')[0],
            Channel =
                version.Contains(
                    "beta",
                    StringComparison.OrdinalIgnoreCase)
                    ? "BETA"
                    : version.Contains(
                        "alpha",
                        StringComparison.OrdinalIgnoreCase)
                        ? "ALPHA"
                        : "RELEASE",

            InstallMode =
                installed
                    ? "INSTALLED · PER USER"
                    : "PORTABLE / DEVELOPMENT",

            ExecutablePath =
                executable,

            InstallDirectory =
                string.IsNullOrWhiteSpace(executable)
                    ? AppContext.BaseDirectory
                    : Path.GetDirectoryName(executable)
                      ?? AppContext.BaseDirectory,

            DataDirectory =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "ForgeCare"),

            UpdatePolicy =
                "MANUAL BETA · SAME APP ID UPGRADES IN PLACE",

            ReleaseFingerprint =
                ComputeFingerprint(
                    executable),

            IsInstalled =
                installed,

            IsPerUserInstall =
                installed
        };
    }

    private static string ComputeFingerprint(
        string executable)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(executable) ||
                !File.Exists(executable))
            {
                return "UNAVAILABLE";
            }

            using var stream =
                File.OpenRead(
                    executable);

            byte[] hash =
                SHA256.HashData(
                    stream);

            string hex =
                Convert.ToHexString(
                    hash);

            return
                $"{hex[..12]}…{hex[^8..]}";
        }
        catch
        {
            return "UNAVAILABLE";
        }
    }
}
