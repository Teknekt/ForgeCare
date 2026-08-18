using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ForgeCare.App.Models;
using Microsoft.Win32;

namespace ForgeCare.App.Services;

public class StartupManagerService
{
    private const string CurrentUserRun =
        @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly string _stateDirectory;
    private readonly string _undoFile;
    private readonly string _disabledStartupDirectory;

    public StartupManagerService()
    {
        _stateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Mindforge Studio", "ForgeCare", "StartupState");

        _undoFile = Path.Combine(_stateDirectory, "startup-undo.json");
        _disabledStartupDirectory = Path.Combine(_stateDirectory, "DisabledStartupFiles");
    }

    public List<StartupChangeItem> BuildPlan(IEnumerable<StartupImpactItem> impactItems)
    {
        return impactItems
            .Select(BuildPlanItem)
            .OrderByDescending(x => x.IsSelected)
            .ThenByDescending(x => x.ImpactScore)
            .ThenBy(x => x.Name)
            .ToList();
    }

    private StartupChangeItem BuildPlanItem(StartupImpactItem item)
    {
        var change = new StartupChangeItem
        {
            Name = item.Name,
            Source = item.Source,
            Command = item.Command,
            Category = item.Category,
            ImpactLevel = item.ImpactLevel,
            ImpactScore = item.ImpactScore,
            Recommendation = item.Recommendation,
            Confidence = item.Confidence
        };

        ResolveHandler(change);

        if (item.Recommendation == "KEEP")
        {
            change.IsLocked = true;
            change.IsSupported = false;
            change.Status = "LOCKED";
            change.StatusReason = "ForgeCare classified this entry as KEEP.";
            return change;
        }

        if (!change.IsSupported)
        {
            change.IsLocked = true;
            change.Status = "BLOCKED";
            change.StatusReason = "This source is outside Sprint 5C's safe current-user handlers.";
            return change;
        }

        change.IsSelected = item.Recommendation == "GOOD CANDIDATE";
        change.Status = change.IsSelected ? "SELECTED" : "AVAILABLE";
        change.StatusReason = change.IsSelected
            ? "Preselected for review. No change has been made."
            : "Available for manual review. No change has been made.";

        return change;
    }

    private static void ResolveHandler(StartupChangeItem item)
    {
        string source = item.Source ?? string.Empty;

        if (source.Contains("Current User Registry", StringComparison.OrdinalIgnoreCase))
        {
            item.HandlerType = "REGISTRY_HKCU";
            item.RegistryPath = CurrentUserRun;
            item.RegistryValueName = item.Name;
            item.IsSupported = true;
            return;
        }

        if (source.Contains("User Startup Folder", StringComparison.OrdinalIgnoreCase))
        {
            item.HandlerType = "STARTUP_FOLDER_USER";
            item.StartupFilePath = ResolveStartupFilePath(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup), item.Name);
            item.IsSupported = !string.IsNullOrWhiteSpace(item.StartupFilePath);
            return;
        }

        // HKLM and All Users remain deliberately locked in 5C.
        item.HandlerType = "UNSUPPORTED_MACHINE_WIDE";
        item.IsSupported = false;
    }

    private static string ResolveStartupFilePath(string folder, string name)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return string.Empty;

        try
        {
            return Directory.EnumerateFiles(folder)
                .FirstOrDefault(path => string.Equals(
                    Path.GetFileNameWithoutExtension(path),
                    name,
                    StringComparison.OrdinalIgnoreCase))
                ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public Task<StartupChangeResult> SimulateDisableAsync(
        IEnumerable<StartupChangeItem> selectedItems)
    {
        var items = selectedItems.Where(x => x.IsSelected).ToList();
        return Task.Run(() => Simulate(items));
    }

    private StartupChangeResult Simulate(IReadOnlyCollection<StartupChangeItem> items)
    {
        var result = new StartupChangeResult
        {
            IsDryRun = true,
            RequestedCount = items.Count
        };

        foreach (var item in items)
            ValidateForDisable(item, result);

        return result;
    }

    private void ValidateForDisable(StartupChangeItem item, StartupChangeResult result)
    {
        if (!item.IsSupported || item.IsLocked || item.Recommendation == "KEEP")
        {
            item.Status = "BLOCKED";
            item.StatusReason = "Safety policy does not allow this entry to be changed.";
            result.BlockedCount++;
            result.Items.Add(item);
            return;
        }

        try
        {
            if (item.HandlerType == "REGISTRY_HKCU")
            {
                using var key = Registry.CurrentUser.OpenSubKey(item.RegistryPath, false);
                object? value = key?.GetValue(
                    item.RegistryValueName, null,
                    RegistryValueOptions.DoNotExpandEnvironmentNames);

                if (value is null)
                {
                    item.Status = "SKIPPED";
                    item.StatusReason = "Registry value no longer exists.";
                    result.SkippedCount++;
                }
                else
                {
                    item.Status = "VALIDATED";
                    item.StatusReason = "HKCU Run value exists and can be backed up.";
                    result.ValidatedCount++;
                }
            }
            else if (item.HandlerType == "STARTUP_FOLDER_USER")
            {
                if (!File.Exists(item.StartupFilePath))
                {
                    item.Status = "SKIPPED";
                    item.StatusReason = "Startup file no longer exists.";
                    result.SkippedCount++;
                }
                else
                {
                    item.Status = "VALIDATED";
                    item.StatusReason = "Current-user Startup file can be moved reversibly.";
                    result.ValidatedCount++;
                }
            }
            else
            {
                item.Status = "BLOCKED";
                item.StatusReason = "Unsupported startup handler.";
                result.BlockedCount++;
            }
        }
        catch (UnauthorizedAccessException)
        {
            item.Status = "BLOCKED";
            item.StatusReason = "Access denied. ForgeCare did not elevate permissions.";
            result.BlockedCount++;
        }
        catch (Exception ex)
        {
            item.Status = "ERROR";
            item.StatusReason = ex.GetType().Name;
            result.ErrorCount++;
        }

        result.Items.Add(item);
    }

    public Task<StartupChangeResult> DisableAsync(
        IEnumerable<StartupChangeItem> selectedItems)
    {
        var items = selectedItems.Where(x => x.IsSelected).ToList();
        return Task.Run(() => Disable(items));
    }

    private StartupChangeResult Disable(IReadOnlyCollection<StartupChangeItem> items)
    {
        var result = new StartupChangeResult
        {
            IsDryRun = false,
            RequestedCount = items.Count
        };

        Directory.CreateDirectory(_stateDirectory);
        Directory.CreateDirectory(_disabledStartupDirectory);

        var undoRecords = LoadUndoRecords();

        foreach (var item in items)
        {
            var validation = new StartupChangeResult { IsDryRun = true };
            ValidateForDisable(item, validation);

            if (validation.ValidatedCount != 1)
            {
                result.BlockedCount += validation.BlockedCount;
                result.SkippedCount += validation.SkippedCount;
                result.ErrorCount += validation.ErrorCount;
                result.Items.Add(item);
                continue;
            }

            try
            {
                StartupUndoRecord record = item.HandlerType switch
                {
                    "REGISTRY_HKCU" => DisableRegistry(item),
                    "STARTUP_FOLDER_USER" => DisableStartupFile(item),
                    _ => throw new InvalidOperationException("No safe handler available.")
                };

                // Save undo state immediately after each successful reversible change.
                undoRecords.RemoveAll(existing => SameIdentity(existing, record));
                undoRecords.Add(record);
                SaveUndoRecords(undoRecords);

                item.Status = "DISABLED";
                item.StatusReason = "Disabled successfully. Undo state stored.";
                result.DisabledCount++;
            }
            catch (UnauthorizedAccessException)
            {
                item.Status = "BLOCKED";
                item.StatusReason = "Access denied. No permission escalation attempted.";
                result.BlockedCount++;
            }
            catch (Exception ex)
            {
                item.Status = "ERROR";
                item.StatusReason = ex.Message;
                result.ErrorCount++;
            }

            result.Items.Add(item);
        }

        return result;
    }

    private static StartupUndoRecord DisableRegistry(StartupChangeItem item)
    {
        using var key = Registry.CurrentUser.OpenSubKey(item.RegistryPath, true)
            ?? throw new InvalidOperationException("HKCU Run key is unavailable.");

        object? value = key.GetValue(
            item.RegistryValueName, null,
            RegistryValueOptions.DoNotExpandEnvironmentNames);

        if (value is null)
            throw new InvalidOperationException("Registry value no longer exists.");

        RegistryValueKind kind = key.GetValueKind(item.RegistryValueName);

        var record = new StartupUndoRecord
        {
            Name = item.Name,
            HandlerType = item.HandlerType,
            RegistryPath = item.RegistryPath,
            RegistryValueName = item.RegistryValueName,
            RegistryValueData = value.ToString() ?? string.Empty,
            RegistryValueKind = (int)kind,
            CreatedUtc = DateTime.UtcNow.ToString("O")
        };

        key.DeleteValue(item.RegistryValueName, true);

        if (key.GetValue(item.RegistryValueName, null,
                RegistryValueOptions.DoNotExpandEnvironmentNames) is not null)
            throw new InvalidOperationException("Registry disable could not be verified.");

        return record;
    }

    private StartupUndoRecord DisableStartupFile(StartupChangeItem item)
    {
        if (!File.Exists(item.StartupFilePath))
            throw new FileNotFoundException("Startup file no longer exists.", item.StartupFilePath);

        string fileName = Path.GetFileName(item.StartupFilePath);
        string disabledPath = BuildUniqueDisabledPath(fileName);

        var record = new StartupUndoRecord
        {
            Name = item.Name,
            HandlerType = item.HandlerType,
            OriginalFilePath = item.StartupFilePath,
            DisabledFilePath = disabledPath,
            CreatedUtc = DateTime.UtcNow.ToString("O")
        };

        File.Move(item.StartupFilePath, disabledPath);

        if (File.Exists(item.StartupFilePath) || !File.Exists(disabledPath))
            throw new InvalidOperationException("Startup file move could not be verified.");

        return record;
    }

    private string BuildUniqueDisabledPath(string fileName)
    {
        string candidate = Path.Combine(_disabledStartupDirectory, fileName);
        if (!File.Exists(candidate))
            return candidate;

        return Path.Combine(
            _disabledStartupDirectory,
            $"{Path.GetFileNameWithoutExtension(fileName)}-{DateTime.UtcNow:yyyyMMddHHmmssfff}{Path.GetExtension(fileName)}");
    }

    public int UndoRecordCount() => LoadUndoRecords().Count;

    public List<StartupUndoRecord> GetUndoRecords() => LoadUndoRecords();

    public Task<StartupChangeResult> RestoreAllAsync() =>
        Task.Run(RestoreAll);

    private StartupChangeResult RestoreAll()
    {
        var result = new StartupChangeResult { IsDryRun = false };
        var records = LoadUndoRecords();
        result.RequestedCount = records.Count;
        var remaining = new List<StartupUndoRecord>();

        foreach (var record in records)
        {
            var item = new StartupChangeItem
            {
                Name = record.Name,
                HandlerType = record.HandlerType
            };

            try
            {
                if (record.HandlerType == "REGISTRY_HKCU")
                    RestoreRegistry(record);
                else if (record.HandlerType == "STARTUP_FOLDER_USER")
                    RestoreStartupFile(record);
                else
                    throw new InvalidOperationException("Unsupported undo handler.");

                item.Status = "RESTORED";
                item.StatusReason = "Original startup state restored.";
                result.RestoredCount++;
            }
            catch (Exception ex)
            {
                item.Status = "ERROR";
                item.StatusReason = ex.Message;
                result.ErrorCount++;
                remaining.Add(record);
            }

            result.Items.Add(item);
        }

        SaveUndoRecords(remaining);
        return result;
    }

    private static void RestoreRegistry(StartupUndoRecord record)
    {
        using var key = Registry.CurrentUser.CreateSubKey(record.RegistryPath, true)
            ?? throw new InvalidOperationException("Could not open HKCU Run.");

        var kind = Enum.IsDefined(typeof(RegistryValueKind), record.RegistryValueKind)
            ? (RegistryValueKind)record.RegistryValueKind
            : RegistryValueKind.String;

        key.SetValue(record.RegistryValueName, record.RegistryValueData, kind);

        if (key.GetValue(record.RegistryValueName, null,
                RegistryValueOptions.DoNotExpandEnvironmentNames) is null)
            throw new InvalidOperationException("Registry restore could not be verified.");
    }

    private static void RestoreStartupFile(StartupUndoRecord record)
    {
        if (!File.Exists(record.DisabledFilePath))
            throw new FileNotFoundException(
                "ForgeCare's disabled startup file is missing.",
                record.DisabledFilePath);

        string? dir = Path.GetDirectoryName(record.OriginalFilePath);
        if (string.IsNullOrWhiteSpace(dir))
            throw new InvalidOperationException("Original Startup folder is invalid.");

        Directory.CreateDirectory(dir);

        if (File.Exists(record.OriginalFilePath))
            throw new IOException(
                "A file already exists at the original location; ForgeCare will not overwrite it.");

        File.Move(record.DisabledFilePath, record.OriginalFilePath);

        if (!File.Exists(record.OriginalFilePath))
            throw new InvalidOperationException("Startup file restore could not be verified.");
    }

    private List<StartupUndoRecord> LoadUndoRecords()
    {
        try
        {
            if (!File.Exists(_undoFile))
                return new();

            return JsonSerializer.Deserialize<List<StartupUndoRecord>>(
                       File.ReadAllText(_undoFile))
                   ?? new();
        }
        catch
        {
            return new();
        }
    }

    private void SaveUndoRecords(List<StartupUndoRecord> records)
    {
        Directory.CreateDirectory(_stateDirectory);

        string json = JsonSerializer.Serialize(
            records,
            new JsonSerializerOptions { WriteIndented = true });

        string temp = _undoFile + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, _undoFile, true);
    }

    private static bool SameIdentity(StartupUndoRecord a, StartupUndoRecord b)
    {
        if (!a.HandlerType.Equals(b.HandlerType, StringComparison.OrdinalIgnoreCase))
            return false;

        return a.HandlerType switch
        {
            "REGISTRY_HKCU" =>
                a.RegistryPath.Equals(b.RegistryPath, StringComparison.OrdinalIgnoreCase) &&
                a.RegistryValueName.Equals(b.RegistryValueName, StringComparison.OrdinalIgnoreCase),

            "STARTUP_FOLDER_USER" =>
                a.OriginalFilePath.Equals(b.OriginalFilePath, StringComparison.OrdinalIgnoreCase),

            _ => false
        };
    }
}