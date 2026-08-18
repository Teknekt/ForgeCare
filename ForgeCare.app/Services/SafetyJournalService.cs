using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class SafetyJournalService
{
    private static readonly Lazy<SafetyJournalService> LazyInstance =
        new(() => new SafetyJournalService());

    public static SafetyJournalService Instance => LazyInstance.Value;

    private readonly object _sync = new();
    private readonly string _directory;
    private readonly string _journalFile;
    private readonly string _snapshotFile;
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    private SafetyJournalService()
    {
        _directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Mindforge Studio", "ForgeCare", "Safety");
        _journalFile = Path.Combine(_directory, "action-journal.json");
        _snapshotFile = Path.Combine(_directory, "safety-snapshots.json");
    }

    public List<SafetyJournalEntry> GetJournal()
    {
        lock (_sync)
            return Load<List<SafetyJournalEntry>>(_journalFile) ?? new();
    }

    public List<SafetySnapshot> GetSnapshots()
    {
        lock (_sync)
            return Load<List<SafetySnapshot>>(_snapshotFile) ?? new();
    }

    public void Record(
        string category, string action, string target, string result,
        string detail, bool reversible, string recovery)
    {
        lock (_sync)
        {
            var entries = Load<List<SafetyJournalEntry>>(_journalFile) ?? new();
            entries.Add(new SafetyJournalEntry
            {
                Timestamp = DateTime.Now,
                Category = category,
                Action = action,
                Target = target,
                Result = result,
                Detail = detail,
                IsReversible = reversible,
                Recovery = recovery
            });

            if (entries.Count > 1000)
                entries = entries.OrderByDescending(x => x.Timestamp).Take(1000).OrderBy(x => x.Timestamp).ToList();

            SaveAtomic(_journalFile, entries);
        }
    }

    public SafetySnapshot CaptureStartupSnapshot(
        string reason,
        IEnumerable<StartupUndoRecord> currentUndoRecords)
    {
        lock (_sync)
        {
            var snapshots = Load<List<SafetySnapshot>>(_snapshotFile) ?? new();
            var snapshot = new SafetySnapshot
            {
                CreatedAt = DateTime.Now,
                Reason = reason,
                StartupUndoRecords = currentUndoRecords.ToList()
            };
            snapshots.Add(snapshot);

            if (snapshots.Count > 50)
                snapshots = snapshots.OrderByDescending(x => x.CreatedAt).Take(50).OrderBy(x => x.CreatedAt).ToList();

            SaveAtomic(_snapshotFile, snapshots);
            return snapshot;
        }
    }

    public void ClearJournal()
    {
        lock (_sync)
            SaveAtomic(_journalFile, new List<SafetyJournalEntry>());
    }

    private T? Load<T>(string path)
    {
        try
        {
            if (!File.Exists(path)) return default;
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), _json);
        }
        catch
        {
            return default;
        }
    }

    private void SaveAtomic<T>(string path, T value)
    {
        Directory.CreateDirectory(_directory);
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value, _json));
        File.Move(temp, path, true);
    }
}
