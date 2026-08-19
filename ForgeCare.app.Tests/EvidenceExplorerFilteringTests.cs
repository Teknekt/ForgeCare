using ForgeCare.App.Models;
using ForgeCare.App.ViewModels;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class EvidenceExplorerFilteringTests
{
    [TestMethod]
    public async Task InitialFacetSelectionsRepresentAllFiltersExplicitly()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        var repository = new EvidenceExplorerTestRepository
        {
            Records = new[]
            {
                Create(sessionId, EvidenceCategory.Cpu, EvidenceSource.SystemScan, "cpu-pressure")
            }
        };
        var viewModel = new EvidenceExplorerViewModel(repository, (_, _) => { });

        await viewModel.LoadSessionAsync(sessionId);

        Assert.IsNotNull(viewModel.SelectedCategoryFacet);
        Assert.IsTrue(viewModel.SelectedCategoryFacet.IsAll);
        Assert.AreEqual("ALL", viewModel.SelectedCategoryFacet.DisplayLabel);
        Assert.IsNotNull(viewModel.SelectedSourceFacet);
        Assert.IsTrue(viewModel.SelectedSourceFacet.IsAll);
        Assert.AreEqual("ALL SOURCES", viewModel.SelectedSourceFacet.DisplayLabel);
        Assert.IsNull(viewModel.SelectedCategory);
        Assert.IsNull(viewModel.SelectedSource);
    }

    [TestMethod]
    public async Task FacetSelectionUpdatesAndClearsNullableFilters()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        var repository = new EvidenceExplorerTestRepository
        {
            Records = new[]
            {
                Create(sessionId, EvidenceCategory.Cpu, EvidenceSource.SystemScan, "cpu-pressure"),
                Create(sessionId, EvidenceCategory.Memory, EvidenceSource.DeepAnalysis, "memory-pressure")
            }
        };
        var viewModel = new EvidenceExplorerViewModel(repository, (_, _) => { });
        await viewModel.LoadSessionAsync(sessionId);

        viewModel.SelectedCategoryFacet = viewModel.CategoryFacets
            .Single(facet => facet.Category == EvidenceCategory.Memory);
        viewModel.SelectedSourceFacet = viewModel.SourceFacets
            .Single(facet => facet.Source == EvidenceSource.DeepAnalysis);

        Assert.AreEqual(EvidenceCategory.Memory, viewModel.SelectedCategory);
        Assert.AreEqual(EvidenceSource.DeepAnalysis, viewModel.SelectedSource);
        Assert.HasCount(1, viewModel.VisibleItems);

        viewModel.SelectedCategoryFacet = viewModel.CategoryFacets.Single(facet => facet.IsAll);
        viewModel.SelectedSourceFacet = viewModel.SourceFacets.Single(facet => facet.IsAll);

        Assert.IsNull(viewModel.SelectedCategory);
        Assert.IsNull(viewModel.SelectedSource);
        Assert.IsTrue(viewModel.SelectedCategoryFacet!.IsAll);
        Assert.IsTrue(viewModel.SelectedSourceFacet!.IsAll);
    }

    [TestMethod]
    public async Task ClearNewSessionAndRefreshKeepFacetObjectsSynchronized()
    {
        string firstSession = Guid.NewGuid().ToString("N");
        string secondSession = Guid.NewGuid().ToString("N");
        var repository = new EvidenceExplorerTestRepository
        {
            Records = new[]
            {
                Create(firstSession, EvidenceCategory.Cpu, EvidenceSource.DeepAnalysis, "cpu-pressure")
            }
        };
        var viewModel = new EvidenceExplorerViewModel(repository, (_, _) => { });
        await viewModel.LoadSessionAsync(firstSession);
        viewModel.SelectedCategoryFacet = viewModel.CategoryFacets
            .Single(facet => facet.Category == EvidenceCategory.Cpu);
        viewModel.SelectedSourceFacet = viewModel.SourceFacets
            .Single(facet => facet.Source == EvidenceSource.DeepAnalysis);

        EvidenceExplorerFacet oldCategoryFacet = viewModel.SelectedCategoryFacet!;
        EvidenceExplorerFacet oldSourceFacet = viewModel.SelectedSourceFacet!;
        await viewModel.RefreshAsync(firstSession);

        Assert.AreEqual(EvidenceCategory.Cpu, viewModel.SelectedCategory);
        Assert.AreEqual(EvidenceSource.DeepAnalysis, viewModel.SelectedSource);
        Assert.AreNotSame(oldCategoryFacet, viewModel.SelectedCategoryFacet);
        Assert.AreNotSame(oldSourceFacet, viewModel.SelectedSourceFacet);
        CollectionAssert.Contains(viewModel.CategoryFacets.ToList(), viewModel.SelectedCategoryFacet);
        CollectionAssert.Contains(viewModel.SourceFacets.ToList(), viewModel.SelectedSourceFacet);

        viewModel.ClearFilters();
        Assert.IsTrue(viewModel.SelectedCategoryFacet!.IsAll);
        Assert.IsTrue(viewModel.SelectedSourceFacet!.IsAll);

        repository.Records = new[]
        {
            Create(secondSession, EvidenceCategory.Memory, EvidenceSource.SystemScan, "memory-pressure")
        };
        await viewModel.LoadSessionAsync(secondSession);

        Assert.IsNull(viewModel.SelectedCategory);
        Assert.IsNull(viewModel.SelectedSource);
        Assert.IsTrue(viewModel.SelectedCategoryFacet!.IsAll);
        Assert.IsTrue(viewModel.SelectedSourceFacet!.IsAll);
    }

    [TestMethod]
    public async Task FacetsUseFullSessionAndRemainStableWhileFiltering()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        var repository = new EvidenceExplorerTestRepository
        {
            Records = new[]
            {
                Create(sessionId, EvidenceCategory.Cpu, EvidenceSource.DeepAnalysis, "cpu-pressure"),
                Create(sessionId, EvidenceCategory.Process, EvidenceSource.DeepAnalysis, "process:alpha"),
                Create(sessionId, EvidenceCategory.Process, EvidenceSource.SystemScan, "process-count")
            }
        };
        var viewModel = new EvidenceExplorerViewModel(repository, (_, _) => { });
        await viewModel.LoadSessionAsync(sessionId);

        Assert.AreEqual(3, viewModel.CategoryFacets.Single(facet => facet.IsAll).Count);
        Assert.AreEqual(2, viewModel.CategoryFacets.Single(facet => facet.Category == EvidenceCategory.Process).Count);
        Assert.AreEqual(2, viewModel.SourceFacets.Single(facet => facet.Source == EvidenceSource.DeepAnalysis).Count);

        viewModel.SelectedCategory = EvidenceCategory.Process;
        viewModel.SelectedSource = EvidenceSource.DeepAnalysis;

        Assert.HasCount(1, viewModel.VisibleItems);
        Assert.AreEqual("process:alpha", viewModel.VisibleItems[0].RawSubject);
        Assert.AreEqual(3, viewModel.CategoryFacets.Single(facet => facet.IsAll).Count);
        Assert.AreEqual(2, viewModel.SourceFacets.Single(facet => facet.Source == EvidenceSource.DeepAnalysis).Count);
    }

    [TestMethod]
    [DataRow("MEMORY-PRESSURE")]
    [DataRow("memory pressure")]
    [DataRow("physical memory was measured")]
    [DataRow("memory")]
    [DataRow("deep analysis")]
    [DataRow("MEMORY:PRESSURE")]
    [DataRow("resource analyzer")]
    [DataRow("usedMemoryGb")]
    [DataRow("used memory GB")]
    [DataRow("31.4")]
    public async Task SearchCoversAllRequiredFieldsCaseInsensitively(string query)
    {
        string sessionId = Guid.NewGuid().ToString("N");
        EvidenceRecord target = Create(
            sessionId,
            EvidenceCategory.Memory,
            EvidenceSource.DeepAnalysis,
            "memory-pressure");
        target.Observation = "Physical memory was measured during analysis.";
        target.CorrelationKey = "memory:pressure";
        target.Collector = "Resource Analyzer";
        target.Metadata["usedMemoryGb"] = "31.4";

        var repository = new EvidenceExplorerTestRepository
        {
            Records = new[]
            {
                target,
                Create(sessionId, EvidenceCategory.Cpu, EvidenceSource.SystemScan, "cpu-pressure")
            }
        };
        var viewModel = new EvidenceExplorerViewModel(repository, (_, _) => { });
        await viewModel.LoadSessionAsync(sessionId);

        viewModel.SearchQuery = "  " + query + "  ";

        Assert.HasCount(1, viewModel.VisibleItems, $"Query '{query}' did not isolate the target.");
        Assert.AreEqual(target.Id, viewModel.VisibleItems[0].Id);
    }

    [TestMethod]
    public async Task ClearSearchAndNullOptionalsAreSafe()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        EvidenceRecord first = Create(sessionId, EvidenceCategory.Cpu, EvidenceSource.SystemScan, "cpu-pressure");
        first.Value = null;
        first.Unit = null;
        first.CorrelationKey = null;
        var repository = new EvidenceExplorerTestRepository
        {
            Records = new[]
            {
                first,
                Create(sessionId, EvidenceCategory.Memory, EvidenceSource.DeepAnalysis, "memory-pressure")
            }
        };
        var viewModel = new EvidenceExplorerViewModel(repository, (_, _) => { });
        await viewModel.LoadSessionAsync(sessionId);

        viewModel.SearchQuery = "no-match";
        Assert.IsTrue(viewModel.IsFilteredEmpty);
        Assert.IsNull(viewModel.SelectedItem);

        viewModel.ClearSearch();
        Assert.HasCount(2, viewModel.VisibleItems);
        Assert.IsNotNull(viewModel.SelectedItem);
        Assert.IsFalse(viewModel.IsFilteredEmpty);
    }

    [TestMethod]
    public async Task FilterChangesPreserveOrFallbackSelection()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        EvidenceRecord newestCpu = Create(sessionId, EvidenceCategory.Cpu, EvidenceSource.DeepAnalysis, "cpu-pressure", 3);
        EvidenceRecord process = Create(sessionId, EvidenceCategory.Process, EvidenceSource.DeepAnalysis, "process:alpha", 2);
        EvidenceRecord memory = Create(sessionId, EvidenceCategory.Memory, EvidenceSource.SystemScan, "memory-pressure", 1);
        var repository = new EvidenceExplorerTestRepository { Records = new[] { memory, process, newestCpu } };
        var viewModel = new EvidenceExplorerViewModel(repository, (_, _) => { });
        await viewModel.LoadSessionAsync(sessionId);

        viewModel.SelectedItem = viewModel.VisibleItems.Single(item => item.Id == process.Id);
        viewModel.SelectedSource = EvidenceSource.DeepAnalysis;
        Assert.AreEqual(process.Id, viewModel.SelectedId);

        viewModel.SelectedCategory = EvidenceCategory.Cpu;
        Assert.AreEqual(newestCpu.Id, viewModel.SelectedId);
    }

    [TestMethod]
    public async Task FiveHundredRecordsLoadSearchAndFilterDeterministically()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        DateTime start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        EvidenceRecord[] records = Enumerable.Range(0, 500)
            .Select(index =>
            {
                EvidenceCategory category = index % 2 == 0 ? EvidenceCategory.Process : EvidenceCategory.Memory;
                EvidenceSource source = index % 5 == 0 ? EvidenceSource.DeepAnalysis : EvidenceSource.SystemScan;
                EvidenceRecord record = Create(sessionId, category, source, $"record-{index}", index);
                record.Observation = index % 10 == 0 ? "Target observation." : "Ordinary observation.";
                return record;
            })
            .Reverse()
            .ToArray();
        var repository = new EvidenceExplorerTestRepository { Records = records };
        var viewModel = new EvidenceExplorerViewModel(repository, (_, _) => { });

        await viewModel.LoadSessionAsync(sessionId);
        viewModel.SelectedCategory = EvidenceCategory.Process;
        viewModel.SelectedSource = EvidenceSource.DeepAnalysis;
        viewModel.SearchQuery = "target";

        Assert.HasCount(50, viewModel.VisibleItems);
        Assert.IsTrue(viewModel.VisibleItems.All(item => item.Category == EvidenceCategory.Process));
        Assert.IsTrue(viewModel.VisibleItems.All(item => item.Source == EvidenceSource.DeepAnalysis));
        Assert.IsTrue(viewModel.VisibleItems.Zip(viewModel.VisibleItems.Skip(1),
            (first, second) => first.TimestampUtc >= second.TimestampUtc).All(value => value));
    }

    private static EvidenceRecord Create(
        string sessionId,
        EvidenceCategory category,
        EvidenceSource source,
        string subject,
        int minuteOffset = 0)
    {
        EvidenceRecord record = TestEvidenceFactory.Create(
            sessionId,
            new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc).AddMinutes(minuteOffset),
            category);
        record.Source = source;
        record.Subject = subject;
        return record;
    }
}
