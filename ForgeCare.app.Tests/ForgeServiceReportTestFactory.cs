using ForgeCare.App.Models;

namespace ForgeCare.App.Tests;

internal static class ForgeServiceReportTestFactory
{
    internal static readonly DateTime ExecutedAt =
        new(2026, 8, 23, 8, 0, 0, DateTimeKind.Utc);

    internal static ForgeReportSession Session() => new()
    {
        SessionId = Guid.NewGuid().ToString("N"),
        StartedAt = new DateTime(2026, 8, 23, 7, 0, 0, DateTimeKind.Local),
        UpdatedAt = new DateTime(2026, 8, 23, 9, 0, 0, DateTimeKind.Local)
    };

    internal static StartupVerificationTargetIdentity Target(
        string name = "Agent",
        string locator = "locator-a",
        StartupVerificationTargetKind kind = StartupVerificationTargetKind.HkcuRun) =>
        new(
            kind,
            name,
            kind == StartupVerificationTargetKind.HkcuRun ? "hkcu-run" : "user-startup",
            Hash(locator),
            Hash("target-" + locator),
            Hash("configuration-" + locator));

    internal static StartupActionReceipt Receipt(
        string sessionId,
        StartupExecutionOutcome outcome = StartupExecutionOutcome.Executed,
        StartupVerificationOperation operation = StartupVerificationOperation.Disable,
        StartupVerificationTargetIdentity? target = null,
        DateTime? executedAt = null,
        string? receiptId = null,
        string? recoveryReference = "undo:test",
        string? supersedesReceiptId = null) =>
        new(
            receiptId ?? Guid.NewGuid().ToString("N"),
            sessionId,
            operation,
            executedAt ?? ExecutedAt,
            [new StartupActionReceiptItem(
                target ?? Target(),
                outcome,
                operation == StartupVerificationOperation.Disable
                    ? new StartupExpectedState(StartupExpectedStateKind.Absent)
                    : new StartupExpectedState(
                        StartupExpectedStateKind.PresentWithMatchingConfiguration,
                        Hash("expected")),
                recoveryReference)],
            supersedesReceiptId);

    internal static StartupVerificationResult Verification(
        StartupActionReceipt receipt,
        StartupVerificationStatus status = StartupVerificationStatus.Verified,
        string? targetId = null,
        string? receiptId = null,
        DateTime? observedAt = null,
        string rationale = "A bounded factual observation was recorded.")
    {
        StartupActionReceiptItem item = receipt.Items[0];
        return new StartupVerificationResult(
            receiptId ?? receipt.ReceiptId,
            targetId ?? item.TargetIdentity.TargetId,
            status,
            (observedAt ?? receipt.ExecutedAtUtc.AddMinutes(1)).AddMinutes(1),
            observedAt ?? receipt.ExecutedAtUtc.AddMinutes(1),
            rationale,
            item.ExpectedState.Summary,
            item.TargetIdentity.SourceLocator);
    }

    internal static EvidenceRecord Evidence(
        string sessionId,
        int index = 0,
        string subject = "cpu-pressure") => new()
    {
        Id = GuidFromInt(index + 1),
        SessionId = sessionId,
        TimestampUtc = ExecutedAt.AddSeconds(index),
        Category = EvidenceCategory.Cpu,
        Source = EvidenceSource.DeepAnalysis,
        Subject = subject,
        Observation = "A factual observation.",
        Severity = EvidenceSeverity.Medium,
        Confidence = EvidenceConfidence.High,
        Collector = "Test",
        Metadata = new Dictionary<string, string> { ["private"] = "not projected" },
        CorrelationKey = "private:correlation"
    };

    internal static string Hash(string value)
    {
        byte[] bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(bytes);
    }

    private static Guid GuidFromInt(int value)
    {
        byte[] bytes = new byte[16];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return new Guid(bytes);
    }
}
