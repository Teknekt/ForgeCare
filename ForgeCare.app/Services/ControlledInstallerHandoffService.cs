using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class ControlledInstallerHandoffService
{
    public async Task<InstallerHandoffResult> ValidateForHandoffAsync(
        string installerPath,
        string expectedSha256,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(installerPath) ||
            !File.Exists(installerPath))
        {
            return Fail(
                "FILE MISSING",
                "The verified installer is no longer present in the staging folder.");
        }

        string expected =
            NormalizeHash(expectedSha256);

        if (!Regex.IsMatch(expected, "^[A-F0-9]{64}$"))
        {
            return Fail(
                "INVALID HASH",
                "The accepted manifest does not contain a valid SHA-256 hash.");
        }

        try
        {
            string actual;

            await using FileStream stream =
                new(
                    installerPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

            byte[] hash =
                await SHA256.HashDataAsync(
                    stream,
                    cancellationToken);

            actual =
                Convert.ToHexString(hash);

            if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(expected),
                    Convert.FromHexString(actual)))
            {
                return Fail(
                    "HASH CHANGED",
                    "The staged installer no longer matches the accepted manifest. ForgeCare will not launch it.");
            }

            return new InstallerHandoffResult
            {
                Success = true,
                State = "READY TO INSTALL",
                Detail =
                    "The staged installer was re-verified immediately before handoff.",
                InstallerPath = installerPath
            };
        }
        catch (OperationCanceledException)
        {
            return Fail(
                "CANCELLED",
                "Installer validation was cancelled.");
        }
        catch (Exception ex)
        {
            return Fail(
                "VALIDATION FAILED",
                ex.Message);
        }
    }

    public InstallerHandoffResult LaunchInstaller(
        string installerPath)
    {
        if (string.IsNullOrWhiteSpace(installerPath) ||
            !File.Exists(installerPath))
        {
            return Fail(
                "FILE MISSING",
                "The installer could not be found at launch time.");
        }

        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true
                });

            return new InstallerHandoffResult
            {
                Success = true,
                State = "INSTALLER LAUNCHED",
                Detail =
                    "Windows received the installer handoff. ForgeCare did not pass silent-install switches or bypass Windows security prompts.",
                InstallerPath = installerPath
            };
        }
        catch (Exception ex)
        {
            return Fail(
                "LAUNCH FAILED",
                ex.Message);
        }
    }

    private static string NormalizeHash(string value) =>
        (value ?? string.Empty)
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Trim()
            .ToUpperInvariant();

    private static InstallerHandoffResult Fail(
        string state,
        string detail) =>
        new()
        {
            Success = false,
            State = state,
            Detail = detail
        };
}
