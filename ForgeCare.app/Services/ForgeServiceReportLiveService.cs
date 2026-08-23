using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class ForgeServiceReportLiveService
{
    private const string EvidenceUnavailableWarning =
        "Evidence references could not be loaded for this report.";

    private readonly IEvidenceRepository _evidenceRepository;
    private readonly ForgeServiceReportBuilder _builder;
    private readonly ForgeServiceReportHtmlRenderer _renderer;

    public ForgeServiceReportLiveService(
        IEvidenceRepository evidenceRepository,
        ForgeServiceReportBuilder builder,
        ForgeServiceReportHtmlRenderer renderer)
    {
        _evidenceRepository = evidenceRepository ?? throw new ArgumentNullException(nameof(evidenceRepository));
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public async Task<ForgeServiceReportLiveResult> GenerateAsync(
        ForgeReportSession session,
        CancellationToken cancellationToken = default)
    {
        if (session == null || !Guid.TryParseExact(session.SessionId, "N", out _))
            return ForgeServiceReportLiveResult.Failed(ForgeServiceReportLiveFailure.InvalidSession);

        IReadOnlyList<EvidenceRecord>? evidence;
        bool evidenceUnavailable = false;
        try
        {
            evidence = await _evidenceRepository.GetBySessionAsync(
                session.SessionId,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            evidence = null;
            evidenceUnavailable = true;
            try
            {
                ForgeServiceReportModel partial = _builder.Build(session);
                partial = WithWarning(partial, EvidenceUnavailableWarning);
                return ForgeServiceReportLiveResult.Complete(
                    _renderer.Render(partial),
                    evidenceUnavailable: true,
                    partial.Warnings);
            }
            catch (Exception generationException)
            {
                return ForgeServiceReportLiveResult.Failed(
                    ForgeServiceReportLiveFailure.GenerationFailed,
                    generationException.GetType().Name + "/" + exception.GetType().Name);
            }
        }

        try
        {
            ForgeServiceReportModel report = _builder.Build(session, evidence);
            return ForgeServiceReportLiveResult.Complete(
                _renderer.Render(report),
                evidenceUnavailable,
                report.Warnings);
        }
        catch (Exception exception)
        {
            return ForgeServiceReportLiveResult.Failed(
                ForgeServiceReportLiveFailure.GenerationFailed,
                exception.GetType().Name);
        }
    }

    private static ForgeServiceReportModel WithWarning(
        ForgeServiceReportModel report,
        string warning) =>
        new(
            report.SessionSummary,
            report.Activities,
            report.Actions,
            report.EvidenceReferences,
            report.Checkpoints,
            report.UnresolvedItems,
            report.Warnings
                .Append(warning)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal));
}
