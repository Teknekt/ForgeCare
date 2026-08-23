using System.Globalization;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class ForgeServiceReportBuilder
{
    private static readonly HashSet<StartupVerificationStatus> TerminalStatuses =
    [
        StartupVerificationStatus.Verified,
        StartupVerificationStatus.ExpectedOutcomeNotObserved,
        StartupVerificationStatus.NotEligible
    ];

    public ForgeServiceReportModel Build(
        ForgeReportSession session,
        IReadOnlyCollection<EvidenceRecord>? evidence = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        var warnings = new List<string>();
        IReadOnlyList<ServiceReportActivity> activities = BuildActivities(session.Actions, warnings);
        IReadOnlyList<ServiceReportCheckpoint> checkpoints = BuildCheckpoints(session.Checkpoints, warnings);
        IReadOnlyList<ServiceReportEvidenceReference> evidenceReferences =
            BuildEvidenceReferences(session.SessionId, evidence, warnings);
        IReadOnlyList<ServiceReportAction> actions = BuildActions(
            session.SessionId,
            session.StartupActionReceipts,
            session.StartupVerificationResults,
            warnings,
            out IReadOnlyList<ServiceReportUnresolvedItem> unresolved);

        if (activities.Count > 0 && actions.Count == 0)
            warnings.Add("This legacy session does not contain typed Verification 2.0 records.");

        string sessionId = IsSessionId(session.SessionId) ? session.SessionId : string.Empty;
        if (sessionId.Length == 0)
            warnings.Add("The report session identity is unavailable or invalid.");

        var summary = new ServiceReportSessionSummary(
            sessionId,
            session.StartedAt,
            session.UpdatedAt,
            checkpoints.LastOrDefault()?.Timestamp,
            activities.Count(value => value.Kind == ServiceReportActivityKind.DiagnosticActivity),
            activities.Count(value => value.Kind != ServiceReportActivityKind.DiagnosticActivity),
            actions.Count,
            evidenceReferences.Count);

        return new ForgeServiceReportModel(
            summary,
            activities,
            actions,
            evidenceReferences,
            checkpoints,
            unresolved,
            warnings.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
    }

    private static IReadOnlyList<ServiceReportActivity> BuildActivities(
        IEnumerable<ForgeReportAction>? source,
        ICollection<string> warnings)
    {
        if (source == null)
        {
            warnings.Add("Legacy report activity data was unavailable.");
            return Array.Empty<ServiceReportActivity>();
        }

        return source
            .Where(value => value != null)
            .Select(value =>
            {
                (ServiceReportActivityKind kind, string category, string title) =
                    ClassifyActivity(value.Category);
                return new ServiceReportActivity(
                    value.Timestamp,
                    kind,
                    category,
                    title,
                    value.IsSuccess);
            })
            .OrderBy(value => value.Timestamp)
            .ThenBy(value => value.Category, StringComparer.Ordinal)
            .ThenBy(value => value.Title, StringComparer.Ordinal)
            .ToArray();
    }

    private static (ServiceReportActivityKind Kind, string Category, string Title) ClassifyActivity(
        string? category) =>
        category?.Trim().ToUpperInvariant() switch
        {
            "SYSTEM" => (ServiceReportActivityKind.DiagnosticActivity, "SYSTEM", "System scan"),
            "ANALYSIS" => (ServiceReportActivityKind.DiagnosticActivity, "ANALYSIS", "Deep analysis"),
            "SERVICES" => (ServiceReportActivityKind.DiagnosticActivity, "SERVICES", "Service analysis"),
            "STORAGE" => (ServiceReportActivityKind.DiagnosticActivity, "STORAGE", "Storage analysis"),
            "DUPLICATES" => (ServiceReportActivityKind.DiagnosticActivity, "DUPLICATES", "Duplicate scan"),
            "CLEANUP" => (ServiceReportActivityKind.SystemChange, "CLEANUP", "Legacy cleanup activity"),
            "STORAGE CLEANUP" => (ServiceReportActivityKind.SystemChange, "STORAGE CLEANUP", "Legacy storage cleanup activity"),
            "RECOVERY" => (ServiceReportActivityKind.Recovery, "RECOVERY", "Legacy recovery activity"),
            "STARTUP" => (ServiceReportActivityKind.LegacyActivity, "STARTUP", "Legacy startup activity"),
            _ => (ServiceReportActivityKind.LegacyActivity, "OTHER", "Legacy report activity")
        };

    private static IReadOnlyList<ServiceReportCheckpoint> BuildCheckpoints(
        IEnumerable<ForgeReportCheckpoint>? source,
        ICollection<string> warnings)
    {
        if (source == null)
        {
            warnings.Add("Session checkpoint data was unavailable.");
            return Array.Empty<ServiceReportCheckpoint>();
        }

        return source
            .Where(value => value != null)
            .Select(value => new ServiceReportCheckpoint(
                value.Timestamp,
                value.HealthScore,
                SafeLabel(value.HealthRating, "Unrated"),
                value.SystemDriveFreeGb,
                value.StorageFreePercent,
                value.AvailableMemoryGb,
                value.MemoryAvailablePercent,
                value.StartupCount))
            .OrderBy(value => value.Timestamp)
            .ThenBy(value => value.HealthScore)
            .ToArray();
    }

    private static IReadOnlyList<ServiceReportEvidenceReference> BuildEvidenceReferences(
        string? sessionId,
        IReadOnlyCollection<EvidenceRecord>? source,
        ICollection<string> warnings)
    {
        if (source == null)
            return Array.Empty<ServiceReportEvidenceReference>();

        if (!IsSessionId(sessionId))
        {
            if (source.Count > 0)
                warnings.Add("Some Evidence records could not be included.");
            return Array.Empty<ServiceReportEvidenceReference>();
        }

        int invalidCount = 0;
        var candidates = new List<EvidenceRecord>();
        foreach (EvidenceRecord? record in source)
        {
            if (record == null ||
                record.Validate().Count > 0 ||
                !string.Equals(record.SessionId, sessionId, StringComparison.Ordinal))
            {
                invalidCount++;
                continue;
            }
            candidates.Add(record);
        }

        var references = new List<ServiceReportEvidenceReference>();
        foreach (IGrouping<Guid, EvidenceRecord> group in candidates.GroupBy(value => value.Id))
        {
            EvidenceRecord[] ordered = group
                .OrderBy(CanonicalEvidenceKey, StringComparer.Ordinal)
                .ToArray();
            if (ordered.Length > 1)
                invalidCount += ordered.Length - 1;

            EvidenceRecord record = ordered[0];
            references.Add(new ServiceReportEvidenceReference(
                record.Id,
                record.Id.ToString("N", CultureInfo.InvariantCulture)[..10],
                record.TimestampUtc,
                record.Category,
                EvidenceDisplayFormatter.FormatCategory(record.Category),
                record.Source,
                EvidenceDisplayFormatter.FormatSource(record.Source),
                SafeEvidenceSubject(record.Subject),
                record.Severity,
                EvidenceDisplayFormatter.FormatSeverity(record.Severity),
                record.Confidence,
                EvidenceDisplayFormatter.FormatConfidence(record.Confidence)));
        }

        if (invalidCount > 0)
            warnings.Add("Some Evidence records could not be included.");

        return references
            .OrderBy(value => value.TimestampUtc)
            .ThenBy(value => value.EvidenceId)
            .ToArray();
    }

    private static IReadOnlyList<ServiceReportAction> BuildActions(
        string? sessionId,
        IEnumerable<StartupActionReceipt>? receiptSource,
        IEnumerable<StartupVerificationResult>? verificationSource,
        ICollection<string> warnings,
        out IReadOnlyList<ServiceReportUnresolvedItem> unresolved)
    {
        var unresolvedItems = new List<ServiceReportUnresolvedItem>();
        StartupActionReceipt[] receipts = SelectReceipts(sessionId, receiptSource, warnings);
        StartupVerificationResult[] results = SelectVerificationResults(verificationSource, warnings);

        var targetKeys = receipts
            .SelectMany(receipt => receipt.Items.Select(item => (receipt.ReceiptId, item.TargetIdentity.TargetId)))
            .ToHashSet();
        StartupVerificationResult[] orphanResults = results
            .Where(result => !targetKeys.Contains((result.ReceiptId, result.TargetId)))
            .ToArray();
        if (orphanResults.Length > 0)
        {
            warnings.Add("Some startup verification data could not be linked to a typed action.");
            unresolvedItems.AddRange(orphanResults.Select(result => new ServiceReportUnresolvedItem(
                ServiceReportUnresolvedKind.OrphanVerification,
                result.ObservationTimestampUtc,
                "A startup verification observation could not be linked to a typed action target.",
                SafeId(result.ReceiptId),
                SafeId(result.TargetId))));
        }

        var resultIndex = results
            .Where(result => targetKeys.Contains((result.ReceiptId, result.TargetId)))
            .GroupBy(result => (result.ReceiptId, result.TargetId))
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(result => result.ObservationTimestampUtc)
                    .ThenBy(result => result.EvaluatedAtUtc)
                    .ThenBy(result => result.Status)
                    .ToArray());

        var actions = new List<ServiceReportAction>();
        foreach (StartupActionReceipt receipt in receipts)
        {
            foreach (StartupActionReceiptItem item in receipt.Items
                         .OrderBy(value => value.TargetIdentity.TargetId, StringComparer.Ordinal))
            {
                StartupVerificationResult[] linked = resultIndex.GetValueOrDefault(
                    (receipt.ReceiptId, item.TargetIdentity.TargetId), Array.Empty<StartupVerificationResult>());
                if (linked.Any(result => !string.Equals(
                        result.SourceLocator,
                        item.TargetIdentity.SourceLocator,
                        StringComparison.Ordinal)))
                {
                    warnings.Add("Some startup verification source information did not match its typed action target.");
                }

                ServiceReportVerification[] projectedVerification = linked
                    .Select(ProjectVerification)
                    .ToArray();
                StartupVerificationResult? terminal = linked
                    .Where(result => TerminalStatuses.Contains(result.Status))
                    .LastOrDefault();
                StartupVerificationResult? latest = linked.LastOrDefault();
                StartupActionReceipt? superseding = FindSupersedingReceipt(receipt, item, receipts);
                bool superseded = superseding != null;
                ServiceReportVerificationStatus verificationStatus = DetermineVerificationStatus(
                    item,
                    terminal,
                    latest,
                    superseded);
                ServiceReportTraceabilityStatus traceability = DetermineTraceabilityStatus(
                    item.ExecutionOutcome,
                    verificationStatus,
                    superseded);

                var action = new ServiceReportAction(
                    receipt.ReceiptId,
                    item.TargetIdentity.TargetId,
                    receipt.Operation,
                    receipt.ExecutedAtUtc,
                    SafeTargetDisplay(item.TargetIdentity.DisplayName),
                    item.TargetIdentity.Kind,
                    SourceDisplay(item.TargetIdentity.Kind),
                    MapExecutionStatus(item.ExecutionOutcome),
                    ExecutionDisplay(item.ExecutionOutcome),
                    ExpectedStateDisplay(item.ExpectedState.Kind),
                    item.IsVerificationEligible,
                    !string.IsNullOrWhiteSpace(item.RecoveryReference),
                    SafeId(receipt.SupersedesReceiptId),
                    superseding?.ReceiptId,
                    verificationStatus,
                    traceability,
                    projectedVerification);
                actions.Add(action);
                AddUnresolved(action, unresolvedItems);
            }
        }

        unresolved = unresolvedItems
            .OrderBy(value => value.Timestamp)
            .ThenBy(value => value.Kind)
            .ThenBy(value => value.ReceiptId, StringComparer.Ordinal)
            .ThenBy(value => value.TargetId, StringComparer.Ordinal)
            .ToArray();
        return actions
            .OrderBy(value => value.ExecutedAtUtc)
            .ThenBy(value => value.ReceiptId, StringComparer.Ordinal)
            .ThenBy(value => value.TargetId, StringComparer.Ordinal)
            .ToArray();
    }

    private static StartupActionReceipt[] SelectReceipts(
        string? sessionId,
        IEnumerable<StartupActionReceipt>? source,
        ICollection<string> warnings)
    {
        if (source == null)
        {
            warnings.Add("Typed startup action data was unavailable.");
            return Array.Empty<StartupActionReceipt>();
        }

        int invalid = 0;
        var valid = new List<StartupActionReceipt>();
        foreach (StartupActionReceipt? receipt in source)
        {
            if (receipt == null ||
                !string.Equals(receipt.SessionId, sessionId, StringComparison.Ordinal) ||
                !Enum.IsDefined(receipt.Operation) ||
                receipt.Items.Any(item => item == null ||
                    !Enum.IsDefined(item.ExecutionOutcome) ||
                    !Enum.IsDefined(item.TargetIdentity.Kind) ||
                    !Enum.IsDefined(item.ExpectedState.Kind)))
            {
                invalid++;
                continue;
            }
            valid.Add(receipt);
        }

        var selected = new List<StartupActionReceipt>();
        foreach (IGrouping<string, StartupActionReceipt> group in valid.GroupBy(
                     value => value.ReceiptId,
                     StringComparer.Ordinal))
        {
            StartupActionReceipt[] ordered = group
                .OrderBy(CanonicalReceiptKey, StringComparer.Ordinal)
                .ToArray();
            selected.Add(ordered[0]);
            if (ordered.Skip(1).Any(value => CanonicalReceiptKey(value) != CanonicalReceiptKey(ordered[0])))
                warnings.Add("Conflicting duplicate startup action data was reduced to one deterministic record.");
        }

        if (invalid > 0)
            warnings.Add("Some typed startup action data could not be included.");

        return selected
            .OrderBy(value => value.ExecutedAtUtc)
            .ThenBy(value => value.ReceiptId, StringComparer.Ordinal)
            .ToArray();
    }

    private static StartupVerificationResult[] SelectVerificationResults(
        IEnumerable<StartupVerificationResult>? source,
        ICollection<string> warnings)
    {
        if (source == null)
        {
            warnings.Add("Startup verification data was unavailable.");
            return Array.Empty<StartupVerificationResult>();
        }

        int invalid = 0;
        var valid = source
            .Where(result =>
            {
                bool isValid = result != null &&
                    Enum.IsDefined(result.Status) &&
                    result.EvaluatedAtUtc.Kind == DateTimeKind.Utc &&
                    result.ObservationTimestampUtc.Kind == DateTimeKind.Utc;
                if (!isValid)
                    invalid++;
                return isValid;
            })
            .ToArray();

        var selected = new List<StartupVerificationResult>();
        foreach (IGrouping<string, StartupVerificationResult> group in valid.GroupBy(
                     VerificationIdentity,
                     StringComparer.Ordinal))
        {
            StartupVerificationResult[] ordered = group
                .OrderBy(CanonicalVerificationKey, StringComparer.Ordinal)
                .ToArray();
            selected.Add(ordered[0]);
            if (ordered.Skip(1).Any(value => CanonicalVerificationKey(value) != CanonicalVerificationKey(ordered[0])))
                warnings.Add("Conflicting duplicate startup verification data was reduced to one deterministic record.");
        }

        if (invalid > 0)
            warnings.Add("Some startup verification data could not be included.");
        return selected.ToArray();
    }

    private static StartupActionReceipt? FindSupersedingReceipt(
        StartupActionReceipt receipt,
        StartupActionReceiptItem item,
        IEnumerable<StartupActionReceipt> receipts) =>
        receipts
            .Where(candidate =>
                candidate.ExecutedAtUtc > receipt.ExecutedAtUtc &&
                candidate.Operation != receipt.Operation &&
                candidate.Items.Any(candidateItem =>
                    candidateItem.IsVerificationEligible &&
                    string.Equals(
                        candidateItem.TargetIdentity.LocatorId,
                        item.TargetIdentity.LocatorId,
                        StringComparison.Ordinal)))
            .OrderBy(candidate => candidate.ExecutedAtUtc)
            .ThenBy(candidate => candidate.ReceiptId, StringComparer.Ordinal)
            .FirstOrDefault();

    private static ServiceReportVerification ProjectVerification(StartupVerificationResult result) =>
        result.Status switch
        {
            StartupVerificationStatus.Verified => new(
                ServiceReportVerificationStatus.Verified,
                result.EvaluatedAtUtc,
                result.ObservationTimestampUtc,
                "Verified",
                "A later System Scan observed the expected startup configuration state."),
            StartupVerificationStatus.ExpectedOutcomeNotObserved => new(
                ServiceReportVerificationStatus.NotVerified,
                result.EvaluatedAtUtc,
                result.ObservationTimestampUtc,
                "Not Verified",
                "A later System Scan did not observe the expected startup configuration state."),
            StartupVerificationStatus.Inconclusive => new(
                ServiceReportVerificationStatus.Inconclusive,
                result.EvaluatedAtUtc,
                result.ObservationTimestampUtc,
                "Inconclusive",
                "The available observation could not establish the expected startup configuration state."),
            _ => new(
                ServiceReportVerificationStatus.NotEligible,
                result.EvaluatedAtUtc,
                result.ObservationTimestampUtc,
                "Not Eligible",
                "No verification claim was applicable to this startup target.")
        };

    private static ServiceReportVerificationStatus DetermineVerificationStatus(
        StartupActionReceiptItem item,
        StartupVerificationResult? terminal,
        StartupVerificationResult? latest,
        bool superseded)
    {
        if (superseded)
            return ServiceReportVerificationStatus.None;
        if (!item.IsVerificationEligible)
            return latest?.Status == StartupVerificationStatus.NotEligible
                ? ServiceReportVerificationStatus.NotEligible
                : ServiceReportVerificationStatus.None;
        if (terminal != null)
        {
            return terminal.Status switch
            {
                StartupVerificationStatus.Verified => ServiceReportVerificationStatus.Verified,
                StartupVerificationStatus.ExpectedOutcomeNotObserved => ServiceReportVerificationStatus.NotVerified,
                _ => ServiceReportVerificationStatus.NotEligible
            };
        }
        return latest?.Status == StartupVerificationStatus.Inconclusive
            ? ServiceReportVerificationStatus.Inconclusive
            : ServiceReportVerificationStatus.Pending;
    }

    private static ServiceReportTraceabilityStatus DetermineTraceabilityStatus(
        StartupExecutionOutcome execution,
        ServiceReportVerificationStatus verification,
        bool superseded)
    {
        if (execution == StartupExecutionOutcome.Failed)
            return ServiceReportTraceabilityStatus.ExecutionFailed;
        if (execution is StartupExecutionOutcome.Blocked or StartupExecutionOutcome.Skipped or StartupExecutionOutcome.DryRun)
            return ServiceReportTraceabilityStatus.NotExecuted;
        if (superseded)
            return ServiceReportTraceabilityStatus.Superseded;
        return verification switch
        {
            ServiceReportVerificationStatus.Verified => ServiceReportTraceabilityStatus.ExecutedAndVerified,
            ServiceReportVerificationStatus.NotVerified => ServiceReportTraceabilityStatus.ExecutedNotVerified,
            ServiceReportVerificationStatus.Inconclusive => ServiceReportTraceabilityStatus.ExecutedInconclusive,
            ServiceReportVerificationStatus.Pending => ServiceReportTraceabilityStatus.PendingVerification,
            _ => ServiceReportTraceabilityStatus.NotEligible
        };
    }

    private static void AddUnresolved(
        ServiceReportAction action,
        ICollection<ServiceReportUnresolvedItem> unresolved)
    {
        (ServiceReportUnresolvedKind Kind, string Summary)? item = action.TraceabilityStatus switch
        {
            ServiceReportTraceabilityStatus.ExecutionFailed =>
                (ServiceReportUnresolvedKind.ExecutionFailed, "A startup action target did not execute successfully."),
            ServiceReportTraceabilityStatus.NotExecuted =>
                (ServiceReportUnresolvedKind.NotExecuted, "A startup action target was not changed."),
            ServiceReportTraceabilityStatus.ExecutedNotVerified =>
                (ServiceReportUnresolvedKind.ExpectedStateNotObserved, "The expected startup configuration state was not observed."),
            ServiceReportTraceabilityStatus.ExecutedInconclusive =>
                (ServiceReportUnresolvedKind.InconclusiveObservation, "The expected startup configuration state remains unresolved after an inconclusive observation."),
            ServiceReportTraceabilityStatus.PendingVerification =>
                (ServiceReportUnresolvedKind.PendingVerification, "The executed startup action is awaiting a conclusive later observation."),
            _ => null
        };
        if (item != null)
        {
            unresolved.Add(new ServiceReportUnresolvedItem(
                item.Value.Kind,
                action.ExecutedAtUtc,
                item.Value.Summary,
                action.ReceiptId,
                action.TargetId));
        }
    }

    private static ServiceReportExecutionStatus MapExecutionStatus(StartupExecutionOutcome value) =>
        (ServiceReportExecutionStatus)value;

    private static string ExecutionDisplay(StartupExecutionOutcome value) => value switch
    {
        StartupExecutionOutcome.Executed => "Executed",
        StartupExecutionOutcome.Failed => "Execution Failed",
        StartupExecutionOutcome.Blocked => "Blocked",
        StartupExecutionOutcome.Skipped => "Skipped",
        _ => "Dry Run — Not Executed"
    };

    private static string ExpectedStateDisplay(StartupExpectedStateKind value) => value switch
    {
        StartupExpectedStateKind.Absent => "The reviewed startup target is absent from its configured source.",
        _ => "The reviewed startup target is present with its expected configuration."
    };

    private static string SourceDisplay(StartupVerificationTargetKind value) => value switch
    {
        StartupVerificationTargetKind.HkcuRun => "Current-user Run entry",
        StartupVerificationTargetKind.UserStartupFolder => "User Startup folder entry",
        _ => "Unsupported startup source"
    };

    private static string SafeTargetDisplay(string? value) =>
        IsSafeDisplay(value) ? value!.Trim() : "Startup target";

    private static string SafeEvidenceSubject(string? value) =>
        IsSafeIdentifier(value)
            ? EvidenceDisplayFormatter.FormatSubject(value)
            : "EVIDENCE OBSERVATION";

    private static string SafeLabel(string? value, string fallback) =>
        IsSafeDisplay(value) ? value!.Trim() : fallback;

    private static bool IsSafeDisplay(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 120)
            return false;
        string text = value.Trim();
        return !text.Contains('\\') &&
            !text.Contains('/') &&
            !text.Contains('=') &&
            !text.Contains("--", StringComparison.Ordinal) &&
            !text.Contains("token", StringComparison.OrdinalIgnoreCase) &&
            !text.Contains("api_key", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSafeIdentifier(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 160 &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is ':' or '-' or '_');

    private static string? SafeId(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Length > 64 ||
        value.Any(character => !char.IsAsciiLetterOrDigit(character))
            ? null
            : value;

    private static bool IsSessionId(string? value) =>
        Guid.TryParseExact(value, "N", out _);

    private static string VerificationIdentity(StartupVerificationResult value) =>
        string.Join('|', value.ReceiptId, value.TargetId, value.ObservationTimestampUtc.ToString("O", CultureInfo.InvariantCulture));

    private static string CanonicalVerificationKey(StartupVerificationResult value) =>
        string.Join('|', VerificationIdentity(value), value.Status, value.EvaluatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            value.ExpectedStateSummary, value.SourceLocator, value.Rationale);

    private static string CanonicalReceiptKey(StartupActionReceipt value) =>
        string.Join('|', value.ReceiptId, value.SessionId, value.Operation,
            value.ExecutedAtUtc.ToString("O", CultureInfo.InvariantCulture), value.SupersedesReceiptId,
            string.Join(';', value.Items
                .OrderBy(item => item.TargetIdentity.TargetId, StringComparer.Ordinal)
                .Select(item => string.Join(',', item.TargetIdentity.TargetId, item.TargetIdentity.LocatorId,
                    item.ExecutionOutcome, item.ExpectedState.Kind, item.RecoveryReference))));

    private static string CanonicalEvidenceKey(EvidenceRecord value) =>
        string.Join('|', value.Id, value.SessionId, value.TimestampUtc.ToString("O", CultureInfo.InvariantCulture),
            value.Category, value.Source, value.Subject, value.Severity, value.Confidence);
}
