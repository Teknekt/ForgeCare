using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App;

public partial class StorageCleanupReviewWindow : Window
{
    private readonly StorageCleanupService _cleanupService;
    private readonly List<StorageCleanupCandidate> _candidates;

    private bool _simulationPassed;
    private bool _executionCompleted;

    public StorageCleanupReviewWindow(
        IEnumerable<StorageFileFinding> findings)
    {
        InitializeComponent();

        _cleanupService =
            new StorageCleanupService();

        _candidates =
            _cleanupService.BuildCandidates(
                findings);

        CandidateListView.ItemsSource =
            _candidates;

        CandidateCountText.Text =
            _candidates.Count.ToString();

        RefreshSelectionSummary();
    }

    private void CandidateCheckBox_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_executionCompleted)
            return;

        _simulationPassed = false;

        ExecuteButton.IsEnabled = false;
        ExecuteButton.Content = "RECYCLE CLEANUP LOCKED";
        ExecuteButton.Background =
            new SolidColorBrush(
                Color.FromRgb(37, 41, 46));
        ExecuteButton.Foreground =
            new SolidColorBrush(
                Color.FromRgb(102, 108, 115));

        SimulateButton.IsEnabled = true;
        SimulateButton.Content = "RUN DRY RUN";

        ModeBadgeText.Text = "REVIEW MODE";
        ResultLabelText.Text = "VALIDATED";
        ResultValueText.Text = "--";

        foreach (var candidate in
                 _candidates.Where(candidate =>
                     candidate.IsSelected))
        {
            candidate.Status = "SELECTED";
            candidate.StatusReason =
                "Selected for review. Run dry run before any file operation.";
        }

        CandidateListView.Items.Refresh();
        RefreshSelectionSummary();
    }

    private void RefreshSelectionSummary()
    {
        var selected =
            _candidates
                .Where(candidate =>
                    candidate.IsSelected &&
                    candidate.CanSelect)
                .ToList();

        SelectedCountText.Text =
            selected.Count.ToString();

        SelectedSizeText.Text =
            FormatBytes(
                selected.Sum(candidate =>
                    candidate.SizeBytes));

        SafetyStatusText.Text =
            selected.Count == 0
                ? "Select one or more files. Nothing is preselected and no file changes occur during review."
                : $"{selected.Count} file(s) selected. Run the dry run before Recycle Bin cleanup is unlocked.";
    }

    private async void SimulateButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_executionCompleted)
            return;

        var selected =
            _candidates
                .Where(candidate =>
                    candidate.IsSelected &&
                    candidate.CanSelect)
                .ToList();

        if (selected.Count == 0)
        {
            MessageBox.Show(
                this,
                "Select at least one file first.",
                "ForgeCare Storage Cleanup",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        try
        {
            SimulateButton.IsEnabled = false;
            ExecuteButton.IsEnabled = false;

            SimulateButton.Content = "VALIDATING...";
            ModeBadgeText.Text = "DRY RUN";

            SafetyTitleText.Text =
                "FILE SAFETY CHECK RUNNING";

            SafetyStatusText.Text =
                "Validating file location, reparse points, size consistency and locks. No files are being changed.";

            var result =
                await _cleanupService
                    .SimulateAsync(
                        selected);

            CandidateListView.Items.Refresh();

            _simulationPassed =
                result.ValidatedFiles > 0 &&
                result.ErrorCount == 0;

            ResultLabelText.Text = "VALIDATED";
            ResultValueText.Text =
                result.ValidatedFiles.ToString();

            SafetyTitleText.Text =
                "DRY RUN COMPLETE — NO CHANGES MADE";

            SafetyStatusText.Text =
                $"{result.ValidatedFiles} validated ({result.DisplayValidatedSize}) · " +
                $"{result.BlockedFiles} blocked · " +
                $"{result.SkippedFiles} skipped · " +
                $"{result.ErrorCount} errors.";

            SimulateButton.Content =
                "DRY RUN COMPLETE";

            if (_simulationPassed)
            {
                ExecuteButton.IsEnabled = true;
                ExecuteButton.Content =
                    $"MOVE {result.ValidatedFiles} TO RECYCLE BIN";

                ExecuteButton.Background =
                    new SolidColorBrush(
                        Color.FromRgb(199, 166, 91));

                ExecuteButton.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(16, 16, 16));

                ModeBadgeText.Text =
                    "RECYCLE CLEANUP AVAILABLE";
            }
        }
        catch (Exception ex)
        {
            _simulationPassed = false;

            SafetyTitleText.Text =
                "DRY RUN FAILED";

            SafetyStatusText.Text =
                "Recycle cleanup remains locked.";

            MessageBox.Show(
                this,
                ex.Message,
                "ForgeCare Storage Dry Run",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void ExecuteButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_simulationPassed ||
            _executionCompleted)
        {
            return;
        }

        var selected =
            _candidates
                .Where(candidate =>
                    candidate.IsSelected &&
                    candidate.CanSelect)
                .ToList();

        if (selected.Count == 0)
            return;

        string preview =
            string.Join(
                Environment.NewLine,
                selected
                    .Take(6)
                    .Select(candidate =>
                        $"• {candidate.Name} — {candidate.DisplaySize}"));

        if (selected.Count > 6)
        {
            preview +=
                $"{Environment.NewLine}• ...and {selected.Count - 6} more";
        }

        var confirmation =
            MessageBox.Show(
                this,
                "ForgeCare will ask Windows to move the selected files to the Recycle Bin." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Files: {selected.Count}" +
                $"{Environment.NewLine}" +
                $"Selected data: {FormatBytes(selected.Sum(x => x.SizeBytes))}" +
                $"{Environment.NewLine}{Environment.NewLine}" +
                preview +
                $"{Environment.NewLine}{Environment.NewLine}" +
                "There is NO permanent-delete fallback in Sprint 7B. " +
                "Files successfully recycled can normally be restored from Windows Recycle Bin." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                "Continue?",
                "Confirm ForgeCare Storage Cleanup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

        if (confirmation !=
            MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            ExecuteButton.IsEnabled = false;
            SimulateButton.IsEnabled = false;

            ExecuteButton.Content =
                "MOVING TO RECYCLE BIN...";

            ModeBadgeText.Text =
                "RECYCLE EXECUTION";

            SafetyTitleText.Text =
                "RECYCLE CLEANUP RUNNING";

            SafetyStatusText.Text =
                "ForgeCare is revalidating every file immediately before requesting a Windows Recycle Bin operation.";

            var result =
                await _cleanupService
                    .MoveToRecycleBinAsync(
                        selected);

            ForgeReportService.Instance.RecordStorageCleanup(
                result,
                "Large-file cleanup");

            _executionCompleted = true;

            CandidateListView.Items.Refresh();

            SectionTitleText.Text =
                "STORAGE CLEANUP RESULTS";

            ResultLabelText.Text =
                "RECYCLED";

            ResultValueText.Text =
                result.RecycledFiles.ToString();

            ModeBadgeText.Text =
                "FORGE COMPLETE";

            SafetyTitleText.Text =
                "STORAGE CLEANUP COMPLETE";

            SafetyStatusText.Text =
                $"{result.RecycledFiles} recycled ({result.DisplayRecycledSize}) · " +
                $"{result.BlockedFiles} blocked · " +
                $"{result.SkippedFiles} skipped · " +
                $"{result.ErrorCount} errors.";

            ExecuteButton.Content =
                "CLEANUP COMPLETE";

            MessageBox.Show(
                this,
                $"ForgeCare storage cleanup completed." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Moved to Recycle Bin: {result.RecycledFiles}" +
                $"{Environment.NewLine}" +
                $"Data moved: {result.DisplayRecycledSize}" +
                $"{Environment.NewLine}" +
                $"Blocked/skipped: {result.BlockedFiles + result.SkippedFiles}" +
                $"{Environment.NewLine}" +
                $"Errors: {result.ErrorCount}",
                "ForgeCare — Storage Cleanup Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            SafetyTitleText.Text =
                "STORAGE CLEANUP INTERRUPTED";

            SafetyStatusText.Text =
                "ForgeCare stopped after an unexpected error.";

            MessageBox.Show(
                this,
                ex.Message,
                "ForgeCare Storage Cleanup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }

    private static string FormatBytes(
        long bytes)
    {
        double gb = bytes / 1024d / 1024d / 1024d;
        if (gb >= 1)
            return $"{gb:0.00} GB";

        double mb = bytes / 1024d / 1024d;
        if (mb >= 1)
            return $"{mb:0.0} MB";

        return $"{bytes / 1024d:0.0} KB";
    }
}
