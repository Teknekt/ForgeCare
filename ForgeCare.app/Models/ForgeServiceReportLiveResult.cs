using System.Collections.ObjectModel;

namespace ForgeCare.App.Models;

public enum ForgeServiceReportLiveFailure
{
    None,
    InvalidSession,
    GenerationFailed
}

public sealed class ForgeServiceReportLiveResult
{
    private ForgeServiceReportLiveResult(
        bool success,
        string? html,
        bool evidenceUnavailable,
        IEnumerable<string> warnings,
        ForgeServiceReportLiveFailure failure,
        string? failureType)
    {
        Success = success;
        Html = html;
        EvidenceUnavailable = evidenceUnavailable;
        Warnings = new ReadOnlyCollection<string>((warnings ?? Array.Empty<string>()).ToArray());
        Failure = failure;
        FailureType = failureType;
    }

    public bool Success { get; }
    public string? Html { get; }
    public bool EvidenceUnavailable { get; }
    public bool GeneratedWithDataNotes => Success && Warnings.Count > 0;
    public IReadOnlyList<string> Warnings { get; }
    public ForgeServiceReportLiveFailure Failure { get; }
    public string? FailureType { get; }

    public static ForgeServiceReportLiveResult Complete(
        string html,
        bool evidenceUnavailable,
        IEnumerable<string> warnings) =>
        new(true, html, evidenceUnavailable, warnings, ForgeServiceReportLiveFailure.None, null);

    public static ForgeServiceReportLiveResult Failed(
        ForgeServiceReportLiveFailure failure,
        string? failureType = null) =>
        new(false, null, false, Array.Empty<string>(), failure, failureType);
}
