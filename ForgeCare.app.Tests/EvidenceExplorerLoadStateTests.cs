using ForgeCare.App.Models;
using ForgeCare.App.Services;
using ForgeCare.App.ViewModels;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class EvidenceExplorerLoadStateTests
{
    [TestMethod]
    public async Task MalformedDocumentProducesTypedStateAndRemainsUnchanged()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        string path = Path.Combine(temp.Path, sessionId + ".json");
        const string original = "{ malformed evidence";
        await File.WriteAllTextAsync(path, original);
        var viewModel = CreateViewModel(new JsonEvidenceRepository(temp.Path));

        await viewModel.LoadSessionAsync(sessionId);

        Assert.AreEqual(EvidenceExplorerLoadState.MalformedDocument, viewModel.LoadState);
        Assert.AreEqual(original, await File.ReadAllTextAsync(path));
        Assert.IsEmpty(viewModel.AllItems);
    }

    [TestMethod]
    public async Task UnsupportedSchemaProducesTypedStateWithVersionsAndRemainsUnchanged()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        string path = Path.Combine(temp.Path, sessionId + ".json");
        string original = $$"""
            {
              "SchemaVersion": 99,
              "SessionId": "{{sessionId}}",
              "Evidence": []
            }
            """;
        await File.WriteAllTextAsync(path, original);
        var viewModel = CreateViewModel(new JsonEvidenceRepository(temp.Path));

        await viewModel.LoadSessionAsync(sessionId);

        Assert.AreEqual(EvidenceExplorerLoadState.UnsupportedSchema, viewModel.LoadState);
        Assert.AreEqual(99, viewModel.UnsupportedSchemaVersion);
        Assert.AreEqual(EvidenceDocument.CurrentSchemaVersion, viewModel.SupportedSchemaVersion);
        Assert.AreEqual(original, await File.ReadAllTextAsync(path));
    }

    [TestMethod]
    public async Task MissingRealRepositoryFileIsEmptyAndCreatesNothing()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        var viewModel = CreateViewModel(new JsonEvidenceRepository(temp.Path));

        await viewModel.LoadSessionAsync(sessionId);

        Assert.AreEqual(EvidenceExplorerLoadState.Empty, viewModel.LoadState);
        Assert.IsEmpty(Directory.GetFiles(temp.Path));
    }

    [TestMethod]
    public async Task SessionMismatchAndInvalidRecordBecomeLoadErrors()
    {
        string requestedSession = Guid.NewGuid().ToString("N");
        EvidenceRecord mismatch = TestEvidenceFactory.Create(Guid.NewGuid().ToString("N"));
        var repository = new EvidenceExplorerTestRepository { Records = new[] { mismatch } };
        var viewModel = CreateViewModel(repository);

        await viewModel.LoadSessionAsync(requestedSession);
        Assert.AreEqual(EvidenceExplorerLoadState.LoadError, viewModel.LoadState);
        Assert.IsEmpty(viewModel.AllItems);

        EvidenceRecord invalid = TestEvidenceFactory.Create(requestedSession);
        invalid.TimestampUtc = DateTime.SpecifyKind(invalid.TimestampUtc, DateTimeKind.Local);
        repository.Records = new[] { invalid };
        await viewModel.RefreshAsync(requestedSession);
        Assert.AreEqual(EvidenceExplorerLoadState.LoadError, viewModel.LoadState);
        Assert.IsEmpty(viewModel.AllItems);
    }

    [TestMethod]
    public async Task TypedFakeExceptionsRemainDistinct()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        var repository = new EvidenceExplorerTestRepository
        {
            ReadException = new MalformedEvidenceDocumentException("test.json", new InvalidDataException())
        };
        var viewModel = CreateViewModel(repository);
        await viewModel.LoadSessionAsync(sessionId);
        Assert.AreEqual(EvidenceExplorerLoadState.MalformedDocument, viewModel.LoadState);

        repository.ReadException = new UnsupportedEvidenceSchemaException(5, 1);
        await viewModel.RefreshAsync(sessionId);
        Assert.AreEqual(EvidenceExplorerLoadState.UnsupportedSchema, viewModel.LoadState);
        Assert.AreEqual(5, viewModel.UnsupportedSchemaVersion);
    }

    private static EvidenceExplorerViewModel CreateViewModel(IEvidenceRepository repository) =>
        new(repository, (_, _) => { });
}
