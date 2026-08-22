using ForgeCare.App.Models;
using ForgeCare.App.Services;
using ForgeCare.App.ViewModels;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class StartupIntelligenceExplorerCompatibilityTests
{
    [TestMethod]
    public async Task GenericExplorerFacetsSearchMetadataAndCorrelationWithoutProductionChanges()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        StartupIntelligenceEntry entry = StartupEvidenceTestFactory.Entry(name: "OneDrive Helper");
        EvidenceRecord record = StartupEvidenceTestFactory.Adapter().Collect(
            new StartupIntelligenceResult([entry]),
            sessionId,
            DateTime.UtcNow).Evidence.Single();
        var repository = new EvidenceExplorerTestRepository { Records = [record] };
        var viewModel = new EvidenceExplorerViewModel(repository, (_, _) => { });

        await viewModel.LoadSessionAsync(sessionId);

        Assert.IsTrue(viewModel.SourceFacets.Any(facet =>
            facet.Source == EvidenceSource.StartupIntelligence && facet.Count == 1));
        Assert.IsTrue(viewModel.CategoryFacets.Any(facet =>
            facet.Category == EvidenceCategory.Startup && facet.Count == 1));
        Assert.AreEqual("STARTUP ENTRY: ONEDRIVE HELPER", viewModel.AllItems.Single().Title);
        Assert.AreEqual(record.CorrelationKey, viewModel.AllItems.Single().CorrelationKey);
        Assert.IsTrue(viewModel.AllItems.Single().Metadata.Any(item => item.RawKey == "signatureStatus"));

        foreach (string query in new[]
                 {
                     "OneDrive Helper",
                     "Example Corp",
                     "Example Corporation LLC",
                     "Verified",
                     "Valid",
                     record.CorrelationKey!
                 })
        {
            viewModel.SearchQuery = query;
            Assert.HasCount(1, viewModel.VisibleItems, $"Explorer should find Startup Evidence by '{query}'.");
        }

        viewModel.SelectedSource = EvidenceSource.StartupIntelligence;
        viewModel.SelectedCategory = EvidenceCategory.Startup;
        Assert.HasCount(1, viewModel.VisibleItems);
    }
}
