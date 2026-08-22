using ForgeCare.App.Models;
using ForgeCare.App.ViewModels;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class EvidenceExplorerViewModelTests
{
    [TestMethod]
    public async Task SuppliedSessionLoadsReadyInDeterministicOrderAndSelectsNewest()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        DateTime timestamp = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);
        EvidenceRecord higherId = Create(sessionId, timestamp, EvidenceCategory.Cpu, EvidenceSource.SystemScan);
        EvidenceRecord lowerId = Create(sessionId, timestamp, EvidenceCategory.Memory, EvidenceSource.DeepAnalysis);
        higherId.Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        lowerId.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        EvidenceRecord newest = Create(sessionId, timestamp.AddMinutes(1), EvidenceCategory.System, EvidenceSource.SystemScan);
        var repository = new EvidenceExplorerTestRepository
        {
            Records = new[] { higherId, newest, lowerId }
        };
        var viewModel = new EvidenceExplorerViewModel(repository, (_, _) => { });

        await viewModel.LoadSessionAsync(sessionId);

        Assert.AreEqual(sessionId, repository.RequestedSessionId);
        Assert.AreEqual(EvidenceExplorerLoadState.Ready, viewModel.LoadState);
        CollectionAssert.AreEqual(
            new[] { newest.Id, lowerId.Id, higherId.Id },
            viewModel.VisibleItems.Select(item => item.Id).ToArray());
        Assert.AreEqual(newest.Id, viewModel.SelectedId);
        Assert.AreEqual(0, repository.AddCalls);
        Assert.AreEqual(0, repository.AddRangeCalls);
    }

    [TestMethod]
    public async Task SameSessionRefreshPreservesFiltersSearchAndSelection()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        EvidenceRecord cpu = Create(sessionId, DateTime.UtcNow, EvidenceCategory.Cpu, EvidenceSource.DeepAnalysis, "cpu-pressure");
        EvidenceRecord memory = Create(sessionId, DateTime.UtcNow.AddMinutes(-1), EvidenceCategory.Memory, EvidenceSource.DeepAnalysis, "memory-pressure");
        var repository = new EvidenceExplorerTestRepository { Records = new[] { cpu, memory } };
        var viewModel = new EvidenceExplorerViewModel(repository, (_, _) => { });
        await viewModel.LoadSessionAsync(sessionId);
        viewModel.SelectedSource = EvidenceSource.DeepAnalysis;
        viewModel.SearchQuery = "memory";
        Assert.AreEqual(memory.Id, viewModel.SelectedId);

        repository.Records = new[]
        {
            Create(sessionId, DateTime.UtcNow.AddMinutes(1), EvidenceCategory.Cpu, EvidenceSource.DeepAnalysis, "new-cpu"),
            memory
        };
        await viewModel.RefreshAsync(sessionId);

        Assert.AreEqual(EvidenceSource.DeepAnalysis, viewModel.SelectedSource);
        Assert.AreEqual("memory", viewModel.SearchQuery);
        Assert.AreEqual(memory.Id, viewModel.SelectedId);
    }

    [TestMethod]
    public async Task SameSessionRefreshFallsBackWhenSelectedRecordDisappears()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        EvidenceRecord old = Create(sessionId, DateTime.UtcNow, subject: "old");
        var repository = new EvidenceExplorerTestRepository { Records = new[] { old } };
        var viewModel = new EvidenceExplorerViewModel(repository, (_, _) => { });
        await viewModel.LoadSessionAsync(sessionId);

        EvidenceRecord replacement = Create(sessionId, DateTime.UtcNow.AddMinutes(1), subject: "replacement");
        repository.Records = new[] { replacement };
        await viewModel.RefreshAsync(sessionId);

        Assert.AreEqual(replacement.Id, viewModel.SelectedId);
    }

    [TestMethod]
    public async Task NewSessionResetsFiltersSearchAndSelection()
    {
        string firstSession = Guid.NewGuid().ToString("N");
        string secondSession = Guid.NewGuid().ToString("N");
        var repository = new EvidenceExplorerTestRepository
        {
            Records = new[] { Create(firstSession, DateTime.UtcNow, EvidenceCategory.Cpu, EvidenceSource.SystemScan) }
        };
        var viewModel = new EvidenceExplorerViewModel(repository, (_, _) => { });
        await viewModel.LoadSessionAsync(firstSession);
        viewModel.SelectedCategory = EvidenceCategory.Cpu;
        viewModel.SelectedSource = EvidenceSource.SystemScan;
        viewModel.SearchQuery = "cpu";

        EvidenceRecord second = Create(secondSession, DateTime.UtcNow, EvidenceCategory.Memory, EvidenceSource.DeepAnalysis);
        repository.Records = new[] { second };
        await viewModel.RefreshAsync(secondSession);

        Assert.AreEqual(secondSession, viewModel.CurrentSessionId);
        Assert.IsNull(viewModel.SelectedCategory);
        Assert.IsNull(viewModel.SelectedSource);
        Assert.AreEqual(string.Empty, viewModel.SearchQuery);
        Assert.AreEqual(second.Id, viewModel.SelectedId);
    }

    [TestMethod]
    public async Task EmptySessionAndGenericFailureHaveDistinctStates()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        var repository = new EvidenceExplorerTestRepository();
        var logged = new List<Exception>();
        var viewModel = new EvidenceExplorerViewModel(repository, (exception, _) => logged.Add(exception));

        await viewModel.LoadSessionAsync(sessionId);
        Assert.AreEqual(EvidenceExplorerLoadState.Empty, viewModel.LoadState);
        Assert.IsFalse(viewModel.IsFilteredEmpty);
        Assert.IsEmpty(logged);

        repository.ReadException = new IOException("Injected read failure.");
        await viewModel.RefreshAsync(sessionId);
        Assert.AreEqual(EvidenceExplorerLoadState.LoadError, viewModel.LoadState);
        Assert.HasCount(1, logged);
        StringAssert.Contains(viewModel.ErrorMessage, "Injected");
    }

    [TestMethod]
    public async Task ProjectionCopiesMetadataAndDoesNotExposeMutableRecord()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        EvidenceRecord record = Create(sessionId, DateTime.UtcNow);
        record.Metadata["processId"] = "42";
        var repository = new EvidenceExplorerTestRepository { Records = new[] { record } };
        var viewModel = new EvidenceExplorerViewModel(repository, (_, _) => { });

        await viewModel.LoadSessionAsync(sessionId);
        record.Metadata["processId"] = "99";
        record.Subject = "mutated";

        EvidenceExplorerItem item = viewModel.AllItems.Single();
        Assert.AreEqual("test-subject", item.RawSubject);
        Assert.AreEqual("42", item.Metadata.Single().Value);
        Assert.IsFalse(typeof(EvidenceExplorerItem).GetProperties().Any(property => property.PropertyType == typeof(EvidenceRecord)));
    }

    private static EvidenceRecord Create(
        string sessionId,
        DateTime timestamp,
        EvidenceCategory category = EvidenceCategory.System,
        EvidenceSource source = EvidenceSource.Manual,
        string subject = "test-subject")
    {
        EvidenceRecord record = TestEvidenceFactory.Create(sessionId, timestamp, category);
        record.Source = source;
        record.Subject = subject;
        return record;
    }
}
