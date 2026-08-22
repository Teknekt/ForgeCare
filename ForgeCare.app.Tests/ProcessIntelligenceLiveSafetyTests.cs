namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ProcessIntelligenceLiveSafetyTests
{
    [TestMethod]
    public void LiveEnrichmentPathContainsNoProcessOrSystemMutationCapabilities()
    {
        string source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ForgeCare.app", "MainWindow.xaml.cs"));
        string helper = Slice(source,
            "private async Task CaptureProcessIntelligenceEvidenceAsync", "// STORAGE DEEP SCAN");
        string[] forbidden =
        [
            "Process.GetProcesses", "Process.GetProcessById", "Process.Start", "Process.Kill",
            "TerminateProcess", "SuspendThread", "PriorityClass", "ProcessorAffinity",
            "Registry.SetValue", "Registry.DeleteValue", "CreateSubKey", "File.Move", "File.Delete",
            "ServiceController", "ControlledInstallerHandoffService", "HttpClient", "WebClient"
        ];
        foreach (string token in forbidden)
            Assert.IsFalse(helper.Contains(token, StringComparison.Ordinal), $"Forbidden helper token: {token}");
    }

    [TestMethod]
    public void HostSharesInspectionAndEvidenceInfrastructureWithoutAnotherRepository()
    {
        string source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ForgeCare.app", "MainWindow.xaml.cs"));
        Assert.AreEqual(1, Count(source, "new JsonEvidenceRepository()"));
        string construction = Slice(source, "_processIntelligenceService =", "_processIntelligenceEvidenceAdapter =");
        StringAssert.Contains(construction, "new WindowsProcessExecutableInspector");
        StringAssert.Contains(construction, "fileInspector");
        StringAssert.Contains(construction, "signatureInspector");
        Assert.AreEqual(1, Count(source, "new WindowsStartupFileInspector()"));
        Assert.AreEqual(1, Count(source, "new WinVerifyTrustStartupSignatureInspector()"));
    }

    private static string Slice(string source, string start, string end)
    {
        int first = source.IndexOf(start, StringComparison.Ordinal);
        int last = source.IndexOf(end, first + start.Length, StringComparison.Ordinal);
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
