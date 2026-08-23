using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ForgeServiceReportLiveServiceTests
{
    [TestMethod]
    public async Task ValidSessionLoadsEvidenceOnceAndReturnsProfessionalHtml()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        var repository = new RecordingEvidenceRepository
        {
            Records = [ForgeServiceReportTestFactory.Evidence(session.SessionId)]
        };
        var service = Create(repository);

        ForgeServiceReportLiveResult result = await service.GenerateAsync(session);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Html);
        StringAssert.Contains(result.Html, "Professional Service Report");
        StringAssert.Contains(result.Html, "CPU PRESSURE");
        Assert.AreEqual(1, repository.GetBySessionCallCount);
        Assert.AreEqual(session.SessionId, repository.LastSessionId);
        Assert.AreEqual(0, repository.WriteCallCount);
    }

    [TestMethod]
    public async Task EmptyEvidenceProducesValidReportWithoutEvidenceReferences()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        var repository = new RecordingEvidenceRepository();

        ForgeServiceReportLiveResult result = await Create(repository).GenerateAsync(session);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.EvidenceUnavailable);
        Assert.IsNotNull(result.Html);
        Assert.AreEqual(1, repository.GetBySessionCallCount);
        Assert.AreEqual(0, repository.WriteCallCount);
    }

    [TestMethod]
    public async Task EvidenceFailureProducesExplicitPartialReport()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        var repository = new RecordingEvidenceRepository
        {
            ReadFailure = new IOException("sensitive path must not escape")
        };

        ForgeServiceReportLiveResult result = await Create(repository).GenerateAsync(session);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.EvidenceUnavailable);
        Assert.IsTrue(result.GeneratedWithDataNotes);
        CollectionAssert.Contains(
            result.Warnings.ToArray(),
            "Evidence references could not be loaded for this report.");
        Assert.IsNotNull(result.Html);
        StringAssert.Contains(result.Html, "Evidence references could not be loaded for this report.");
        Assert.IsFalse(result.Html.Contains("sensitive path", StringComparison.Ordinal));
        Assert.AreEqual(1, repository.GetBySessionCallCount);
    }

    [TestMethod]
    public async Task InvalidSessionFailsWithoutRepositoryAccess()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        session.SessionId = "invalid";
        var repository = new RecordingEvidenceRepository();

        ForgeServiceReportLiveResult result = await Create(repository).GenerateAsync(session);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ForgeServiceReportLiveFailure.InvalidSession, result.Failure);
        Assert.AreEqual(0, repository.GetBySessionCallCount);
        Assert.AreEqual(0, repository.WriteCallCount);
    }

    [TestMethod]
    public async Task RestoredLegacySessionGeneratesTruthfulDataNote()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        session.Actions.Add(new ForgeReportAction
        {
            Timestamp = DateTime.Now,
            Category = "SYSTEM",
            Title = "Legacy diagnostic",
            IsSuccess = true
        });
        var repository = new RecordingEvidenceRepository();

        ForgeServiceReportLiveResult result = await Create(repository).GenerateAsync(session);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.GeneratedWithDataNotes);
        Assert.IsTrue(result.Warnings.Any(value =>
            value.Contains("legacy session", StringComparison.Ordinal)));
        StringAssert.Contains(result.Html!, "System scan");
    }

    [TestMethod]
    public async Task SameSnapshotAndEvidenceReturnDeterministicHtml()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        var repository = new RecordingEvidenceRepository
        {
            Records = [ForgeServiceReportTestFactory.Evidence(session.SessionId)]
        };
        var service = Create(repository);

        ForgeServiceReportLiveResult first = await service.GenerateAsync(session);
        ForgeServiceReportLiveResult second = await service.GenerateAsync(session);

        Assert.AreEqual(first.Html, second.Html);
        Assert.AreEqual(2, repository.GetBySessionCallCount);
    }

    [TestMethod]
    public async Task CancellationRemainsCancellationAndDoesNotBecomePartialSuccess()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        var repository = new RecordingEvidenceRepository
        {
            ReadFailure = new OperationCanceledException()
        };

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => Create(repository).GenerateAsync(session));
    }

    private static ForgeServiceReportLiveService Create(IEvidenceRepository repository) =>
        new(repository, new ForgeServiceReportBuilder(), new ForgeServiceReportHtmlRenderer());

    private sealed class RecordingEvidenceRepository : IEvidenceRepository
    {
        public IReadOnlyList<EvidenceRecord> Records { get; init; } = Array.Empty<EvidenceRecord>();
        public Exception? ReadFailure { get; init; }
        public int GetBySessionCallCount { get; private set; }
        public int WriteCallCount { get; private set; }
        public string? LastSessionId { get; private set; }

        public Task AddAsync(EvidenceRecord record, CancellationToken cancellationToken = default)
        {
            WriteCallCount++;
            throw new InvalidOperationException("The professional report path must remain read-only.");
        }

        public Task AddRangeAsync(IReadOnlyCollection<EvidenceRecord> records, CancellationToken cancellationToken = default)
        {
            WriteCallCount++;
            throw new InvalidOperationException("The professional report path must remain read-only.");
        }

        public Task<EvidenceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<EvidenceRecord?>(null);

        public Task<IReadOnlyList<EvidenceRecord>> GetBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            GetBySessionCallCount++;
            LastSessionId = sessionId;
            return ReadFailure == null
                ? Task.FromResult(Records)
                : Task.FromException<IReadOnlyList<EvidenceRecord>>(ReadFailure);
        }

        public Task<IReadOnlyList<EvidenceRecord>> GetByCategoryAsync(EvidenceCategory category, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EvidenceRecord>>(Array.Empty<EvidenceRecord>());

        public Task<IReadOnlyList<EvidenceRecord>> GetByCorrelationKeyAsync(string correlationKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EvidenceRecord>>(Array.Empty<EvidenceRecord>());
    }
}
