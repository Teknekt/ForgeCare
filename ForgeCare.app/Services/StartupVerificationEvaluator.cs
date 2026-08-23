using System.Collections.ObjectModel;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class StartupVerificationEvaluator
{
    public IReadOnlyList<StartupVerificationResult> Evaluate(
        StartupActionReceipt receipt,
        StartupObservationSnapshot observation,
        DateTime evaluatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        ArgumentNullException.ThrowIfNull(observation);
        if (evaluatedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Evaluation timestamp must be UTC.", nameof(evaluatedAtUtc));

        var results = new List<StartupVerificationResult>(receipt.Items.Count);
        foreach (StartupActionReceiptItem item in receipt.Items)
            results.Add(EvaluateItem(receipt, item, observation, evaluatedAtUtc));
        return new ReadOnlyCollection<StartupVerificationResult>(results);
    }

    private static StartupVerificationResult EvaluateItem(
        StartupActionReceipt receipt,
        StartupActionReceiptItem item,
        StartupObservationSnapshot observation,
        DateTime evaluatedAtUtc)
    {
        if (!item.IsVerificationEligible)
            return Result(receipt, item, observation, evaluatedAtUtc,
                StartupVerificationStatus.NotEligible,
                "This target did not have an eligible executed startup mutation.");

        if (!string.Equals(receipt.SessionId, observation.SessionId, StringComparison.Ordinal))
            return Result(receipt, item, observation, evaluatedAtUtc,
                StartupVerificationStatus.NotEligible,
                "The startup observation belongs to a different report session.");

        if (observation.ObservedAtUtc <= receipt.ExecutedAtUtc)
            return Result(receipt, item, observation, evaluatedAtUtc,
                StartupVerificationStatus.Inconclusive,
                "The startup observation was not later than the execution receipt.");

        StartupSourceObservation[] sources = observation.Sources
            .Where(source => source.SourceKind == item.TargetIdentity.Kind)
            .ToArray();
        if (sources.Length != 1)
            return Result(receipt, item, observation, evaluatedAtUtc,
                StartupVerificationStatus.Inconclusive,
                "The expected startup source did not have one unambiguous observation result.");

        StartupSourceObservation source = sources[0];
        if (source.Status != StartupSourceObservationStatus.Completed)
            return Result(receipt, item, observation, evaluatedAtUtc,
                StartupVerificationStatus.Inconclusive,
                "The expected state could not be established because the startup source was not completely observed.");

        if (source.IsAmbiguous)
            return Result(receipt, item, observation, evaluatedAtUtc,
                StartupVerificationStatus.Inconclusive,
                "The expected state could not be established because the startup observation was ambiguous.");

        StartupVerificationTargetIdentity[] matches = source.Targets
            .Where(target => string.Equals(
                target.LocatorId,
                item.TargetIdentity.LocatorId,
                StringComparison.Ordinal))
            .ToArray();
        if (matches.Length > 1)
            return Result(receipt, item, observation, evaluatedAtUtc,
                StartupVerificationStatus.Inconclusive,
                "Multiple startup observations matched the reviewed configuration target.");

        return receipt.Operation switch
        {
            StartupVerificationOperation.Disable => EvaluateDisable(
                receipt, item, observation, evaluatedAtUtc, matches),
            StartupVerificationOperation.Restore => EvaluateRestore(
                receipt, item, observation, evaluatedAtUtc, matches),
            _ => Result(receipt, item, observation, evaluatedAtUtc,
                StartupVerificationStatus.NotEligible,
                "The startup operation is not eligible for configuration verification.")
        };
    }

    private static StartupVerificationResult EvaluateDisable(
        StartupActionReceipt receipt,
        StartupActionReceiptItem item,
        StartupObservationSnapshot observation,
        DateTime evaluatedAtUtc,
        IReadOnlyCollection<StartupVerificationTargetIdentity> matches)
    {
        if (item.ExpectedState.Kind != StartupExpectedStateKind.Absent)
            return Result(receipt, item, observation, evaluatedAtUtc,
                StartupVerificationStatus.Inconclusive,
                "The receipt did not contain the expected disable configuration state.");

        return matches.Count == 0
            ? Result(receipt, item, observation, evaluatedAtUtc,
                StartupVerificationStatus.Verified,
                "A later startup observation no longer contained the reviewed startup target after the disable operation.")
            : Result(receipt, item, observation, evaluatedAtUtc,
                StartupVerificationStatus.ExpectedOutcomeNotObserved,
                "The later startup observation still contained the reviewed startup target.");
    }

    private static StartupVerificationResult EvaluateRestore(
        StartupActionReceipt receipt,
        StartupActionReceiptItem item,
        StartupObservationSnapshot observation,
        DateTime evaluatedAtUtc,
        IReadOnlyList<StartupVerificationTargetIdentity> matches)
    {
        if (item.ExpectedState.Kind != StartupExpectedStateKind.PresentWithMatchingConfiguration ||
            string.IsNullOrWhiteSpace(item.ExpectedState.ExpectedConfigurationFingerprint))
        {
            return Result(receipt, item, observation, evaluatedAtUtc,
                StartupVerificationStatus.Inconclusive,
                "The receipt did not contain a complete expected restore configuration state.");
        }

        if (matches.Count == 0)
            return Result(receipt, item, observation, evaluatedAtUtc,
                StartupVerificationStatus.ExpectedOutcomeNotObserved,
                "The later startup observation did not contain the reviewed restored target.");

        bool fingerprintMatches = string.Equals(
            matches[0].ConfigurationFingerprint,
            item.ExpectedState.ExpectedConfigurationFingerprint,
            StringComparison.Ordinal);
        return fingerprintMatches
            ? Result(receipt, item, observation, evaluatedAtUtc,
                StartupVerificationStatus.Verified,
                "A later startup observation contained the reviewed target with the expected configuration fingerprint after the restore operation.")
            : Result(receipt, item, observation, evaluatedAtUtc,
                StartupVerificationStatus.ExpectedOutcomeNotObserved,
                "The later startup observation contained the reviewed target with a different configuration fingerprint.");
    }

    private static StartupVerificationResult Result(
        StartupActionReceipt receipt,
        StartupActionReceiptItem item,
        StartupObservationSnapshot observation,
        DateTime evaluatedAtUtc,
        StartupVerificationStatus status,
        string rationale) =>
        new(
            receipt.ReceiptId,
            item.TargetIdentity.TargetId,
            status,
            evaluatedAtUtc,
            observation.ObservedAtUtc,
            rationale,
            item.ExpectedState.Summary,
            item.TargetIdentity.SourceLocator);
}
