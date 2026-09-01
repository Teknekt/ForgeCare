using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class RemoteUpdateDiscoveryService
{
    private const string ForgeCareAppId =
        "{0F34D1F2-0B94-4F4F-A63D-F0A15E7D11C7}";

    private readonly HttpClient _httpClient;

    public RemoteUpdateDiscoveryService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ??
            new HttpClient
            {
                Timeout =
                    TimeSpan.FromSeconds(8)
            };

        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "ForgeCare-Technician-Edition/UpdateDiscovery");
        }
    }

    public async Task<RemoteUpdateCheckResult> CheckAsync(
        string? manifestUrl,
        string expectedChannel,
        CancellationToken cancellationToken = default)
    {
        string current =
            GetCurrentVersion();

        if (!Uri.TryCreate(
                manifestUrl?.Trim(),
                UriKind.Absolute,
                out Uri? uri))
        {
            return Invalid(
                current,
                string.Empty,
                "INVALID URL",
                "Enter an absolute HTTPS URL to a ForgeCare release-manifest.json file.");
        }

        if (!string.Equals(
                uri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase))
        {
            return Invalid(
                current,
                uri.GetLeftPart(UriPartial.Path),
                "HTTPS REQUIRED",
                "Remote update discovery only accepts HTTPS manifest URLs.");
        }

        string safeManifestUrl = uri.GetLeftPart(UriPartial.Path);

        try
        {
            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    uri,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Invalid(
                    current,
                    safeManifestUrl,
                    "CHECK FAILED",
                    $"Manifest request returned HTTP {(int)response.StatusCode}.");
            }

            string json =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            UpdateManifest? manifest =
                JsonSerializer.Deserialize<UpdateManifest>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (manifest == null)
            {
                return Invalid(
                    current,
                    safeManifestUrl,
                    "INVALID MANIFEST",
                    "The remote manifest was empty.");
            }

            if (!string.Equals(
                    manifest.Product,
                    "ForgeCare",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Invalid(
                    current,
                    safeManifestUrl,
                    "INVALID MANIFEST",
                    "Remote manifest product is not ForgeCare.");
            }

            if (!string.Equals(
                    NormalizeAppId(manifest.AppId),
                    NormalizeAppId(ForgeCareAppId),
                    StringComparison.OrdinalIgnoreCase))
            {
                return Invalid(
                    current,
                    safeManifestUrl,
                    "INVALID MANIFEST",
                    "Remote manifest AppId does not match this ForgeCare product identity.");
            }

            string channel =
                NormalizeChannel(manifest.Channel);
            string requestedChannel =
                string.IsNullOrWhiteSpace(expectedChannel)
                    ? string.Empty
                    : NormalizeChannel(expectedChannel);

            if (!TryVersion(
                    current,
                    out Version installed) ||
                !TryVersion(
                    manifest.Version,
                    out Version available))
            {
                return Invalid(
                    current,
                    safeManifestUrl,
                    "INVALID MANIFEST",
                    "Remote manifest version could not be compared safely.");
            }

            if (!string.IsNullOrWhiteSpace(requestedChannel) &&
                !string.Equals(
                    channel,
                    requestedChannel,
                    StringComparison.OrdinalIgnoreCase))
            {
                return new RemoteUpdateCheckResult
                {
                    CurrentVersion = current,
                    AvailableVersion = manifest.Version,
                    Channel = channel,
                    State = "CHANNEL MISMATCH",
                    Detail =
                        $"Manifest channel is '{channel}', while ForgeCare is configured for '{requestedChannel}'. No update action is offered.",
                    ManifestUrl = safeManifestUrl,
                    CheckedAt = DateTime.Now
                };
            }

            int compare =
                available.CompareTo(
                    installed);

            return new RemoteUpdateCheckResult
            {
                CurrentVersion = current,
                AvailableVersion = manifest.Version,
                Channel = channel,
                State =
                    compare > 0
                        ? "UPDATE AVAILABLE"
                        : compare == 0
                            ? "CURRENT"
                            : "OLDER BUILD",

                Detail =
                    compare > 0
                        ? $"ForgeCare {manifest.Version} is available on the {channel} channel. Download and SHA-256 verification require an explicit technician action."
                        : compare == 0
                            ? "The remote manifest matches the running ForgeCare version."
                            : "The remote manifest describes an older ForgeCare build.",

                ManifestUrl = safeManifestUrl,
                CheckedAt = DateTime.Now,
                UpdateAvailable = compare > 0,
                InstallerFile = manifest.Installer?.File ?? string.Empty,
                InstallerSha256 = manifest.Installer?.Sha256 ?? string.Empty
            };
        }
        catch (TaskCanceledException)
        {
            return Invalid(
                current,
                safeManifestUrl,
                "CHECK TIMEOUT",
                "The remote manifest check timed out.");
        }
        catch (HttpRequestException ex)
        {
            CrashLogService.RecordPrivacySafe(ex, "Remote update manifest request");
            return Invalid(
                current,
                safeManifestUrl,
                "OFFLINE / CHECK FAILED",
                "ForgeCare could not reach the update manifest. Check connectivity and try again.");
        }
        catch (Exception ex)
        {
            CrashLogService.RecordPrivacySafe(ex, "Remote update manifest validation");
            return Invalid(
                current,
                safeManifestUrl,
                "CHECK FAILED",
                "ForgeCare could not validate the remote update manifest.");
        }
    }

    private static RemoteUpdateCheckResult Invalid(
        string currentVersion,
        string? manifestUrl,
        string state,
        string detail)
    {
        return new RemoteUpdateCheckResult
        {
            CurrentVersion = currentVersion,
            State = state,
            Detail = detail,
            ManifestUrl = manifestUrl ?? string.Empty,
            CheckedAt = DateTime.Now
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
        out Version version)
    {
        string value =
            (raw ?? string.Empty)
                .Trim()
                .TrimStart('v', 'V');

        int dash =
            value.IndexOf('-');

        if (dash >= 0)
            value = value[..dash];

        if (Version.TryParse(value, out Version? parsed) && parsed is not null)
        {
            version = parsed;
            return true;
        }

        version = new Version(0, 0);
        return false;
    }

    private static string NormalizeAppId(
        string value)
    {
        return (value ?? string.Empty)
            .Trim()
            .Trim('{', '}');
    }

    private static string NormalizeChannel(string? value)
    {
        string channel = (value ?? string.Empty).Trim();
        return channel.Length is > 0 and <= 20 &&
            channel.All(character => char.IsLetterOrDigit(character) || character is '-' or '_')
                ? channel
                : "unknown";
    }
}
