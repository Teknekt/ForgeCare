using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class StartupVerificationLiveResult
{
    public StartupVerificationLiveResult(
        int receiptCount,
        int evaluatedCount,
        int persistedCount,
        int failureCount)
    {
        ReceiptCount = receiptCount;
        EvaluatedCount = evaluatedCount;
        PersistedCount = persistedCount;
        FailureCount = failureCount;
    }

    public int ReceiptCount { get; }
    public int EvaluatedCount { get; }
    public int PersistedCount { get; }
    public int FailureCount { get; }
}

public sealed class StartupVerificationLiveService
{
    private readonly ForgeReportService _reportService;
    private readonly StartupObservationSnapshotBuilder _snapshotBuilder;
    private readonly StartupVerificationEvaluator _evaluator;
    private readonly Func<DateTime> _utcNow;
    private readonly Action<Exception, string> _log;

    public StartupVerificationLiveService(
        ForgeReportService reportService,
        StartupObservationSnapshotBuilder? snapshotBuilder = null,
        StartupVerificationEvaluator? evaluator = null)
        : this(
            reportService,
            snapshotBuilder ?? new StartupObservationSnapshotBuilder(),
            evaluator ?? new StartupVerificationEvaluator(),
            () => DateTime.UtcNow,
            CrashLogService.RecordPrivacySafe)
    {
    }

    internal StartupVerificationLiveService(
        ForgeReportService reportService,
        StartupObservationSnapshotBuilder snapshotBuilder,
        StartupVerificationEvaluator evaluator,
        Func<DateTime> utcNow,
        Action<Exception, string> log)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _snapshotBuilder = snapshotBuilder ?? throw new ArgumentNullException(nameof(snapshotBuilder));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public StartupVerificationLiveResult VerifyAfterSystemScan(SystemSnapshot snapshot)
    {
        int receiptCount = 0;
        int evaluatedCount = 0;
        int persistedCount = 0;
        int failureCount = 0;

        try
        {
            ForgeReportSession session = _reportService.Snapshot();
            StartupObservationSnapshot observation =
                _snapshotBuilder.Build(snapshot, session.SessionId);
            IReadOnlyList<StartupActionReceipt> pending =
                _reportService.GetPendingStartupActionReceipts();
            receiptCount = pending.Count;

            foreach (StartupActionReceipt receipt in pending)
            {
                IReadOnlyList<StartupVerificationResult> results;
                try
                {
                    results = _evaluator.Evaluate(receipt, observation, _utcNow());
                }
                catch (Exception ex)
                {
                    failureCount++;
                    LogBounded(ex, "Startup verification evaluation");
                    continue;
                }

                foreach (StartupVerificationResult result in results)
                {
                    evaluatedCount++;
                    try
                    {
                        if (_reportService.RecordStartupVerificationResult(result))
                            persistedCount++;
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        LogBounded(ex, "Startup verification result persistence");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            failureCount++;
            LogBounded(ex, "Startup verification after System Scan");
        }

        return new StartupVerificationLiveResult(
            receiptCount,
            evaluatedCount,
            persistedCount,
            failureCount);
    }

    private void LogBounded(Exception exception, string context)
    {
        _log(exception, context);
    }
}
