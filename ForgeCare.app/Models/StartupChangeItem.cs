using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ForgeCare.App.Models;

public class StartupChangeItem : INotifyPropertyChanged
{
    private bool _isSelected;
    private string _status = "PENDING";
    private string _statusReason = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ImpactLevel { get; set; } = string.Empty;
    public int ImpactScore { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public string Confidence { get; set; } = string.Empty;

    public string HandlerType { get; set; } = string.Empty;
    public string RegistryPath { get; set; } = string.Empty;
    public string RegistryValueName { get; set; } = string.Empty;
    public string StartupFilePath { get; set; } = string.Empty;

    public bool IsSupported { get; set; }
    public bool IsLocked { get; set; }
    public bool CanSelect => IsSupported && !IsLocked;

    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
    }

    public string Status
    {
        get => _status;
        set { if (_status != value) { _status = value; OnPropertyChanged(); } }
    }

    public string StatusReason
    {
        get => _statusReason;
        set { if (_statusReason != value) { _statusReason = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}