using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ForgeCare.App.Models;

public class CleanupItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public string Name { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public int FileCount { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsSelected
    {
        get => _isSelected;

        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;

            OnPropertyChanged();
        }
    }

    public double SizeMb =>
        SizeBytes / 1024d / 1024d;

    public double SizeGb =>
        SizeBytes / 1024d / 1024d / 1024d;

    public string DisplaySize
    {
        get
        {
            if (SizeGb >= 1)
            {
                return $"{SizeGb:0.00} GB";
            }

            return $"{SizeMb:0.0} MB";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}