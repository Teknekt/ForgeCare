using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

internal static class StartupVerificationTestFactory
{
    internal static readonly DateTime ExecutedAt =
        new(2026, 8, 23, 10, 0, 0, DateTimeKind.Utc);
    internal static readonly DateTime ObservedAt = ExecutedAt.AddMinutes(5);
    internal static readonly DateTime EvaluatedAt = ObservedAt.AddSeconds(1);

    internal static readonly string SessionId = Guid.Parse("11111111-1111-1111-1111-111111111111").ToString("N");
    internal static readonly string ReceiptId = Guid.Parse("22222222-2222-2222-2222-222222222222").ToString("N");

    internal static StartupVerificationTargetIdentity Hkcu(
        string name = "Example Agent",
        string value = @"C:\Program Files\Example\agent.exe") =>
        new StartupVerificationIdentityBuilder().BuildHkcuRun(name, value);

    internal static StartupVerificationTargetIdentity UserFile(
        string name = "Example.lnk",
        string value = @"C:\Users\Alice\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\Example.lnk") =>
        new StartupVerificationIdentityBuilder().BuildUserStartupFolder(name, value);

    internal static StartupActionReceipt Receipt(
        StartupVerificationOperation operation,
        StartupExecutionOutcome outcome,
        StartupVerificationTargetIdentity? target = null,
        string? sessionId = null,
        DateTime? executedAt = null,
        string? supersedesReceiptId = null)
    {
        target ??= Hkcu();
        var expected = operation == StartupVerificationOperation.Disable
            ? new StartupExpectedState(StartupExpectedStateKind.Absent)
            : new StartupExpectedState(
                StartupExpectedStateKind.PresentWithMatchingConfiguration,
                target.ConfigurationFingerprint);
        return new StartupActionReceipt(
            ReceiptId,
            sessionId ?? SessionId,
            operation,
            executedAt ?? ExecutedAt,
            new[] { new StartupActionReceiptItem(target, outcome, expected, "undo:example") },
            supersedesReceiptId);
    }

    internal static StartupObservationSnapshot Observation(
        StartupVerificationTargetKind kind,
        StartupSourceObservationStatus status = StartupSourceObservationStatus.Completed,
        IEnumerable<StartupVerificationTargetIdentity>? targets = null,
        bool ambiguous = false,
        string? sessionId = null,
        DateTime? observedAt = null) =>
        new(
            sessionId ?? SessionId,
            observedAt ?? ObservedAt,
            new[] { new StartupSourceObservation(kind, status, targets ?? Array.Empty<StartupVerificationTargetIdentity>(), ambiguous) });

    internal static StartupVerificationResult Evaluate(
        StartupActionReceipt receipt,
        StartupObservationSnapshot observation) =>
        new StartupVerificationEvaluator().Evaluate(receipt, observation, EvaluatedAt).Single();
}
