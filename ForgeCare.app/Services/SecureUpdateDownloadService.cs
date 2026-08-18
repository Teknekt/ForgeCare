using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class SecureUpdateDownloadService
{
    private readonly HttpClient _httpClient;

    public SecureUpdateDownloadService()
    {
        _httpClient =
            new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "ForgeCare-Technician-Edition/SecureUpdateDownload");
    }

    public async Task<SecureUpdateDownloadResult> DownloadAndVerifyAsync(
        string manifestUrl,
        string installerFile,
        string expectedSha256,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(manifestUrl, UriKind.Absolute, out Uri? manifestUri) ||
            !string.Equals(manifestUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return Fail("INVALID SOURCE", "The accepted manifest source is not HTTPS.");
        }

        if (string.IsNullOrWhiteSpace(installerFile))
            return Fail("NO INSTALLER", "The manifest does not define an installer artifact.");

        string expected = NormalizeHash(expectedSha256);
        if (!Regex.IsMatch(expected, "^[A-F0-9]{64}$"))
            return Fail("INVALID HASH", "The manifest does not contain a valid SHA-256 installer hash.");

        Uri artifactUri;
        if (Uri.TryCreate(installerFile, UriKind.Absolute, out Uri? absolute))
        {
            artifactUri = absolute;
        }
        else
        {
            artifactUri = new Uri(manifestUri, installerFile);
        }

        if (!string.Equals(artifactUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return Fail("HTTPS REQUIRED", "Update artifacts must be downloaded over HTTPS.");

        string safeName = Path.GetFileName(artifactUri.LocalPath);
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "ForgeCare-Update.exe";

        string root =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ForgeCare",
                "Updates");

        Directory.CreateDirectory(root);

        string finalPath = Path.Combine(root, safeName);
        string partialPath = finalPath + ".partial";

        try
        {
            if (File.Exists(partialPath))
                File.Delete(partialPath);

            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    artifactUri,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Fail(
                    "DOWNLOAD FAILED",
                    $"Artifact request returned HTTP {(int)response.StatusCode} {response.ReasonPhrase}.");

            long? total = response.Content.Headers.ContentLength;

            await using Stream input =
                await response.Content.ReadAsStreamAsync(cancellationToken);

            await using FileStream output =
                new(
                    partialPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    true);

            byte[] buffer = new byte[81920];
            long received = 0;

            while (true)
            {
                int read =
                    await input.ReadAsync(
                        buffer.AsMemory(0, buffer.Length),
                        cancellationToken);

                if (read <= 0)
                    break;

                await output.WriteAsync(
                    buffer.AsMemory(0, read),
                    cancellationToken);

                received += read;

                if (total is > 0)
                    progress?.Report(
                        (int)Math.Clamp(received * 100L / total.Value, 0, 100));
            }

            await output.FlushAsync(cancellationToken);

            string actual;
            await using (FileStream verify =
                new(partialPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] hash =
                    await SHA256.HashDataAsync(
                        verify,
                        cancellationToken);

                actual =
                    Convert.ToHexString(hash);
            }

            if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(expected),
                    Convert.FromHexString(actual)))
            {
                File.Delete(partialPath);

                return new SecureUpdateDownloadResult
                {
                    Success = false,
                    State = "HASH MISMATCH",
                    Detail = "The downloaded installer failed SHA-256 verification and was deleted.",
                    ExpectedSha256 = expected,
                    ActualSha256 = actual
                };
            }

            if (File.Exists(finalPath))
                File.Delete(finalPath);

            File.Move(partialPath, finalPath);

            progress?.Report(100);

            return new SecureUpdateDownloadResult
            {
                Success = true,
                State = "VERIFIED",
                Detail = "Installer downloaded and SHA-256 verified. ForgeCare has not executed it.",
                DownloadPath = finalPath,
                ExpectedSha256 = expected,
                ActualSha256 = actual
            };
        }
        catch (OperationCanceledException)
        {
            TryDelete(partialPath);
            return Fail("CANCELLED", "The update download was cancelled.");
        }
        catch (Exception ex)
        {
            TryDelete(partialPath);
            return Fail("DOWNLOAD FAILED", ex.Message);
        }
    }

    private static string NormalizeHash(string value) =>
        (value ?? string.Empty)
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Trim()
            .ToUpperInvariant();

    private static SecureUpdateDownloadResult Fail(
        string state,
        string detail) =>
        new()
        {
            Success = false,
            State = state,
            Detail = detail
        };

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
}
