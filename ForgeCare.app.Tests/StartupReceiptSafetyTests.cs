using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public class StartupReceiptSafetyTests
{
    [TestMethod]
    public void ReceiptProductionPathHasNoScannerEvidenceNetworkOrNewMutationDependencies()
    {
        string root = FindRepositoryRoot();
        string source = string.Join('\n', new[]
        {
            File.ReadAllText(Path.Combine(root, "ForgeCare.app", "Services", "StartupActionReceiptBuilder.cs")),
            File.ReadAllText(Path.Combine(root, "ForgeCare.app", "Services", "StartupReceiptIntegrationService.cs"))
        });

        foreach (string forbidden in new[]
        {
            "StartupScanner", "SystemScanner", "EvidenceService", "JsonEvidenceRepository",
            "ForgeWorkflowService", "Registry.SetValue", "Registry.DeleteValue", "File.Move",
            "File.Delete", "Process.Start", "Process.Kill", "ServiceController", "HttpClient",
            "WebClient", "MainWindow", "System.Windows"
        })
            Assert.IsFalse(source.Contains(forbidden, StringComparison.Ordinal), forbidden);
    }

    [TestMethod]
    public void LiveHostsDoNotRunVerificationEvaluatorOrSystemScanForReceipts()
    {
        string root = FindRepositoryRoot();
        string startupHost = File.ReadAllText(Path.Combine(root, "ForgeCare.app", "StartupReviewWindow.xaml.cs"));
        string mainHost = File.ReadAllText(Path.Combine(root, "ForgeCare.app", "MainWindow.xaml.cs"));
        string mainRestore = Between(
            mainHost,
            "private async void SafetyRestoreStartupButton_Click",
            "private void ClearSafetyJournalButton_Click");

        foreach (string source in new[] { startupHost, mainRestore })
        {
            Assert.IsFalse(source.Contains("StartupVerificationEvaluator", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("SystemScanner.Scan", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("StartupScanner", StringComparison.Ordinal));
        }
    }

    private static string Between(string source, string start, string end)
    {
        int startIndex = source.IndexOf(start, StringComparison.Ordinal);
        int endIndex = source.IndexOf(end, startIndex, StringComparison.Ordinal);
        return source[startIndex..endIndex];
    }

    private static string FindRepositoryRoot()
    {
        string? current = AppContext.BaseDirectory;
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current, "ForgeCare.app")))
                return current;
            current = Directory.GetParent(current)?.FullName;
        }
        throw new DirectoryNotFoundException();
    }
}
