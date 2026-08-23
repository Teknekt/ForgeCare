using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class StartupObservationSnapshotBuilder
{
    private readonly StartupVerificationIdentityBuilder _identityBuilder;

    public StartupObservationSnapshotBuilder(
        StartupVerificationIdentityBuilder? identityBuilder = null)
    {
        _identityBuilder = identityBuilder ?? new StartupVerificationIdentityBuilder();
    }

    public StartupObservationSnapshot Build(
        SystemSnapshot snapshot,
        string sessionId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        StartupSourceObservation hkcu = BuildSource(
            snapshot,
            StartupScanSourceKind.CurrentUserRegistry,
            StartupVerificationTargetKind.HkcuRun);
        StartupSourceObservation userFolder = BuildSource(
            snapshot,
            StartupScanSourceKind.UserStartupFolder,
            StartupVerificationTargetKind.UserStartupFolder);

        return new StartupObservationSnapshot(
            sessionId,
            snapshot.ScanTime.ToUniversalTime(),
            new[] { hkcu, userFolder });
    }

    private StartupSourceObservation BuildSource(
        SystemSnapshot snapshot,
        StartupScanSourceKind scanSource,
        StartupVerificationTargetKind targetKind)
    {
        StartupScanSourceResult[] matchingSources = snapshot.StartupSourceResults
            .Where(source => source.Source == scanSource)
            .ToArray();
        if (matchingSources.Length != 1)
        {
            return new StartupSourceObservation(
                targetKind,
                StartupSourceObservationStatus.Unknown,
                Array.Empty<StartupVerificationTargetIdentity>(),
                isAmbiguous: matchingSources.Length > 1);
        }

        StartupScanSourceResult source = matchingSources[0];
        var targets = new List<StartupVerificationTargetIdentity>();
        bool ambiguous = false;
        foreach (StartupItem item in source.Items)
        {
            try
            {
                targets.Add(targetKind switch
                {
                    StartupVerificationTargetKind.HkcuRun =>
                        _identityBuilder.BuildHkcuRun(item.Name, item.Command),
                    StartupVerificationTargetKind.UserStartupFolder =>
                        _identityBuilder.BuildUserStartupFolder(
                            string.IsNullOrWhiteSpace(item.Command) ? item.Name : item.Command,
                            item.Command),
                    _ => throw new InvalidOperationException(
                        "The startup source is not supported by live verification.")
                });
            }
            catch (ArgumentException)
            {
                ambiguous = true;
            }
        }

        if (targets.GroupBy(target => target.LocatorId, StringComparer.Ordinal).Any(group => group.Count() > 1))
            ambiguous = true;

        return new StartupSourceObservation(
            targetKind,
            MapStatus(source.Status),
            targets,
            ambiguous);
    }

    private static StartupSourceObservationStatus MapStatus(
        StartupScanSourceStatus status) =>
        status switch
        {
            StartupScanSourceStatus.Completed => StartupSourceObservationStatus.Completed,
            StartupScanSourceStatus.Failed => StartupSourceObservationStatus.Failed,
            StartupScanSourceStatus.Unavailable => StartupSourceObservationStatus.Unavailable,
            _ => StartupSourceObservationStatus.Unknown
        };
}
