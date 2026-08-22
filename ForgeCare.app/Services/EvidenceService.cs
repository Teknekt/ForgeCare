using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class EvidenceService
{
    private readonly IEvidenceRepository _repository;

    public EvidenceService(IEvidenceRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task<EvidenceCollectionResult> AddAsync(
        EvidenceRecord record,
        CancellationToken cancellationToken = default) =>
        AddRangeAsync(new[] { record }, cancellationToken);

    public async Task<EvidenceCollectionResult> AddRangeAsync(
        IEnumerable<EvidenceRecord> records,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);

        var result = new EvidenceCollectionResult();
        var validRecords = new List<EvidenceRecord>();

        foreach (EvidenceRecord record in records)
        {
            IReadOnlyList<string> validationErrors = record.Validate();
            if (validationErrors.Count == 0)
            {
                validRecords.Add(record);
                continue;
            }

            result.Errors.AddRange(
                validationErrors.Select(error => $"Evidence {record.Id}: {error}"));
        }

        if (validRecords.Count == 0)
            return result;

        try
        {
            await _repository.AddRangeAsync(validRecords, cancellationToken);
            result.Evidence.AddRange(validRecords);
        }
        catch (Exception ex)
        {
            CrashLogService.Record(ex, "Evidence persistence failure");
            result.Errors.Add($"Evidence could not be persisted: {ex.Message}");
        }

        return result;
    }

    public Task<EvidenceCollectionResult> GetBySessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default) =>
        QueryAsync(
            () => _repository.GetBySessionAsync(sessionId, cancellationToken),
            "Evidence session query failure");

    public Task<EvidenceCollectionResult> GetByCategoryAsync(
        EvidenceCategory category,
        CancellationToken cancellationToken = default) =>
        QueryAsync(
            () => _repository.GetByCategoryAsync(category, cancellationToken),
            "Evidence category query failure");

    public Task<EvidenceCollectionResult> GetByCorrelationKeyAsync(
        string correlationKey,
        CancellationToken cancellationToken = default) =>
        QueryAsync(
            () => _repository.GetByCorrelationKeyAsync(correlationKey, cancellationToken),
            "Evidence correlation query failure");

    public async Task<(EvidenceRecord? Evidence, string? Error)> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return (await _repository.GetByIdAsync(id, cancellationToken), null);
        }
        catch (Exception ex)
        {
            CrashLogService.Record(ex, "Evidence ID query failure");
            return (null, ex.Message);
        }
    }

    public EvidenceSummary BuildSummary(IEnumerable<EvidenceRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        List<EvidenceRecord> values = records.ToList();

        return new EvidenceSummary
        {
            TotalCount = values.Count,
            InformationalCount = values.Count(record => record.Severity == EvidenceSeverity.Informational),
            LowCount = values.Count(record => record.Severity == EvidenceSeverity.Low),
            MediumCount = values.Count(record => record.Severity == EvidenceSeverity.Medium),
            HighCount = values.Count(record => record.Severity == EvidenceSeverity.High),
            CriticalCount = values.Count(record => record.Severity == EvidenceSeverity.Critical),
            UnknownCount = values.Count(record => record.Severity == EvidenceSeverity.Unknown),
            Categories = values
                .GroupBy(record => record.Category)
                .ToDictionary(group => group.Key, group => group.Count())
        };
    }

    private static async Task<EvidenceCollectionResult> QueryAsync(
        Func<Task<IReadOnlyList<EvidenceRecord>>> query,
        string logContext)
    {
        var result = new EvidenceCollectionResult();

        try
        {
            result.Evidence.AddRange(await query());
        }
        catch (Exception ex)
        {
            CrashLogService.Record(ex, logContext);
            result.Errors.Add(ex.Message);
        }

        return result;
    }
}
