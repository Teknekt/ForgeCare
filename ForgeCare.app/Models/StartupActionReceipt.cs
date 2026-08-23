using System.Collections.ObjectModel;

namespace ForgeCare.App.Models;

public enum StartupVerificationOperation
{
    Disable,
    Restore
}

public enum StartupExecutionOutcome
{
    Executed,
    Failed,
    Blocked,
    Skipped,
    DryRun
}

public sealed class StartupActionReceiptItem
{
    public StartupActionReceiptItem(
        StartupVerificationTargetIdentity targetIdentity,
        StartupExecutionOutcome executionOutcome,
        StartupExpectedState expectedState,
        string? recoveryReference = null)
    {
        TargetIdentity = targetIdentity ?? throw new ArgumentNullException(nameof(targetIdentity));
        ExecutionOutcome = executionOutcome;
        ExpectedState = expectedState ?? throw new ArgumentNullException(nameof(expectedState));
        RecoveryReference = BoundOptional(recoveryReference, 80);
    }

    public StartupVerificationTargetIdentity TargetIdentity { get; }
    public StartupExecutionOutcome ExecutionOutcome { get; }
    public StartupExpectedState ExpectedState { get; }
    public string? RecoveryReference { get; }
    public bool IsVerificationEligible =>
        ExecutionOutcome == StartupExecutionOutcome.Executed && TargetIdentity.IsSupported;

    private static string? BoundOptional(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        string normalized = value.Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }
}

public sealed class StartupActionReceipt
{
    public StartupActionReceipt(
        string receiptId,
        string sessionId,
        StartupVerificationOperation operation,
        DateTime executedAtUtc,
        IReadOnlyList<StartupActionReceiptItem> items,
        string? supersedesReceiptId = null)
    {
        if (!Guid.TryParseExact(receiptId, "N", out _))
            throw new ArgumentException("Receipt ID must be a GUID in N format.", nameof(receiptId));
        if (!Guid.TryParseExact(sessionId, "N", out _))
            throw new ArgumentException("Session ID must be a GUID in N format.", nameof(sessionId));
        if (executedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Execution timestamp must be UTC.", nameof(executedAtUtc));
        if (!string.IsNullOrWhiteSpace(supersedesReceiptId) &&
            !Guid.TryParseExact(supersedesReceiptId, "N", out _))
            throw new ArgumentException("Superseded receipt ID must be a GUID in N format.", nameof(supersedesReceiptId));

        ReceiptId = receiptId;
        SessionId = sessionId;
        Operation = operation;
        ExecutedAtUtc = executedAtUtc;
        Items = new ReadOnlyCollection<StartupActionReceiptItem>(
            (items ?? throw new ArgumentNullException(nameof(items))).ToList());
        SupersedesReceiptId = string.IsNullOrWhiteSpace(supersedesReceiptId)
            ? null
            : supersedesReceiptId;
    }

    public string ReceiptId { get; }
    public string SessionId { get; }
    public StartupVerificationOperation Operation { get; }
    public DateTime ExecutedAtUtc { get; }
    public IReadOnlyList<StartupActionReceiptItem> Items { get; }
    public string? SupersedesReceiptId { get; }
}
