using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ForgeCare.App.ViewModels;

namespace ForgeCare.App.Views;

public partial class EvidenceExplorerView : UserControl
{
    public EvidenceExplorerView()
    {
        InitializeComponent();
    }

    public event EventHandler? RefreshRequested;

    public EvidenceExplorerViewModel? ViewModel =>
        DataContext as EvidenceExplorerViewModel;

    public void SetViewModel(EvidenceExplorerViewModel viewModel)
    {
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) =>
        RefreshRequested?.Invoke(this, EventArgs.Empty);

    private void ClearFiltersButton_Click(object sender, RoutedEventArgs e) =>
        ViewModel?.ClearFilters();

    private void EvidenceExplorerView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.F)
        {
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape &&
            SearchTextBox.IsKeyboardFocusWithin &&
            !string.IsNullOrEmpty(ViewModel?.SearchQuery))
        {
            ViewModel?.ClearSearch();
            e.Handled = true;
        }
    }
}
