using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class ExternalTestPreflightService
{
    public ExternalTestPreflightResult Run()
    {
        var checks =
            new List<ExternalTestCheck>();

        checks.Add(
            new ExternalTestCheck
            {
                Name = "Windows platform",
                Status = OperatingSystem.IsWindows()
                    ? "PASS"
                    : "FAIL",
                Detail = RuntimeInformation.OSDescription
            });

        checks.Add(
            new ExternalTestCheck
            {
                Name = "64-bit process",
                Status = Environment.Is64BitProcess
                    ? "PASS"
                    : "WARN",
                Detail =
                    $"Process {RuntimeInformation.ProcessArchitecture} · OS {RuntimeInformation.OSArchitecture}"
            });

        string executable =
            Environment.ProcessPath
            ?? string.Empty;

        checks.Add(
            new ExternalTestCheck
            {
                Name = "Standalone executable",
                Status =
                    !string.IsNullOrWhiteSpace(executable) &&
                    File.Exists(executable)
                        ? "PASS"
                        : "WARN",
                Detail =
                    string.IsNullOrWhiteSpace(executable)
                        ? "Process path unavailable."
                        : executable
            });

        string localData =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "ForgeCare");

        try
        {
            Directory.CreateDirectory(localData);

            string probe =
                Path.Combine(
                    localData,
                    ".write-test");

            File.WriteAllText(
                probe,
                DateTime.Now.ToString("O"));

            File.Delete(probe);

            checks.Add(
                new ExternalTestCheck
                {
                    Name = "Local data write",
                    Status = "PASS",
                    Detail = localData
                });
        }
        catch (Exception ex)
        {
            checks.Add(
                new ExternalTestCheck
                {
                    Name = "Local data write",
                    Status = "FAIL",
                    Detail = ex.Message
                });
        }

        string desktop =
            Environment.GetFolderPath(
                Environment.SpecialFolder.DesktopDirectory);

        checks.Add(
            new ExternalTestCheck
            {
                Name = "Desktop path",
                Status =
                    Directory.Exists(desktop)
                        ? "PASS"
                        : "WARN",
                Detail =
                    string.IsNullOrWhiteSpace(desktop)
                        ? "Desktop path unavailable."
                        : desktop
            });

        string version =
            Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
            ?? "unknown";

        checks.Add(
            new ExternalTestCheck
            {
                Name = "Build identity",
                Status =
                    version.Contains(
                        "beta",
                        StringComparison.OrdinalIgnoreCase)
                        ? "PASS"
                        : "WARN",
                Detail =
                    $"ForgeCare {version}"
            });

        bool crashLogExists =
            File.Exists(
                CrashLogService.CrashLogPath);

        checks.Add(
            new ExternalTestCheck
            {
                Name = "Crash diagnostics",
                Status = "PASS",
                Detail =
                    crashLogExists
                        ? $"Existing log: {CrashLogService.CrashLogPath}"
                        : $"Ready: {CrashLogService.CrashLogPath}"
            });

        return new ExternalTestPreflightResult
        {
            Checks = checks
        };
    }
}
