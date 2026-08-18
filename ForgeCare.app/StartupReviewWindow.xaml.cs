using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App;

public partial class StartupReviewWindow : Window
{
    private readonly StartupManagerService _manager;
    private readonly List<StartupChangeItem> _items;
    private bool _simulationPassed;
    private bool _executionCompleted;

    public StartupReviewWindow(IEnumerable<StartupImpactItem> impactItems)
    {
        InitializeComponent();
        _manager = new StartupManagerService();
        _items = _manager.BuildPlan(impactItems);
        StartupChangeListView.ItemsSource = _items;
        RefreshSummary();
        RefreshUndoState();
    }

    private void RefreshSummary()
    {
        var selected = _items.Where(x => x.IsSelected && x.CanSelect).ToList();
        AvailableCountText.Text = _items.Count(x => x.CanSelect).ToString();
        SelectedCountText.Text = selected.Count.ToString();
        SelectedImpactText.Text = selected.Sum(x => x.ImpactScore).ToString();

        if (!_executionCompleted)
            SafetyStatusText.Text = selected.Count == 0
                ? "Select one or more supported current-user entries. KEEP and machine-wide entries remain locked."
                : $"{selected.Count} selected. Run a dry run before live disable is unlocked.";
    }

    private void RefreshUndoState()
    {
        int count = _manager.UndoRecordCount();
        UndoCountText.Text = count.ToString();
        RestoreButton.IsEnabled = count > 0;
        RestoreButton.Opacity = count > 0 ? 1.0 : 0.45;
    }

    private void SelectionCheckBox_Click(object sender, RoutedEventArgs e)
    {
        _simulationPassed = false;
        ExecuteButton.IsEnabled = false;
        ExecuteButton.Content = "LIVE DISABLE LOCKED";
        ExecuteButton.Background = new SolidColorBrush(Color.FromRgb(37, 41, 46));
        ExecuteButton.Foreground = new SolidColorBrush(Color.FromRgb(102, 108, 115));
        SimulateButton.IsEnabled = true;
        SimulateButton.Content = "RUN DRY RUN";
        ModeBadgeText.Text = "REVIEW MODE";
        RefreshSummary();
    }

    private async void SimulateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_executionCompleted) return;

        var selected = _items.Where(x => x.IsSelected && x.CanSelect).ToList();

        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Select at least one supported startup entry first.",
                "ForgeCare Startup Review", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            SimulateButton.IsEnabled = false;
            ExecuteButton.IsEnabled = false;
            SimulateButton.Content = "VALIDATING...";
            ModeBadgeText.Text = "DRY RUN";
            SafetyTitleText.Text = "STARTUP SAFETY CHECK RUNNING";
            SafetyStatusText.Text = "Re-reading every selected source. No changes are being made.";

            var result = await _manager.SimulateDisableAsync(selected);
            StartupChangeListView.Items.Refresh();

            _simulationPassed = result.ValidatedCount > 0 && result.ErrorCount == 0;

            SafetyTitleText.Text = "DRY RUN COMPLETE — NO CHANGES MADE";
            SafetyStatusText.Text =
                $"{result.ValidatedCount} validated · {result.BlockedCount} blocked · " +
                $"{result.SkippedCount} skipped · {result.ErrorCount} errors.";

            SimulateButton.Content = "DRY RUN COMPLETE";

            if (_simulationPassed)
            {
                ExecuteButton.IsEnabled = true;
                ExecuteButton.Content =
                    $"DISABLE {result.ValidatedCount} STARTUP ITEM" +
                    (result.ValidatedCount == 1 ? "" : "S");
                ExecuteButton.Background = new SolidColorBrush(Color.FromRgb(199, 166, 91));
                ExecuteButton.Foreground = new SolidColorBrush(Color.FromRgb(16, 16, 16));
                ModeBadgeText.Text = "LIVE CHANGE AVAILABLE";
            }
        }
        catch (Exception ex)
        {
            _simulationPassed = false;
            SafetyTitleText.Text = "DRY RUN FAILED";
            SafetyStatusText.Text = "Live startup changes remain locked.";
            MessageBox.Show(this, ex.Message, "ForgeCare Startup Dry Run",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_simulationPassed || _executionCompleted) return;

        var selected = _items.Where(x => x.IsSelected && x.CanSelect).ToList();
        if (selected.Count == 0) return;

        string names = string.Join(Environment.NewLine,
            selected.Take(8).Select(x => $"• {x.Name}"));
        if (selected.Count > 8)
            names += $"{Environment.NewLine}• ...and {selected.Count - 8} more";

        var confirmation = MessageBox.Show(
            this,
            "ForgeCare is about to disable supported CURRENT-USER startup entries." +
            $"{Environment.NewLine}{Environment.NewLine}Selected: {selected.Count}" +
            $"{Environment.NewLine}Combined heuristic impact score: {selected.Sum(x => x.ImpactScore)}" +
            $"{Environment.NewLine}{Environment.NewLine}{names}" +
            $"{Environment.NewLine}{Environment.NewLine}" +
            "Registry values are backed up before removal; Startup-folder files are moved reversibly. " +
            "RESTORE ALL can reverse successful changes." +
            $"{Environment.NewLine}{Environment.NewLine}Continue?",
            "Confirm ForgeCare Startup Changes",
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

        if (confirmation != MessageBoxResult.Yes) return;

        try
        {
            ExecuteButton.IsEnabled = false;
            SimulateButton.IsEnabled = false;
            ExecuteButton.Content = "FORGING STARTUP...";
            ModeBadgeText.Text = "LIVE EXECUTION";
            SafetyTitleText.Text = "STARTUP CHANGES RUNNING";
            SafetyStatusText.Text = "Each source is validated again immediately before modification.";

            SafetyJournalService.Instance.CaptureStartupSnapshot(
                "Before startup disable",
                _manager.GetUndoRecords());

            var result = await _manager.DisableAsync(selected);

            SafetyJournalService.Instance.Record(
                "STARTUP",
                "Disable startup entries",
                $"{selected.Count} selected item(s)",
                result.ErrorCount == 0 ? "COMPLETE" : "COMPLETE WITH ERRORS",
                $"{result.DisabledCount} disabled · {result.BlockedCount} blocked · {result.ErrorCount} errors.",
                reversible: result.DisabledCount > 0,
                recovery: "RESTORE STARTUP");

            ForgeReportService.Instance.RecordStartupChange(
                result,
                isRestore: false);

            _executionCompleted = true;
            StartupChangeListView.Items.Refresh();

            SectionTitleText.Text = "STARTUP CHANGE RESULTS";
            ModeBadgeText.Text = "FORGE COMPLETE";
            SafetyTitleText.Text = "STARTUP FORGE COMPLETE";
            SafetyStatusText.Text =
                $"{result.DisabledCount} disabled · {result.BlockedCount} blocked · " +
                $"{result.SkippedCount} skipped · {result.ErrorCount} errors.";
            ExecuteButton.Content = "CHANGES COMPLETE";
            RefreshUndoState();

            MessageBox.Show(this,
                $"Startup changes completed.{Environment.NewLine}{Environment.NewLine}" +
                $"Disabled: {result.DisabledCount}{Environment.NewLine}" +
                $"Blocked: {result.BlockedCount}{Environment.NewLine}" +
                $"Skipped: {result.SkippedCount}{Environment.NewLine}" +
                $"Errors: {result.ErrorCount}",
                "ForgeCare — Startup Forge Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            SafetyTitleText.Text = "STARTUP CHANGE INTERRUPTED";
            SafetyStatusText.Text = "ForgeCare stopped after an unexpected error.";
            MessageBox.Show(this, ex.Message, "ForgeCare Startup Change Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        int count = _manager.UndoRecordCount();
        if (count <= 0) return;

        var confirmation = MessageBox.Show(
            this,
            $"ForgeCare has {count} startup undo record{(count == 1 ? "" : "s")}." +
            $"{Environment.NewLine}{Environment.NewLine}Restore all recorded startup entries?",
            "Restore ForgeCare Startup Changes",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

        if (confirmation != MessageBoxResult.Yes) return;

        try
        {
            RestoreButton.IsEnabled = false;
            ExecuteButton.IsEnabled = false;
            SimulateButton.IsEnabled = false;
            ModeBadgeText.Text = "RESTORING";
            SafetyTitleText.Text = "RESTORING STARTUP STATE";

            SafetyJournalService.Instance.CaptureStartupSnapshot(
                "Before startup restore",
                _manager.GetUndoRecords());

            var result = await _manager.RestoreAllAsync();

            SafetyJournalService.Instance.Record(
                "STARTUP",
                "Restore startup entries",
                $"{count} undo record(s)",
                result.ErrorCount == 0 ? "RESTORED" : "RESTORE WITH ERRORS",
                $"{result.RestoredCount} restored · {result.ErrorCount} errors.",
                reversible: false,
                recovery: "NONE");

            ForgeReportService.Instance.RecordStartupChange(
                result,
                isRestore: true);

            SectionTitleText.Text = "RESTORE RESULTS";
            StartupChangeListView.ItemsSource = result.Items;
            SafetyTitleText.Text = "RESTORE COMPLETE";
            SafetyStatusText.Text =
                $"{result.RestoredCount} restored · {result.BlockedCount} blocked · {result.ErrorCount} errors.";
            ModeBadgeText.Text = "RESTORE COMPLETE";
            RefreshUndoState();

            MessageBox.Show(this,
                $"Startup restore completed.{Environment.NewLine}{Environment.NewLine}" +
                $"Restored: {result.RestoredCount}{Environment.NewLine}Errors: {result.ErrorCount}",
                "ForgeCare — Restore Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "ForgeCare Startup Restore Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}