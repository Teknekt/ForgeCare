namespace ForgeCare.App.Tests;

[TestClass]
public sealed class StartupIntelligenceLiveIntegrationTests
{
    [TestMethod]
    public void SystemScanSuccessPathRunsStartupIntelligenceAfterSystemScanEvidence()
    {
        string source = ReadMainWindowSource();
        string scanHandler = ExtractMethod(source, "ScanButton_Click");

        int systemEvidence = scanHandler.IndexOf(
            "CaptureSystemScanEvidenceAsync",
            StringComparison.Ordinal);
        int startupIntelligence = scanHandler.IndexOf(
            "CaptureStartupIntelligenceEvidenceAsync",
            StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, systemEvidence, "System Scan Evidence capture is missing from the successful scan path.");
        Assert.IsGreaterThan(systemEvidence, startupIntelligence, "Startup Intelligence must run after System Scan Evidence capture.");
    }

    [TestMethod]
    public void LiveHelperReusesSnapshotSessionTimestampAndEvidenceService()
    {
        string source = ReadMainWindowSource();
        string helper = ExtractMethod(source, "CaptureStartupIntelligenceEvidenceAsync");

        StringAssert.Contains(helper, "_forgeReportService");
        StringAssert.Contains(helper, ".Snapshot()");
        StringAssert.Contains(helper, ".SessionId");
        StringAssert.Contains(helper, "Guid.TryParseExact");
        StringAssert.Contains(helper, "snapshot.StartupItems");
        StringAssert.Contains(helper, "snapshot.ScanTime.ToUniversalTime()");
        StringAssert.Contains(helper, "_startupIntelligenceService.AnalyzeAsync");
        StringAssert.Contains(helper, "_startupIntelligenceEvidenceAdapter.Collect");
        StringAssert.Contains(helper, "_evidenceService.AddRangeAsync");
    }

    [TestMethod]
    public void LiveHelperHasAnIsolatedPrivacySafeFailureBoundary()
    {
        string source = ReadMainWindowSource();
        string helper = ExtractMethod(source, "CaptureStartupIntelligenceEvidenceAsync");
        string[] forbiddenTokens =
        [
            "_scanner", "StartupScanner", "SystemScanner", "StartupManagerService",
            "StartupChangeItem", "Registry.SetValue", "Registry.DeleteValue",
            "CreateSubKey", "File.Move", "File.Delete", "Process.Start",
            "Process.Kill", "ServiceController", "ControlledInstallerHandoffService",
            ".Command", ".Arguments"
        ];

        StringAssert.Contains(helper, "catch (OperationCanceledException)");
        StringAssert.Contains(helper, "catch (Exception ex)");
        StringAssert.Contains(helper, "intelligence.Entries.Count == 0");

        foreach (string token in forbiddenTokens)
        {
            Assert.IsFalse(
                helper.Contains(token, StringComparison.Ordinal),
                $"The live Startup Intelligence helper contains forbidden token '{token}'.");
        }
    }

    [TestMethod]
    public void MainWindowRetainsOneSharedEvidenceRepositoryConstruction()
    {
        string source = ReadMainWindowSource();

        Assert.AreEqual(
            1,
            CountOccurrences(source, "new JsonEvidenceRepository()"),
            "MainWindow should construct one shared Evidence repository.");
        StringAssert.Contains(source, "new EvidenceService(\n                evidenceRepository)");
        StringAssert.Contains(source, "new EvidenceExplorerViewModel(\n                evidenceRepository)");
    }

    private static string ReadMainWindowSource() =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ForgeCare.app", "MainWindow.xaml.cs"));

    private static string ExtractMethod(string source, string methodName)
    {
        string[] declarationPrefixes =
        [
            "private async Task ",
            "private async void ",
            "private static void ",
            "private void "
        ];
        int nameIndex = declarationPrefixes
            .Select(prefix => source.IndexOf(prefix + methodName, StringComparison.Ordinal))
            .FirstOrDefault(index => index >= 0, -1);
        Assert.IsGreaterThanOrEqualTo(0, nameIndex, $"Method '{methodName}' was not found.");

        int openingBrace = source.IndexOf('{', nameIndex);
        Assert.IsGreaterThanOrEqualTo(0, openingBrace, $"Method '{methodName}' has no opening brace.");

        int depth = 0;
        for (int index = openingBrace; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source[nameIndex..(index + 1)];
        }

        throw new AssertFailedException($"Method '{methodName}' has no closing brace.");
    }

    private static int CountOccurrences(string value, string token)
    {
        int count = 0;
        int index = 0;
        while ((index = value.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "ForgeCare.app")) &&
                Directory.Exists(Path.Combine(directory.FullName, "ForgeCare.app.Tests")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new AssertFailedException("Repository root could not be located.");
    }
}
