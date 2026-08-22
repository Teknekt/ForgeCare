using System.Reflection;
using ForgeCare.App.Models;
using ForgeCare.App.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForgeCare.app.Tests;

[TestClass]
public sealed class ForgePlanAttentionSafetyTests
{
    [TestMethod]
    public void PhaseCProductionHasNoLiveExecutionPersistenceOrWpfDependency()
    {
        string root = FindRepositoryRoot();
        string[] files =
        [
            "Models/ForgePlanAttentionItem.cs",
            "Services/ForgePlanAttentionPresenter.cs",
            "Services/ProcessSystemCpuAttentionRule.cs"
        ];
        string source = string.Join('\n', files.Select(file =>
            File.ReadAllText(Path.Combine(root, "ForgeCare.app", file.Replace('/', Path.DirectorySeparatorChar)))));
        string[] forbidden =
        [
            "MainWindow", "System.Windows", "IEvidenceRepository", "JsonEvidenceRepository",
            "EvidenceService", "ForgeReportService", "SystemScanner", "ResourceAnalyzerService",
            "StartupScanner", "System.Diagnostics.Process", "Process.GetProcesses", "Process.Start",
            "Process.Kill", "StartupManagerService", "CleanupExecutor", "StorageCleanupService",
            "ServiceController", "Microsoft.Win32", "Registry.", "File.Write", "File.Move",
            "File.Delete", "ControlledInstallerHandoffService", "HttpClient", "WebClient", "Socket",
            "ForgePlanItem", "ForgePlanResult", "CanExecute", "IsSelected", "ActionLabel"
        ];
        foreach (string token in forbidden)
            Assert.IsFalse(source.Contains(token, StringComparison.Ordinal), token);
    }

    [TestMethod]
    public void PresenterOnlyExposesPureProjectionMethod()
    {
        string[] methods = typeof(ForgePlanAttentionPresenter)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .ToArray();
        CollectionAssert.AreEquivalent(new[] { "Present" }, methods);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "ForgeCare.app")) &&
                Directory.Exists(Path.Combine(current.FullName, "ForgeCare.app.Tests")))
                return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
