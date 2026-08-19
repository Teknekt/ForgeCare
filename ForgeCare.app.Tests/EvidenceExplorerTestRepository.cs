using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

internal sealed class EvidenceExplorerTestRepository : IEvidenceRepository
{
    public IReadOnlyList<EvidenceRecord> Records { get; set; } = Array.Empty<EvidenceRecord>();

    public Exception? ReadException { get; set; }

    public string? RequestedSessionId { get; private set; }

    public int AddCalls { get; private set; }

    public int AddRangeCalls { get; private set; }

    public Task AddAsync(EvidenceRecord record, CancellationToken cancellationToken = default)
    {
        AddCalls++;
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IReadOnlyCollection<EvidenceRecord> records, CancellationToken cancellationToken = default)
    {
        AddRangeCalls++;
        return Task.CompletedTask;
    }

    public Task<EvidenceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Records.FirstOrDefault(record => record.Id == id));

    public Task<IReadOnlyList<EvidenceRecord>> GetBySessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        RequestedSessionId = sessionId;
        return ReadException == null
            ? Task.FromResult(Records)
            : Task.FromException<IReadOnlyList<EvidenceRecord>>(ReadException);
    }

    public Task<IReadOnlyList<EvidenceRecord>> GetByCategoryAsync(
        EvidenceCategory category,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EvidenceRecord>>(
            Records.Where(record => record.Category == category).ToArray());

    public Task<IReadOnlyList<EvidenceRecord>> GetByCorrelationKeyAsync(
        string correlationKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EvidenceRecord>>(
            Records.Where(record => string.Equals(
                record.CorrelationKey,
                correlationKey,
                StringComparison.OrdinalIgnoreCase)).ToArray());
}
