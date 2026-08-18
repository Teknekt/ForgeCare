using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class UpdateDiscoveryService
{
    private const string ForgeCareAppId =
        "{0F34D1F2-0B94-4F4F-A63D-F0A15E7D11C7}";

    public UpdateCheckResult CheckLocalManifest(
        string manifestPath)
    {
        string current =
            GetCurrentVersion();

        if (string.IsNullOrWhiteSpace(manifestPath) ||
            !File.Exists(manifestPath))
        {
            return new UpdateCheckResult
            {
                CurrentVersion = current,
                State = "NO MANIFEST",
                Detail = "Choose a ForgeCare release-manifest.json file."
            };
        }

        try
        {
            var manifest =
                JsonSerializer.Deserialize<UpdateManifest>(
                    File.ReadAllText(manifestPath),
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (manifest == null)
                throw new InvalidDataException("The update manifest is empty.");

            if (!string.Equals(
                    manifest.Product,
                    "ForgeCare",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Invalid(
                    current,
                    manifest,
                    "Manifest product is not ForgeCare.");
            }

            if (!string.Equals(
                    NormalizeAppId(manifest.AppId),
                    NormalizeAppId(ForgeCareAppId),
                    StringComparison.OrdinalIgnoreCase))
            {
                return Invalid(
                    current,
                    manifest,
                    "Manifest AppId does not match this ForgeCare installation.");
            }

            if (!TryVersion(manifest.Version, out Version? available) ||
                !TryVersion(current, out Version? installed))
            {
                return Invalid(
                    current,
                    manifest,
                    "Manifest version could not be compared safely.");
            }

            int compare =
                available.CompareTo(installed);

            return new UpdateCheckResult
            {
                CurrentVersion = current,
                AvailableVersion = manifest.Version,
                Manifest = manifest,
                UpdateAvailable = compare > 0,
                State =
                    compare > 0
                        ? "UPDATE AVAILABLE"
                        : compare == 0
                            ? "CURRENT"
                            : "OLDER BUILD",

                Detail =
                    compare > 0
                        ? $"ForgeCare {manifest.Version} is newer than {current}. Sprint 14A performs discovery only; installation remains manual."
                        : compare == 0
                            ? "The selected manifest matches the running ForgeCare version."
                            : "The selected manifest describes an older ForgeCare build."
            };
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult
            {
                CurrentVersion = current,
                State = "INVALID MANIFEST",
                Detail = ex.Message
            };
        }
    }

    private static UpdateCheckResult Invalid(
        string current,
        UpdateManifest manifest,
        string detail)
    {
        return new UpdateCheckResult
        {
            CurrentVersion = current,
            AvailableVersion = manifest.Version,
            Manifest = manifest,
            State = "INVALID MANIFEST",
            Detail = detail
        };
    }

    private static string GetCurrentVersion()
    {
        string raw =
            Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
            ?? "0.0.0";

        return raw.Split('+')[0].Trim();
    }

    private static bool TryVersion(
        string raw,
        out Version? version)
    {
        string value =
            (raw ?? string.Empty)
                .Trim()
                .TrimStart('v', 'V');

        int dash =
            value.IndexOf('-');

        if (dash >= 0)
            value = value[..dash];

        return Version.TryParse(
            value,
            out version);
    }

    private static string NormalizeAppId(
        string value)
    {
        return (value ?? string.Empty)
            .Trim()
            .Trim('{', '}');
    }
}
