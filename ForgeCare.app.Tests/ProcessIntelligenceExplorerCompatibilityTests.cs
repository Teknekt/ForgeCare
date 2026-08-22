using ForgeCare.App.Models;
using ForgeCare.App.ViewModels;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ProcessIntelligenceExplorerCompatibilityTests
{
    [TestMethod]
    public async Task GenericExplorerProjectsFacetsMetadataAndSearchWithoutWrites()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        EvidenceRecord record = ProcessEvidenceTestFactory.Adapter().Collect(
            ProcessEvidenceTestFactory.Result(ProcessEvidenceTestFactory.Group()), sessionId, DateTime.UtcNow).Evidence.Single();
        var repository = new EvidenceExplorerTestRepository { Records = [record] };
        var viewModel = new EvidenceExplorerViewModel(repository, (_, _) => { });

        await viewModel.LoadSessionAsync(sessionId);

        Assert.IsTrue(viewModel.SourceFacets.Any(facet => facet.Source == EvidenceSource.ProcessIntelligence && facet.Count == 1));
        Assert.IsTrue(viewModel.CategoryFacets.Any(facet => facet.Category == EvidenceCategory.Process && facet.Count == 1));
        Assert.AreEqual("PROCESS APPLICATION: VENDOR APP", viewModel.AllItems.Single().Title);
        string[] searches = ["Vendor App", "Vendor Company", "Vendor Product", "Verified", "Valid", "HIGH", record.CorrelationKey!];
        foreach (string search in searches)
        {
            viewModel.SearchQuery = search;
            Assert.HasCount(1, viewModel.VisibleItems, $"Search failed: {search}");
        }
        Assert.IsTrue(viewModel.AllItems.Single().Metadata.Any(item => item.RawKey == "companyName"));
        Assert.AreEqual("HIGH", viewModel.AllItems.Single().SeverityDisplay);
        Assert.AreEqual("HIGH", viewModel.AllItems.Single().ConfidenceDisplay);
        Assert.AreEqual(0, repository.AddCalls);
        Assert.AreEqual(0, repository.AddRangeCalls);
    }
}
