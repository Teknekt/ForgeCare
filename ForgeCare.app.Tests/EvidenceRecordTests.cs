using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class EvidenceRecordTests
{
    [TestMethod]
    public void ValidRecordPassesValidation()
    {
        EvidenceRecord record = TestEvidenceFactory.Create();

        Assert.IsEmpty(record.Validate());
    }

    [TestMethod]
    public void EmptyIdIsRejected()
    {
        EvidenceRecord record = TestEvidenceFactory.Create();
        record.Id = Guid.Empty;

        CollectionAssert.Contains(record.Validate().ToList(), "Evidence Id must not be empty.");
    }

    [TestMethod]
    public void EmptySessionIdIsRejected()
    {
        EvidenceRecord record = TestEvidenceFactory.Create();
        record.SessionId = " ";

        CollectionAssert.Contains(record.Validate().ToList(), "Evidence SessionId must not be empty.");
    }

    [TestMethod]
    public void NonUtcTimestampIsRejected()
    {
        EvidenceRecord record = TestEvidenceFactory.Create();
        record.TimestampUtc = DateTime.Now;

        Assert.IsTrue(record.Validate().Any(error => error.Contains("DateTimeKind.Utc")));
    }

    [TestMethod]
    public void UnknownEnumsAreValidAndSeverityIsIndependentFromConfidence()
    {
        EvidenceRecord record = TestEvidenceFactory.Create();
        record.Source = EvidenceSource.Unknown;
        record.Severity = EvidenceSeverity.High;
        record.Confidence = EvidenceConfidence.Unknown;

        Assert.IsEmpty(record.Validate());
        Assert.AreNotEqual(record.Severity.ToString(), record.Confidence.ToString());
    }

    [TestMethod]
    public void OptionalStructuredValuesMetadataAndCorrelationKeyArePreserved()
    {
        EvidenceRecord record = TestEvidenceFactory.Create();
        record.Value = 51.0;
        record.Unit = "GB";
        record.Metadata["sample"] = "system-scan";
        record.CorrelationKey = "memory:available";

        Assert.AreEqual(51.0, record.Value);
        Assert.AreEqual("GB", record.Unit);
        Assert.AreEqual("system-scan", record.Metadata["sample"]);
        Assert.AreEqual("memory:available", record.CorrelationKey);
        Assert.IsEmpty(record.Validate());
    }

    [TestMethod]
    public async Task EvidenceServiceRejectsInvalidRecordsBeforeRepositoryWrite()
    {
        var repository = new RecordingRepository();
        var service = new EvidenceService(repository);
        EvidenceRecord record = TestEvidenceFactory.Create();
        record.Id = Guid.Empty;

        EvidenceCollectionResult result = await service.AddAsync(record);

        Assert.IsFalse(result.Success);
        Assert.IsFalse(repository.WasCalled);
    }

    private sealed class RecordingRepository : IEvidenceRepository
    {
        public bool WasCalled { get; private set; }

        public Task AddAsync(EvidenceRecord record, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IReadOnlyCollection<EvidenceRecord> records, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }

        public Task<EvidenceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<EvidenceRecord?>(null);

        public Task<IReadOnlyList<EvidenceRecord>> GetBySessionAsync(string sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EvidenceRecord>>(Array.Empty<EvidenceRecord>());

        public Task<IReadOnlyList<EvidenceRecord>> GetByCategoryAsync(EvidenceCategory category, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EvidenceRecord>>(Array.Empty<EvidenceRecord>());

        public Task<IReadOnlyList<EvidenceRecord>> GetByCorrelationKeyAsync(string correlationKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EvidenceRecord>>(Array.Empty<EvidenceRecord>());
    }
}
