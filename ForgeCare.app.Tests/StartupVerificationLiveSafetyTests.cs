namespace ForgeCare.App.Tests;

[TestClass]
public class StartupVerificationLiveSafetyTests
{
    [TestMethod]
    public void LiveVerificationPathIsReadOnlyAndDoesNotRescanOrWriteEvidence()
    {
        string root = FindRepositoryRoot();
        string live = File.ReadAllText(Path.Combine(root, "ForgeCare.app", "Services", "StartupVerificationLiveService.cs"));
        string builder = File.ReadAllText(Path.Combine(root, "ForgeCare.app", "Services", "StartupObservationSnapshotBuilder.cs"));
        string combined = live + builder;

        string[] forbidden =
        {
            "StartupManagerService", "Registry.SetValue", "Registry.DeleteValue",
            "CreateSubKey", "File.Move", "File.Delete", "File.Write",
            "Process.Start", "Process.Kill", "ServiceController",
            "ControlledInstallerHandoffService", "AddAsync", "AddRangeAsync",
            "SystemScanner", "StartupScanner", "HttpClient"
        };

        foreach (string token in forbidden)
            Assert.IsFalse(combined.Contains(token, StringComparison.Ordinal), token);

        StringAssert.Contains(live, "CrashLogService.RecordPrivacySafe");
    }

    [TestMethod]
    public void MainWindowInvokesVerificationOnlyAfterExistingScanEvidencePaths()
    {
        string root = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(root, "ForgeCare.app", "MainWindow.xaml.cs"));
        int systemEvidence = source.IndexOf("await CaptureSystemScanEvidenceAsync", StringComparison.Ordinal);
        int startupEvidence = source.IndexOf("await CaptureStartupIntelligenceEvidenceAsync", StringComparison.Ordinal);
        int verification = source.IndexOf("_startupVerificationLiveService.VerifyAfterSystemScan", StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, systemEvidence);
        Assert.IsGreaterThan(systemEvidence, startupEvidence);
        Assert.IsGreaterThan(startupEvidence, verification);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "ForgeCare.app")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
