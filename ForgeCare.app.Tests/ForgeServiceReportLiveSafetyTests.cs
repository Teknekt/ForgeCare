namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ForgeServiceReportLiveSafetyTests
{
    [TestMethod]
    public void LiveServiceHasOneReadAndNoMutationOrAttentionDependencies()
    {
        string source = Read("ForgeCare.app", "Services", "ForgeServiceReportLiveService.cs");

        Assert.AreEqual(1, Count(source, "GetBySessionAsync("));
        StringAssert.Contains(source, "ForgeServiceReportBuilder");
        StringAssert.Contains(source, "ForgeServiceReportHtmlRenderer");
        string[] forbidden =
        {
            "AddAsync(", "AddRangeAsync(", "EvidenceService", "EvidenceCorrelationEngine",
            "ForgePlanAttention", "StartupScanner", "SystemScanner", "ResourceAnalyzerService",
            "StartupManagerService", "Registry", "ServiceController", "Process.Start",
            "Process.Kill", "HttpClient", "File.Write", "File.Move", "File.Delete"
        };

        foreach (string value in forbidden)
            Assert.IsFalse(source.Contains(value, StringComparison.Ordinal), value);
    }

    [TestMethod]
    public void HostProfessionalHandlerUsesApprovedPipelineWithoutDiagnosticsOrMutation()
    {
        string source = Read("ForgeCare.app", "MainWindow.xaml.cs");
        string handler = Slice(
            source,
            "private async void ExportProfessionalReportButton_Click(",
            "private static void RecordProfessionalReportFailure(");

        StringAssert.Contains(handler, "_forgeReportService.Snapshot()");
        StringAssert.Contains(handler, "_forgeServiceReportLiveService.GenerateAsync(session)");
        StringAssert.Contains(handler, "_forgeReportService.ExportProfessionalHtmlAsync(");
        string[] forbidden =
        {
            "EvidenceCorrelationEngine", "ForgePlanAttention", "SystemScanner", "StartupScanner",
            "ResourceAnalyzerService", "StartupManagerService", "AddAsync(", "AddRangeAsync(",
            "Registry", "ServiceController", "Process.Kill", "SafetyJournalService"
        };

        foreach (string value in forbidden)
            Assert.IsFalse(handler.Contains(value, StringComparison.Ordinal), value);
    }

    [TestMethod]
    public void MainWindowSharesEvidenceRepositoryWithProfessionalReportService()
    {
        string source = Read("ForgeCare.app", "MainWindow.xaml.cs");
        string construction = Slice(
            source,
            "_forgeServiceReportLiveService =",
            "Loaded +=");

        StringAssert.Contains(construction, "new ForgeServiceReportLiveService(");
        StringAssert.Contains(construction, "_evidenceRepository,");
        Assert.IsFalse(construction.Contains("new JsonEvidenceRepository", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ReportsUiKeepsProfessionalAndLegacyCommandsDistinct()
    {
        string xaml = Read("ForgeCare.app", "MainWindow.xaml");

        StringAssert.Contains(xaml, "Click=\"ExportProfessionalReportButton_Click\"");
        StringAssert.Contains(xaml, "Click=\"ExportReportButton_Click\"");
        StringAssert.Contains(xaml, "EXPORT PROFESSIONAL SERVICE REPORT");
        StringAssert.Contains(xaml, "EXPORT LEGACY HTML REPORT");
        StringAssert.Contains(xaml, "DisplayReportKind");
    }

    private static int Count(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static string Slice(string source, string start, string end)
    {
        int startIndex = source.IndexOf(start, StringComparison.Ordinal);
        int endIndex = source.IndexOf(end, startIndex, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, startIndex);
        Assert.IsGreaterThan(startIndex, endIndex);
        return source[startIndex..endIndex];
    }

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { FindRepositoryRoot() }.Concat(parts).ToArray()));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ForgeCare.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
