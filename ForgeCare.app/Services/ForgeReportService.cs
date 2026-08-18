using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class ForgeReportService
{
    private static readonly Lazy<ForgeReportService>
        LazyInstance =
            new(() =>
                new ForgeReportService());

    public static ForgeReportService Instance =>
        LazyInstance.Value;

    private readonly object _sync =
        new();

    private readonly string _reportDirectory;
    private readonly string _sessionFile;
    private readonly string _archiveFile;

    private readonly JsonSerializerOptions _jsonOptions =
        new()
        {
            WriteIndented = true
        };

    private ForgeReportSession _session;

    private ForgeReportService()
    {
        _reportDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "ForgeCare",
                "Reports");

        _sessionFile =
            Path.Combine(
                _reportDirectory,
                "current-session.json");

        _archiveFile =
            Path.Combine(
                _reportDirectory,
                "report-history.json");

        _session =
            LoadSession() ??
            CreateSession();
    }

    public ForgeReportSession Snapshot()
    {
        lock (_sync)
        {
            // Serialize/deserialize gives the UI a detached snapshot
            // so report rendering cannot mutate the live session.
            string json =
                JsonSerializer.Serialize(
                    _session,
                    _jsonOptions);

            return JsonSerializer.Deserialize<
                       ForgeReportSession>(
                           json,
                           _jsonOptions)
                   ?? CreateSession();
        }
    }

    public ForgeReportSession StartNewSession()
    {
        lock (_sync)
        {
            _session =
                CreateSession();

            SaveSession();

            return Snapshot();
        }
    }

    public void RecordSystemScan(
        SystemSnapshot snapshot,
        HealthResult health)
    {
        lock (_sync)
        {
            _session.ComputerName =
                snapshot.ComputerName;

            _session.OperatingSystem =
                snapshot.OperatingSystem;

            _session.ProcessorName =
                snapshot.ProcessorName;

            _session.Checkpoints.Add(
                new ForgeReportCheckpoint
                {
                    Timestamp =
                        snapshot.ScanTime,

                    HealthScore =
                        health.Score,

                    HealthRating =
                        health.Rating,

                    SystemDriveFreeGb =
                        snapshot.SystemDriveFreeGb,

                    StorageFreePercent =
                        health.StorageFreePercent,

                    AvailableMemoryGb =
                        snapshot.AvailableMemoryGb,

                    MemoryAvailablePercent =
                        health.MemoryAvailablePercent,

                    StartupCount =
                        health.StartupCount
                });

            AddAction(
                "SYSTEM",
                _session.Checkpoints.Count == 1
                    ? "Initial system profile captured"
                    : "System profile rescanned",
                "PROFILE CAPTURED",
                $"Health {health.Score}/100 · " +
                $"{health.StorageFreePercent:0.0}% storage free · " +
                $"{health.StartupCount} startup items.",
                $"Health {health.Score}",
                true);

            TouchAndSave();
        }
    }

    public void RecordResourceAnalysis(
        ResourceAnalysisResult result)
    {
        lock (_sync)
        {
            _session.DeepAnalysisRuns++;

            AddAction(
                "ANALYSIS",
                "Deep system analysis",
                result.OverallPressure,
                $"CPU {result.CpuUsagePercent:0.0}% · " +
                $"Memory {result.MemoryUsedPercent:0.0}% · " +
                $"{result.ProcessCount} processes.",
                $"{result.OverallPressure} pressure",
                true);

            TouchAndSave();
        }
    }

    public void RecordServiceAnalysis(
        ServiceAnalysisResult result)
    {
        lock (_sync)
        {
            _session.ServiceAnalysisRuns++;
            _session.LastServiceReviewCount =
                result.ReviewCount;

            AddAction(
                "SERVICES",
                "Service Intelligence",
                "ANALYZED",
                $"{result.TotalCount} services · " +
                $"{result.RunningCount} running · " +
                $"{result.ReviewCount} contextual review candidates.",
                $"{result.ReviewCount} review",
                true);

            TouchAndSave();
        }
    }

    public void RecordStorageAnalysis(
        StorageAnalysisResult result)
    {
        lock (_sync)
        {
            _session.StorageAnalysisRuns++;
            _session.LastLargeFileCount =
                result.LargeFileCount;

            AddAction(
                "STORAGE",
                "Storage deep scan",
                "ANALYZED",
                $"{result.ScannedFiles:N0} files inspected · " +
                $"{result.LargeFileCount} large-file findings · " +
                $"{result.DisplayLargeFileSize} in large files.",
                $"{result.LargeFileCount} large files",
                true);

            TouchAndSave();
        }
    }

    public void RecordDuplicateScan(
        DuplicateScanResult result)
    {
        lock (_sync)
        {
            _session.DuplicateScanRuns++;
            _session.LastDuplicateGroupCount =
                result.DuplicateGroupCount;
            _session.LastDuplicateReclaimableBytes =
                result.ReclaimableBytes;

            AddAction(
                "DUPLICATES",
                "Exact duplicate scan",
                "ANALYZED",
                $"{result.DuplicateGroupCount} exact SHA-256 groups · " +
                $"{result.DuplicateFileCount} copies · " +
                $"{result.DisplayReclaimable} potential recovery.",
                result.DisplayReclaimable,
                true);

            TouchAndSave();
        }
    }

    public void RecordCleanup(
        CleanupExecutionResult result)
    {
        lock (_sync)
        {
            _session.TotalRecoveredBytes +=
                result.ReclaimedBytes;

            AddAction(
                "CLEANUP",
                "Temporary-file cleanup",
                result.ErrorCount == 0
                    ? "COMPLETE"
                    : "COMPLETE WITH ERRORS",
                $"{result.DeletedFiles:N0} files deleted · " +
                $"{result.ReclaimedSize} reclaimed · " +
                $"{result.SkippedFiles:N0} skipped.",
                result.ReclaimedSize,
                result.ErrorCount == 0);

            SafetyJournalService.Instance.Record(
                "CLEANUP",
                "Temporary-file cleanup",
                $"{result.DeletedFiles:N0} file(s)",
                result.ErrorCount == 0 ? "COMPLETE" : "COMPLETE WITH ERRORS",
                $"{result.ReclaimedSize} reclaimed · {result.SkippedFiles:N0} skipped.",
                reversible: false,
                recovery: "NONE");

            TouchAndSave();
        }
    }

    public void RecordStartupChange(
        StartupChangeResult result,
        bool isRestore)
    {
        lock (_sync)
        {
            if (isRestore)
            {
                _session.StartupEntriesRestored +=
                    result.RestoredCount;
            }
            else
            {
                _session.StartupEntriesDisabled +=
                    result.DisabledCount;
            }

            AddAction(
                "STARTUP",
                isRestore
                    ? "Startup entries restored"
                    : "Startup entries disabled",
                result.ErrorCount == 0
                    ? "COMPLETE"
                    : "COMPLETE WITH ERRORS",
                isRestore
                    ? $"{result.RestoredCount} restored · {result.ErrorCount} errors."
                    : $"{result.DisabledCount} disabled · {result.BlockedCount} blocked · {result.ErrorCount} errors.",
                isRestore
                    ? $"{result.RestoredCount} restored"
                    : $"{result.DisabledCount} disabled",
                result.ErrorCount == 0);

            TouchAndSave();
        }
    }

    public void RecordStorageCleanup(
        StorageCleanupResult result,
        string title)
    {
        lock (_sync)
        {
            _session.TotalRecoveredBytes +=
                result.RecycledBytes;

            AddAction(
                "STORAGE CLEANUP",
                title,
                result.ErrorCount == 0
                    ? "COMPLETE"
                    : "COMPLETE WITH ERRORS",
                $"{result.RecycledFiles} file(s) moved to Recycle Bin · " +
                $"{result.DisplayRecycledSize} recovered · " +
                $"{result.BlockedFiles} blocked · " +
                $"{result.SkippedFiles} skipped.",
                result.DisplayRecycledSize,
                result.ErrorCount == 0);

            SafetyJournalService.Instance.Record(
                "STORAGE",
                "Recycle reviewed files",
                $"{result.RecycledFiles:N0} file(s)",
                result.ErrorCount == 0 ? "COMPLETE" : "COMPLETE WITH ERRORS",
                $"{result.DisplayRecycledSize} moved to Windows Recycle Bin.",
                reversible: result.RecycledFiles > 0,
                recovery: "WINDOWS RECYCLE BIN");

            TouchAndSave();
        }
    }


    public void UpdateMetadata(
        ForgeReportMetadata metadata)
    {
        lock (_sync)
        {
            _session.Metadata =
                metadata ?? new ForgeReportMetadata();

            if (string.IsNullOrWhiteSpace(
                    _session.Metadata.JobId))
            {
                _session.Metadata.JobId =
                    CreateJobId();
            }

            TouchAndSave();
        }
    }

    public List<ForgeReportArchiveEntry> GetArchive()
    {
        lock (_sync)
        {
            try
            {
                if (!File.Exists(
                        _archiveFile))
                {
                    return new List<ForgeReportArchiveEntry>();
                }

                return JsonSerializer.Deserialize<
                           List<ForgeReportArchiveEntry>>(
                               File.ReadAllText(
                                   _archiveFile),
                               _jsonOptions)
                       ?? new List<ForgeReportArchiveEntry>();
            }
            catch
            {
                return new List<ForgeReportArchiveEntry>();
            }
        }
    }

    private void RecordExport(
        ForgeReportSession session,
        string path)
    {
        lock (_sync)
        {
            var archive =
                GetArchive();

            archive.Add(
                new ForgeReportArchiveEntry
                {
                    ExportedAt =
                        DateTime.Now,

                    JobId =
                        session.Metadata.JobId,

                    CustomerName =
                        session.Metadata.CustomerName,

                    DeviceLabel =
                        session.Metadata.DeviceLabel,

                    ComputerName =
                        session.ComputerName,

                    FilePath =
                        path,

                    RecoveredStorage =
                        session.DisplayRecovered,

                    ActionCount =
                        session.ActionCount
                });

            if (archive.Count > 200)
            {
                archive =
                    archive
                        .OrderByDescending(
                            entry =>
                                entry.ExportedAt)
                        .Take(200)
                        .OrderBy(
                            entry =>
                                entry.ExportedAt)
                        .ToList();
            }

            Directory.CreateDirectory(
                _reportDirectory);

            string temp =
                _archiveFile + ".tmp";

            File.WriteAllText(
                temp,
                JsonSerializer.Serialize(
                    archive,
                    _jsonOptions));

            File.Move(
                temp,
                _archiveFile,
                overwrite: true);
        }
    }

    private static string CreateJobId()
    {
        return
            $"FC-{DateTime.Now:yyyyMMdd-HHmm}";
    }

    public async Task ExportHtmlAsync(
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("No export path was supplied.", nameof(path));

        string fullPath = Path.GetFullPath(path.Trim());

        if (!string.Equals(Path.GetExtension(fullPath), ".html", StringComparison.OrdinalIgnoreCase))
            fullPath = Path.ChangeExtension(fullPath, ".html");

        ForgeReportSession snapshot = Snapshot();
        string html = BuildHtml(snapshot);

        if (string.IsNullOrWhiteSpace(html))
            throw new InvalidOperationException("ForgeCare generated an empty HTML report.");

        string? directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("ForgeCare could not resolve the report destination folder.");

        Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(
            fullPath,
            html,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var exportedFile = new FileInfo(fullPath);
        if (!exportedFile.Exists || exportedFile.Length == 0)
            throw new IOException($"ForgeCare could not verify the exported report at:{Environment.NewLine}{fullPath}");

        RecordExport(snapshot, fullPath);
    }

    private static ForgeReportSession CreateSession()
    {
        return new ForgeReportSession
        {
            SessionId =
                Guid.NewGuid().ToString("N"),
            StartedAt =
                DateTime.Now,
            UpdatedAt =
                DateTime.Now,
            Metadata =
                new ForgeReportMetadata
                {
                    JobId =
                        CreateJobId(),
                    CompanyName =
                        "Mindforge Studio"
                }
        };
    }

    private ForgeReportSession? LoadSession()
    {
        try
        {
            if (!File.Exists(
                    _sessionFile))
            {
                return null;
            }

            string json =
                File.ReadAllText(
                    _sessionFile);

            return JsonSerializer.Deserialize<
                ForgeReportSession>(
                    json,
                    _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private void TouchAndSave()
    {
        _session.UpdatedAt =
            DateTime.Now;

        SaveSession();
    }

    private void SaveSession()
    {
        Directory.CreateDirectory(
            _reportDirectory);

        string json =
            JsonSerializer.Serialize(
                _session,
                _jsonOptions);

        string temp =
            _sessionFile + ".tmp";

        File.WriteAllText(
            temp,
            json);

        File.Move(
            temp,
            _sessionFile,
            overwrite: true);
    }

    private void AddAction(
        string category,
        string title,
        string result,
        string detail,
        string metric,
        bool success)
    {
        _session.Actions.Add(
            new ForgeReportAction
            {
                Timestamp =
                    DateTime.Now,
                Category =
                    category,
                Title =
                    title,
                Result =
                    result,
                Detail =
                    detail,
                Metric =
                    metric,
                IsSuccess =
                    success
            });
    }

    private static string BuildHtml(
        ForgeReportSession session)
    {
        ForgeReportCheckpoint? before =
            session.Before;

        ForgeReportCheckpoint? current =
            session.Current;

        ForgeReportMetadata metadata =
            session.Metadata ??
            new ForgeReportMetadata();

        string company =
            string.IsNullOrWhiteSpace(
                metadata.CompanyName)
                ? "Mindforge Studio"
                : metadata.CompanyName;

        string jobId =
            string.IsNullOrWhiteSpace(
                metadata.JobId)
                ? session.SessionId[
                    ..Math.Min(
                        10,
                        session.SessionId.Length)]
                : metadata.JobId;

        string device =
            string.IsNullOrWhiteSpace(
                metadata.DeviceLabel)
                ? session.ComputerName
                : metadata.DeviceLabel;

        string healthBefore =
            before == null
                ? "—"
                : before.HealthScore.ToString();

        string healthCurrent =
            current == null
                ? "—"
                : current.HealthScore.ToString();

        string healthDelta =
            before == null ||
            current == null
                ? "—"
                : FormatSigned(
                    current.HealthScore -
                    before.HealthScore);

        string freeBefore =
            before == null
                ? "—"
                : $"{before.SystemDriveFreeGb:0.0} GB";

        string freeCurrent =
            current == null
                ? "—"
                : $"{current.SystemDriveFreeGb:0.0} GB";

        string freeDelta =
            before == null ||
            current == null
                ? "—"
                : $"{current.SystemDriveFreeGb - before.SystemDriveFreeGb:+0.0;-0.0;0.0} GB";

        string startupBefore =
            before == null
                ? "—"
                : before.StartupCount.ToString();

        string startupCurrent =
            current == null
                ? "—"
                : current.StartupCount.ToString();

        string startupDelta =
            before == null ||
            current == null
                ? "—"
                : FormatSigned(
                    current.StartupCount -
                    before.StartupCount);

        var actions =
            new StringBuilder();

        foreach (var action in
                 session.Actions
                     .OrderBy(action =>
                         action.Timestamp))
        {
            actions.Append(
                $"""
                <tr>
                    <td>{Html(action.Timestamp.ToString("HH:mm:ss"))}</td>
                    <td>{Html(action.Category)}</td>
                    <td>{Html(action.Title)}</td>
                    <td>{Html(action.Result)}</td>
                    <td>{Html(action.Metric)}</td>
                    <td>{Html(action.Detail)}</td>
                </tr>
                """);
        }

        string notes =
            string.IsNullOrWhiteSpace(
                metadata.TechnicianNotes)
                ? "No technician notes were added."
                : Html(metadata.TechnicianNotes)
                    .Replace(
                        "\r\n",
                        "<br>")
                    .Replace(
                        "\n",
                        "<br>");

        string summary =
            string.IsNullOrWhiteSpace(
                metadata.ServiceSummary)
                ? "ForgeCare diagnostic, optimization and verification session."
                : Html(metadata.ServiceSummary);

        return
            $$"""
            <!doctype html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width,initial-scale=1">
                <title>ForgeCare Report · {{Html(jobId)}}</title>
                <style>
                    :root {
                        color-scheme: dark;
                        --bg:#0c0d0f;
                        --card:#141619;
                        --card2:#111316;
                        --line:#292d32;
                        --text:#f5f5f5;
                        --muted:#8c949d;
                        --gold:#c7a65b;
                        --green:#6ebe8c;
                        --red:#e15050;
                    }
                    * { box-sizing:border-box; }
                    body {
                        margin:0;
                        background:var(--bg);
                        color:var(--text);
                        font-family:Segoe UI,Arial,sans-serif;
                    }
                    main {
                        max-width:1180px;
                        margin:auto;
                        padding:42px;
                    }
                    .masthead {
                        display:flex;
                        justify-content:space-between;
                        gap:28px;
                        align-items:flex-start;
                        padding-bottom:24px;
                        border-bottom:1px solid var(--line);
                    }
                    .brand {
                        color:var(--gold);
                        font-weight:700;
                        letter-spacing:.11em;
                        font-size:12px;
                    }
                    h1 {
                        margin:.3rem 0 0;
                        font-size:38px;
                        letter-spacing:-.03em;
                    }
                    h2 {
                        margin:32px 0 12px;
                        font-size:19px;
                    }
                    .muted { color:var(--muted); }
                    .job {
                        min-width:260px;
                        text-align:right;
                        font-size:12px;
                        line-height:1.7;
                    }
                    .job strong { color:var(--gold); }
                    .grid {
                        display:grid;
                        grid-template-columns:repeat(4,1fr);
                        gap:14px;
                        margin:18px 0;
                    }
                    .two {
                        display:grid;
                        grid-template-columns:1fr 1fr;
                        gap:14px;
                    }
                    .card {
                        background:var(--card);
                        border:1px solid var(--line);
                        border-radius:14px;
                        padding:18px;
                    }
                    .label {
                        color:var(--muted);
                        font-size:10px;
                        text-transform:uppercase;
                        letter-spacing:.07em;
                    }
                    .value {
                        margin-top:7px;
                        font-size:24px;
                    }
                    .gold { color:var(--gold); }
                    .green { color:var(--green); }
                    .compare-row {
                        display:grid;
                        grid-template-columns:1fr 110px 1fr 110px;
                        align-items:center;
                        border-bottom:1px solid var(--line);
                        padding:11px 0;
                        gap:10px;
                    }
                    .compare-row:last-child { border-bottom:0; }
                    .delta {
                        font-weight:700;
                        color:var(--gold);
                        text-align:right;
                    }
                    .notes {
                        line-height:1.65;
                        white-space:normal;
                    }
                    table {
                        width:100%;
                        border-collapse:collapse;
                        margin-top:8px;
                    }
                    th,td {
                        border-bottom:1px solid var(--line);
                        padding:11px 9px;
                        text-align:left;
                        vertical-align:top;
                        font-size:11px;
                    }
                    th {
                        color:var(--gold);
                        font-size:9px;
                        text-transform:uppercase;
                        letter-spacing:.05em;
                    }
                    footer {
                        color:var(--muted);
                        font-size:10px;
                        line-height:1.6;
                        margin-top:30px;
                        padding-top:18px;
                        border-top:1px solid var(--line);
                    }
                    @media(max-width:800px) {
                        .grid { grid-template-columns:1fr 1fr; }
                        .two { grid-template-columns:1fr; }
                        .masthead { display:block; }
                        .job { text-align:left; margin-top:18px; }
                    }
                    @media print {
                        :root { color-scheme:light; }
                        body { background:white; color:#111; }
                        main { max-width:none; padding:18mm; }
                        .card { background:white; border-color:#ccc; break-inside:avoid; }
                        .muted,.label,footer { color:#555; }
                        .brand,.gold,.delta,th { color:#80661f; }
                        .green { color:#267247; }
                        .masthead,.compare-row,footer { border-color:#ccc; }
                        table { break-inside:auto; }
                        tr { break-inside:avoid; }
                    }
                </style>
            </head>
            <body>
            <main>
                <header class="masthead">
                    <div>
                        <div class="brand">{{Html(company.ToUpperInvariant())}}</div>
                        <h1>FORGECARE SERVICE REPORT</h1>
                        <p class="muted">{{summary}}</p>
                    </div>
                    <div class="job">
                        <div><strong>JOB</strong> {{Html(jobId)}}</div>
                        <div>Technician: {{Html(string.IsNullOrWhiteSpace(metadata.TechnicianName) ? "—" : metadata.TechnicianName)}}</div>
                        <div>Started: {{Html(session.StartedAt.ToString("yyyy-MM-dd HH:mm"))}}</div>
                        <div>Updated: {{Html(session.UpdatedAt.ToString("yyyy-MM-dd HH:mm"))}}</div>
                    </div>
                </header>

                <h2>Service information</h2>
                <div class="grid">
                    <div class="card">
                        <div class="label">Customer</div>
                        <div class="value">{{Html(string.IsNullOrWhiteSpace(metadata.CustomerName) ? "—" : metadata.CustomerName)}}</div>
                    </div>
                    <div class="card">
                        <div class="label">Device</div>
                        <div class="value">{{Html(string.IsNullOrWhiteSpace(device) ? "Not scanned" : device)}}</div>
                    </div>
                    <div class="card">
                        <div class="label">Computer</div>
                        <div class="value">{{Html(string.IsNullOrWhiteSpace(session.ComputerName) ? "—" : session.ComputerName)}}</div>
                    </div>
                    <div class="card">
                        <div class="label">Recovered storage</div>
                        <div class="value green">{{Html(session.DisplayRecovered)}}</div>
                    </div>
                </div>

                <h2>Before → After verification</h2>
                <div class="card">
                    <div class="compare-row">
                        <div><span class="label">Metric</span><br>Health score</div>
                        <div>{{healthBefore}}</div>
                        <div>{{healthCurrent}}</div>
                        <div class="delta">{{healthDelta}}</div>
                    </div>
                    <div class="compare-row">
                        <div><span class="label">Metric</span><br>System drive free</div>
                        <div>{{freeBefore}}</div>
                        <div>{{freeCurrent}}</div>
                        <div class="delta">{{freeDelta}}</div>
                    </div>
                    <div class="compare-row">
                        <div><span class="label">Metric</span><br>Startup items</div>
                        <div>{{startupBefore}}</div>
                        <div>{{startupCurrent}}</div>
                        <div class="delta">{{startupDelta}}</div>
                    </div>
                    <div class="compare-row">
                        <div class="label">Columns</div>
                        <div class="label">Before</div>
                        <div class="label">After</div>
                        <div class="label" style="text-align:right">Change</div>
                    </div>
                </div>

                <h2>Latest diagnostic findings</h2>
                <div class="grid">
                    <div class="card">
                        <div class="label">Service review</div>
                        <div class="value">{{session.LastServiceReviewCount}}</div>
                    </div>
                    <div class="card">
                        <div class="label">Large files</div>
                        <div class="value">{{session.LastLargeFileCount}}</div>
                    </div>
                    <div class="card">
                        <div class="label">Exact duplicate groups</div>
                        <div class="value">{{session.LastDuplicateGroupCount}}</div>
                    </div>
                    <div class="card">
                        <div class="label">Duplicate opportunity</div>
                        <div class="value">{{Html(session.DisplayDuplicateOpportunity)}}</div>
                    </div>
                </div>

                <h2>Technician notes</h2>
                <div class="card notes">{{notes}}</div>

                <h2>Session activity</h2>
                <div class="card">
                    <table>
                        <thead>
                            <tr>
                                <th>Time</th>
                                <th>Category</th>
                                <th>Action</th>
                                <th>Result</th>
                                <th>Metric</th>
                                <th>Detail</th>
                            </tr>
                        </thead>
                        <tbody>
                            {{actions}}
                        </tbody>
                    </table>
                </div>

                <footer>
                    Generated by ForgeCare for {{Html(company)}} · Job {{Html(jobId)}}.<br>
                    Diagnostic values are observations from the recorded session. Recovered-storage totals reflect ForgeCare-recorded cleanup actions.
                    Heuristic recommendations and health scores are decision-support indicators and are not guarantees of hardware or software condition.
                    This HTML report includes print styling and can be printed or saved as PDF from a modern browser.
                </footer>
            </main>
            </body>
            </html>
            """;
    }

    private static string FormatSigned(
        int value)
    {
        return value switch
        {
            > 0 => $"+{value}",
            < 0 => value.ToString(),
            _ => "0"
        };
    }

    private static string Html(
        string? value)
    {
        return WebUtility.HtmlEncode(
            value ?? string.Empty);
    }
}
