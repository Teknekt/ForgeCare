using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public interface IEvidenceRepository
{
    Task AddAsync(
        EvidenceRecord record,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyCollection<EvidenceRecord> records,
        CancellationToken cancellationToken = default);

    Task<EvidenceRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceRecord>> GetBySessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceRecord>> GetByCategoryAsync(
        EvidenceCategory category,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceRecord>> GetByCorrelationKeyAsync(
        string correlationKey,
        CancellationToken cancellationToken = default);
}
