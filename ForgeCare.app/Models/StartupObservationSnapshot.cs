using System.Collections.ObjectModel;

namespace ForgeCare.App.Models;

public enum StartupSourceObservationStatus
{
    Completed,
    Failed,
    Unavailable,
    Unknown
}

public sealed class StartupSourceObservation
{
    public StartupSourceObservation(
        StartupVerificationTargetKind sourceKind,
        StartupSourceObservationStatus status,
        IEnumerable<StartupVerificationTargetIdentity> targets,
        bool isAmbiguous = false)
    {
        SourceKind = sourceKind;
        Status = status;
        Targets = new ReadOnlyCollection<StartupVerificationTargetIdentity>(
            (targets ?? throw new ArgumentNullException(nameof(targets))).ToList());
        IsAmbiguous = isAmbiguous;
    }

    public StartupVerificationTargetKind SourceKind { get; }
    public StartupSourceObservationStatus Status { get; }
    public IReadOnlyList<StartupVerificationTargetIdentity> Targets { get; }
    public bool IsAmbiguous { get; }
}

public sealed class StartupObservationSnapshot
{
    public StartupObservationSnapshot(
        string sessionId,
        DateTime observedAtUtc,
        IEnumerable<StartupSourceObservation> sources)
    {
        if (!Guid.TryParseExact(sessionId, "N", out _))
            throw new ArgumentException("Session ID must be a GUID in N format.", nameof(sessionId));
        if (observedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Observation timestamp must be UTC.", nameof(observedAtUtc));

        SessionId = sessionId;
        ObservedAtUtc = observedAtUtc;
        Sources = new ReadOnlyCollection<StartupSourceObservation>(
            (sources ?? throw new ArgumentNullException(nameof(sources))).ToList());
    }

    public string SessionId { get; }
    public DateTime ObservedAtUtc { get; }
    public IReadOnlyList<StartupSourceObservation> Sources { get; }
}
