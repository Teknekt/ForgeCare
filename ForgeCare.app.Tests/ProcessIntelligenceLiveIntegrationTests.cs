using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ProcessIntelligenceLiveIntegrationTests
{
    [TestMethod]
    public void MainWindowOrdersProcessCaptureAfterDeepAnalysisAndUsesAuthoritativeInputs()
    {
        string source = MainWindowSource();
        string handler = Slice(source, "private async void RunDeepAnalysisButton_Click", "private async Task CaptureDeepAnalysisEvidenceAsync");
        int deep = handler.IndexOf("await CaptureDeepAnalysisEvidenceAsync", StringComparison.Ordinal);
        int process = handler.IndexOf("await CaptureProcessIntelligenceEvidenceAsync", StringComparison.Ordinal);
        string helper = Slice(source, "private async Task CaptureProcessIntelligenceEvidenceAsync", "// STORAGE DEEP SCAN");

        Assert.IsGreaterThanOrEqualTo(0, deep);
        Assert.IsGreaterThan(deep, process);
        StringAssert.Contains(helper, "_forgeReportService");
        StringAssert.Contains(helper, ".Snapshot()");
        StringAssert.Contains(helper, ".SessionId");
        StringAssert.Contains(helper, "result.ProcessObservations");
        StringAssert.Contains(helper, "result.AnalysisTime.ToUniversalTime()");
        StringAssert.Contains(helper, "_evidenceService.AddRangeAsync");
        StringAssert.Contains(helper, "result.ProcessObservations.Count == 0");
        StringAssert.Contains(helper, "catch (Exception ex)");
    }

    [TestMethod]
    public void PostAnalysisHelperDoesNotRescanReacquireOrConstructPathMetadata()
    {
        string helper = Slice(MainWindowSource(),
            "private async Task CaptureProcessIntelligenceEvidenceAsync", "// STORAGE DEEP SCAN");
        string[] forbidden =
        [
            "Process.GetProcesses", "Process.GetProcessById", "Process.Start", "Process.Kill",
            "CanonicalExecutablePath", "normalizedExecutablePath", "new EvidenceRecord",
            "File.Move", "File.Delete", "Registry", "ServiceController"
        ];
        foreach (string token in forbidden)
            Assert.IsFalse(helper.Contains(token, StringComparison.Ordinal), $"Forbidden helper token: {token}");
    }

    [TestMethod]
    public void AnalyzerUsesExactlyExistingTwoEnumerationsAndBuildsObservationsFromSecondLoop()
    {
        string root = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(root, "ForgeCare.app", "Services", "ResourceAnalyzerService.cs"));

        Assert.AreEqual(2, Count(source, "Process.GetProcesses()"));
        Assert.IsFalse(source.Contains("Process.GetProcessById", StringComparison.Ordinal));
        StringAssert.Contains(source, "processObservations.Add");
        StringAssert.Contains(source, "TryGetStartTimeUtc(process)");
        StringAssert.Contains(source, "TryGetExecutablePath(process)");
        StringAssert.Contains(source, "ProcessObservations =");
        StringAssert.Contains(source, ".Take(14)");
    }

    [TestMethod]
    public async Task RepeatedCapturesAppendDeepAndProcessEvidenceWithoutDeduplication()
    {
        using var temp = new TemporaryDirectory();
        string sessionId = Guid.NewGuid().ToString("N");
        DateTime firstTime = new(2026, 8, 22, 10, 0, 0, DateTimeKind.Utc);
        var service = new EvidenceService(new JsonEvidenceRepository(temp.Path));
        EvidenceCollectionResult firstProcess = ProcessEvidenceTestFactory.Adapter().Collect(
            ProcessEvidenceTestFactory.Result(ProcessEvidenceTestFactory.Group()), sessionId, firstTime);
        EvidenceCollectionResult secondProcess = ProcessEvidenceTestFactory.Adapter().Collect(
            ProcessEvidenceTestFactory.Result(ProcessEvidenceTestFactory.Group()), sessionId, firstTime.AddMinutes(1));
        EvidenceRecord firstDeep = ProcessEvidenceTestFactory.ExistingRecord(sessionId, EvidenceSource.DeepAnalysis, firstTime);
        EvidenceRecord secondDeep = ProcessEvidenceTestFactory.ExistingRecord(sessionId, EvidenceSource.DeepAnalysis, firstTime.AddMinutes(1));

        await service.AddRangeAsync([firstDeep, .. firstProcess.Evidence]);
        await service.AddRangeAsync([secondDeep, .. secondProcess.Evidence]);
        IReadOnlyList<EvidenceRecord> records = await new JsonEvidenceRepository(temp.Path).GetBySessionAsync(sessionId);

        Assert.HasCount(4, records);
        Assert.AreEqual(2, records.Count(record => record.Source == EvidenceSource.DeepAnalysis));
        Assert.AreEqual(2, records.Count(record => record.Source == EvidenceSource.ProcessIntelligence));
        Assert.AreEqual(firstProcess.Evidence[0].CorrelationKey, secondProcess.Evidence[0].CorrelationKey);
        Assert.AreNotEqual(firstProcess.Evidence[0].Id, secondProcess.Evidence[0].Id);
    }

    private static string MainWindowSource()
    {
        string root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, "ForgeCare.app", "MainWindow.xaml.cs"));
    }

    private static string Slice(string source, string start, string end)
    {
        int first = source.IndexOf(start, StringComparison.Ordinal);
        int last = source.IndexOf(end, first + start.Length, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, first);
        Assert.IsGreaterThan(first, last);
        return source[first..last];
    }

    private static int Count(string value, string token) =>
        (value.Length - value.Replace(token, string.Empty, StringComparison.Ordinal).Length) / token.Length;

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "ForgeCare.app")) &&
                Directory.Exists(Path.Combine(directory.FullName, "ForgeCare.app.Tests"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new AssertFailedException("Repository root could not be located.");
    }
}
