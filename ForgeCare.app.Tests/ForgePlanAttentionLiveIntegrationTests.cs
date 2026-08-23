using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ForgePlanAttentionLiveIntegrationTests
{
    [TestMethod]
    public async Task BuildLoadsCurrentSessionExactlyOnceAndPresentsCorrelation()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        EvidenceRecord evidence = TestEvidenceFactory.Create(
            sessionId,
            new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc));
        var repository = new EvidenceExplorerTestRepository { Records = new[] { evidence } };
        var service = CreateService(repository, new ProducingRule(evidence.Id));

        ForgePlanAttentionBuildResult result = await service.BuildAsync(sessionId);

        Assert.AreEqual(ForgePlanAttentionBuildState.Ready, result.State);
        Assert.HasCount(1, result.Items);
        Assert.AreEqual(sessionId, repository.RequestedSessionId);
        Assert.AreEqual(1, repository.GetBySessionCalls);
        Assert.AreEqual(0, repository.AddCalls);
        Assert.AreEqual(0, repository.AddRangeCalls);
    }

    [TestMethod]
    public async Task EmptyAndRepositoryFailureAreIsolatedResults()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        var repository = new EvidenceExplorerTestRepository();
        var logged = new List<Exception>();
        var service = CreateService(repository, new NoOpRule(), logged);

        ForgePlanAttentionBuildResult empty = await service.BuildAsync(sessionId);
        Assert.AreEqual(ForgePlanAttentionBuildState.Empty, empty.State);

        repository.ReadException = new IOException("private-path-data");
        ForgePlanAttentionBuildResult failed = await service.BuildAsync(sessionId);

        Assert.AreEqual(ForgePlanAttentionBuildState.Failed, failed.State);
        Assert.IsEmpty(failed.Items);
        Assert.HasCount(1, logged);
        Assert.IsFalse(logged[0].Message.Contains("private-path-data", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task RuleFailurePreservesValidAttentionAsPartialSuccess()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        EvidenceRecord evidence = TestEvidenceFactory.Create(
            sessionId,
            new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc));
        var repository = new EvidenceExplorerTestRepository { Records = new[] { evidence } };
        var service = new ForgePlanAttentionLiveService(
            repository,
            new EvidenceCorrelationEngine(new IEvidenceCorrelationRule[]
            {
                new ProducingRule(evidence.Id),
                new ThrowingRule()
            }),
            new ForgePlanAttentionPresenter(),
            (_, _) => { });

        ForgePlanAttentionBuildResult result = await service.BuildAsync(sessionId);

        Assert.AreEqual(ForgePlanAttentionBuildState.PartialSuccess, result.State);
        Assert.HasCount(1, result.Items);
        Assert.AreEqual(1, result.CorrelationErrorCount);
        Assert.AreEqual(1, repository.GetBySessionCalls);
    }

    [TestMethod]
    public void MainWindowRegistersBothRulesAndUsesSharedRepository()
    {
        string source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ForgeCare.app", "MainWindow.xaml.cs"))
            .ReplaceLineEndings("\n");

        StringAssert.Contains(source, "new StartupProcessAttentionRule()");
        StringAssert.Contains(source, "new ProcessSystemCpuAttentionRule()");
        Assert.AreEqual(1, Count(source, "new JsonEvidenceRepository()"));
        StringAssert.Contains(source, "new ForgePlanAttentionLiveService(\n                _evidenceRepository");
    }

    private static ForgePlanAttentionLiveService CreateService(
        IEvidenceRepository repository,
        IEvidenceCorrelationRule rule,
        List<Exception>? logged = null) =>
        new(
            repository,
            new EvidenceCorrelationEngine(new[] { rule }),
            new ForgePlanAttentionPresenter(),
            (exception, _) => logged?.Add(exception));

    private static int Count(string source, string value) =>
        source.Split(value, StringSplitOptions.None).Length - 1;

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ForgeCare.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }

    private sealed class NoOpRule : IEvidenceCorrelationRule
    {
        public string RuleId => "no-op";
        public IReadOnlyList<TechnicianAttentionItem> Evaluate(EvidenceCorrelationContext context) =>
            Array.Empty<TechnicianAttentionItem>();
    }

    private sealed class ProducingRule(Guid evidenceId) : IEvidenceCorrelationRule
    {
        public string RuleId => "test-rule";

        public IReadOnlyList<TechnicianAttentionItem> Evaluate(EvidenceCorrelationContext context) =>
            new[]
            {
                new TechnicianAttentionItem(
                    "attention:test",
                    context.SessionId,
                    RuleId,
                    "Review observation",
                    "Evidence deserves technician review.",
                    EvidenceCategory.System,
                    TechnicianAttentionPriority.Low,
                    EvidenceConfidence.High,
                    "Related observations were recorded.",
                    "Inspect the supporting Evidence.",
                    "entity:test",
                    new[] { evidenceId },
                    Array.Empty<string>(),
                    context.Records.Single().TimestampUtc)
            };
    }

    private sealed class ThrowingRule : IEvidenceCorrelationRule
    {
        public string RuleId => "throwing-rule";
        public IReadOnlyList<TechnicianAttentionItem> Evaluate(EvidenceCorrelationContext context) =>
            throw new InvalidOperationException("Injected rule failure.");
    }
}
