using System.Collections.ObjectModel;

namespace ForgeCare.App.Models;

public enum ServiceReportActivityKind
{
    DiagnosticActivity,
    SystemChange,
    Recovery,
    LegacyActivity
}

public enum ServiceReportExecutionStatus
{
    Executed,
    Failed,
    Blocked,
    Skipped,
    DryRun
}

public enum ServiceReportVerificationStatus
{
    None,
    Pending,
    Verified,
    NotVerified,
    Inconclusive,
    NotEligible
}

public enum ServiceReportTraceabilityStatus
{
    ExecutedAndVerified,
    ExecutedNotVerified,
    ExecutedInconclusive,
    PendingVerification,
    ExecutionFailed,
    NotExecuted,
    Superseded,
    NotEligible
}

public enum ServiceReportUnresolvedKind
{
    ExecutionFailed,
    NotExecuted,
    ExpectedStateNotObserved,
    InconclusiveObservation,
    PendingVerification,
    OrphanVerification,
    IncompleteTraceability
}

public sealed record ServiceReportSessionSummary(
    string SessionId,
    DateTime StartedAt,
    DateTime UpdatedAt,
    DateTime? LatestCheckpointAt,
    int DiagnosticActivityCount,
    int NonDiagnosticActivityCount,
    int StartupActionTargetCount,
    int EvidenceReferenceCount);

public sealed record ServiceReportActivity(
    DateTime Timestamp,
    ServiceReportActivityKind Kind,
    string Category,
    string Title,
    bool RecordedSuccessful);

public sealed record ServiceReportCheckpoint(
    DateTime Timestamp,
    int HealthScore,
    string HealthRating,
    double SystemDriveFreeGb,
    double StorageFreePercent,
    double AvailableMemoryGb,
    double MemoryAvailablePercent,
    int StartupCount);

public sealed record ServiceReportEvidenceReference(
    Guid EvidenceId,
    string Reference,
    DateTime TimestampUtc,
    EvidenceCategory Category,
    string CategoryDisplay,
    EvidenceSource Source,
    string SourceDisplay,
    string SubjectDisplay,
    EvidenceSeverity Severity,
    string SeverityDisplay,
    EvidenceConfidence Confidence,
    string ConfidenceDisplay);

public sealed record ServiceReportVerification(
    ServiceReportVerificationStatus Status,
    DateTime EvaluatedAtUtc,
    DateTime ObservationTimestampUtc,
    string StatusDisplay,
    string Summary);

public sealed class ServiceReportAction
{
    public ServiceReportAction(
        string receiptId,
        string targetId,
        StartupVerificationOperation operation,
        DateTime executedAtUtc,
        string targetDisplayName,
        StartupVerificationTargetKind targetKind,
        string sourceDisplay,
        ServiceReportExecutionStatus executionStatus,
        string executionDisplay,
        string expectedState,
        bool verificationEligible,
        bool hasRecoveryReference,
        string? supersedesReceiptId,
        string? supersededByReceiptId,
        ServiceReportVerificationStatus verificationStatus,
        ServiceReportTraceabilityStatus traceabilityStatus,
        IEnumerable<ServiceReportVerification> verifications)
    {
        ReceiptId = receiptId;
        TargetId = targetId;
        Operation = operation;
        ExecutedAtUtc = executedAtUtc;
        TargetDisplayName = targetDisplayName;
        TargetKind = targetKind;
        SourceDisplay = sourceDisplay;
        ExecutionStatus = executionStatus;
        ExecutionDisplay = executionDisplay;
        ExpectedState = expectedState;
        VerificationEligible = verificationEligible;
        HasRecoveryReference = hasRecoveryReference;
        SupersedesReceiptId = supersedesReceiptId;
        SupersededByReceiptId = supersededByReceiptId;
        VerificationStatus = verificationStatus;
        TraceabilityStatus = traceabilityStatus;
        Verifications = Copy(verifications);
    }

    public string ReceiptId { get; }
    public string TargetId { get; }
    public StartupVerificationOperation Operation { get; }
    public DateTime ExecutedAtUtc { get; }
    public string TargetDisplayName { get; }
    public StartupVerificationTargetKind TargetKind { get; }
    public string SourceDisplay { get; }
    public ServiceReportExecutionStatus ExecutionStatus { get; }
    public string ExecutionDisplay { get; }
    public string ExpectedState { get; }
    public bool VerificationEligible { get; }
    public bool HasRecoveryReference { get; }
    public string? SupersedesReceiptId { get; }
    public string? SupersededByReceiptId { get; }
    public ServiceReportVerificationStatus VerificationStatus { get; }
    public ServiceReportTraceabilityStatus TraceabilityStatus { get; }
    public IReadOnlyList<ServiceReportVerification> Verifications { get; }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToArray());
}

public sealed record ServiceReportUnresolvedItem(
    ServiceReportUnresolvedKind Kind,
    DateTime Timestamp,
    string Summary,
    string? ReceiptId,
    string? TargetId);

public sealed class ForgeServiceReportModel
{
    public ForgeServiceReportModel(
        ServiceReportSessionSummary sessionSummary,
        IEnumerable<ServiceReportActivity> activities,
        IEnumerable<ServiceReportAction> actions,
        IEnumerable<ServiceReportEvidenceReference> evidenceReferences,
        IEnumerable<ServiceReportCheckpoint> checkpoints,
        IEnumerable<ServiceReportUnresolvedItem> unresolvedItems,
        IEnumerable<string> warnings)
    {
        SessionSummary = sessionSummary ?? throw new ArgumentNullException(nameof(sessionSummary));
        Activities = Copy(activities);
        Actions = Copy(actions);
        EvidenceReferences = Copy(evidenceReferences);
        Checkpoints = Copy(checkpoints);
        UnresolvedItems = Copy(unresolvedItems);
        Warnings = Copy(warnings);
    }

    public ServiceReportSessionSummary SessionSummary { get; }
    public IReadOnlyList<ServiceReportActivity> Activities { get; }
    public IReadOnlyList<ServiceReportAction> Actions { get; }
    public IReadOnlyList<ServiceReportEvidenceReference> EvidenceReferences { get; }
    public IReadOnlyList<ServiceReportCheckpoint> Checkpoints { get; }
    public IReadOnlyList<ServiceReportUnresolvedItem> UnresolvedItems { get; }
    public IReadOnlyList<string> Warnings { get; }
    public ServiceReportCheckpoint? InitialCheckpoint => Checkpoints.FirstOrDefault();
    public ServiceReportCheckpoint? LatestCheckpoint => Checkpoints.LastOrDefault();

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToArray());
}
