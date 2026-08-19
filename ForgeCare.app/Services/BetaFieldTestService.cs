using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class BetaFieldTestService
{
    private readonly string _directory;
    private readonly string _sessionFile;
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public BetaFieldTestService(string? dataRoot = null)
    {
        _directory = Path.Combine(
            dataRoot ??
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ForgeCare"),
            "Beta");

        _sessionFile = Path.Combine(_directory, "field-test-session.json");
    }

    public BetaFieldTestSession StartNew(string testerName)
    {
        string version =
            Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
            ?? "unknown";

        var session = new BetaFieldTestSession
        {
            StartedAt = DateTime.Now,
            BuildVersion = version,
            ComputerName = Environment.MachineName,
            WindowsDescription = RuntimeInformation.OSDescription,
            Architecture = $"{RuntimeInformation.ProcessArchitecture} / {RuntimeInformation.OSArchitecture}",
            TesterName = testerName,
            Steps = CreateDefaultSteps()
        };

        Save(session);
        return session;
    }

    public BetaFieldTestSession? Load()
    {
        try
        {
            if (!File.Exists(_sessionFile))
                return null;

            return JsonSerializer.Deserialize<BetaFieldTestSession>(
                File.ReadAllText(_sessionFile), _json);
        }
        catch
        {
            return null;
        }
    }

    public void Save(BetaFieldTestSession session)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            _sessionFile,
            JsonSerializer.Serialize(session, _json),
            Encoding.UTF8);
    }

    public void Complete(
        BetaFieldTestSession session,
        string overallStatus,
        string notes)
    {
        session.CompletedAt = DateTime.Now;
        session.OverallStatus = overallStatus;
        session.Notes = notes;
        Save(session);
    }

    public string ExportIssuePackage(
        BetaIssueReport issue,
        string destinationZip,
        BetaDiagnosticsService diagnostics)
    {
        Directory.CreateDirectory(_directory);

        string root = Path.Combine(_directory, "issue-" + issue.IssueId);
        if (Directory.Exists(root))
            Directory.Delete(root, true);

        Directory.CreateDirectory(root);

        File.WriteAllText(
            Path.Combine(root, "issue.json"),
            JsonSerializer.Serialize(issue, _json),
            Encoding.UTF8);

        File.WriteAllText(
            Path.Combine(root, "issue.txt"),
            BuildIssueText(issue),
            Encoding.UTF8);

        File.WriteAllText(
            Path.Combine(root, "environment.txt"),
            diagnostics.GetEnvironmentSummary(),
            Encoding.UTF8);

        if (File.Exists(CrashLogService.CrashLogPath))
        {
            File.Copy(
                CrashLogService.CrashLogPath,
                Path.Combine(root, "crash.log"),
                true);
        }

        BetaFieldTestSession? session = Load();
        if (session != null)
        {
            File.WriteAllText(
                Path.Combine(root, "field-test-session.json"),
                JsonSerializer.Serialize(session, _json),
                Encoding.UTF8);
        }

        if (File.Exists(destinationZip))
            File.Delete(destinationZip);

        ZipFile.CreateFromDirectory(
            root,
            destinationZip,
            CompressionLevel.Optimal,
            includeBaseDirectory: false);

        Directory.Delete(root, true);
        return destinationZip;
    }

    private static List<BetaFieldTestStep> CreateDefaultSteps()
    {
        return new()
        {
            new() { Id = "launch", Title = "Launch / Preflight" },
            new() { Id = "settings", Title = "Settings persistence" },
            new() { Id = "diagnostics", Title = "Read-only diagnostics" },
            new()
            {
                Id = "evidence",
                Title = "Evidence persistence / restart",
                Detail = "Run System Scan and Deep Analysis, confirm the same-session Evidence JSON contains both sources, restart ForgeCare, and confirm the document remains readable."
            },
            new() { Id = "workflow", Title = "Guided workflow" },
            new() { Id = "safe-action", Title = "Safe action test" },
            new() { Id = "verify", Title = "Verification scan" },
            new() { Id = "report", Title = "HTML report export" },
            new() { Id = "debug", Title = "Debug bundle export" },
            new() { Id = "restart", Title = "Restart / persistence" }
        };
    }

    private static string BuildIssueText(BetaIssueReport issue)
    {
        return
            $"ForgeCare Beta Issue{Environment.NewLine}" +
            $"Issue: {issue.IssueId}{Environment.NewLine}" +
            $"Created: {issue.CreatedAt:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
            $"Build: {issue.BuildVersion}{Environment.NewLine}" +
            $"Machine: {issue.ComputerName}{Environment.NewLine}" +
            $"Area: {issue.Area}{Environment.NewLine}" +
            $"Severity: {issue.Severity}{Environment.NewLine}{Environment.NewLine}" +
            $"DESCRIPTION{Environment.NewLine}{issue.Description}{Environment.NewLine}{Environment.NewLine}" +
            $"REPRODUCTION{Environment.NewLine}{issue.ReproductionSteps}{Environment.NewLine}{Environment.NewLine}" +
            $"EXPECTED{Environment.NewLine}{issue.ExpectedResult}{Environment.NewLine}{Environment.NewLine}" +
            $"ACTUAL{Environment.NewLine}{issue.ActualResult}{Environment.NewLine}";
    }
}
