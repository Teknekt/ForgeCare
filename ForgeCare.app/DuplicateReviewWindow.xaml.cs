using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App;

public partial class DuplicateReviewWindow : Window
{
    private readonly List<DuplicateGroup> _groups;
    private readonly List<StorageCleanupCandidate> _candidates;
    private readonly StorageCleanupService _cleanupService;

    private bool _simulationPassed;
    private bool _executionCompleted;

    public DuplicateReviewWindow(
        IEnumerable<DuplicateGroup> groups)
    {
        InitializeComponent();

        _groups =
            groups.ToList();

        _cleanupService =
            new StorageCleanupService();

        _candidates =
            _groups
                .SelectMany(group =>
                    group.Files.Select(file =>
                        new StorageCleanupCandidate
                        {
                            GroupId =
                                group.GroupId,

                            Name =
                                file.Name,

                            FullPath =
                                file.FullPath,

                            Location =
                                file.Location,

                            Category =
                                "EXACT DUPLICATE",

                            CleanupClass =
                                "EXACT DUPLICATE",

                            Recommendation =
                                "Select only redundant copies. At least one copy must remain.",

                            SizeBytes =
                                file.SizeBytes,

                            LastWriteTime =
                                file.LastWriteTime,

                            CanSelect =
                                true,

                            IsSelected =
                                false,

                            Status =
                                "READY",

                            StatusReason =
                                "SHA-256 confirmed exact duplicate. Nothing selected."
                        }))
                .ToList();

        DuplicateFileListView.ItemsSource =
            _candidates;

        GroupCountText.Text =
            _groups.Count.ToString();

        RefreshSelectionSummary();
    }

    private void DuplicateCheckBox_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_executionCompleted)
            return;

        _simulationPassed = false;

        ExecuteButton.IsEnabled = false;
        ExecuteButton.Content =
            "RECYCLE DUPLICATES LOCKED";

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

        foreach (var candidate in _candidates)
        {
            if (candidate.IsSelected)
            {
                candidate.Status = "SELECTED";
                candidate.StatusReason =
                    "Selected for duplicate cleanup review.";
            }
            else if (candidate.Status != "RECYCLED")
            {
                candidate.Status = "READY";
                candidate.StatusReason =
                    "SHA-256 confirmed exact duplicate. Nothing selected.";
            }
        }

        DuplicateFileListView.Items.Refresh();
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
                ? "Select redundant copies manually. ForgeCare will preserve at least one copy from every duplicate group."
                : $"{selected.Count} copy/copies selected. Dry run will verify that every affected group keeps at least one copy.";
    }

    private bool ValidateGroupPreservation(
        IReadOnlyCollection<StorageCleanupCandidate> selected,
        bool updateStatuses)
    {
        bool valid =
            true;

        foreach (var group in _groups)
        {
            var groupCandidates =
                _candidates
                    .Where(candidate =>
                        candidate.GroupId ==
                        group.GroupId)
                    .ToList();

            int selectedCount =
                groupCandidates.Count(candidate =>
                    selected.Contains(candidate));

            if (selectedCount >=
                groupCandidates.Count)
            {
                valid = false;

                if (updateStatuses)
                {
                    foreach (var candidate in
                             groupCandidates.Where(candidate =>
                                 candidate.IsSelected))
                    {
                        candidate.Status = "BLOCKED";
                        candidate.StatusReason =
                            "All copies in this duplicate group were selected. Keep at least one copy.";
                    }
                }
            }
        }

        return valid;
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
                "Select at least one redundant duplicate copy first.",
                "ForgeCare Duplicate Review",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        if (!ValidateGroupPreservation(
                selected,
                updateStatuses: true))
        {
            DuplicateFileListView.Items.Refresh();

            _simulationPassed = false;

            SafetyTitleText.Text =
                "DRY RUN BLOCKED";

            SafetyStatusText.Text =
                "At least one affected duplicate group has every copy selected. Deselect one copy from each blocked group.";

            return;
        }

        try
        {
            SimulateButton.IsEnabled = false;
            ExecuteButton.IsEnabled = false;
            SimulateButton.Content = "VALIDATING...";
            ModeBadgeText.Text = "DRY RUN";

            SafetyTitleText.Text =
                "DUPLICATE SAFETY CHECK RUNNING";

            var result =
                await _cleanupService
                    .SimulateAsync(
                        selected);

            DuplicateFileListView.Items.Refresh();

            _simulationPassed =
                result.ValidatedFiles > 0 &&
                result.BlockedFiles == 0 &&
                result.ErrorCount == 0 &&
                ValidateGroupPreservation(
                    selected,
                    updateStatuses: false);

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
                    $"MOVE {result.ValidatedFiles} DUPLICATE" +
                    (result.ValidatedFiles == 1 ? "" : "S") +
                    " TO RECYCLE BIN";

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
                "Duplicate cleanup remains locked.";

            MessageBox.Show(
                this,
                ex.Message,
                "ForgeCare Duplicate Dry Run",
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

        if (selected.Count == 0 ||
            !ValidateGroupPreservation(
                selected,
                updateStatuses: true))
        {
            DuplicateFileListView.Items.Refresh();
            ExecuteButton.IsEnabled = false;
            return;
        }

        var confirmation =
            MessageBox.Show(
                this,
                "ForgeCare will move only the selected redundant duplicate copies to Windows Recycle Bin." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Selected copies: {selected.Count}" +
                $"{Environment.NewLine}" +
                $"Selected data: {FormatBytes(selected.Sum(x => x.SizeBytes))}" +
                $"{Environment.NewLine}{Environment.NewLine}" +
                "At least one copy in every affected exact-duplicate group will remain." +
                $"{Environment.NewLine}" +
                "There is no permanent-delete fallback." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                "Continue?",
                "Confirm Duplicate Cleanup",
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
                "MOVING DUPLICATES...";

            ModeBadgeText.Text =
                "RECYCLE EXECUTION";

            SafetyTitleText.Text =
                "DUPLICATE CLEANUP RUNNING";

            var result =
                await _cleanupService
                    .MoveToRecycleBinAsync(
                        selected);

            ForgeReportService.Instance.RecordStorageCleanup(
                result,
                "Exact duplicate cleanup");

            _executionCompleted = true;

            DuplicateFileListView.Items.Refresh();

            ResultLabelText.Text = "RECYCLED";
            ResultValueText.Text =
                result.RecycledFiles.ToString();

            ModeBadgeText.Text = "FORGE COMPLETE";
            SafetyTitleText.Text =
                "DUPLICATE CLEANUP COMPLETE";

            SafetyStatusText.Text =
                $"{result.RecycledFiles} recycled ({result.DisplayRecycledSize}) · " +
                $"{result.BlockedFiles} blocked · " +
                $"{result.SkippedFiles} skipped · " +
                $"{result.ErrorCount} errors.";

            ExecuteButton.Content =
                "CLEANUP COMPLETE";

            MessageBox.Show(
                this,
                $"Duplicate cleanup completed." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Copies recycled: {result.RecycledFiles}" +
                $"{Environment.NewLine}" +
                $"Data moved: {result.DisplayRecycledSize}" +
                $"{Environment.NewLine}" +
                $"Errors: {result.ErrorCount}",
                "ForgeCare — Duplicate Cleanup Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            SafetyTitleText.Text =
                "DUPLICATE CLEANUP INTERRUPTED";

            MessageBox.Show(
                this,
                ex.Message,
                "ForgeCare Duplicate Cleanup Error",
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
        double gb =
            bytes / 1024d / 1024d / 1024d;

        if (gb >= 1)
            return $"{gb:0.00} GB";

        double mb =
            bytes / 1024d / 1024d;

        if (mb >= 1)
            return $"{mb:0.0} MB";

        return $"{bytes / 1024d:0.0} KB";
    }
}
