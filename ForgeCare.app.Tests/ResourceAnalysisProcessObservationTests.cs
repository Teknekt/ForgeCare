using System.Text.Json;
using ForgeCare.App.Models;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ResourceAnalysisProcessObservationTests
{
    [TestMethod]
    public void CompleteObservationsAreDefensivelyCopiedAndIndependentFromTopProcesses()
    {
        var observations = Enumerable.Range(1, 20)
            .Select(index => ProcessIntelligenceTestFactory.Observation(
                index,
                $"process-{index}",
                $@"C:\Apps\process-{index}.exe",
                startTimeUtc: new DateTime(2026, 8, 22, 10, 0, index, DateTimeKind.Utc)))
            .ToList();
        var top = Enumerable.Range(1, 14)
            .Select(index => new ResourceProcessInfo { ProcessId = index, Name = $"process-{index}" })
            .ToList();
        var result = new ResourceAnalysisResult
        {
            ProcessObservations = observations,
            TopProcesses = top
        };
        observations.Clear();
        top.Clear();

        Assert.HasCount(20, result.ProcessObservations);
        Assert.IsEmpty(result.TopProcesses);
        Assert.AreEqual(1, result.ProcessObservations[0].ProcessId);
        Assert.AreEqual(@"C:\Apps\process-1.exe", result.ProcessObservations[0].ExecutablePath);
        Assert.AreEqual(DateTimeKind.Utc, result.ProcessObservations[0].StartTimeUtc!.Value.Kind);
        Assert.ThrowsExactly<NotSupportedException>(() =>
            ((IList<ProcessInstanceObservation>)result.ProcessObservations).Add(
                ProcessIntelligenceTestFactory.Observation()));
    }

    [TestMethod]
    public void DefaultObservationCollectionIsEmptyAndExcludedFromJsonSerialization()
    {
        var empty = new ResourceAnalysisResult();
        var populated = new ResourceAnalysisResult
        {
            ProcessObservations = [ProcessIntelligenceTestFactory.Observation()]
        };

        Assert.IsEmpty(empty.ProcessObservations);
        string json = JsonSerializer.Serialize(populated);
        Assert.IsFalse(json.Contains("ProcessObservations", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains(@"C:\Apps\app.exe", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TopProcessesCanRemainCappedWhileCompleteObservationsContainMoreItems()
    {
        var result = new ResourceAnalysisResult
        {
            TopProcesses = Enumerable.Range(1, 14)
                .Select(index => new ResourceProcessInfo { ProcessId = index, Name = $"top-{index}" })
                .ToList(),
            ProcessObservations = Enumerable.Range(1, 30)
                .Select(index => ProcessIntelligenceTestFactory.Observation(index))
                .ToArray()
        };

        Assert.HasCount(14, result.TopProcesses);
        Assert.HasCount(30, result.ProcessObservations);
    }
}
