using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ProcessIntelligenceServiceTests
{
    [TestMethod]
    public async Task EmptyInputReturnsSuccessfulImmutableResult()
    {
        var inspector = new FakeProcessExecutableInspector();
        ProcessIntelligenceResult result = await Service(inspector).AnalyzeAsync([]);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.PartialSuccess);
        Assert.IsEmpty(result.Entries);
        Assert.IsEmpty(result.Groups);
        Assert.AreEqual(0, inspector.TotalCalls);
    }

    [TestMethod]
    public async Task ObservationSnapshotRetainsPidStartTimeAndTransientPath()
    {
        DateTime start = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        ProcessInstanceObservation observation = ProcessIntelligenceTestFactory.Observation(
            42, "agent", @"C:\Apps\agent.exe", startTimeUtc: start);

        ProcessIntelligenceEntry entry = (await Service(new()).AnalyzeAsync([observation])).Entries.Single();

        Assert.AreEqual(42, entry.Observation.ProcessId);
        Assert.AreEqual(start, entry.Observation.StartTimeUtc);
        Assert.AreEqual(@"C:\Apps\agent.exe", entry.Observation.ExecutablePath);
        Assert.AreSame(observation, entry.Observation);
    }

    [TestMethod]
    public async Task SamePathCaseVariantsAggregateAndInspectOnce()
    {
        var inspector = new FakeProcessExecutableInspector();
        ProcessInstanceObservation[] observations =
        [
            ProcessIntelligenceTestFactory.Observation(1, "one", @"C:\Apps\Agent.exe"),
            ProcessIntelligenceTestFactory.Observation(2, "two", @"c:\apps\agent.EXE")
        ];

        ProcessIntelligenceResult result = await Service(inspector).AnalyzeAsync(observations);

        Assert.HasCount(1, result.Groups);
        Assert.AreEqual(2, result.Groups[0].MemberCount);
        Assert.AreEqual(1, inspector.TotalCalls);
    }

    [TestMethod]
    public async Task SameNameDifferentPathsRemainSeparate()
    {
        ProcessInstanceObservation[] observations =
        [
            ProcessIntelligenceTestFactory.Observation(1, "agent", @"C:\One\agent.exe"),
            ProcessIntelligenceTestFactory.Observation(2, "agent", @"D:\Two\agent.exe")
        ];

        ProcessIntelligenceResult result = await Service(new()).AnalyzeAsync(observations);
        Assert.HasCount(2, result.Groups);
    }

    [TestMethod]
    public async Task DifferentNamesSamePathAggregateByExecutableIdentity()
    {
        ProcessInstanceObservation[] observations =
        [
            ProcessIntelligenceTestFactory.Observation(1, "first", @"C:\Apps\agent.exe"),
            ProcessIntelligenceTestFactory.Observation(2, "second", @"C:\Apps\agent.exe")
        ];

        ProcessIntelligenceResult result = await Service(new()).AnalyzeAsync(observations);
        Assert.HasCount(1, result.Groups);
        Assert.AreEqual(2, result.Groups[0].MemberCount);
    }

    [TestMethod]
    public async Task PathlessAndMalformedObservationsRemainSeparateProvisionalGroups()
    {
        ProcessInstanceObservation[] observations =
        [
            ProcessIntelligenceTestFactory.Observation(1, "same", null),
            ProcessIntelligenceTestFactory.Observation(2, "same", "not-a-full-path"),
            ProcessIntelligenceTestFactory.Observation(3, "same", null)
        ];

        ProcessIntelligenceResult result = await Service(new()).AnalyzeAsync(observations);

        Assert.HasCount(3, result.Groups);
        Assert.IsTrue(result.Entries.All(entry => entry.IdentityStrength == ProcessIdentityStrength.Provisional));
        Assert.IsTrue(result.Entries.All(entry => entry.Classification == ProcessIdentityClassification.Unknown));
        Assert.IsTrue(result.PartialSuccess);
    }

    [TestMethod]
    public async Task InspectorFailurePreservesOtherEntriesAndProducesBoundedErrors()
    {
        var inspector = new FakeProcessExecutableInspector
        {
            ExceptionFactory = path => path.Contains("bad", StringComparison.OrdinalIgnoreCase)
                ? new IOException("sensitive path must not be echoed")
                : null
        };

        ProcessIntelligenceResult result = await Service(inspector).AnalyzeAsync(
        [
            ProcessIntelligenceTestFactory.Observation(1, path: @"C:\Apps\good.exe"),
            ProcessIntelligenceTestFactory.Observation(2, path: @"C:\Users\Alice\bad.exe")
        ]);

        Assert.HasCount(2, result.Entries);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.PartialSuccess);
        Assert.IsLessThanOrEqualTo(ProcessIntelligenceService.MaximumMessages, result.Errors.Count);
        Assert.IsFalse(string.Join(" ", result.Errors).Contains("Alice", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task InspectorWarningsAreBoundedAndDoNotEchoSensitiveInput()
    {
        var inspector = new FakeProcessExecutableInspector
        {
            Handler = path => ProcessIntelligenceTestFactory.Inspection(
                path,
                warnings: Enumerable.Repeat(@"C:\Users\Alice\SUPER_SECRET_VALUE", 40).ToArray())
        };

        ProcessIntelligenceResult result = await Service(inspector).AnalyzeAsync(
            [ProcessIntelligenceTestFactory.Observation(path: @"C:\Users\Alice\app.exe")]);

        Assert.IsTrue(result.PartialSuccess);
        Assert.IsLessThanOrEqualTo(ProcessIntelligenceService.MaximumMessages, result.Warnings.Count);
        string messages = string.Join(" ", result.Warnings.Concat(result.Errors));
        Assert.IsFalse(messages.Contains("Alice", StringComparison.Ordinal));
        Assert.IsFalse(messages.Contains("SUPER_SECRET_VALUE", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CollectionBearingModelsDefensivelyCopyInputs()
    {
        var warnings = new List<string> { "first" };
        ProcessExecutableInspection inspection = ProcessIntelligenceTestFactory.Inspection(
            @"C:\Apps\app.exe", warnings: warnings);
        warnings.Add("mutated");

        var entries = new List<ProcessIntelligenceEntry>();
        var groups = new List<ProcessApplicationGroup>();
        var resultWarnings = new List<string> { "one" };
        var result = new ProcessIntelligenceResult(entries, groups, resultWarnings, []);
        resultWarnings.Add("two");

        Assert.HasCount(1, inspection.Warnings);
        Assert.HasCount(1, result.Warnings);
        Assert.IsEmpty(result.Entries);
        Assert.IsEmpty(result.Groups);
    }

    [TestMethod]
    public async Task CancellationPropagates()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
            Service(new()).AnalyzeAsync([ProcessIntelligenceTestFactory.Observation()], source.Token));
    }

    [TestMethod]
    public void ObservationRejectsInvalidMetricsAndNonUtcStartTime()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            ProcessIntelligenceTestFactory.Observation(cpu: double.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            ProcessIntelligenceTestFactory.Observation(memory: -1));
        Assert.ThrowsExactly<ArgumentException>(() =>
            ProcessIntelligenceTestFactory.Observation(startTimeUtc: DateTime.Now));
    }

    private static ProcessIntelligenceService Service(FakeProcessExecutableInspector inspector) => new(inspector);
}
