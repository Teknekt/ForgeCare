using System;
using System.Threading;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using ForgeCare.App.Models;
using ForgeCare.App.Services;
using Microsoft.Win32;

namespace ForgeCare.App;

public partial class MainWindow : Window
{
    private readonly SystemScanner _scanner;
    private readonly HealthScoreService _healthScoreService;
    private readonly RecommendationService _recommendationService;
    private readonly CleanupScanner _cleanupScanner;
    private readonly OptimizationService _optimizationService;
    private readonly StartupImpactService _startupImpactService;
    private readonly ResourceAnalyzerService _resourceAnalyzerService;
    private readonly ServiceAnalyzerService _serviceAnalyzerService;
    private readonly ResourceHistoryService _resourceHistoryService;
    private readonly StorageDeepScannerService _storageDeepScannerService;
    private readonly DuplicateScannerService _duplicateScannerService;
    private readonly ForgeReportService _forgeReportService;
    private readonly ForgePlanService _forgePlanService;
    private readonly ForgeWorkflowService _forgeWorkflowService;
    private readonly SafetyJournalService _safetyJournalService;
    private readonly ForgeCareSettingsService _settingsService;
    private readonly BetaDiagnosticsService _betaDiagnosticsService;
    private readonly ExternalTestPreflightService _externalTestPreflightService;
    private readonly BetaFieldTestService _betaFieldTestService;
    private readonly ReleaseIdentityService _releaseIdentityService;
    private readonly UpdateDiscoveryService _updateDiscoveryService;
    private readonly RemoteUpdateDiscoveryService _remoteUpdateDiscoveryService;
    private readonly RemoteUpdateSettingsService _remoteUpdateSettingsService;
    private readonly SecureUpdateDownloadService _secureUpdateDownloadService;
    private readonly ControlledInstallerHandoffService _controlledInstallerHandoffService;
    private readonly StabilityRecoveryService _stabilityRecoveryService;
    private readonly UxStateService _uxStateService;
    private readonly RegressionSuiteService _regressionSuiteService;
    private SecureUpdateDownloadResult? _lastSecureUpdateDownload;
    private RemoteUpdateCheckResult? _lastRemoteUpdateCheck;
    private RemoteUpdateSettings _remoteUpdateSettings = new();

    private BetaFieldTestSession? _betaFieldTestSession;

    private ForgeCareSettings _settings = new();

    private CleanupResult? _latestCleanupResult;
    private SystemSnapshot? _latestSystemSnapshot;
    private HealthResult? _latestHealthResult;
    private StartupImpactResult? _latestStartupImpactResult;
    private StorageAnalysisResult? _latestStorageAnalysisResult;
    private DuplicateScanResult? _latestDuplicateScanResult;
    private ServiceAnalysisResult? _latestServiceAnalysisResult;
    private OptimizationResult? _latestOptimizationResult;
    private ForgePlanResult? _latestForgePlanResult;
    private ResourceAnalysisResult? _latestResourceAnalysisResult;
    private ForgeWorkflowSummary? _latestWorkflowSummary;
    private CancellationTokenSource? _duplicateScanCancellation;
    private string? _lastExportedReportPath;

    public MainWindow()
    {
        InitializeComponent();

        _scanner =
            new SystemScanner();

        _healthScoreService =
            new HealthScoreService();

        _recommendationService =
            new RecommendationService();

        _cleanupScanner =
            new CleanupScanner();

        _optimizationService =
            new OptimizationService();

        _startupImpactService =
            new StartupImpactService();

        _resourceAnalyzerService =
            new ResourceAnalyzerService();

        _serviceAnalyzerService =
            new ServiceAnalyzerService();

        _resourceHistoryService =
            new ResourceHistoryService();

        _storageDeepScannerService =
            new StorageDeepScannerService();

        _duplicateScannerService =
            new DuplicateScannerService();

        _forgeReportService =
            ForgeReportService.Instance;

        _forgePlanService =
            new ForgePlanService();

        _forgeWorkflowService =
            new ForgeWorkflowService();

        _safetyJournalService =
            SafetyJournalService.Instance;

        _settingsService =
            new ForgeCareSettingsService();

        _betaDiagnosticsService =
            new BetaDiagnosticsService();

        _externalTestPreflightService =
            new ExternalTestPreflightService();

        _betaFieldTestService =
            new BetaFieldTestService();

        _releaseIdentityService =
            new ReleaseIdentityService();

        _updateDiscoveryService =
            new UpdateDiscoveryService();

        _remoteUpdateDiscoveryService =
            new RemoteUpdateDiscoveryService();

        _remoteUpdateSettingsService =
            new RemoteUpdateSettingsService();

        _secureUpdateDownloadService =
            new SecureUpdateDownloadService();

        _controlledInstallerHandoffService =
            new ControlledInstallerHandoffService();

        _stabilityRecoveryService =
            new StabilityRecoveryService();

        _uxStateService =
            new UxStateService();

        _regressionSuiteService =
            new RegressionSuiteService();

        Loaded +=
            MainWindow_Loaded;
    }



    // ============================================================
    // SAFETY & RECOVERY
    // ============================================================

    private void UpdateSafetyUi()
    {
        if (SafetyJournalListView == null)
            return;

        var journal = _safetyJournalService.GetJournal()
            .OrderByDescending(x => x.Timestamp)
            .ToList();

        var snapshots = _safetyJournalService.GetSnapshots()
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var startupManager = new StartupManagerService();
        int startupUndo = startupManager.UndoRecordCount();

        SafetyJournalListView.ItemsSource = journal.Take(200).ToList();
        SafetySnapshotListView.ItemsSource = snapshots.Take(50).ToList();

        SafetyJournalCountText.Text = journal.Count.ToString();
        SafetySnapshotCountText.Text = snapshots.Count.ToString();
        SafetyStartupUndoCountText.Text = startupUndo.ToString();

        SafetyReversibleCountText.Text =
            journal.Count(x => x.IsReversible).ToString();

        SafetyRestoreStartupButton.IsEnabled = startupUndo > 0;
        SafetyStateText.Text =
            startupUndo > 0
                ? "RECOVERY AVAILABLE"
                : "PROTECTED";
    }

    private void RefreshSafetyButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateSafetyUi();
    }

    private async void SafetyRestoreStartupButton_Click(object sender, RoutedEventArgs e)
    {
        var manager = new StartupManagerService();
        int count = manager.UndoRecordCount();

        if (count <= 0)
        {
            UpdateSafetyUi();
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            $"Restore all {count} ForgeCare startup undo record(s)?\\n\\n" +
            "ForgeCare will not overwrite an existing startup file or silently elevate permissions.",
            "ForgeCare Recovery",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (confirmation != MessageBoxResult.Yes)
            return;

        try
        {
            SafetyRestoreStartupButton.IsEnabled = false;

            _safetyJournalService.CaptureStartupSnapshot(
                "Before Safety Center startup restore",
                manager.GetUndoRecords());

            var result = await manager.RestoreAllAsync();

            _safetyJournalService.Record(
                "RECOVERY",
                "Restore startup state",
                $"{count} undo record(s)",
                result.ErrorCount == 0 ? "RESTORED" : "RESTORE WITH ERRORS",
                $"{result.RestoredCount} restored · {result.ErrorCount} errors.",
                false,
                "NONE");

            _forgeReportService.RecordStartupChange(result, isRestore: true);
            UpdateReportUi();
            UpdateWorkflowUi();

            MessageBox.Show(
                this,
                $"Recovery complete.\\n\\nRestored: {result.RestoredCount}\\nErrors: {result.ErrorCount}",
                "ForgeCare Recovery",
                MessageBoxButton.OK,
                result.ErrorCount == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            _safetyJournalService.Record(
                "RECOVERY", "Restore startup state", "Startup undo records",
                "FAILED", ex.Message, true, "RETRY RESTORE");

            MessageBox.Show(this, ex.Message, "ForgeCare Recovery Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            UpdateSafetyUi();
        }
    }

    private void ClearSafetyJournalButton_Click(object sender, RoutedEventArgs e)
    {
        var confirmation = MessageBox.Show(
            this,
            "Clear ForgeCare's action journal?\\n\\nThis does NOT remove startup undo data, snapshots, report history, Recycle Bin contents, or change Windows.",
            "Clear Action Journal",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmation != MessageBoxResult.Yes)
            return;

        _safetyJournalService.ClearJournal();
        UpdateSafetyUi();
    }

    // ============================================================
    // FORGE REPORT
    // ============================================================

    private void UpdateReportUi()
    {
        UpdateSafetyUi();
        ForgeReportSession session =
            _forgeReportService.Snapshot();

        if (!ReportJobIdTextBox.IsKeyboardFocusWithin)
            ReportJobIdTextBox.Text = session.Metadata.JobId;

        if (!ReportCustomerTextBox.IsKeyboardFocusWithin)
            ReportCustomerTextBox.Text = session.Metadata.CustomerName;

        if (!ReportDeviceLabelTextBox.IsKeyboardFocusWithin)
            ReportDeviceLabelTextBox.Text = session.Metadata.DeviceLabel;

        if (!ReportTechnicianTextBox.IsKeyboardFocusWithin)
            ReportTechnicianTextBox.Text = session.Metadata.TechnicianName;

        if (!ReportCompanyTextBox.IsKeyboardFocusWithin)
            ReportCompanyTextBox.Text = session.Metadata.CompanyName;

        if (!ReportServiceSummaryTextBox.IsKeyboardFocusWithin)
            ReportServiceSummaryTextBox.Text = session.Metadata.ServiceSummary;

        if (!ReportTechnicianNotesTextBox.IsKeyboardFocusWithin)
            ReportTechnicianNotesTextBox.Text = session.Metadata.TechnicianNotes;

        var reportArchive =
            _forgeReportService.GetArchive()
                .OrderByDescending(
                    entry =>
                        entry.ExportedAt)
                .Take(30)
                .ToList();

        ReportArchiveListView.ItemsSource =
            reportArchive;

        ReportArchiveCountText.Text =
            reportArchive.Count.ToString();

        ReportSessionText.Text =
            session.SessionId.Length >= 10
                ? session.SessionId[..10].ToUpperInvariant()
                : session.SessionId.ToUpperInvariant();

        ReportStartedText.Text =
            session.StartedAt.ToString(
                "yyyy-MM-dd HH:mm");

        ReportDeviceText.Text =
            string.IsNullOrWhiteSpace(
                session.ComputerName)
                ? "NOT SCANNED"
                : session.ComputerName;

        ReportActionsText.Text =
            session.ActionCount.ToString();

        ReportRecoveredText.Text =
            session.DisplayRecovered;

        ReportCheckpointText.Text =
            session.Checkpoints.Count.ToString();

        ReportServiceReviewText.Text =
            session.LastServiceReviewCount.ToString();

        ReportLargeFilesText.Text =
            session.LastLargeFileCount.ToString();

        ReportDuplicateGroupsText.Text =
            session.LastDuplicateGroupCount.ToString();

        ReportDuplicateOpportunityText.Text =
            session.DisplayDuplicateOpportunity;

        ReportActivityListView.ItemsSource =
            session.Actions
                .OrderByDescending(
                    action =>
                        action.Timestamp)
                .Take(80)
                .ToList();

        ForgeReportCheckpoint? before =
            session.Before;

        ForgeReportCheckpoint? current =
            session.Current;

        ReportBeforeHealthText.Text =
            before == null
                ? "--"
                : before.HealthScore.ToString();

        ReportCurrentHealthText.Text =
            current == null
                ? "--"
                : current.HealthScore.ToString();

        ReportBeforeStorageText.Text =
            before == null
                ? "--"
                : $"{before.SystemDriveFreeGb:0.0} GB";

        ReportCurrentStorageText.Text =
            current == null
                ? "--"
                : $"{current.SystemDriveFreeGb:0.0} GB";

        ReportBeforeStartupText.Text =
            before == null
                ? "--"
                : before.StartupCount.ToString();

        ReportCurrentStartupText.Text =
            current == null
                ? "--"
                : current.StartupCount.ToString();

        ReportStatusText.Text =
            session.Checkpoints.Count switch
            {
                0 =>
                    "Run System Scan to capture the BEFORE checkpoint.",

                1 when session.ActionCount <= 1 =>
                    "Initial checkpoint captured. Perform diagnostics/actions, then run System Scan again for AFTER.",

                1 =>
                    "Actions are being recorded. Run System Scan again when finished to create the AFTER comparison.",

                _ =>
                    $"Before/current comparison ready · {session.ActionCount} session events recorded."
            };
    }


    private void SaveReportDetailsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var metadata =
            new ForgeReportMetadata
            {
                JobId =
                    ReportJobIdTextBox.Text.Trim(),

                CustomerName =
                    ReportCustomerTextBox.Text.Trim(),

                DeviceLabel =
                    ReportDeviceLabelTextBox.Text.Trim(),

                TechnicianName =
                    ReportTechnicianTextBox.Text.Trim(),

                CompanyName =
                    ReportCompanyTextBox.Text.Trim(),

                ServiceSummary =
                    ReportServiceSummaryTextBox.Text.Trim(),

                TechnicianNotes =
                    ReportTechnicianNotesTextBox.Text.Trim()
            };

        _forgeReportService.UpdateMetadata(
            metadata);

        ForgeReportSession savedSession =
            _forgeReportService.Snapshot();

        ReportDetailsSavedText.Text =
            $"SAVED LOCALLY · {DateTime.Now:HH:mm:ss}";

        ReportDetailsSavedText.Foreground =
            new SolidColorBrush(
                Color.FromRgb(110, 190, 140));

        ReportStatusText.Text =
            $"Report details saved for job {savedSession.Metadata.JobId}.";

        // Do not call UpdateReportUi() here.
        // That method intentionally recomputes the workflow/report status text,
        // which previously made a successful save look like the button did nothing.
    }

    private void RefreshReportButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        UpdateReportUi();
    }

    private void NewReportSessionButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var answer =
            MessageBox.Show(
                this,
                "Start a new ForgeCare report session? The current report remains exportable only if you export it before resetting.",
                "Start New Forge Report Session",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

        if (answer !=
            MessageBoxResult.Yes)
        {
            return;
        }

        _forgeReportService.StartNewSession();

        _latestSystemSnapshot = null;
        _latestHealthResult = null;
        _latestCleanupResult = null;
        _latestStartupImpactResult = null;
        _latestStorageAnalysisResult = null;
        _latestDuplicateScanResult = null;
        _latestServiceAnalysisResult = null;
        _latestOptimizationResult = null;
        _latestForgePlanResult = null;
        _latestResourceAnalysisResult = null;

        UpdateReportUi();
        UpdateWorkflowUi();
    }

    private static string SanitizeReportFileName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Report";

        foreach (char invalid in
                 System.IO.Path.GetInvalidFileNameChars())
        {
            value =
                value.Replace(
                    invalid,
                    '-');
        }

        return value.Trim();
    }

    private ForgeReportMetadata ReadReportMetadataFromUi()
    {
        return new ForgeReportMetadata
        {
            JobId = ReportJobIdTextBox.Text.Trim(),
            CustomerName = ReportCustomerTextBox.Text.Trim(),
            DeviceLabel = ReportDeviceLabelTextBox.Text.Trim(),
            TechnicianName = ReportTechnicianTextBox.Text.Trim(),
            CompanyName = ReportCompanyTextBox.Text.Trim(),
            ServiceSummary = ReportServiceSummaryTextBox.Text.Trim(),
            TechnicianNotes = ReportTechnicianNotesTextBox.Text.Trim()
        };
    }

    private async Task<string> ExportReportToPathAsync(
        string requestedPath)
    {
        _forgeReportService.UpdateMetadata(
            ReadReportMetadataFromUi());

        string exportPath =
            Path.GetFullPath(
                requestedPath);

        if (!string.Equals(
                Path.GetExtension(exportPath),
                ".html",
                StringComparison.OrdinalIgnoreCase))
        {
            exportPath =
                Path.ChangeExtension(
                    exportPath,
                    ".html");
        }

        ReportStatusText.Text =
            $"Writing HTML report to {exportPath}";

        await _forgeReportService
            .ExportHtmlAsync(
                exportPath);

        if (!File.Exists(exportPath))
            throw new IOException(
                "ForgeCare completed the export operation, but the HTML file was not found.");

        long bytes =
            new FileInfo(
                exportPath).Length;

        if (bytes <= 0)
            throw new IOException(
                "ForgeCare created the HTML report, but the file is empty.");

        _lastExportedReportPath =
            exportPath;

        LastReportPathText.Text =
            exportPath;

        OpenLastReportButton.IsEnabled =
            true;

        UpdateReportUi();

        ReportStatusText.Text =
            $"HTML report exported · {bytes:N0} bytes";

        ReportDetailsSavedText.Text =
            $"SAVED + EXPORTED · {DateTime.Now:HH:mm:ss}";

        ReportDetailsSavedText.Foreground =
            new SolidColorBrush(
                Color.FromRgb(110, 190, 140));

        return exportPath;
    }

    private void OpenReportPath(
        string reportPath)
    {
        if (!File.Exists(reportPath))
        {
            throw new FileNotFoundException(
                "The exported ForgeCare report no longer exists.",
                reportPath);
        }

        Process.Start(
            new ProcessStartInfo
            {
                FileName =
                    reportPath,

                UseShellExecute =
                    true
            });
    }

    private async void ExportReportDirectButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            ExportReportDirectButton.IsEnabled =
                false;

            ExportReportDirectButton.Content =
                "EXPORTING...";

            ForgeReportSession session =
                _forgeReportService.Snapshot();

            string desktop =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.DesktopDirectory);

            string path =
                Path.Combine(
                    desktop,
                    $"ForgeCare-{SanitizeReportFileName(session.Metadata.JobId)}-{DateTime.Now:yyyyMMdd-HHmmss}.html");

            string exported =
                await ExportReportToPathAsync(
                    path);

            var answer =
                MessageBox.Show(
                    this,
                    $"ForgeCare wrote the HTML report directly to Desktop.{Environment.NewLine}{Environment.NewLine}" +
                    $"{exported}{Environment.NewLine}{Environment.NewLine}" +
                    "Open it now?",
                    "ForgeCare Direct HTML Export",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information,
                    MessageBoxResult.Yes);

            if (answer ==
                MessageBoxResult.Yes)
            {
                OpenReportPath(
                    exported);
            }
        }
        catch (Exception ex)
        {
            CrashLogService.Record(
                ex,
                "Direct Desktop HTML export");

            ReportStatusText.Text =
                "DIRECT HTML EXPORT FAILED";

            MessageBox.Show(
                this,
                $"Direct Desktop export failed.{Environment.NewLine}{Environment.NewLine}" +
                $"{ex.GetType().Name}: {ex.Message}",
                "ForgeCare Direct Export Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            ExportReportDirectButton.IsEnabled =
                true;

            ExportReportDirectButton.Content =
                "EXPORT DIRECT TO DESKTOP";
        }
    }

    private void OpenLastReportButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(
                    _lastExportedReportPath))
            {
                MessageBox.Show(
                    this,
                    "No report has been exported during this ForgeCare run yet.",
                    "ForgeCare Report",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            OpenReportPath(
                _lastExportedReportPath);
        }
        catch (Exception ex)
        {
            CrashLogService.Record(
                ex,
                "Open last HTML report");

            MessageBox.Show(
                this,
                ex.Message,
                "ForgeCare Report Open Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void ExportReportButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            _forgeReportService.UpdateMetadata(
                ReadReportMetadataFromUi());

            ForgeReportSession session =
                _forgeReportService.Snapshot();

            var dialog =
                new SaveFileDialog
                {
                    Title =
                        "Export ForgeCare Session Report",

                    Filter =
                        "HTML Report (*.html)|*.html",

                    DefaultExt =
                        ".html",

                    AddExtension =
                        true,

                    OverwritePrompt =
                        true,

                    FileName =
                        $"ForgeCare-{SanitizeReportFileName(session.Metadata.JobId)}-{DateTime.Now:yyyyMMdd-HHmmss}.html"
                };

            bool? dialogResult =
                dialog.ShowDialog(this);

            if (dialogResult != true)
            {
                ReportStatusText.Text =
                    $"Save dialog result: {dialogResult?.ToString() ?? "null"} · export cancelled.";

                return;
            }

            ExportReportButton.IsEnabled =
                false;

            ExportReportButton.Content =
                "EXPORTING REPORT...";

            string exported =
                await ExportReportToPathAsync(
                    dialog.FileName);

            var answer =
                MessageBox.Show(
                    this,
                    $"ForgeCare HTML report exported successfully.{Environment.NewLine}{Environment.NewLine}" +
                    $"{exported}{Environment.NewLine}{Environment.NewLine}" +
                    "Open the exported report now?",
                    "ForgeCare Report Export",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information,
                    MessageBoxResult.Yes);

            if (answer ==
                MessageBoxResult.Yes)
            {
                OpenReportPath(
                    exported);
            }
        }
        catch (Exception ex)
        {
            CrashLogService.Record(
                ex,
                "HTML report export");

            ReportStatusText.Text =
                "HTML REPORT EXPORT FAILED";

            ReportDetailsSavedText.Text =
                "EXPORT FAILED";

            ReportDetailsSavedText.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(225, 80, 80));

            MessageBox.Show(
                this,
                $"ForgeCare could not export the HTML report.{Environment.NewLine}{Environment.NewLine}" +
                $"Error type: {ex.GetType().Name}{Environment.NewLine}" +
                $"Message: {ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                $"Crash log:{Environment.NewLine}{CrashLogService.CrashLogPath}",
                "ForgeCare Report Export Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            ExportReportButton.IsEnabled =
                true;

            ExportReportButton.Content =
                "EXPORT HTML REPORT";
        }
    }



    private void UpdatePersistentWorkflowBar(ForgeWorkflowSummary summary)
    {
        PersistentWorkflowStageText.Text = summary.Stage;
        PersistentWorkflowStripText.Text = summary.StageStrip;
        PersistentWorkflowCurrentText.Text = summary.CurrentTitle;
        PersistentWorkflowProgressText.Text = $"{summary.ProgressPercent:0}%";
        PersistentWorkflowProgressBar.Value = summary.ProgressPercent;
        PersistentContinueButton.IsEnabled = summary.CanContinue;
        PersistentContinueButton.Content = summary.IsComplete ? "VIEW REPORT →" : "CONTINUE →";

        if (summary.IsComplete)
            ShowForgeCompletion();
        else
            ForgeCompletionBorder.Visibility = Visibility.Collapsed;
    }

    private void PersistentContinueButton_Click(object sender, RoutedEventArgs e)
    {
        if (_latestWorkflowSummary == null)
            UpdateWorkflowUi();

        if (_latestWorkflowSummary != null)
            SelectMainTab(_latestWorkflowSummary.CurrentRoute);
    }

    private void OpenWorkflowButton_Click(object sender, RoutedEventArgs e)
    {
        SelectMainTab("WORKFLOW");
    }

    private void ShowForgeCompletion()
    {
        var session = _forgeReportService.Snapshot();
        var before = session.Before;
        var current = session.Current;

        CompletionHealthText.Text =
            before != null && current != null
                ? $"{before.HealthScore}  →  {current.HealthScore}" : "--";
        CompletionStorageText.Text = session.DisplayRecovered;
        CompletionStartupText.Text =
            before != null && current != null
                ? $"{before.StartupCount}  →  {current.StartupCount}" : "--";
        CompletionActionsText.Text = session.SuccessfulActionCount.ToString();
        ForgeCompletionBorder.Visibility = Visibility.Visible;
    }

    private void CompletionViewReportButton_Click(object sender, RoutedEventArgs e)
    {
        SelectMainTab("REPORTS");
    }

    // ============================================================
    // GUIDED SERVICE WORKFLOW
    // ============================================================

    private void UpdateWorkflowUi()
    {
        ForgeReportSession reportSession =
            _forgeReportService.Snapshot();

        var steps =
            _forgeWorkflowService.Build(
                hasProfile:
                    _latestSystemSnapshot != null,

                hasDeepAnalysis:
                    _latestResourceAnalysisResult != null,

                hasServiceAnalysis:
                    _latestServiceAnalysisResult != null,

                hasStorageAnalysis:
                    _latestStorageAnalysisResult != null,

                hasOptimizationAnalysis:
                    _latestOptimizationResult != null &&
                    _latestStartupImpactResult != null,

                hasDuplicateScan:
                    _latestDuplicateScanResult != null,

                hasForgePlan:
                    _latestForgePlanResult != null,

                reportSession:
                    reportSession);

        WorkflowStepListView.ItemsSource =
            steps;

        _latestWorkflowSummary =
            _forgeWorkflowService.Summarize(steps);

        UpdatePersistentWorkflowBar(_latestWorkflowSummary);

        int required =
            steps.Count(step =>
                step.IsRequired);

        int complete =
            steps.Count(step =>
                step.IsRequired &&
                (step.Status == "COMPLETE" ||
                 step.Status == "READY"));

        WorkflowProgressText.Text =
            $"{complete}/{required}";

        WorkflowStateText.Text =
            complete == required
                ? "READY TO DELIVER"
                : complete >= 4
                    ? "FORGING"
                    : complete >= 1
                        ? "IN PROGRESS"
                        : "READY";

        ForgeWorkflowStep? next =
            steps.FirstOrDefault(step =>
                step.Status == "NEXT");

        if (next == null)
        {
            next =
                steps.FirstOrDefault(step =>
                    step.Status == "READY");
        }

        if (next == null)
        {
            next =
                steps.FirstOrDefault(step =>
                    step.Status == "OPTIONAL");
        }

        WorkflowNextStepText.Text =
            next == null
                ? "Workflow complete. Review the report."
                : $"{next.Number}. {next.Title}";

        WorkflowNextRouteText.Text =
            next == null
                ? "REPORTS"
                : next.Route;

        WorkflowNextButton.IsEnabled =
            next != null;

        WorkflowStatusText.Text =
            complete == required
                ? "Required service workflow complete. Review and export the report."
                : "ForgeCare guides the job but never bypasses the safety controls inside each subsystem.";
        UpdateTechnicianGuidanceUi();
    }

    private void RefreshWorkflowButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        UpdateWorkflowUi();
    }

    private void WorkflowNextButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        string route =
            WorkflowNextRouteText.Text;

        SelectMainTab(
            route);

        WorkflowStatusText.Text =
            $"Opened {route}. Complete the indicated action, then return to WORKFLOW or press REFRESH WORKFLOW.";
    }

    private void StartServiceWorkflowButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var answer =
            MessageBox.Show(
                this,
                "Start a fresh guided service workflow? This starts a new Forge Report session and clears the current in-memory analysis state. It does not undo changes already made to Windows.",
                "Start New ForgeCare Workflow",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

        if (answer !=
            MessageBoxResult.Yes)
        {
            return;
        }

        _forgeReportService.StartNewSession();

        _latestSystemSnapshot = null;
        _latestHealthResult = null;
        _latestCleanupResult = null;
        _latestStartupImpactResult = null;
        _latestStorageAnalysisResult = null;
        _latestDuplicateScanResult = null;
        _latestServiceAnalysisResult = null;
        _latestOptimizationResult = null;
        _latestForgePlanResult = null;
        _latestResourceAnalysisResult = null;

        UpdateReportUi();
        UpdateWorkflowUi();

        SelectMainTab(
            "DASHBOARD");
    }

    private void SelectMainTab(
        string header)
    {
        foreach (object item in
                 MainTabs.Items)
        {
            if (item is
                System.Windows.Controls.TabItem tabItem &&
                string.Equals(
                    tabItem.Header?.ToString(),
                    header,
                    StringComparison.OrdinalIgnoreCase))
            {
                MainTabs.SelectedItem =
                    tabItem;

                if (ShellStatusText != null)
                {
                    ShellStatusText.Text =
                        $"View: {header} · {DateTime.Now:HH:mm:ss}";
                }

                if (CurrentViewTitleText != null)
                    CurrentViewTitleText.Text = header;

                _uxStateService.SaveLastTab(header);

                return;
            }
        }
    }

    // ============================================================
    // REGRESSION / FIELD-TEST HARDENING
    // ============================================================

    private void RunRegressionSuiteButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        RunRegressionSuiteButton.IsEnabled =
            false;

        RegressionSuiteStateText.Text =
            "RUNNING";

        RegressionSuiteDetailText.Text =
            "Running read-only ForgeCare regression checks...";

        try
        {
            RegressionSuiteResult result =
                _regressionSuiteService.Run();

            RegressionSuiteListView.ItemsSource =
                result.Checks;

            RegressionSuitePassText.Text =
                result.Passed.ToString();

            RegressionSuiteWarnText.Text =
                result.Warnings.ToString();

            RegressionSuiteFailText.Text =
                result.Failed.ToString();

            RegressionSuiteStateText.Text =
                result.Overall;

            RegressionSuiteDetailText.Text =
                $"{result.Checks.Count} checks completed · {result.StartedAt:HH:mm:ss} → {result.CompletedAt:HH:mm:ss}";

            RegressionSuiteStateText.Foreground =
                result.Failed > 0
                    ? new SolidColorBrush(
                        Color.FromRgb(225, 80, 80))
                    : result.Warnings > 0
                        ? new SolidColorBrush(
                            Color.FromRgb(225, 170, 60))
                        : new SolidColorBrush(
                            Color.FromRgb(110, 190, 140));
        }
        catch (Exception ex)
        {
            CrashLogService.Record(
                ex,
                "Regression suite");

            RegressionSuiteStateText.Text =
                "SUITE FAILED";

            RegressionSuiteDetailText.Text =
                ex.Message;
        }
        finally
        {
            RunRegressionSuiteButton.IsEnabled =
                true;
        }
    }

    // ============================================================
    // UX / PRODUCT NAVIGATION
    // ============================================================

    private void OpenCommandPaletteButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        CommandPaletteBorder.Visibility = Visibility.Visible;
        CommandPaletteSearchTextBox.Text = string.Empty;
        UpdateCommandPaletteResults();
        CommandPaletteSearchTextBox.Focus();
    }

    private void CloseCommandPaletteButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        CommandPaletteBorder.Visibility = Visibility.Collapsed;
    }

    private void CommandPaletteSearchTextBox_TextChanged(
        object sender,
        System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateCommandPaletteResults();
    }

    private void UpdateCommandPaletteResults()
    {
        if (CommandPaletteListView == null)
            return;

        string query = CommandPaletteSearchTextBox.Text.Trim();

        var destinations = new[]
        {
            new NavigationDestination { Header = "DASHBOARD", Shortcut = "Ctrl+1", Description = "Overview, health and next action" },
            new NavigationDestination { Header = "ANALYSIS", Shortcut = "Ctrl+2", Description = "Deep system analysis" },
            new NavigationDestination { Header = "SERVICES", Description = "Windows service intelligence" },
            new NavigationDestination { Header = "STORAGE", Description = "Storage and large-file review" },
            new NavigationDestination { Header = "OPTIMIZE", Description = "Optimization findings" },
            new NavigationDestination { Header = "FORGE PLAN", Description = "Technician-reviewed plan" },
            new NavigationDestination { Header = "WORKFLOW", Shortcut = "Ctrl+3", Description = "Guided service workflow" },
            new NavigationDestination { Header = "SAFETY", Description = "Recovery and reversible state" },
            new NavigationDestination { Header = "REPORTS", Shortcut = "Ctrl+4", Description = "Professional service report" },
            new NavigationDestination { Header = "TOOLS", Shortcut = "Ctrl+5", Description = "Diagnostics and recovery" },
            new NavigationDestination { Header = "SETTINGS", Shortcut = "Ctrl+,", Description = "Profile, distribution and updates" }
        };

        CommandPaletteListView.ItemsSource =
            string.IsNullOrWhiteSpace(query)
                ? destinations
                : destinations.Where(x =>
                    x.Header.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    x.Description.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void CommandPaletteListView_MouseDoubleClick(
        object sender,
        System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CommandPaletteListView.SelectedItem is NavigationDestination destination)
        {
            CommandPaletteBorder.Visibility = Visibility.Collapsed;
            SelectMainTab(destination.Header);
        }
    }

    // ============================================================
    // KEYBOARD NAVIGATION
    // ============================================================

    private void MainWindow_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.Escape &&
            CommandPaletteBorder?.Visibility == Visibility.Visible)
        {
            CommandPaletteBorder.Visibility = Visibility.Collapsed;
            e.Handled = true;
            return;
        }

        if ((Keyboard.Modifiers &
             ModifierKeys.Control) == 0)
        {
            return;
        }

        if (e.Key == Key.K)
        {
            OpenCommandPaletteButton_Click(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        string? route =
            e.Key switch
            {
                Key.D1 or Key.NumPad1 => "DASHBOARD",
                Key.D2 or Key.NumPad2 => "ANALYSIS",
                Key.D3 or Key.NumPad3 => "WORKFLOW",
                Key.D4 or Key.NumPad4 => "REPORTS",
                Key.D5 or Key.NumPad5 => "TOOLS",
                Key.OemComma => "SETTINGS",
                _ => null
            };

        if (route == null)
            return;

        SelectMainTab(route);

        e.Handled =
            true;
    }

    // ============================================================
    // FORGE PLAN / ORCHESTRATION
    // ============================================================

    private void BuildForgePlanButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _latestForgePlanResult =
            _forgePlanService.Build(
                _latestSystemSnapshot,
                _latestHealthResult,
                _latestCleanupResult,
                _latestStartupImpactResult,
                _latestStorageAnalysisResult,
                _latestDuplicateScanResult,
                _latestServiceAnalysisResult,
                _latestOptimizationResult);

        ForgePlanListView.ItemsSource =
            _latestForgePlanResult.Items;

        UpdateWorkflowUi();

        ForgePlanTotalText.Text =
            _latestForgePlanResult.TotalCount.ToString();

        ForgePlanLowText.Text =
            _latestForgePlanResult.LowRiskCount.ToString();

        ForgePlanMediumText.Text =
            _latestForgePlanResult.MediumRiskCount.ToString();

        ForgePlanHighText.Text =
            _latestForgePlanResult.HighRiskCount.ToString();

        ForgePlanSelectedText.Text =
            _latestForgePlanResult.SelectedCount.ToString();

        ForgePlanStatusText.Text =
            _latestForgePlanResult.TotalCount == 0
                ? "No plan items yet. Run System Scan and the relevant analyzers first."
                : $"{_latestForgePlanResult.TotalCount} correlated plan items ready. " +
                  "Selected actions are routed into ForgeCare’s existing review and dry-run workflows; technician safety gates are never bypassed.";

        ExecuteForgePlanButton.IsEnabled =
            _latestForgePlanResult.Items.Any(item =>
                item.IsSelected &&
                item.CanExecute);
    }

    private void ForgePlanSelection_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_latestForgePlanResult == null)
            return;

        ForgePlanSelectedText.Text =
            _latestForgePlanResult.Items.Count(item =>
                item.IsSelected).ToString();

        ExecuteForgePlanButton.IsEnabled =
            _latestForgePlanResult.Items.Any(item =>
                item.IsSelected &&
                item.CanExecute);
    }

    private void ExecuteForgePlanButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_latestForgePlanResult == null)
            return;

        var selected =
            _latestForgePlanResult.Items
                .Where(item =>
                    item.IsSelected &&
                    item.CanExecute)
                .OrderByDescending(item =>
                    item.Priority)
                .ToList();

        if (selected.Count == 0)
        {
            MessageBox.Show(
                this,
                "Select at least one executable Forge Plan item.",
                "ForgeCare Forge Plan",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        // 8B deliberately opens ONE existing safety workflow at a time.
        // After it closes, rebuild the plan and continue with the next item.
        var next =
            selected.First();

        ForgePlanStatusText.Text =
            $"Routing '{next.Title}' into its existing ForgeCare safety workflow.";

        switch (next.Route)
        {
            case "CLEANUP":
                if (_latestCleanupResult != null)
                {
                    var cleanupWindow =
                        new CleanupReviewWindow(
                            _latestCleanupResult.Items)
                        {
                            Owner = this
                        };

                    cleanupWindow.ShowDialog();
                }
                break;

            case "DUPLICATES":
                if (_latestDuplicateScanResult != null)
                {
                    var duplicateWindow =
                        new DuplicateReviewWindow(
                            _latestDuplicateScanResult.Groups)
                        {
                            Owner = this
                        };

                    duplicateWindow.ShowDialog();
                }
                break;

            case "STORAGE":
                if (_latestStorageAnalysisResult != null)
                {
                    var storageWindow =
                        new StorageCleanupReviewWindow(
                            _latestStorageAnalysisResult.LargeFiles)
                        {
                            Owner = this
                        };

                    storageWindow.ShowDialog();
                }
                break;

            case "STARTUP":
                if (_latestStartupImpactResult != null)
                {
                    var startupWindow =
                        new StartupReviewWindow(
                            _latestStartupImpactResult.Items)
                        {
                            Owner = this
                        };

                    startupWindow.ShowDialog();
                }
                break;
        }

        UpdateReportUi();
        UpdateWorkflowUi();

        ForgePlanStatusText.Text =
            "Safety workflow closed. Re-run BUILD / REFRESH FORGE PLAN to refresh findings before continuing.";
    }

    // ============================================================
    // SYSTEM SCAN
    // ============================================================

    private async void ScanButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            ScanButton.IsEnabled = false;

            ScanButton.Content =
                "FORGING SYSTEM PROFILE...";

            ScanStatusText.Text =
                "Inspecting system...";

            HealthScoreText.Text =
                "--";

            HealthRatingText.Text =
                "SCANNING";

            StartupListView.ItemsSource =
                null;

            RecommendationListView.ItemsSource =
                null;

            StartupListFooter.Text =
                "Inspecting Windows startup sources...";

            var snapshot =
                await _scanner.ScanAsync();

            var health =
                _healthScoreService.Calculate(
                    snapshot);

            var recommendations =
                _recommendationService.Generate(
                    snapshot,
                    health);

            // Keep the latest system profile available for the
            // Optimize Engine. Optimization never runs against
            // guessed or stale placeholder data.
            _latestSystemSnapshot =
                snapshot;

            _latestHealthResult =
                health;

            _forgeReportService.RecordSystemScan(
                snapshot,
                health);

            UpdateReportUi();
            UpdateWorkflowUi();

            OptimizationProfileText.Text =
                "SYSTEM PROFILE READY";

            OptimizationStatusText.Text =
                "System profile ready. Run optimization analysis when you are ready.";

            ComputerNameText.Text =
                snapshot.ComputerName;

            OperatingSystemText.Text =
                snapshot.OperatingSystem;

            ProcessorText.Text =
                snapshot.ProcessorName;

            MemoryText.Text =
                $"{snapshot.AvailableMemoryGb:0.0} GB free / " +
                $"{snapshot.TotalMemoryGb:0.0} GB";

            StorageText.Text =
                $"{snapshot.SystemDriveFreeGb:0.0} GB free / " +
                $"{snapshot.SystemDriveTotalGb:0.0} GB";

            HealthScoreText.Text =
                health.Score.ToString();

            HealthRatingText.Text =
                health.Rating;

            HealthRatingText.Foreground =
                GetHealthBrush(
                    health.Score);

            StorageStatusText.Text =
                health.StorageStatus;

            StorageDetailText.Text =
                $"{health.StorageFreePercent:0.0}% free";

            StorageStatusText.Foreground =
                GetStatusBrush(
                    health.StorageStatus);

            MemoryStatusText.Text =
                health.MemoryStatus;

            MemoryDetailText.Text =
                $"{health.MemoryAvailablePercent:0.0}% available";

            MemoryStatusText.Foreground =
                GetStatusBrush(
                    health.MemoryStatus);

            StartupStatusText.Text =
                health.StartupStatus;

            StartupDetailText.Text =
                $"{health.StartupCount} startup items";

            StartupStatusText.Foreground =
                GetStatusBrush(
                    health.StartupStatus);

            StartupListView.ItemsSource =
                snapshot.StartupItems;

            StartupListTitle.Text =
                $"STARTUP ITEMS ({snapshot.StartupItems.Count})";

            StartupListFooter.Text =
                $"{snapshot.StartupItems.Count} startup items detected";

            RecommendationListView.ItemsSource =
                recommendations;

            ScanStatusText.Text =
                $"Forge completed {snapshot.ScanTime:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "ForgeCare Scan Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            ScanStatusText.Text =
                "Forge interrupted.";

            HealthRatingText.Text =
                "SCAN FAILED";

            HealthRatingText.Foreground =
                GetHealthBrush(0);

            StartupListFooter.Text =
                "Startup inspection failed.";
        }
        finally
        {
            ScanButton.IsEnabled = true;

            ScanButton.Content =
                "RUN SYSTEM SCAN";
        }
    }




    // ============================================================
    // PRODUCT IDENTITY / RELEASE POLISH
    // ============================================================

    private void UpdateProductIdentityUi()
    {
        Assembly assembly =
            Assembly.GetExecutingAssembly();

        string informationalVersion =
            assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";

        string displayVersion =
            informationalVersion
                .Split('+')[0]
                .Trim();

        string channel =
            displayVersion.Contains(
                "alpha",
                StringComparison.OrdinalIgnoreCase)
                ? "DEVELOPMENT BUILD"
                : displayVersion.Contains(
                    "beta",
                    StringComparison.OrdinalIgnoreCase)
                    ? "TEST CHANNEL"
                    : "STABLE";

        string versionLabel =
            displayVersion.StartsWith(
                "v",
                StringComparison.OrdinalIgnoreCase)
                ? displayVersion
                : $"v{displayVersion}";

        HeaderVersionText.Text =
            versionLabel;

        SettingsProductVersionText.Text =
            versionLabel;

        SettingsBuildChannelText.Text =
            channel;

        SettingsRuntimeText.Text =
            RuntimeInformation.FrameworkDescription;

        SettingsArchitectureText.Text =
            $"{RuntimeInformation.ProcessArchitecture} · {RuntimeInformation.OSArchitecture}";

        SettingsDistributionText.Text =
            Environment.Is64BitProcess
                ? "SELF-CONTAINED WIN-X64 READY"
                : "DEVELOPMENT / NON-X64 PROCESS";
    }

    private void UpdateProfileReadinessUi()
    {
        bool hasTechnician =
            !string.IsNullOrWhiteSpace(
                SettingsTechnicianTextBox.Text);

        bool hasCompany =
            !string.IsNullOrWhiteSpace(
                SettingsCompanyTextBox.Text);

        bool ready =
            hasTechnician &&
            hasCompany;

        SettingsIdentityStateText.Text =
            ready
                ? "PROFILE READY"
                : "PROFILE INCOMPLETE";

        SettingsIdentityStateText.Foreground =
            ready
                ? new SolidColorBrush(
                    Color.FromRgb(110, 190, 140))
                : new SolidColorBrush(
                    Color.FromRgb(225, 170, 60));

        SettingsProfileNoticeTitle.Text =
            ready
                ? "TECHNICIAN IDENTITY READY"
                : "FIRST-RUN CHECK";

        SettingsProfileNoticeText.Text =
            ready
                ? "Technician and company identity are configured. New report sessions can inherit these defaults automatically."
                : "Add at least a technician name and company/brand before using ForgeCare for customer-facing reports.";

        SettingsProfileNoticeBorder.BorderBrush =
            ready
                ? new SolidColorBrush(
                    Color.FromRgb(55, 88, 68))
                : new SolidColorBrush(
                    Color.FromRgb(82, 70, 42));
    }

    // ============================================================
    // STABILITY & RECOVERY
    // ============================================================

    private void UpdateStabilityRecoveryUi()
    {
        if (RecoveryStateText == null)
            return;

        StabilityRecoveryResult result =
            _stabilityRecoveryService.Inspect();

        RecoveryStateText.Text =
            result.State;

        RecoveryPreviousSessionText.Text =
            result.PreviousSessionUnclean
                ? "UNCLEAN SHUTDOWN DETECTED"
                : "CLEAN / NO FLAG";

        RecoveryPartialCountText.Text =
            result.StalePartialFileCount.ToString();

        RecoveryStagingCountText.Text =
            result.StaleStagingDirectoryCount.ToString();

        RecoveryBytesText.Text =
            result.RecoverableSizeText;

        RecoveryFindingListView.ItemsSource =
            result.Findings;

        CleanRecoveryFilesButton.IsEnabled =
            result.StalePartialFileCount > 0 ||
            result.StaleStagingDirectoryCount > 0;

        RecoveryStateText.Foreground =
            result.State switch
            {
                "HEALTHY" =>
                    new SolidColorBrush(
                        Color.FromRgb(110, 190, 140)),

                "RECOVERY AVAILABLE" =>
                    new SolidColorBrush(
                        Color.FromRgb(225, 170, 60)),

                _ =>
                    new SolidColorBrush(
                        Color.FromRgb(225, 80, 80))
            };

        RecoveryStatusText.Text =
            result.PreviousSessionUnclean
                ? "Review crash diagnostics before continuing system-changing work."
                : "Recovery inspection complete.";
    }

    private void RunRecoveryInspectionButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        UpdateStabilityRecoveryUi();

        RecoveryStatusText.Text +=
            $" · {DateTime.Now:HH:mm:ss}";
    }

    private void CleanRecoveryFilesButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        MessageBoxResult answer =
            MessageBox.Show(
                this,
                "Remove stale ForgeCare transient files only? This cleanup is limited to old update .partial files and old diagnostic bundle staging folders. Technician settings, reports, safety data and verified installers are not removed.",
                "ForgeCare Recovery Cleanup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        StabilityRecoveryResult result =
            _stabilityRecoveryService.CleanSafeTransientFiles();

        UpdateStabilityRecoveryUi();

        RecoveryStatusText.Text =
            $"SAFE TRANSIENT CLEANUP COMPLETE · {DateTime.Now:HH:mm:ss} · remaining {result.RecoverableSizeText}";
    }

    // ============================================================
    // BETA READINESS / DIAGNOSTICS
    // ============================================================

    private void UpdateBetaDiagnosticsUi()
    {
        if (BetaEnvironmentText == null)
            return;

        BetaEnvironmentText.Text =
            _betaDiagnosticsService.GetEnvironmentSummary();

        BetaCrashLogText.Text =
            File.Exists(CrashLogService.CrashLogPath)
                ? CrashLogService.CrashLogPath
                : "No crash log has been created yet.";

        BetaDataRootText.Text =
            _betaDiagnosticsService.DataRoot;

        BetaStatusText.Text =
            "READY · diagnostics are local-only";
    }

    private void RefreshBetaDiagnosticsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        UpdateBetaDiagnosticsUi();
        BetaStatusText.Text =
            $"REFRESHED · {DateTime.Now:HH:mm:ss}";
    }

    private void OpenDiagnosticsFolderButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Directory.CreateDirectory(CrashLogService.DiagnosticsRoot);

        Process.Start(new ProcessStartInfo
        {
            FileName = CrashLogService.DiagnosticsRoot,
            UseShellExecute = true
        });
    }

    private void ExportDebugBundleButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Title = "Export ForgeCare Debug Bundle",
                Filter = "ZIP Archive (*.zip)|*.zip",
                DefaultExt = ".zip",
                AddExtension = true,
                FileName = $"ForgeCare-DebugBundle-{DateTime.Now:yyyyMMdd-HHmmss}.zip"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            string output =
                _betaDiagnosticsService.ExportDebugBundle(dialog.FileName);

            BetaStatusText.Text =
                $"DEBUG BUNDLE EXPORTED · {DateTime.Now:HH:mm:ss}";

            MessageBox.Show(
                this,
                $"ForgeCare debug bundle created successfully.{Environment.NewLine}{Environment.NewLine}{output}",
                "ForgeCare Support Diagnostics",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            CrashLogService.Record(ex, "Debug bundle export");
            BetaStatusText.Text = "DEBUG BUNDLE EXPORT FAILED";

            MessageBox.Show(
                this,
                ex.Message,
                "ForgeCare Debug Bundle Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // ============================================================
    // FIELD TEST / SUPPORT HARDENING
    // ============================================================

    private void LoadBetaFieldTestUi()
    {
        if (BetaFieldStepListView == null)
            return;

        _betaFieldTestSession =
            _betaFieldTestService.Load();

        if (_betaFieldTestSession == null)
        {
            BetaFieldStateText.Text =
                "NO ACTIVE FIELD TEST";

            BetaFieldStepListView.ItemsSource =
                null;

            BetaFieldSessionText.Text =
                "Start a controlled field-test session on this machine.";

            return;
        }

        BetaFieldStateText.Text =
            _betaFieldTestSession.OverallStatus;

        BetaFieldSessionText.Text =
            $"{_betaFieldTestSession.BuildVersion} · {_betaFieldTestSession.ComputerName} · started {_betaFieldTestSession.DisplayStarted}";

        BetaFieldStepListView.ItemsSource =
            _betaFieldTestSession.Steps;
    }

    private void StartBetaFieldTestButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        string tester =
            SettingsTechnicianTextBox.Text.Trim();

        _betaFieldTestSession =
            _betaFieldTestService.StartNew(
                tester);

        LoadBetaFieldTestUi();

        BetaFieldStatusText.Text =
            $"FIELD TEST STARTED · {DateTime.Now:HH:mm:ss}";
    }

    private void BetaFieldStepPassButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SetSelectedBetaFieldStep(
            "PASS");
    }

    private void BetaFieldStepWarnButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SetSelectedBetaFieldStep(
            "WARN");
    }

    private void BetaFieldStepFailButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SetSelectedBetaFieldStep(
            "FAIL");
    }

    private void SetSelectedBetaFieldStep(
        string status)
    {
        if (_betaFieldTestSession == null ||
            BetaFieldStepListView.SelectedItem
                is not BetaFieldTestStep selected)
        {
            BetaFieldStatusText.Text =
                "Select a field-test step first.";

            return;
        }

        selected.Status =
            status;

        selected.Detail =
            BetaFieldStepNoteTextBox.Text.Trim();

        _betaFieldTestService.Save(
            _betaFieldTestSession);

        BetaFieldStepListView.Items.Refresh();

        BetaFieldStatusText.Text =
            $"{selected.Title} → {status}";
    }

    private void CompleteBetaFieldTestButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_betaFieldTestSession == null)
            return;

        string overall =
            _betaFieldTestSession.Steps.Any(
                step =>
                    step.Status == "FAIL")
                ? "FAIL"
                : _betaFieldTestSession.Steps.Any(
                    step =>
                        step.Status == "WARN" ||
                        step.Status == "PENDING")
                    ? "PASS WITH WARNINGS"
                    : "PASS";

        _betaFieldTestService.Complete(
            _betaFieldTestSession,
            overall,
            BetaFieldOverallNotesTextBox.Text.Trim());

        LoadBetaFieldTestUi();

        BetaFieldStatusText.Text =
            $"FIELD TEST COMPLETE · {overall}";
    }

    private void ExportBetaIssueButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            string description =
                BetaIssueDescriptionTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(description))
            {
                MessageBox.Show(
                    this,
                    "Add a short issue description before exporting the issue package.",
                    "ForgeCare Support Issue",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            string build =
                Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion
                ?? "unknown";

            var issue =
                new BetaIssueReport
                {
                    BuildVersion =
                        build,

                    Area =
                        BetaIssueAreaTextBox.Text.Trim(),

                    Severity =
                        BetaIssueSeverityTextBox.Text.Trim(),

                    Description =
                        description,

                    ReproductionSteps =
                        BetaIssueReproductionTextBox.Text.Trim(),

                    ExpectedResult =
                        BetaIssueExpectedTextBox.Text.Trim(),

                    ActualResult =
                        BetaIssueActualTextBox.Text.Trim()
                };

            var dialog =
                new SaveFileDialog
                {
                    Title =
                        "Export ForgeCare Support Issue Package",

                    Filter =
                        "ZIP Archive (*.zip)|*.zip",

                    DefaultExt =
                        ".zip",

                    AddExtension =
                        true,

                    FileName =
                        $"ForgeCare-Issue-{issue.IssueId}.zip"
                };

            if (dialog.ShowDialog(this) != true)
                return;

            string output =
                _betaFieldTestService.ExportIssuePackage(
                    issue,
                    dialog.FileName,
                    _betaDiagnosticsService);

            BetaIssueStatusText.Text =
                $"ISSUE PACKAGE EXPORTED · {DateTime.Now:HH:mm:ss}";

            MessageBox.Show(
                this,
                $"ForgeCare issue package created.{Environment.NewLine}{Environment.NewLine}{output}",
                "ForgeCare Support Issue",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            CrashLogService.Record(
                ex,
                "Beta issue package export");

            BetaIssueStatusText.Text =
                "ISSUE PACKAGE EXPORT FAILED";

            MessageBox.Show(
                this,
                ex.Message,
                "ForgeCare Support Issue Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // ============================================================
    // SPRINT 13A — FIRST EXTERNAL MACHINE TEST
    // ============================================================

    private void UpdateExternalTestPreflightUi()
    {
        if (ExternalTestCheckListView == null)
            return;

        ExternalTestPreflightResult result =
            _externalTestPreflightService.Run();

        ExternalTestCheckListView.ItemsSource =
            result.Checks;

        ExternalTestStateText.Text =
            result.State;

        ExternalTestPassText.Text =
            result.Passed.ToString();

        ExternalTestWarningText.Text =
            result.Warnings.ToString();

        ExternalTestFailText.Text =
            result.Failed.ToString();

        ExternalTestStateText.Foreground =
            result.Failed > 0
                ? new SolidColorBrush(
                    Color.FromRgb(225, 80, 80))
                : result.Warnings > 0
                    ? new SolidColorBrush(
                        Color.FromRgb(225, 170, 60))
                    : new SolidColorBrush(
                        Color.FromRgb(110, 190, 140));

        ExternalTestStatusText.Text =
            result.IsReady
                ? "Preflight complete. ForgeCare is ready for controlled external-machine testing."
                : "Preflight found blocking conditions. Review failed checks before external testing.";
    }

    private void RunExternalPreflightButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        UpdateExternalTestPreflightUi();

        ExternalTestStatusText.Text +=
            $" · {DateTime.Now:HH:mm:ss}";
    }

    private void OpenBetaChecklistButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            string path =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "BETA_TEST_CHECKLIST.md");

            if (!File.Exists(path))
            {
                MessageBox.Show(
                    this,
                    "The field-test checklist was not found beside the running ForgeCare build.",
                    "ForgeCare Field Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            Process.Start(
                new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
        }
        catch (Exception ex)
        {
            CrashLogService.Record(
                ex,
                "Open beta checklist");

            MessageBox.Show(
                this,
                ex.Message,
                "ForgeCare Field Test Checklist",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // ============================================================
    // SPRINT 12B — TECHNICIAN UX / GUIDED ACTIONS
    // ============================================================

    private void UpdateTechnicianGuidanceUi()
    {
        if (GuidedActionTitleText == null)
            return;

        ForgeReportSession session =
            _forgeReportService.Snapshot();

        ForgeReportAction? latestAction =
            session.Actions
                .OrderByDescending(action => action.Timestamp)
                .FirstOrDefault();

        if (latestAction == null)
        {
            LastResultTitleText.Text = "No completed service action yet";
            LastResultDetailText.Text = "Run a System Scan to begin the technician result feed.";
            LastResultMetricText.Text = "READY";
            LastResultMetricText.Foreground =
                new SolidColorBrush(Color.FromRgb(140, 148, 157));
        }
        else
        {
            LastResultTitleText.Text = latestAction.Title;
            LastResultDetailText.Text =
                string.IsNullOrWhiteSpace(latestAction.Detail)
                    ? latestAction.Result
                    : latestAction.Detail;
            LastResultMetricText.Text =
                string.IsNullOrWhiteSpace(latestAction.Metric)
                    ? latestAction.Result
                    : latestAction.Metric;
            LastResultMetricText.Foreground =
                latestAction.IsSuccess
                    ? new SolidColorBrush(Color.FromRgb(110, 190, 140))
                    : new SolidColorBrush(Color.FromRgb(225, 170, 60));
        }

        if (_latestSystemSnapshot == null)
        {
            SetGuidedAction("SCAN", "Capture system baseline",
                "ForgeCare needs a current system profile before it can recommend deeper technician actions.",
                "DASHBOARD", "RUN SYSTEM SCAN");
            return;
        }

        if (_latestResourceAnalysisResult == null)
        {
            SetGuidedAction("ANALYZE", "Run Deep System Analysis",
                "Capture live CPU, memory and process pressure before planning optimization work.",
                "ANALYSIS", "OPEN ANALYSIS");
            return;
        }

        if (_latestServiceAnalysisResult == null)
        {
            SetGuidedAction("ANALYZE", "Review Windows services",
                "Service Intelligence adds contextual service findings to the Forge Plan.",
                "SERVICES", "OPEN SERVICES");
            return;
        }

        if (_latestStorageAnalysisResult == null)
        {
            SetGuidedAction("ANALYZE", "Inspect storage opportunities",
                "Run Storage Deep Scan to find large-file opportunities before building the Forge Plan.",
                "STORAGE", "OPEN STORAGE");
            return;
        }

        if (_latestOptimizationResult == null)
        {
            SetGuidedAction("PLAN", "Generate optimization findings",
                "ForgeCare has enough diagnostic context to calculate optimization opportunities.",
                "OPTIMIZE", "OPEN OPTIMIZE");
            return;
        }

        if (_latestForgePlanResult == null)
        {
            SetGuidedAction("PLAN", "Build the Forge Plan",
                "Correlate diagnostics into a technician-reviewed plan. No actions execute automatically.",
                "FORGE PLAN", "OPEN FORGE PLAN");
            return;
        }

        if (_latestWorkflowSummary?.IsComplete == true)
        {
            SetGuidedAction("REPORT", "Deliver verified service report",
                "Required workflow steps are complete. Review the before/after result and export the customer report.",
                "REPORTS", "OPEN REPORTS");
            return;
        }

        SetGuidedAction(
            _latestWorkflowSummary?.Stage ?? "FORGE",
            _latestWorkflowSummary?.CurrentTitle ?? "Continue guided service workflow",
            "ForgeCare selected the next recommended technician step from the active workflow.",
            _latestWorkflowSummary?.CurrentRoute ?? "WORKFLOW",
            "CONTINUE FORGE");
    }

    private void SetGuidedAction(
        string stage,
        string title,
        string detail,
        string route,
        string buttonText)
    {
        GuidedActionStageText.Text = stage;
        GuidedActionTitleText.Text = title;
        GuidedActionDetailText.Text = detail;
        GuidedActionRouteText.Text = route;
        GuidedActionButton.Content = buttonText;
        GuidedActionButton.IsEnabled = true;
    }

    private void GuidedActionButton_Click(object sender, RoutedEventArgs e)
    {
        string route = GuidedActionRouteText.Text;

        SelectMainTab(route);

        if (string.Equals(route, "DASHBOARD", StringComparison.OrdinalIgnoreCase) &&
            _latestSystemSnapshot == null)
        {
            ScanButton.Focus();
        }
    }

    private void QuickForgeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_latestSystemSnapshot == null)
        {
            SelectMainTab("DASHBOARD");
            ScanButton.Focus();
            GuidedActionDetailText.Text =
                "Quick Forge starts with an explicit System Scan. Navigation is guided; system-changing actions remain manual and safety-gated.";
            return;
        }

        if (_latestWorkflowSummary == null)
            UpdateWorkflowUi();

        SelectMainTab(_latestWorkflowSummary?.CurrentRoute ?? "WORKFLOW");
    }

    private void OpenDemoSessionButton_Click(object sender, RoutedEventArgs e)
    {
        var demo = new DemoSessionWindow { Owner = this };
        demo.ShowDialog();
    }

    // ============================================================
    // SPRINT 13C — INSTALLER / UPDATE FOUNDATION
    // ============================================================

    private void UpdateReleaseIdentityUi()
    {
        if (ReleaseInstallModeText == null)
            return;

        ReleaseIdentity identity =
            _releaseIdentityService.Inspect();

        ReleaseInstallModeText.Text =
            identity.InstallMode;

        ReleaseVersionText.Text =
            identity.Version;

        ReleaseChannelText.Text =
            identity.Channel;

        ReleaseUpdatePolicyText.Text =
            identity.UpdatePolicy;

        ReleaseFingerprintText.Text =
            identity.ReleaseFingerprint;

        ReleaseExecutablePathText.Text =
            identity.ExecutablePath;

        ReleaseDataPathText.Text =
            identity.DataDirectory;

        ReleaseInstallStateText.Text =
            identity.IsInstalled
                ? "INSTALLER MANAGED"
                : "PORTABLE MODE";

        ReleaseInstallStateText.Foreground =
            identity.IsInstalled
                ? new SolidColorBrush(
                    Color.FromRgb(110, 190, 140))
                : new SolidColorBrush(
                    Color.FromRgb(225, 170, 60));

        ReleaseStatusText.Text =
            identity.IsInstalled
                ? "This build is running from the per-user ForgeCare installation directory. Future ForgeCare installers with the same AppId upgrade this installation in place."
                : "This build is running in portable mode. Install with the ForgeCare Setup package to enter the installer-managed update path.";
    }

    private void RefreshReleaseIdentityButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        UpdateReleaseIdentityUi();

        ReleaseStatusText.Text +=
            $" · refreshed {DateTime.Now:HH:mm:ss}";
    }

    private void OpenReleaseFolderButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            ReleaseIdentity identity =
                _releaseIdentityService.Inspect();

            Directory.CreateDirectory(
                identity.InstallDirectory);

            Process.Start(
                new ProcessStartInfo
                {
                    FileName =
                        identity.InstallDirectory,

                    UseShellExecute =
                        true
                });
        }
        catch (Exception ex)
        {
            CrashLogService.Record(
                ex,
                "Open release folder");

            MessageBox.Show(
                this,
                ex.Message,
                "ForgeCare Release Folder",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // ============================================================
    // SPRINT 14B — REMOTE UPDATE DISCOVERY
    // ============================================================

    private void LoadRemoteUpdateSettingsUi()
    {
        if (RemoteManifestUrlTextBox == null)
            return;

        _remoteUpdateSettings =
            _remoteUpdateSettingsService.Load();

        RemoteManifestUrlTextBox.Text =
            _remoteUpdateSettings.ManifestUrl;

        RemoteUpdateChannelTextBox.Text =
            string.IsNullOrWhiteSpace(
                _remoteUpdateSettings.Channel)
                ? "stable"
                : _remoteUpdateSettings.Channel;

        RemoteUpdateLastCheckText.Text =
            _remoteUpdateSettings.LastSuccessfulCheck.HasValue
                ? $"{_remoteUpdateSettings.LastSuccessfulCheck:yyyy-MM-dd HH:mm:ss} · {_remoteUpdateSettings.LastKnownState} · {_remoteUpdateSettings.LastKnownAvailableVersion}"
                : "No successful remote update check has been cached.";

        RemoteUpdateStateText.Text =
            _remoteUpdateSettings.LastKnownState;

        RemoteUpdateAvailableVersionText.Text =
            string.IsNullOrWhiteSpace(
                _remoteUpdateSettings.LastKnownAvailableVersion)
                ? "—"
                : _remoteUpdateSettings.LastKnownAvailableVersion;
    }

    private void SaveRemoteUpdateSettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _remoteUpdateSettings.ManifestUrl =
            RemoteManifestUrlTextBox.Text.Trim();

        _remoteUpdateSettings.Channel =
            string.IsNullOrWhiteSpace(
                RemoteUpdateChannelTextBox.Text)
                ? "stable"
                : RemoteUpdateChannelTextBox.Text.Trim();

        _remoteUpdateSettingsService.Save(
            _remoteUpdateSettings);

        RemoteUpdateDetailText.Text =
            "Remote update discovery settings saved locally.";

        RemoteUpdateStateText.Text =
            "CONFIG SAVED";
    }

    private async void CheckRemoteUpdateButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            CheckRemoteUpdateButton.IsEnabled =
                false;

            CheckRemoteUpdateButton.Content =
                "CHECKING...";

            string url =
                RemoteManifestUrlTextBox.Text.Trim();

            string channel =
                string.IsNullOrWhiteSpace(
                    RemoteUpdateChannelTextBox.Text)
                    ? "beta"
                    : RemoteUpdateChannelTextBox.Text.Trim();

            RemoteUpdateStateText.Text =
                "CHECKING";

            RemoteUpdateDetailText.Text =
                "Contacting remote ForgeCare release manifest...";

            RemoteUpdateCheckResult result =
                await _remoteUpdateDiscoveryService.CheckAsync(
                    url,
                    channel);

            _lastRemoteUpdateCheck =
                result;

            DownloadVerifiedUpdateButton.IsEnabled =
                result.UpdateAvailable &&
                !string.IsNullOrWhiteSpace(result.InstallerFile) &&
                !string.IsNullOrWhiteSpace(result.InstallerSha256);

            SecureDownloadStateText.Text =
                DownloadVerifiedUpdateButton.IsEnabled
                    ? "READY"
                    : "NOT AVAILABLE";

            SecureDownloadDetailText.Text =
                DownloadVerifiedUpdateButton.IsEnabled
                    ? "A newer installer artifact is available. Download remains explicit and SHA-256 verification is mandatory."
                    : "A verified installer download becomes available only after a valid newer remote manifest is accepted.";

            RemoteUpdateStateText.Text =
                result.State;

            RemoteUpdateAvailableVersionText.Text =
                string.IsNullOrWhiteSpace(
                    result.AvailableVersion)
                    ? "—"
                    : result.AvailableVersion;

            RemoteUpdateDetailText.Text =
                result.Detail;

            RemoteUpdateStateText.Foreground =
                result.State switch
                {
                    "UPDATE AVAILABLE" =>
                        new SolidColorBrush(
                            Color.FromRgb(225, 170, 60)),

                    "CURRENT" =>
                        new SolidColorBrush(
                            Color.FromRgb(110, 190, 140)),

                    "OLDER BUILD" =>
                        new SolidColorBrush(
                            Color.FromRgb(105, 165, 230)),

                    "CHANNEL MISMATCH" =>
                        new SolidColorBrush(
                            Color.FromRgb(225, 170, 60)),

                    _ =>
                        new SolidColorBrush(
                            Color.FromRgb(225, 80, 80))
                };

            if (result.State is
                "UPDATE AVAILABLE" or
                "CURRENT" or
                "OLDER BUILD")
            {
                _remoteUpdateSettings.ManifestUrl =
                    url;

                _remoteUpdateSettings.Channel =
                    channel;

                _remoteUpdateSettings.LastSuccessfulCheck =
                    result.CheckedAt;

                _remoteUpdateSettings.LastKnownAvailableVersion =
                    result.AvailableVersion;

                _remoteUpdateSettings.LastKnownState =
                    result.State;

                _remoteUpdateSettingsService.Save(
                    _remoteUpdateSettings);

                RemoteUpdateLastCheckText.Text =
                    $"{result.CheckedAt:yyyy-MM-dd HH:mm:ss} · {result.State} · {result.AvailableVersion}";
            }
        }
        catch (Exception ex)
        {
            CrashLogService.Record(
                ex,
                "Remote update discovery");

            RemoteUpdateStateText.Text =
                "CHECK FAILED";

            RemoteUpdateDetailText.Text =
                ex.Message;
        }
        finally
        {
            CheckRemoteUpdateButton.IsEnabled =
                true;

            CheckRemoteUpdateButton.Content =
                "CHECK REMOTE UPDATE";
        }
    }

    // ============================================================
    // SPRINT 14C — SECURE UPDATE DOWNLOAD
    // ============================================================

    private async void DownloadVerifiedUpdateButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_lastRemoteUpdateCheck == null ||
            !_lastRemoteUpdateCheck.UpdateAvailable)
        {
            SecureDownloadStateText.Text =
                "NOT AVAILABLE";

            SecureDownloadDetailText.Text =
                "Run a successful remote update check first.";

            return;
        }

        DownloadVerifiedUpdateButton.IsEnabled =
            false;

        SecureDownloadProgressBar.Value =
            0;

        SecureDownloadStateText.Text =
            "DOWNLOADING";

        SecureDownloadDetailText.Text =
            "Downloading installer to ForgeCare's local update staging folder...";

        try
        {
            var progress =
                new Progress<int>(
                    value =>
                    {
                        SecureDownloadProgressBar.Value =
                            value;

                        SecureDownloadProgressText.Text =
                            $"{value}%";
                    });

            SecureUpdateDownloadResult result =
                await _secureUpdateDownloadService.DownloadAndVerifyAsync(
                    _lastRemoteUpdateCheck.ManifestUrl,
                    _lastRemoteUpdateCheck.InstallerFile,
                    _lastRemoteUpdateCheck.InstallerSha256,
                    progress);

            _lastSecureUpdateDownload =
                result.Success
                    ? result
                    : null;

            PrepareInstallerHandoffButton.IsEnabled =
                result.Success;

            InstallerHandoffStateText.Text =
                result.Success
                    ? "VERIFIED / READY"
                    : "NOT READY";

            InstallerHandoffDetailText.Text =
                result.Success
                    ? "The verified installer can now be prepared for an explicit technician-approved handoff."
                    : "Download and verify a newer installer before preparing installation.";

            SecureDownloadStateText.Text =
                result.State;

            SecureDownloadDetailText.Text =
                result.Detail;

            SecureDownloadPathText.Text =
                string.IsNullOrWhiteSpace(result.DownloadPath)
                    ? "—"
                    : result.DownloadPath;

            SecureDownloadHashText.Text =
                string.IsNullOrWhiteSpace(result.ActualSha256)
                    ? "—"
                    : result.ActualSha256;

            SecureDownloadStateText.Foreground =
                result.Success
                    ? new SolidColorBrush(
                        Color.FromRgb(110, 190, 140))
                    : new SolidColorBrush(
                        Color.FromRgb(225, 80, 80));

            if (result.Success)
            {
                SecureDownloadProgressBar.Value =
                    100;

                SecureDownloadProgressText.Text =
                    "100%";
            }
        }
        catch (Exception ex)
        {
            CrashLogService.Record(
                ex,
                "Secure update download");

            SecureDownloadStateText.Text =
                "DOWNLOAD FAILED";

            SecureDownloadDetailText.Text =
                ex.Message;
        }
        finally
        {
            DownloadVerifiedUpdateButton.IsEnabled =
                _lastRemoteUpdateCheck?.UpdateAvailable == true &&
                !string.IsNullOrWhiteSpace(
                    _lastRemoteUpdateCheck.InstallerFile) &&
                !string.IsNullOrWhiteSpace(
                    _lastRemoteUpdateCheck.InstallerSha256);
        }
    }

    // ============================================================
    // SPRINT 14D — CONTROLLED INSTALLER HANDOFF
    // ============================================================

    private async void PrepareInstallerHandoffButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_lastSecureUpdateDownload?.Success != true ||
            _lastRemoteUpdateCheck == null)
        {
            InstallerHandoffStateText.Text =
                "NOT READY";

            InstallerHandoffDetailText.Text =
                "A verified installer from the current ForgeCare session is required.";

            return;
        }

        PrepareInstallerHandoffButton.IsEnabled =
            false;

        InstallerHandoffStateText.Text =
            "RE-VERIFYING";

        InstallerHandoffDetailText.Text =
            "ForgeCare is validating the staged installer again before enabling installation.";

        InstallerHandoffResult validation =
            await _controlledInstallerHandoffService.ValidateForHandoffAsync(
                _lastSecureUpdateDownload.DownloadPath,
                _lastRemoteUpdateCheck.InstallerSha256);

        InstallerHandoffStateText.Text =
            validation.State;

        InstallerHandoffDetailText.Text =
            validation.Detail;

        ConfirmInstallerHandoffCheckBox.IsChecked =
            false;

        LaunchVerifiedInstallerButton.IsEnabled =
            validation.Success;

        ConfirmInstallerHandoffCheckBox.IsEnabled =
            validation.Success;

        InstallerHandoffStateText.Foreground =
            validation.Success
                ? new SolidColorBrush(
                    Color.FromRgb(110, 190, 140))
                : new SolidColorBrush(
                    Color.FromRgb(225, 80, 80));

        if (!validation.Success)
        {
            PrepareInstallerHandoffButton.IsEnabled =
                _lastSecureUpdateDownload?.Success == true;
        }
    }

    private void ConfirmInstallerHandoffCheckBox_Checked(
        object sender,
        RoutedEventArgs e)
    {
        LaunchVerifiedInstallerButton.IsEnabled =
            ConfirmInstallerHandoffCheckBox.IsChecked == true &&
            _lastSecureUpdateDownload?.Success == true;
    }

    private async void LaunchVerifiedInstallerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ConfirmInstallerHandoffCheckBox.IsChecked != true ||
            _lastSecureUpdateDownload?.Success != true ||
            _lastRemoteUpdateCheck == null)
        {
            InstallerHandoffStateText.Text =
                "CONFIRMATION REQUIRED";

            InstallerHandoffDetailText.Text =
                "Explicit technician confirmation is required before ForgeCare can hand the installer to Windows.";

            return;
        }

        LaunchVerifiedInstallerButton.IsEnabled =
            false;

        ConfirmInstallerHandoffCheckBox.IsEnabled =
            false;

        InstallerHandoffStateText.Text =
            "FINAL VERIFY";

        InstallerHandoffDetailText.Text =
            "Performing final SHA-256 verification immediately before launch...";

        InstallerHandoffResult validation =
            await _controlledInstallerHandoffService.ValidateForHandoffAsync(
                _lastSecureUpdateDownload.DownloadPath,
                _lastRemoteUpdateCheck.InstallerSha256);

        if (!validation.Success)
        {
            InstallerHandoffStateText.Text =
                validation.State;

            InstallerHandoffDetailText.Text =
                validation.Detail;

            ConfirmInstallerHandoffCheckBox.IsChecked =
                false;

            PrepareInstallerHandoffButton.IsEnabled =
                true;

            return;
        }

        InstallerHandoffResult launch =
            _controlledInstallerHandoffService.LaunchInstaller(
                validation.InstallerPath);

        InstallerHandoffStateText.Text =
            launch.State;

        InstallerHandoffDetailText.Text =
            launch.Success
                ? "Installer launched through Windows Shell. ForgeCare will now close so the installer can update application files safely."
                : launch.Detail;

        InstallerHandoffStateText.Foreground =
            launch.Success
                ? new SolidColorBrush(
                    Color.FromRgb(110, 190, 140))
                : new SolidColorBrush(
                    Color.FromRgb(225, 80, 80));

        if (launch.Success)
        {
            await Task.Delay(700);
            Application.Current.Shutdown();
            return;
        }

        ConfirmInstallerHandoffCheckBox.IsEnabled =
            true;

        ConfirmInstallerHandoffCheckBox.IsChecked =
            false;

        PrepareInstallerHandoffButton.IsEnabled =
            true;
    }

    // ============================================================
    // SPRINT 14A — OFFLINE UPDATE DISCOVERY
    // ============================================================

    private void CheckLocalUpdateManifestButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog =
            new OpenFileDialog
            {
                Title =
                    "Select ForgeCare release manifest",

                Filter =
                    "ForgeCare release manifest (release-manifest.json)|release-manifest.json|JSON files (*.json)|*.json|All files (*.*)|*.*",

                CheckFileExists =
                    true
            };

        if (dialog.ShowDialog(this) != true)
            return;

        UpdateManifestPathText.Text =
            dialog.FileName;

        UpdateCheckResult result =
            _updateDiscoveryService.CheckLocalManifest(
                dialog.FileName);

        UpdateDiscoveryStateText.Text =
            result.State;

        UpdateCurrentVersionText.Text =
            result.CurrentVersion;

        UpdateAvailableVersionText.Text =
            string.IsNullOrWhiteSpace(
                result.AvailableVersion)
                ? "—"
                : result.AvailableVersion;

        UpdateDiscoveryDetailText.Text =
            result.Detail;

        UpdateDiscoveryStateText.Foreground =
            result.State switch
            {
                "UPDATE AVAILABLE" =>
                    new SolidColorBrush(
                        Color.FromRgb(225, 170, 60)),

                "CURRENT" =>
                    new SolidColorBrush(
                        Color.FromRgb(110, 190, 140)),

                "OLDER BUILD" =>
                    new SolidColorBrush(
                        Color.FromRgb(105, 165, 230)),

                _ =>
                    new SolidColorBrush(
                        Color.FromRgb(225, 80, 80))
            };
    }

    // ============================================================
    // SETTINGS / TECHNICIAN PROFILE
    // ============================================================

    private void LoadSettingsUi()
    {
        _settings =
            _settingsService.Load();

        SettingsTechnicianTextBox.Text =
            _settings.TechnicianName;

        SettingsCompanyTextBox.Text =
            _settings.CompanyName;

        SettingsCustomerTextBox.Text =
            _settings.DefaultCustomerName;

        SettingsDeviceLabelTextBox.Text =
            _settings.DefaultDeviceLabel;

        SettingsAutoFillCheckBox.IsChecked =
            _settings.AutoFillReportDetails;

        SettingsRecoveryConfirmCheckBox.IsChecked =
            _settings.ConfirmBeforeRecoveryActions;

        SettingsPathText.Text =
            _settingsService.SettingsFilePath;

        SettingsStatusText.Text =
            "READY · preferences are stored locally";
    }

    private void ApplySettingsToReportDefaults()
    {
        if (!_settings.AutoFillReportDetails)
            return;

        ForgeReportSession session =
            _forgeReportService.Snapshot();

        ForgeReportMetadata metadata =
            session.Metadata;

        bool changed = false;

        if (string.IsNullOrWhiteSpace(metadata.TechnicianName) &&
            !string.IsNullOrWhiteSpace(_settings.TechnicianName))
        {
            metadata.TechnicianName =
                _settings.TechnicianName;

            changed = true;
        }

        if (string.IsNullOrWhiteSpace(metadata.CompanyName) &&
            !string.IsNullOrWhiteSpace(_settings.CompanyName))
        {
            metadata.CompanyName =
                _settings.CompanyName;

            changed = true;
        }

        if (string.IsNullOrWhiteSpace(metadata.CustomerName) &&
            !string.IsNullOrWhiteSpace(_settings.DefaultCustomerName))
        {
            metadata.CustomerName =
                _settings.DefaultCustomerName;

            changed = true;
        }

        if (string.IsNullOrWhiteSpace(metadata.DeviceLabel) &&
            !string.IsNullOrWhiteSpace(_settings.DefaultDeviceLabel))
        {
            metadata.DeviceLabel =
                _settings.DefaultDeviceLabel;

            changed = true;
        }

        if (changed)
            _forgeReportService.UpdateMetadata(metadata);
    }

    private void SaveSettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _settings =
            new ForgeCareSettings
            {
                TechnicianName =
                    SettingsTechnicianTextBox.Text.Trim(),

                CompanyName =
                    SettingsCompanyTextBox.Text.Trim(),

                DefaultCustomerName =
                    SettingsCustomerTextBox.Text.Trim(),

                DefaultDeviceLabel =
                    SettingsDeviceLabelTextBox.Text.Trim(),

                AutoFillReportDetails =
                    SettingsAutoFillCheckBox.IsChecked == true,

                ConfirmBeforeRecoveryActions =
                    SettingsRecoveryConfirmCheckBox.IsChecked == true
            };

        _settingsService.Save(
            _settings);

        ApplySettingsToReportDefaults();
        UpdateReportUi();
        UpdateProfileReadinessUi();

        SettingsStatusText.Text =
            $"SAVED LOCALLY · {DateTime.Now:HH:mm:ss}";

        SettingsStatusText.Foreground =
            new SolidColorBrush(
                Color.FromRgb(110, 190, 140));
    }

    private void ResetSettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        MessageBoxResult result =
            MessageBox.Show(
                "Reset ForgeCare technician preferences to defaults?",
                "ForgeCare Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        _settingsService.Reset();

        _settings =
            new ForgeCareSettings();

        LoadSettingsUi();
        UpdateProfileReadinessUi();

        SettingsStatusText.Text =
            "RESET COMPLETE · defaults restored";
    }

    private void OpenForgeCareDataButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Directory.CreateDirectory(
            _settingsService.DataRoot);

        Process.Start(
            new ProcessStartInfo
            {
                FileName =
                    _settingsService.DataRoot,

                UseShellExecute =
                    true
            });
    }

    // ============================================================
    // RESOURCE BASELINE / HISTORY
    // ============================================================

    private async void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        UpdateProductIdentityUi();
        UpdateReleaseIdentityUi();
        LoadRemoteUpdateSettingsUi();
        LoadSettingsUi();
        UpdateProfileReadinessUi();
        ApplySettingsToReportDefaults();
        UpdateBetaDiagnosticsUi();
        UpdateExternalTestPreflightUi();
        LoadBetaFieldTestUi();
        UpdateStabilityRecoveryUi();

        UpdateReportUi();
        UpdateTechnicianGuidanceUi();
        UpdateWorkflowUi();

        SelectMainTab(
            _uxStateService.LoadLastTab());

        try
        {
            var baseline =
                await _resourceHistoryService
                    .GetBaselineAsync();

            UpdateBaselineUi(
                baseline);
        }
        catch
        {
            BaselineStateText.Text =
                "UNAVAILABLE";

            BaselineStatusText.Text =
                "Historical baseline could not be loaded. Live analysis remains available.";
        }
    }

    private void UpdateBaselineUi(
        ResourceBaselineResult baseline)
    {
        BaselineSampleCountText.Text =
            baseline.SampleCount.ToString();

        BaselineStateText.Text =
            baseline.BaselineState;

        BaselineConfidenceText.Text =
            baseline.Confidence;

        BaselineCpuAverageText.Text =
            baseline.SampleCount == 0
                ? "--"
                : $"{baseline.AverageCpuPercent:0.0}%";

        BaselineMemoryAverageText.Text =
            baseline.SampleCount == 0
                ? "--"
                : $"{baseline.AverageMemoryUsedPercent:0.0}%";

        BaselineCpuTrendText.Text =
            baseline.CpuTrend;

        BaselineMemoryTrendText.Text =
            baseline.MemoryTrend;

        BaselineProcessListView.ItemsSource =
            baseline.PersistentProcesses;

        ClearBaselineButton.IsEnabled =
            baseline.SampleCount > 0;

        BaselineStatusText.Text =
            baseline.SampleCount switch
            {
                0 =>
                    "No historical samples yet. Each Deep Analysis run will add one local baseline sample.",

                1 =>
                    "First sample recorded. ForgeCare needs repeated runs before calling a pattern persistent.",

                < 5 =>
                    $"{baseline.SampleCount} samples recorded. Baseline is learning; conclusions remain low-confidence.",

                < 10 =>
                    $"{baseline.SampleCount} samples recorded. Baseline is forming and recurring processes can now be compared.",

                _ =>
                    $"{baseline.SampleCount} samples recorded. Baseline is established from recent Deep Analysis runs."
            };
    }

    private async void ClearBaselineButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var answer =
            MessageBox.Show(
                this,
                "Clear ForgeCare's locally stored Deep Analysis history? This does not change Windows, processes, services or system settings.",
                "Clear Resource Baseline",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        if (answer !=
            MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            ClearBaselineButton.IsEnabled =
                false;

            await _resourceHistoryService
                .ClearAsync();

            UpdateBaselineUi(
                new ResourceBaselineResult());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "ForgeCare Baseline Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // ============================================================
    // DEEP SYSTEM ANALYSIS
    // ============================================================

    private async void RunDeepAnalysisButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_latestSystemSnapshot == null)
        {
            AnalysisOverallPressureText.Text =
                "--";

            AnalysisCpuValueText.Text =
                "--";

            AnalysisMemoryValueText.Text =
                "--";

            AnalysisProcessValueText.Text =
                "--";

            AnalysisTopProcessesListView.ItemsSource =
                null;

            AnalysisInsightsListView.ItemsSource =
                null;

            AnalysisStatusText.Text =
                "System profile required. Run System Scan on the Dashboard first.";

            MessageBox.Show(
                this,
                "Run System Scan on the Dashboard before starting Deep System Analysis.",
                "ForgeCare Deep Analysis",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        try
        {
            RunDeepAnalysisButton.IsEnabled =
                false;

            RunDeepAnalysisButton.Content =
                "SAMPLING SYSTEM...";

            AnalysisStatusText.Text =
                "Sampling active processes and resource pressure...";

            AnalysisOverallPressureText.Text =
                "ANALYZING";

            AnalysisTopProcessesListView.ItemsSource =
                null;

            AnalysisInsightsListView.ItemsSource =
                null;

            var result =
                await _resourceAnalyzerService
                    .AnalyzeAsync(
                        _latestSystemSnapshot);

            _latestResourceAnalysisResult =
                result;

            AnalysisOverallPressureText.Text =
                result.OverallPressure;

            AnalysisCpuValueText.Text =
                $"{result.CpuUsagePercent:0.0}%";

            AnalysisCpuStatusText.Text =
                result.CpuStatus;

            AnalysisMemoryValueText.Text =
                $"{result.MemoryUsedPercent:0.0}%";

            AnalysisMemoryStatusText.Text =
                result.MemoryStatus;

            AnalysisProcessValueText.Text =
                result.ProcessCount.ToString("N0");

            AnalysisProcessStatusText.Text =
                result.ProcessStatus;

            AnalysisHighCpuText.Text =
                result.HighCpuProcessCount.ToString();

            AnalysisHighMemoryText.Text =
                result.HighMemoryProcessCount.ToString();

            AnalysisAvailableMemoryText.Text =
                $"{result.AvailableMemoryGb:0.0} GB";

            AnalysisTopProcessesListView.ItemsSource =
                result.TopProcesses;

            AnalysisInsightsListView.ItemsSource =
                result.Insights;

            _forgeReportService.RecordResourceAnalysis(
                result);

            var baseline =
                await _resourceHistoryService
                    .RecordAsync(
                        result);

            UpdateReportUi();
            UpdateWorkflowUi();

            UpdateBaselineUi(
                baseline);

            AnalysisStatusText.Text =
                $"Deep analysis completed {result.AnalysisTime:HH:mm:ss}. " +
                $"Baseline sample {baseline.SampleCount} recorded locally.";
        }
        catch (Exception ex)
        {
            AnalysisOverallPressureText.Text =
                "FAILED";

            AnalysisStatusText.Text =
                "Deep analysis was interrupted.";

            MessageBox.Show(
                this,
                ex.Message,
                "ForgeCare Deep Analysis Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            RunDeepAnalysisButton.IsEnabled =
                true;

            RunDeepAnalysisButton.Content =
                "RUN DEEP ANALYSIS";
        }
    }



    // ============================================================
    // STORAGE DEEP SCAN
    // ============================================================

    private async void RunStorageDeepScanButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            RunStorageDeepScanButton.IsEnabled =
                false;

            RunStorageDeepScanButton.Content =
                "SCANNING STORAGE...";

            StorageDeepStatusText.Text =
                "Scanning common user storage locations. Reparse points and inaccessible folders are skipped.";

            StorageScannedSizeText.Text =
                "--";

            StorageScannedFilesText.Text =
                "--";

            StorageLargeFilesText.Text =
                "--";

            StorageLargeFileSizeText.Text =
                "--";

            StorageSkippedText.Text =
                "--";

            StorageLocationListView.ItemsSource =
                null;

            StorageLargeFileListView.ItemsSource =
                null;

            _latestStorageAnalysisResult =
                null;

            ReviewStorageCleanupButton.IsEnabled =
                false;

            var result =
                await _storageDeepScannerService
                    .AnalyzeAsync();

            _latestStorageAnalysisResult =
                result;

            _forgeReportService.RecordStorageAnalysis(
                result);

            UpdateReportUi();
            UpdateWorkflowUi();

            ReviewStorageCleanupButton.IsEnabled =
                result.LargeFiles.Count > 0;

            StorageScannedSizeText.Text =
                result.DisplayScannedSize;

            StorageScannedFilesText.Text =
                result.ScannedFiles.ToString("N0");

            StorageLargeFilesText.Text =
                result.LargeFileCount.ToString();

            StorageLargeFileSizeText.Text =
                result.DisplayLargeFileSize;

            StorageSkippedText.Text =
                result.SkippedDirectories.ToString();

            StorageLocationListView.ItemsSource =
                result.Locations
                    .OrderByDescending(
                        location =>
                            location.SizeBytes)
                    .ToList();

            StorageLargeFileListView.ItemsSource =
                result.LargeFiles;

            StorageDeepStatusText.Text =
                result.HitSafetyLimit
                    ? $"Storage scan completed {result.AnalysisTime:HH:mm:ss}. Safety limit reached after {result.ScannedFiles:N0} files; results are partial."
                    : $"Storage scan completed {result.AnalysisTime:HH:mm:ss}. {result.ScannedFiles:N0} files inspected.";
        }
        catch (Exception ex)
        {
            StorageDeepStatusText.Text =
                "Storage analysis failed.";

            MessageBox.Show(
                this,
                ex.Message,
                "ForgeCare Storage Deep Scan",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            RunStorageDeepScanButton.IsEnabled =
                true;

            RunStorageDeepScanButton.Content =
                "RUN STORAGE DEEP SCAN";
        }
    }


    private void ReviewStorageCleanupButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_latestStorageAnalysisResult == null ||
            _latestStorageAnalysisResult.LargeFiles.Count == 0)
        {
            MessageBox.Show(
                this,
                "Run Storage Deep Scan first and make sure large-file findings are available.",
                "ForgeCare Storage Cleanup Review",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        var reviewWindow =
            new StorageCleanupReviewWindow(
                _latestStorageAnalysisResult.LargeFiles)
            {
                Owner = this
            };

        reviewWindow.ShowDialog();
    }


    private async void FindDuplicatesButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _duplicateScanCancellation?.Dispose();
        _duplicateScanCancellation = new CancellationTokenSource();

        try
        {
            FindDuplicatesButton.IsEnabled = false;
            CancelDuplicateScanButton.IsEnabled = true;
            FindDuplicatesButton.Content = "SCANNING...";
            DuplicateProgressBar.Visibility = Visibility.Visible;
            DuplicateProgressBar.IsIndeterminate = true;
            DuplicateProgressBar.Value = 0;

            DuplicateStatusText.Text =
                "Discovering candidate files. ForgeCare will quick-fingerprint same-size files before full SHA-256 verification.";

            DuplicateGroupCountText.Text = "--";
            DuplicateFileCountText.Text = "--";
            DuplicateReclaimableText.Text = "--";
            DuplicateHashedText.Text = "--";
            DuplicateGroupListView.ItemsSource = null;
            ReviewDuplicatesButton.IsEnabled = false;
            _latestDuplicateScanResult = null;

            var progress = new Progress<DuplicateScanProgress>(update =>
            {
                DuplicateProgressStageText.Text = update.Stage;
                DuplicateProgressDetailText.Text =
                    update.Stage == "Discovering files"
                        ? $"{update.InspectedFiles:N0} inspected · {update.CandidateFiles:N0} ≥10 MB candidates"
                        : $"{update.HashedFiles:N0} verified · {FormatDuplicateProgressBytes(update.HashedBytes)} hashed";

                if (update.Stage == "Verifying exact matches")
                {
                    DuplicateProgressBar.IsIndeterminate = false;
                    DuplicateProgressBar.Value = update.Percent;
                }
                else if (update.Stage == "Complete")
                {
                    DuplicateProgressBar.IsIndeterminate = false;
                    DuplicateProgressBar.Value = 100;
                }
            });

            var result = await _duplicateScannerService.ScanAsync(
                progress,
                _duplicateScanCancellation.Token);

            _latestDuplicateScanResult = result;
            _forgeReportService.RecordDuplicateScan(result);
            UpdateReportUi();
            UpdateWorkflowUi();

            DuplicateGroupCountText.Text = result.DuplicateGroupCount.ToString();
            DuplicateFileCountText.Text = result.DuplicateFileCount.ToString();
            DuplicateReclaimableText.Text = result.DisplayReclaimable;
            DuplicateHashedText.Text = result.DisplayHashedBytes;
            DuplicateGroupListView.ItemsSource = result.Groups;
            ReviewDuplicatesButton.IsEnabled = result.Groups.Count > 0;

            string limits =
                result.HitFileLimit || result.HitHashByteLimit
                    ? " Scan safety limit reached; results are partial."
                    : string.Empty;

            DuplicateStatusText.Text =
                $"Duplicate scan completed {result.ScanTime:HH:mm:ss}. " +
                $"{result.HashedFiles:N0} full files hashed; {result.DuplicateGroupCount} exact duplicate groups found." +
                limits;
        }
        catch (OperationCanceledException)
        {
            DuplicateStatusText.Text = "Duplicate scan cancelled safely.";
            DuplicateProgressStageText.Text = "CANCELLED";
            DuplicateProgressDetailText.Text = "No files were changed.";
        }
        catch (Exception ex)
        {
            DuplicateStatusText.Text = "Duplicate scan failed.";
            MessageBox.Show(this, ex.Message, "ForgeCare Duplicate Scanner",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            FindDuplicatesButton.IsEnabled = true;
            CancelDuplicateScanButton.IsEnabled = false;
            FindDuplicatesButton.Content = "FIND EXACT DUPLICATES";
            DuplicateProgressBar.IsIndeterminate = false;
            _duplicateScanCancellation?.Dispose();
            _duplicateScanCancellation = null;
        }
    }

    private void CancelDuplicateScanButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        CancelDuplicateScanButton.IsEnabled = false;
        DuplicateProgressStageText.Text = "CANCELLING...";
        _duplicateScanCancellation?.Cancel();
    }

    private static string FormatDuplicateProgressBytes(long bytes)
    {
        double gb = bytes / 1024d / 1024d / 1024d;
        if (gb >= 1) return $"{gb:0.00} GB";

        double mb = bytes / 1024d / 1024d;
        return $"{mb:0.0} MB";
    }

    private void ReviewDuplicatesButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_latestDuplicateScanResult == null ||
            _latestDuplicateScanResult.Groups.Count == 0)
        {
            MessageBox.Show(
                this,
                "Run Exact Duplicate Scan first.",
                "ForgeCare Duplicate Review",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        var reviewWindow =
            new DuplicateReviewWindow(
                _latestDuplicateScanResult.Groups)
            {
                Owner = this
            };

        reviewWindow.ShowDialog();
    }

    // ============================================================
    // SERVICE INTELLIGENCE
    // ============================================================

    private async void AnalyzeServicesButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            AnalyzeServicesButton.IsEnabled =
                false;

            AnalyzeServicesButton.Content =
                "ANALYZING SERVICES...";

            ServiceStatusText.Text =
                "Reading Windows service state and startup configuration...";

            ServiceListView.ItemsSource =
                null;

            ServiceInsightsListView.ItemsSource =
                null;

            ServiceTotalText.Text =
                "--";

            ServiceRunningText.Text =
                "--";

            ServiceStoppedText.Text =
                "--";

            ServiceAutomaticText.Text =
                "--";

            ServiceManualText.Text =
                "--";

            ServiceDisabledText.Text =
                "--";

            ServiceReviewText.Text =
                "--";

            var result =
                await _serviceAnalyzerService
                    .AnalyzeAsync();

            _latestServiceAnalysisResult =
                result;

            ServiceTotalText.Text =
                result.TotalCount.ToString();

            ServiceRunningText.Text =
                result.RunningCount.ToString();

            ServiceStoppedText.Text =
                result.StoppedCount.ToString();

            ServiceAutomaticText.Text =
                result.AutomaticCount.ToString();

            ServiceManualText.Text =
                result.ManualCount.ToString();

            ServiceDisabledText.Text =
                result.DisabledCount.ToString();

            ServiceReviewText.Text =
                result.ReviewCount.ToString();

            ServiceListView.ItemsSource =
                result.Services;

            ServiceInsightsListView.ItemsSource =
                result.Insights;

            _forgeReportService.RecordServiceAnalysis(
                result);

            UpdateReportUi();
            UpdateWorkflowUi();

            ServiceStatusText.Text =
                $"Service analysis completed {result.AnalysisTime:HH:mm:ss}. " +
                "Read-only classification; no service configuration was changed.";
        }
        catch (Exception ex)
        {
            ServiceStatusText.Text =
                "Service analysis failed.";

            MessageBox.Show(
                this,
                ex.Message,
                "ForgeCare Service Intelligence",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            AnalyzeServicesButton.IsEnabled =
                true;

            AnalyzeServicesButton.Content =
                "ANALYZE SERVICES";
        }
    }

    // ============================================================
    // STARTUP REVIEW / SAFE DISABLE
    // ============================================================

    private void ReviewStartupChangesButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_latestStartupImpactResult is null ||
            _latestStartupImpactResult.Items.Count == 0)
        {
            MessageBox.Show(
                this,
                "Run System Scan and Analyze Optimization before opening Startup Review.",
                "ForgeCare Startup Review",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        var reviewWindow =
            new StartupReviewWindow(
                _latestStartupImpactResult.Items)
            {
                Owner = this
            };

        reviewWindow.ShowDialog();
    }

    // ============================================================
    // CLEANUP ANALYZER
    // ============================================================

    private async void AnalyzeCleanupButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            AnalyzeCleanupButton.IsEnabled =
                false;

            AnalyzeCleanupButton.Content =
                "ANALYZING...";

            CleanupStatusText.Text =
                "Inspecting temporary files...";

            CleanupTotalSizeText.Text =
                "--";

            CleanupTotalFilesText.Text =
                "--";

            CleanupSelectedSizeText.Text =
                "--";

            CleanupSelectedFilesText.Text =
                "--";

            CleanupSourceCountText.Text =
                "--";

            ReviewCleanupButton.IsEnabled =
                false;

            CleanupListView.ItemsSource =
                null;

            if (_latestCleanupResult != null)
            {
                foreach (var oldItem in
                         _latestCleanupResult.Items)
                {
                    oldItem.PropertyChanged -=
                        CleanupItem_PropertyChanged;
                }
            }

            var result =
                await _cleanupScanner.ScanAsync();

            _latestCleanupResult =
                result;

            CleanupListView.ItemsSource =
                result.Items;

            foreach (var item in result.Items)
            {
                item.PropertyChanged +=
                    CleanupItem_PropertyChanged;
            }

            CleanupTotalSizeText.Text =
                result.DisplayTotalSize;

            CleanupTotalFilesText.Text =
                result.TotalFiles.ToString("N0");

            CleanupSourceCountText.Text =
                result.Items.Count.ToString();

            UpdateCleanupSelectionSummary();

            CleanupStatusText.Text =
                $"{result.Items.Count} cleanup sources analyzed.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "ForgeCare Cleanup Analyzer",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            CleanupStatusText.Text =
                "Cleanup analysis failed.";
        }
        finally
        {
            AnalyzeCleanupButton.IsEnabled =
                true;

            AnalyzeCleanupButton.Content =
                "ANALYZE CLEANUP";
        }
    }

    // ============================================================
    // CLEANUP SELECTION
    // ============================================================

    private void CleanupItem_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName ==
            nameof(CleanupItem.IsSelected))
        {
            UpdateCleanupSelectionSummary();
        }
    }

    private void UpdateCleanupSelectionSummary()
    {
        if (_latestCleanupResult == null)
        {
            CleanupSelectedSizeText.Text =
                "--";

            CleanupSelectedFilesText.Text =
                "--";

            ReviewCleanupButton.IsEnabled =
                false;

            return;
        }

        var selected =
            _latestCleanupResult.Items
                .Where(item => item.IsSelected)
                .ToList();

        long selectedBytes =
            selected.Sum(
                item => item.SizeBytes);

        int selectedFiles =
            selected.Sum(
                item => item.FileCount);

        CleanupSelectedSizeText.Text =
            FormatBytes(
                selectedBytes);

        CleanupSelectedFilesText.Text =
            selectedFiles.ToString("N0");

        ReviewCleanupButton.IsEnabled =
            selected.Count > 0 &&
            selectedBytes > 0;
    }

    // ============================================================
    // CLEANUP REVIEW
    // ============================================================

    private void ReviewCleanupButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_latestCleanupResult == null)
        {
            return;
        }

        var selected =
            _latestCleanupResult.Items
                .Where(item => item.IsSelected)
                .ToList();

        if (selected.Count == 0)
        {
            MessageBox.Show(
                "No cleanup sources are selected.",
                "ForgeCare Cleanup Review",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        var reviewWindow =
            new CleanupReviewWindow(
                selected)
            {
                Owner = this
            };

        reviewWindow.ShowDialog();
    }

    // ============================================================
    // OPTIMIZATION ENGINE
    // ============================================================

    private void AnalyzeOptimizationButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_latestSystemSnapshot == null ||
            _latestHealthResult == null)
        {
            OptimizationImpactText.Text =
                "--";

            OptimizationCountText.Text =
                "--";

            OptimizationScoreText.Text =
                "--";

            OptimizationProfileText.Text =
                "SYSTEM PROFILE REQUIRED";

            OptimizationStatusText.Text =
                "Run System Scan on the Dashboard before analyzing optimization opportunities.";

            OptimizationListView.ItemsSource =
                null;

            StartupImpactListView.ItemsSource =
                null;

            StartupImpactTotalText.Text =
                "--";

            StartupImpactHighText.Text =
                "--";

            StartupImpactCandidatesText.Text =
                "--";

            StartupImpactAverageText.Text =
                "--";

            StartupImpactStatusText.Text =
                "Run System Scan before analyzing startup impact.";

            MessageBox.Show(
                "ForgeCare needs a current system profile before it can build an optimization plan. Run System Scan on the Dashboard first.",
                "ForgeCare Optimization",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        try
        {
            AnalyzeOptimizationButton.IsEnabled =
                false;

            AnalyzeOptimizationButton.Content =
                "ANALYZING SYSTEM...";

            OptimizationStatusText.Text =
                "Evaluating startup load, storage headroom and memory pressure...";

            var result =
                _optimizationService.Analyze(
                    _latestSystemSnapshot,
                    _latestHealthResult);

            _latestOptimizationResult =
                result;

            var startupImpact =
                _startupImpactService.Analyze(
                    _latestSystemSnapshot.StartupItems);

            _latestStartupImpactResult =
                startupImpact;

            UpdateWorkflowUi();

            OptimizationListView.ItemsSource =
                result.Recommendations;

            StartupImpactListView.ItemsSource =
                startupImpact.Items;

            StartupImpactTotalText.Text =
                startupImpact.TotalItems.ToString();

            StartupImpactHighText.Text =
                startupImpact.HighImpactCount.ToString();

            StartupImpactCandidatesText.Text =
                startupImpact.DisableCandidateCount.ToString();

            StartupImpactAverageText.Text =
                startupImpact.AverageImpactScore.ToString();

            StartupImpactStatusText.Text =
                startupImpact.TotalItems == 0
                    ? "No startup items were available for impact analysis."
                    : $"{startupImpact.TotalItems} startup entries classified. " +
                      $"{startupImpact.DisableCandidateCount} look like good candidates for manual review.";

            OptimizationImpactText.Text =
                result.ImpactRating;

            OptimizationCountText.Text =
                result.RecommendationCount.ToString();

            OptimizationScoreText.Text =
                result.TotalEstimatedImpact.ToString();

            OptimizationImpactText.Foreground =
                GetOptimizationImpactBrush(
                    result.ImpactRating);

            OptimizationProfileText.Text =
                "PROFILE ANALYZED";

            OptimizationStatusText.Text =
                result.RecommendationCount == 0
                    ? "No optimization recommendations were generated."
                    : $"Optimization plan ready — {result.RecommendationCount} recommendations generated.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "ForgeCare Optimization Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            OptimizationImpactText.Text =
                "ERROR";

            OptimizationImpactText.Foreground =
                GetHealthBrush(0);

            OptimizationStatusText.Text =
                "Optimization analysis was interrupted.";
        }
        finally
        {
            AnalyzeOptimizationButton.IsEnabled =
                true;

            AnalyzeOptimizationButton.Content =
                "ANALYZE OPTIMIZATION";
        }
    }

    private static Brush GetOptimizationImpactBrush(
        string impactRating)
    {
        return impactRating.ToUpperInvariant() switch
        {
            "HIGH" =>
                new SolidColorBrush(
                    Color.FromRgb(
                        225,
                        80,
                        80)),

            "MODERATE" =>
                new SolidColorBrush(
                    Color.FromRgb(
                        225,
                        170,
                        60)),

            "LOW" =>
                new SolidColorBrush(
                    Color.FromRgb(
                        110,
                        190,
                        140)),

            _ =>
                new SolidColorBrush(
                    Color.FromRgb(
                        140,
                        148,
                        157))
        };
    }

    // ============================================================
    // FORMAT HELPERS
    // ============================================================

    private static string FormatBytes(
        long bytes)
    {
        double gb =
            bytes /
            1024d /
            1024d /
            1024d;

        if (gb >= 1)
        {
            return $"{gb:0.00} GB";
        }

        double mb =
            bytes /
            1024d /
            1024d;

        if (mb >= 1)
        {
            return $"{mb:0.0} MB";
        }

        double kb =
            bytes /
            1024d;

        return $"{kb:0.0} KB";
    }

    // ============================================================
    // STATUS COLORS
    // ============================================================

    private static Brush GetStatusBrush(
        string status)
    {
        return status.ToLowerInvariant() switch
        {
            "healthy" =>
                new SolidColorBrush(
                    Color.FromRgb(
                        80,
                        200,
                        120)),

            "good" =>
                new SolidColorBrush(
                    Color.FromRgb(
                        110,
                        190,
                        140)),

            "attention" =>
                new SolidColorBrush(
                    Color.FromRgb(
                        225,
                        170,
                        60)),

            "low space" =>
                new SolidColorBrush(
                    Color.FromRgb(
                        225,
                        80,
                        80)),

            "heavy" =>
                new SolidColorBrush(
                    Color.FromRgb(
                        225,
                        80,
                        80)),

            "high usage" =>
                new SolidColorBrush(
                    Color.FromRgb(
                        225,
                        80,
                        80)),

            _ =>
                new SolidColorBrush(
                    Color.FromRgb(
                        140,
                        148,
                        157))
        };
    }

    private static Brush GetHealthBrush(
        int score)
    {
        return score switch
        {
            >= 90 =>
                new SolidColorBrush(
                    Color.FromRgb(
                        80,
                        200,
                        120)),

            >= 75 =>
                new SolidColorBrush(
                    Color.FromRgb(
                        110,
                        190,
                        140)),

            >= 60 =>
                new SolidColorBrush(
                    Color.FromRgb(
                        225,
                        170,
                        60)),

            >= 40 =>
                new SolidColorBrush(
                    Color.FromRgb(
                        225,
                        120,
                        60)),

            _ =>
                new SolidColorBrush(
                    Color.FromRgb(
                        225,
                        80,
                        80))
        };
    }
}