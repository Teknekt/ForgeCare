using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ProcessAggregationTests
{
    [TestMethod]
    public async Task ThreeInstancesAggregateDefinedResourceMetrics()
    {
        ProcessInstanceObservation[] observations =
        [
            ProcessIntelligenceTestFactory.Observation(30, cpu: 60, memory: 300, pressure: 70, pressureLevel: "HIGH"),
            ProcessIntelligenceTestFactory.Observation(10, cpu: 50, memory: 200, pressure: 45, pressureLevel: "MODERATE"),
            ProcessIntelligenceTestFactory.Observation(20, cpu: 25, memory: 100, pressure: 20, pressureLevel: "LOW")
        ];

        ProcessApplicationGroup group = (await Service().AnalyzeAsync(observations)).Groups.Single();

        Assert.AreEqual(135, group.TotalCpuPercent);
        Assert.AreEqual(600, group.TotalMemoryMb);
        Assert.AreEqual(60, group.MaximumInstanceCpuPercent);
        Assert.AreEqual(300, group.MaximumInstanceMemoryMb);
        Assert.AreEqual(70, group.MaximumPressureScore);
        Assert.AreEqual("HIGH", group.StrongestPressureLevel);
        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, group.Members.Select(x => x.Observation.ProcessId).ToArray());
    }

    [TestMethod]
    public async Task GroupOrderingIsDeterministic()
    {
        ProcessInstanceObservation[] observations =
        [
            ProcessIntelligenceTestFactory.Observation(1, "zeta", @"C:\Apps\zeta.exe", 10, 100, 20),
            ProcessIntelligenceTestFactory.Observation(2, "alpha", @"C:\Apps\alpha.exe", 20, 200, 50),
            ProcessIntelligenceTestFactory.Observation(3, "beta", @"C:\Apps\beta.exe", 30, 300, 50)
        ];

        ProcessIntelligenceResult first = await Service().AnalyzeAsync(observations);
        ProcessIntelligenceResult second = await Service().AnalyzeAsync(observations.Reverse());

        CollectionAssert.AreEqual(
            first.Groups.Select(group => group.CanonicalExecutablePath).ToArray(),
            second.Groups.Select(group => group.CanonicalExecutablePath).ToArray());
        Assert.AreEqual("beta", first.Groups[0].DisplayName);
    }

    [TestMethod]
    [DataRow(100)]
    [DataRow(300)]
    public async Task ConstructedScaleRetainsAllObservationsAndCachesUniquePaths(int count)
    {
        var inspector = new FakeProcessExecutableInspector();
        ProcessInstanceObservation[] observations = Enumerable.Range(0, count)
            .Select(index => ProcessIntelligenceTestFactory.Observation(
                index + 1,
                $"app-{index % 10}",
                $@"C:\Apps\app-{index % 10}.exe",
                index % 20,
                50 + index,
                index % 80))
            .ToArray();

        ProcessIntelligenceResult result = await new ProcessIntelligenceService(inspector).AnalyzeAsync(observations);

        Assert.HasCount(count, result.Entries);
        Assert.HasCount(10, result.Groups);
        Assert.AreEqual(10, inspector.TotalCalls);
        Assert.IsLessThanOrEqualTo(ProcessIntelligenceService.MaximumConcurrency, inspector.MaximumActive);
    }

    private static ProcessIntelligenceService Service() => new(new FakeProcessExecutableInspector
    {
        Handler = path => new ProcessExecutableInspection(
            path, ProcessFileInspectionStatus.Available, true, Path.GetFileName(path), Path.GetExtension(path),
            null, Path.GetFileNameWithoutExtension(path), null, null, null, null,
            ProcessSignatureStatus.Valid, null)
    });
}
