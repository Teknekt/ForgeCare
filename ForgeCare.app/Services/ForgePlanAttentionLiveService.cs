using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class ForgePlanAttentionLiveService
{
    private readonly IEvidenceRepository _repository;
    private readonly EvidenceCorrelationEngine _correlationEngine;
    private readonly ForgePlanAttentionPresenter _presenter;
    private readonly Action<Exception, string> _logFailure;

    public ForgePlanAttentionLiveService(
        IEvidenceRepository repository,
        EvidenceCorrelationEngine correlationEngine,
        ForgePlanAttentionPresenter presenter,
        Action<Exception, string>? logFailure = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _correlationEngine = correlationEngine ?? throw new ArgumentNullException(nameof(correlationEngine));
        _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        _logFailure = logFailure ?? CrashLogService.Record;
    }

    public async Task<ForgePlanAttentionBuildResult> BuildAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IReadOnlyList<EvidenceRecord> evidence =
                await _repository.GetBySessionAsync(sessionId, cancellationToken);

            EvidenceCorrelationResult correlation =
                _correlationEngine.Correlate(evidence, sessionId);

            IReadOnlyList<ForgePlanAttentionItem> items =
                _presenter.Present(correlation.Items, evidence);

            ForgePlanAttentionBuildState state = correlation.PartialSuccess
                ? ForgePlanAttentionBuildState.PartialSuccess
                : items.Count == 0 && correlation.Errors.Count > 0
                    ? ForgePlanAttentionBuildState.Failed
                    : items.Count == 0
                        ? ForgePlanAttentionBuildState.Empty
                        : ForgePlanAttentionBuildState.Ready;

            return new ForgePlanAttentionBuildResult(
                state,
                items,
                correlation.Errors.Count);
        }
        catch (OperationCanceledException ex)
        {
            _logFailure(
                new OperationCanceledException(
                    $"Forge Plan attention build canceled ({ex.GetType().Name})."),
                "Forge Plan attention build canceled");
            return new ForgePlanAttentionBuildResult(ForgePlanAttentionBuildState.Failed);
        }
        catch (Exception ex)
        {
            _logFailure(
                new InvalidOperationException(
                    $"Forge Plan attention build failed ({ex.GetType().Name})."),
                "Forge Plan attention build failed");
            return new ForgePlanAttentionBuildResult(ForgePlanAttentionBuildState.Failed);
        }
    }
}
