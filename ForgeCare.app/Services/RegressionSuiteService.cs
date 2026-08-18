using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class RegressionSuiteService
{
    public RegressionSuiteResult Run()
    {
        var suite =
            new RegressionSuiteResult
            {
                StartedAt = DateTime.Now
            };

        Check(
            suite,
            "Environment",
            "Windows platform",
            OperatingSystem.IsWindows(),
            RuntimeInformation.OSDescription);

        Check(
            suite,
            "Environment",
            "64-bit process",
            Environment.Is64BitProcess,
            $"Process {RuntimeInformation.ProcessArchitecture} · OS {RuntimeInformation.OSArchitecture}");

        string executable =
            Environment.ProcessPath
            ?? string.Empty;

        Check(
            suite,
            "Release",
            "Executable exists",
            !string.IsNullOrWhiteSpace(executable) &&
            File.Exists(executable),
            string.IsNullOrWhiteSpace(executable)
                ? "Process path unavailable."
                : executable);

        string version =
            Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
            ?? "unknown";

        Check(
            suite,
            "Release",
            "Version identity",
            !string.IsNullOrWhiteSpace(version) &&
            version != "unknown",
            version);

        string dataRoot =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "ForgeCare");

        try
        {
            Directory.CreateDirectory(dataRoot);

            string probe =
                Path.Combine(
                    dataRoot,
                    ".regression-write-test");

            File.WriteAllText(
                probe,
                DateTime.Now.ToString("O"));

            File.Delete(probe);

            Pass(
                suite,
                "Persistence",
                "Local data write",
                dataRoot);
        }
        catch (Exception ex)
        {
            Fail(
                suite,
                "Persistence",
                "Local data write",
                ex.Message);
        }

        string settings =
            Path.Combine(
                dataRoot,
                "Settings");

        TryDirectoryCheck(
            suite,
            "Persistence",
            "Settings directory",
            settings);

        string reports =
            Path.Combine(
                dataRoot,
                "Reports");

        TryDirectoryCheck(
            suite,
            "Reports",
            "Reports directory",
            reports);

        string safety =
            Path.Combine(
                dataRoot,
                "Safety");

        TryDirectoryCheck(
            suite,
            "Safety",
            "Safety directory",
            safety);

        string diagnostics =
            CrashLogService.DiagnosticsRoot;

        TryDirectoryCheck(
            suite,
            "Diagnostics",
            "Diagnostics directory",
            diagnostics);

        string recoveryRoot =
            AppLifecycleRecoveryService.RecoveryRoot;

        TryDirectoryCheck(
            suite,
            "Recovery",
            "Recovery directory",
            recoveryRoot);

        var identity =
            new ReleaseIdentityService()
                .Inspect();

        Check(
            suite,
            "Release",
            "Release fingerprint",
            !string.IsNullOrWhiteSpace(identity.ReleaseFingerprint) &&
            identity.ReleaseFingerprint != "UNAVAILABLE",
            identity.ReleaseFingerprint);

        var preflight =
            new ExternalTestPreflightService()
                .Run();

        if (preflight.Failed > 0)
        {
            Fail(
                suite,
                "External test",
                "External-machine preflight",
                $"{preflight.Failed} failed · {preflight.Warnings} warnings");
        }
        else if (preflight.Warnings > 0)
        {
            Warn(
                suite,
                "External test",
                "External-machine preflight",
                $"{preflight.Passed} pass · {preflight.Warnings} warnings");
        }
        else
        {
            Pass(
                suite,
                "External test",
                "External-machine preflight",
                $"{preflight.Passed} pass");
        }

        StabilityRecoveryResult recovery =
            new StabilityRecoveryService()
                .Inspect();

        if (recovery.PreviousSessionUnclean)
        {
            Warn(
                suite,
                "Recovery",
                "Previous session state",
                "Previous session did not record a clean shutdown.");
        }
        else
        {
            Pass(
                suite,
                "Recovery",
                "Previous session state",
                "No unclean-session flag.");
        }

        if (recovery.StalePartialFileCount > 0 ||
            recovery.StaleStagingDirectoryCount > 0)
        {
            Warn(
                suite,
                "Recovery",
                "Transient recovery state",
                $"{recovery.StalePartialFileCount} partial · {recovery.StaleStagingDirectoryCount} staging");
        }
        else
        {
            Pass(
                suite,
                "Recovery",
                "Transient recovery state",
                "No stale ForgeCare transient files.");
        }

        suite.CompletedAt =
            DateTime.Now;

        return suite;
    }

    private static void TryDirectoryCheck(
        RegressionSuiteResult suite,
        string area,
        string check,
        string path)
    {
        try
        {
            Directory.CreateDirectory(path);

            Pass(
                suite,
                area,
                check,
                path);
        }
        catch (Exception ex)
        {
            Fail(
                suite,
                area,
                check,
                ex.Message);
        }
    }

    private static void Check(
        RegressionSuiteResult suite,
        string area,
        string check,
        bool success,
        string detail)
    {
        if (success)
            Pass(suite, area, check, detail);
        else
            Fail(suite, area, check, detail);
    }

    private static void Pass(
        RegressionSuiteResult suite,
        string area,
        string check,
        string detail)
    {
        suite.Checks.Add(
            new RegressionCheckResult
            {
                Area = area,
                Check = check,
                Status = "PASS",
                Detail = detail
            });
    }

    private static void Warn(
        RegressionSuiteResult suite,
        string area,
        string check,
        string detail)
    {
        suite.Checks.Add(
            new RegressionCheckResult
            {
                Area = area,
                Check = check,
                Status = "WARN",
                Detail = detail
            });
    }

    private static void Fail(
        RegressionSuiteResult suite,
        string area,
        string check,
        string detail)
    {
        suite.Checks.Add(
            new RegressionCheckResult
            {
                Area = area,
                Check = check,
                Status = "FAIL",
                Detail = detail
            });
    }
}
