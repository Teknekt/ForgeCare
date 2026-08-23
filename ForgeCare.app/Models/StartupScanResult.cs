using System.Collections.ObjectModel;

namespace ForgeCare.App.Models;

public enum StartupScanSourceKind
{
    CurrentUserRegistry,
    LocalMachineRegistry,
    UserStartupFolder,
    CommonStartupFolder
}

public enum StartupScanSourceStatus
{
    Completed,
    Failed,
    Unavailable,
    Unknown
}

public sealed class StartupScanSourceResult
{
    public StartupScanSourceResult(
        StartupScanSourceKind source,
        StartupScanSourceStatus status,
        IEnumerable<StartupItem> items)
    {
        Source = source;
        Status = status;
        Items = new ReadOnlyCollection<StartupItem>(
            (items ?? throw new ArgumentNullException(nameof(items)))
                .Select(Clone)
                .ToList());
    }

    public StartupScanSourceKind Source { get; }
    public StartupScanSourceStatus Status { get; }
    public IReadOnlyList<StartupItem> Items { get; }

    private static StartupItem Clone(StartupItem item) =>
        new()
        {
            Name = item.Name,
            Command = item.Command,
            Source = item.Source
        };
}

public sealed class StartupScanResult
{
    public StartupScanResult(
        IEnumerable<StartupItem> items,
        IEnumerable<StartupScanSourceResult> sourceResults)
    {
        Items = new ReadOnlyCollection<StartupItem>(
            (items ?? throw new ArgumentNullException(nameof(items)))
                .Select(Clone)
                .ToList());
        SourceResults = new ReadOnlyCollection<StartupScanSourceResult>(
            (sourceResults ?? throw new ArgumentNullException(nameof(sourceResults)))
                .ToList());
    }

    public IReadOnlyList<StartupItem> Items { get; }
    public IReadOnlyList<StartupScanSourceResult> SourceResults { get; }

    private static StartupItem Clone(StartupItem item) =>
        new()
        {
            Name = item.Name,
            Command = item.Command,
            Source = item.Source
        };
}
