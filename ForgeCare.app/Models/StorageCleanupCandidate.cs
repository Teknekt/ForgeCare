using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ForgeCare.App.Models;

public class StorageCleanupCandidate : INotifyPropertyChanged
{
    private bool _isSelected;
    private string _status = "READY";
    private string _statusReason = string.Empty;

    public string GroupId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string CleanupClass { get; set; } = string.Empty;

    public string Recommendation { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime LastWriteTime { get; set; }

    public bool CanSelect { get; set; } = true;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            if (_status == value)
                return;

            _status = value;
            OnPropertyChanged();
        }
    }

    public string StatusReason
    {
        get => _statusReason;
        set
        {
            if (_statusReason == value)
                return;

            _statusReason = value;
            OnPropertyChanged();
        }
    }

    public string DisplaySize
    {
        get
        {
            double gb = SizeBytes / 1024d / 1024d / 1024d;
            if (gb >= 1)
                return $"{gb:0.00} GB";

            double mb = SizeBytes / 1024d / 1024d;
            return $"{mb:0.0} MB";
        }
    }

    public string DisplayLastWrite =>
        LastWriteTime == DateTime.MinValue
            ? "Unknown"
            : LastWriteTime.ToString("yyyy-MM-dd");

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
