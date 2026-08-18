using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App;

public partial class CleanupReviewWindow : Window
{
    private readonly List<CleanupItem> _selectedItems;
    private readonly CleanupExecutor _cleanupExecutor;

    private bool _simulationPassed;
    private bool _executionCompleted;

    public CleanupReviewWindow(
        IEnumerable<CleanupItem> selectedItems)
    {
        InitializeComponent();

        _selectedItems =
            selectedItems
                .Where(item => item.IsSelected)
                .ToList();

        _cleanupExecutor =
            new CleanupExecutor();

        LoadReview();
    }

    // ============================================================
    // INITIAL REVIEW
    // ============================================================

    private void LoadReview()
    {
        SelectedSourcesList.ItemsSource =
            _selectedItems;

        SourceCountText.Text =
            _selectedItems.Count.ToString();

        long totalBytes =
            _selectedItems.Sum(
                item => item.SizeBytes);

        SelectedSizeText.Text =
            FormatBytes(totalBytes);

        CleanableSizeText.Text =
            "--";

        SkippedFilesText.Text =
            "--";
    }

    // ============================================================
    // SIMULATION
    // ============================================================

    private async void SimulateButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_executionCompleted)
        {
            return;
        }

        try
        {
            SimulateButton.IsEnabled =
                false;

            ExecuteButton.IsEnabled =
                false;

            SimulateButton.Content =
                "VALIDATING...";

            ReviewSubtitleText.Text =
                "Validating cleanup candidates...";

            SafetyTitleText.Text =
                "SAFETY CHECK RUNNING";

            SafetyStatusText.Text =
                "Validating paths, reparse points and file locks.";

            var result =
                await _cleanupExecutor.SimulateAsync(
                    _selectedItems);

            CleanableSizeText.Text =
                result.CleanableSize;

            SkippedFilesText.Text =
                result.SkippedFiles.ToString("N0");

            SectionTitleText.Text =
                "SAFETY SIMULATION LOG";

            SelectedSourcesList.ItemsSource =
                result.LogEntries;

            SelectedSourcesList.ItemTemplate =
                CreateLogTemplate();

            ReviewSubtitleText.Text =
                "Safety simulation completed.";

            SafetyTitleText.Text =
                "DRY RUN COMPLETE — NO CHANGES MADE";

            SafetyStatusText.Text =
                $"{result.CleanableFiles:N0} files passed safety checks. " +
                $"{result.SkippedFiles:N0} skipped. " +
                $"{result.SafetyBlockedFiles:N0} blocked. " +
                $"{result.ErrorCount:N0} scan errors.";

            SimulateButton.Content =
                "SIMULATION COMPLETE";

            _simulationPassed =
                result.CleanableFiles > 0;

            if (_simulationPassed)
            {
                EnableLiveCleanup();
            }
            else
            {
                ExecuteButton.Content =
                    "NOTHING TO CLEAN";

                ExecuteButton.IsEnabled =
                    false;
            }
        }
        catch (Exception ex)
        {
            _simulationPassed =
                false;

            MessageBox.Show(
                this,
                ex.Message,
                "ForgeCare Safety Simulation",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            SafetyTitleText.Text =
                "SIMULATION FAILED";

            SafetyStatusText.Text =
                "Live cleanup remains locked.";

            SimulateButton.Content =
                "SIMULATION FAILED";
        }
    }

    // ============================================================
    // ENABLE LIVE MODE
    // ============================================================

    private void EnableLiveCleanup()
    {
        ExecuteButton.IsEnabled =
            true;

        ExecuteButton.Cursor =
            System.Windows.Input.Cursors.Hand;

        ExecuteButton.Content =
            "EXECUTE LIVE CLEANUP";

        ExecuteButton.Background =
            new SolidColorBrush(
                Color.FromRgb(
                    199,
                    166,
                    91));

        ExecuteButton.Foreground =
            new SolidColorBrush(
                Color.FromRgb(
                    16,
                    16,
                    16));

        ModeBadgeText.Text =
            "LIVE CLEANUP AVAILABLE";

        SafetyStatusText.Text +=
            " Review the simulation log before executing.";
    }

    // ============================================================
    // LIVE EXECUTION
    // ============================================================

    private async void ExecuteButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_simulationPassed ||
            _executionCompleted)
        {
            return;
        }

        long selectedBytes =
            _selectedItems.Sum(
                item => item.SizeBytes);

        int selectedFiles =
            _selectedItems.Sum(
                item => item.FileCount);

        string confirmationMessage =
            "You are about to run LIVE CLEANUP." +
            $"{Environment.NewLine}{Environment.NewLine}" +

            $"Selected sources: {_selectedItems.Count}" +
            $"{Environment.NewLine}" +

            $"Detected data: {FormatBytes(selectedBytes)}" +
            $"{Environment.NewLine}" +

            $"Detected files: {selectedFiles:N0}" +
            $"{Environment.NewLine}{Environment.NewLine}" +

            "ForgeCare will permanently delete eligible files " +
            "inside its approved temporary-file locations." +
            $"{Environment.NewLine}{Environment.NewLine}" +

            "Locked, protected, blocked and non-allowlisted files " +
            "will be skipped." +
            $"{Environment.NewLine}{Environment.NewLine}" +

            "Continue with LIVE CLEANUP?";

        var confirmation =
            MessageBox.Show(
                this,
                confirmationMessage,
                "Confirm ForgeCare Live Cleanup",
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
            ExecuteButton.IsEnabled =
                false;

            SimulateButton.IsEnabled =
                false;

            ExecuteButton.Content =
                "FORGING CLEANUP...";

            ModeBadgeText.Text =
                "LIVE EXECUTION";

            ReviewSubtitleText.Text =
                "ForgeCare is cleaning approved temporary files...";

            SafetyTitleText.Text =
                "LIVE CLEANUP RUNNING";

            SafetyStatusText.Text =
                "Safety policy is being revalidated before every file operation.";

            var result =
                await _cleanupExecutor.ExecuteAsync(
                    _selectedItems);

            _executionCompleted =
                true;

            ForgeReportService.Instance.RecordCleanup(
                result);

            ResultSizeLabel.Text =
                "RECLAIMED";

            CleanableSizeText.Text =
                result.ReclaimedSize;

            ResultFilesLabel.Text =
                "DELETED";

            SkippedFilesText.Text =
                result.DeletedFiles.ToString("N0");

            SectionTitleText.Text =
                "LIVE EXECUTION LOG";

            SelectedSourcesList.ItemsSource =
                result.LogEntries;

            SelectedSourcesList.ItemTemplate =
                CreateLogTemplate();

            ModeBadgeText.Text =
                "FORGE COMPLETE";

            ReviewSubtitleText.Text =
                "Cleanup operation completed.";

            SafetyTitleText.Text =
                "LIVE CLEANUP COMPLETE";

            SafetyStatusText.Text =
                $"{result.DeletedFiles:N0} files deleted · " +
                $"{result.ReclaimedSize} reclaimed · " +
                $"{result.SkippedFiles:N0} skipped · " +
                $"{result.SafetyBlockedFiles:N0} blocked · " +
                $"{result.ErrorCount:N0} errors.";

            ExecuteButton.Content =
                "CLEANUP COMPLETE";

            MessageBox.Show(
                this,
                $"ForgeCare cleanup completed." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Space reclaimed: {result.ReclaimedSize}" +
                $"{Environment.NewLine}" +
                $"Files deleted: {result.DeletedFiles:N0}" +
                $"{Environment.NewLine}" +
                $"Files skipped: {result.SkippedFiles:N0}",
                "ForgeCare — Forge Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "ForgeCare Cleanup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            SafetyTitleText.Text =
                "CLEANUP INTERRUPTED";

            SafetyStatusText.Text =
                "ForgeCare stopped execution after an unexpected error.";

            ExecuteButton.Content =
                "CLEANUP INTERRUPTED";
        }
    }

    // ============================================================
    // LOG TEMPLATE
    // ============================================================

    private static DataTemplate CreateLogTemplate()
    {
        const string template =
            """
            <DataTemplate
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">

                <Border Background="#141619"
                        BorderBrush="#292D32"
                        BorderThickness="1"
                        CornerRadius="8"
                        Margin="0,0,0,8"
                        Padding="12">

                    <Grid>

                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="110"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="100"/>
                        </Grid.ColumnDefinitions>

                        <TextBlock Text="{Binding Status}"
                                   Foreground="#C7A65B"
                                   FontSize="11"
                                   FontWeight="Bold"
                                   VerticalAlignment="Center"/>

                        <StackPanel Grid.Column="1">

                            <TextBlock Text="{Binding Path}"
                                       Foreground="White"
                                       FontSize="11"
                                       TextTrimming="CharacterEllipsis"/>

                            <TextBlock Text="{Binding Reason}"
                                       Foreground="#747C85"
                                       FontSize="10"
                                       Margin="0,3,0,0"/>

                        </StackPanel>

                        <TextBlock Grid.Column="2"
                                   Text="{Binding DisplaySize}"
                                   Foreground="#8C949D"
                                   FontSize="11"
                                   HorizontalAlignment="Right"
                                   VerticalAlignment="Center"/>

                    </Grid>

                </Border>

            </DataTemplate>
            """;

        return
            (DataTemplate)XamlReader.Parse(
                template);
    }

    // ============================================================
    // CLOSE
    // ============================================================

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }

    // ============================================================
    // FORMAT
    // ============================================================

    private static string FormatBytes(
        long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
        {
            return
                $"{bytes / 1024d / 1024d / 1024d:0.00} GB";
        }

        if (bytes >= 1024L * 1024)
        {
            return
                $"{bytes / 1024d / 1024d:0.0} MB";
        }

        if (bytes >= 1024)
        {
            return
                $"{bytes / 1024d:0.0} KB";
        }

        return $"{bytes} B";
    }
}