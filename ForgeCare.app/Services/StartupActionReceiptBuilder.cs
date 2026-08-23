using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class StartupActionReceiptBuildResult
{
    public StartupActionReceiptBuildResult(
        StartupActionReceipt? receipt,
        IEnumerable<string> warnings)
    {
        Receipt = receipt;
        Warnings = new ReadOnlyCollection<string>(
            (warnings ?? throw new ArgumentNullException(nameof(warnings)))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Take(50)
                .ToList());
    }

    public StartupActionReceipt? Receipt { get; }
    public IReadOnlyList<string> Warnings { get; }
}

public sealed class StartupActionReceiptBuilder
{
    private readonly StartupVerificationIdentityBuilder _identityBuilder;

    public StartupActionReceiptBuilder(StartupVerificationIdentityBuilder? identityBuilder = null)
    {
        _identityBuilder = identityBuilder ?? new StartupVerificationIdentityBuilder();
    }

    public StartupActionReceiptBuildResult BuildDisable(
        string sessionId,
        DateTime executedAtUtc,
        StartupChangeResult mutationResult,
        IEnumerable<StartupChangeItem> reviewedItems,
        IEnumerable<StartupUndoRecord> currentUndoRecords)
    {
        ArgumentNullException.ThrowIfNull(mutationResult);
        return Build(
            sessionId,
            executedAtUtc,
            StartupVerificationOperation.Disable,
            mutationResult,
            reviewedItems,
            currentUndoRecords);
    }

    public StartupActionReceiptBuildResult BuildRestore(
        string sessionId,
        DateTime executedAtUtc,
        StartupChangeResult mutationResult,
        IEnumerable<StartupUndoRecord> originalUndoRecords)
    {
        ArgumentNullException.ThrowIfNull(mutationResult);
        StartupUndoRecord[] undo = (originalUndoRecords ??
            throw new ArgumentNullException(nameof(originalUndoRecords))).ToArray();
        var reviewed = new List<StartupChangeItem>();
        for (int index = 0; index < mutationResult.Items.Count; index++)
        {
            StartupChangeItem item = mutationResult.Items[index];
            StartupUndoRecord? record = index < undo.Length ? undo[index] : null;
            reviewed.Add(new StartupChangeItem
            {
                Name = item.Name,
                HandlerType = item.HandlerType,
                RegistryPath = record?.RegistryPath ?? string.Empty,
                RegistryValueName = record?.RegistryValueName ?? string.Empty,
                StartupFilePath = record?.OriginalFilePath ?? string.Empty
            });
        }

        return Build(
            sessionId,
            executedAtUtc,
            StartupVerificationOperation.Restore,
            mutationResult,
            reviewed,
            undo);
    }

    private StartupActionReceiptBuildResult Build(
        string sessionId,
        DateTime executedAtUtc,
        StartupVerificationOperation operation,
        StartupChangeResult mutationResult,
        IEnumerable<StartupChangeItem> reviewedItems,
        IEnumerable<StartupUndoRecord> undoRecords)
    {
        StartupChangeItem[] reviewed = (reviewedItems ??
            throw new ArgumentNullException(nameof(reviewedItems))).ToArray();
        StartupUndoRecord[] undo = (undoRecords ??
            throw new ArgumentNullException(nameof(undoRecords))).ToArray();
        var items = new List<StartupActionReceiptItem>();
        var warnings = new List<string>();

        for (int index = 0; index < mutationResult.Items.Count; index++)
        {
            StartupChangeItem resultItem = mutationResult.Items[index];
            try
            {
                StartupChangeItem context = FindReviewedContext(resultItem, reviewed) ?? resultItem;
                StartupUndoRecord? undoRecord = FindUndoRecord(context, undo, index, operation);
                StartupExecutionOutcome outcome = MapOutcome(mutationResult.IsDryRun, resultItem.Status);
                StartupVerificationTargetIdentity identity = BuildIdentity(context, undoRecord);
                StartupExpectedState expected = BuildExpectedState(operation, identity);
                string? recoveryReference = undoRecord == null ? null : BuildUndoReference(undoRecord);
                items.Add(new StartupActionReceiptItem(identity, outcome, expected, recoveryReference));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                warnings.Add($"One startup target receipt item was omitted ({ex.GetType().Name}).");
            }
        }

        if (items.Count == 0)
            return new StartupActionReceiptBuildResult(null, warnings);

        var receipt = new StartupActionReceipt(
            Guid.NewGuid().ToString("N"),
            sessionId,
            operation,
            executedAtUtc,
            items);
        return new StartupActionReceiptBuildResult(receipt, warnings);
    }

    private StartupVerificationTargetIdentity BuildIdentity(
        StartupChangeItem item,
        StartupUndoRecord? undo)
    {
        string name = string.IsNullOrWhiteSpace(item.Name) ? "Unnamed startup target" : item.Name;
        return item.HandlerType switch
        {
            "REGISTRY_HKCU" => _identityBuilder.BuildHkcuRun(
                string.IsNullOrWhiteSpace(item.RegistryValueName) ? name : item.RegistryValueName,
                undo?.RegistryValueData ?? item.Command),
            "STARTUP_FOLDER_USER" => _identityBuilder.BuildUserStartupFolder(
                undo?.OriginalFilePath ?? item.StartupFilePath ?? name,
                undo?.OriginalFilePath ?? item.StartupFilePath),
            _ => _identityBuilder.BuildUnsupported(name)
        };
    }

    private static StartupExpectedState BuildExpectedState(
        StartupVerificationOperation operation,
        StartupVerificationTargetIdentity identity)
    {
        if (operation == StartupVerificationOperation.Disable)
            return new StartupExpectedState(StartupExpectedStateKind.Absent);

        if (string.IsNullOrWhiteSpace(identity.ConfigurationFingerprint))
            throw new InvalidOperationException("Restore target did not have a configuration fingerprint.");

        return new StartupExpectedState(
            StartupExpectedStateKind.PresentWithMatchingConfiguration,
            identity.ConfigurationFingerprint);
    }

    private static StartupExecutionOutcome MapOutcome(bool isDryRun, string? status)
    {
        if (isDryRun)
            return StartupExecutionOutcome.DryRun;

        return (status ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "DISABLED" or "RESTORED" => StartupExecutionOutcome.Executed,
            "BLOCKED" => StartupExecutionOutcome.Blocked,
            "SKIPPED" => StartupExecutionOutcome.Skipped,
            _ => StartupExecutionOutcome.Failed
        };
    }

    private static StartupChangeItem? FindReviewedContext(
        StartupChangeItem result,
        IEnumerable<StartupChangeItem> reviewed)
    {
        StartupChangeItem[] matches = reviewed.Where(candidate => SameTarget(candidate, result)).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    private static StartupUndoRecord? FindUndoRecord(
        StartupChangeItem item,
        IReadOnlyList<StartupUndoRecord> undo,
        int resultIndex,
        StartupVerificationOperation operation)
    {
        if (operation == StartupVerificationOperation.Restore)
            return resultIndex < undo.Count ? undo[resultIndex] : null;

        StartupUndoRecord[] matches = undo.Where(record => SameTarget(item, record)).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    private static bool SameTarget(StartupChangeItem left, StartupChangeItem right)
    {
        if (!string.Equals(left.HandlerType, right.HandlerType, StringComparison.OrdinalIgnoreCase))
            return false;
        return left.HandlerType switch
        {
            "REGISTRY_HKCU" =>
                string.Equals(left.RegistryPath, right.RegistryPath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(left.RegistryValueName, right.RegistryValueName, StringComparison.OrdinalIgnoreCase),
            "STARTUP_FOLDER_USER" =>
                string.Equals(left.StartupFilePath, right.StartupFilePath, StringComparison.OrdinalIgnoreCase),
            _ => string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static bool SameTarget(StartupChangeItem item, StartupUndoRecord record)
    {
        if (!string.Equals(item.HandlerType, record.HandlerType, StringComparison.OrdinalIgnoreCase))
            return false;
        return item.HandlerType switch
        {
            "REGISTRY_HKCU" =>
                string.Equals(item.RegistryPath, record.RegistryPath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.RegistryValueName, record.RegistryValueName, StringComparison.OrdinalIgnoreCase),
            "STARTUP_FOLDER_USER" =>
                string.Equals(item.StartupFilePath, record.OriginalFilePath, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static string BuildUndoReference(StartupUndoRecord record)
    {
        if (Guid.TryParseExact(record.Id, "N", out _))
            return "undo:" + record.Id;

        string structural = record.HandlerType switch
        {
            "REGISTRY_HKCU" => $"{record.HandlerType}|{record.RegistryPath}|{record.RegistryValueName}",
            "STARTUP_FOLDER_USER" => $"{record.HandlerType}|{record.OriginalFilePath}",
            _ => $"{record.HandlerType}|{record.Name}|{record.CreatedUtc}"
        };
        string hash = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(structural.ToLowerInvariant())))[..20];
        return "legacy-undo:" + hash;
    }
}
