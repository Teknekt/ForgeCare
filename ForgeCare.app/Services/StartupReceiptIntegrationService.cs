using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class StartupReceiptIntegrationService
{
    private readonly StartupActionReceiptBuilder _builder;
    private readonly Action<Exception, string> _log;

    public StartupReceiptIntegrationService(
        StartupActionReceiptBuilder? builder = null,
        Action<Exception, string>? log = null)
    {
        _builder = builder ?? new StartupActionReceiptBuilder();
        _log = log ?? CrashLogService.Record;
    }

    public bool TryRecordDisable(
        string sessionId,
        DateTime executedAtUtc,
        StartupChangeResult result,
        IEnumerable<StartupChangeItem> reviewedItems,
        IEnumerable<StartupUndoRecord> undoRecords,
        Func<StartupActionReceipt, bool> persist)
    {
        return TryRecord(
            () => _builder.BuildDisable(
                sessionId, executedAtUtc, result, reviewedItems, undoRecords),
            persist,
            StartupVerificationOperation.Disable);
    }

    public bool TryRecordRestore(
        string sessionId,
        DateTime executedAtUtc,
        StartupChangeResult result,
        IEnumerable<StartupUndoRecord> originalUndoRecords,
        Func<StartupActionReceipt, bool> persist)
    {
        return TryRecord(
            () => _builder.BuildRestore(
                sessionId, executedAtUtc, result, originalUndoRecords),
            persist,
            StartupVerificationOperation.Restore);
    }

    private bool TryRecord(
        Func<StartupActionReceiptBuildResult> build,
        Func<StartupActionReceipt, bool> persist,
        StartupVerificationOperation operation)
    {
        ArgumentNullException.ThrowIfNull(persist);
        try
        {
            StartupActionReceiptBuildResult built = build();
            if (built.Warnings.Count > 0)
            {
                _log(
                    new InvalidOperationException(
                        $"Startup receipt {operation} completed with {built.Warnings.Count} target warning(s)."),
                    "Startup receipt integration");
            }
            if (built.Receipt == null)
                return false;
            return persist(built.Receipt);
        }
        catch (Exception ex)
        {
            var sanitized = new InvalidOperationException(
                $"Startup receipt {operation} integration failed ({ex.GetType().Name}).");
            _log(sanitized, "Startup receipt integration");
            return false;
        }
    }
}
