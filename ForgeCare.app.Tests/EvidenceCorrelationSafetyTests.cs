using System.Reflection;
using ForgeCare.App.Models;
using ForgeCare.App.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForgeCare.app.Tests;

[TestClass]
public sealed class EvidenceCorrelationSafetyTests
{
    [TestMethod]
    public void AttentionDomainContainsNoExecutionOrSelectionContract()
    {
        string[] names = typeof(TechnicianAttentionItem)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToArray();
        foreach (string forbidden in new[]
                 { "IsSelected", "CanExecute", "Route", "ActionLabel", "Risk", "Reversible", "Reviewed", "Accepted" })
            CollectionAssert.DoesNotContain(names, forbidden);
    }

    [TestMethod]
    public void CorrelationProductionSurfaceHasNoForbiddenDependencies()
    {
        string root = FindRepositoryRoot();
        string[] files =
        [
            "Models/TechnicianAttentionItem.cs",
            "Models/EvidenceCorrelationResult.cs",
            "Services/IEvidenceCorrelationRule.cs",
            "Services/EvidenceCorrelationEngine.cs",
            "Services/StartupProcessAttentionRule.cs"
        ];
        string source = string.Join('\n', files.Select(file =>
            File.ReadAllText(Path.Combine(root, "ForgeCare.app", file.Replace('/', Path.DirectorySeparatorChar)))));
        string[] forbidden =
        [
            "SystemScanner", "ResourceAnalyzerService", "StartupScanner",
            "System.Diagnostics.Process", "Process.GetProcesses", "Process.GetProcessById",
            "Process.Start", "Process.Kill", "StartupManagerService", "CleanupExecutor",
            "StorageCleanupService", "ServiceController", "Microsoft.Win32", "Registry.",
            "FileVersionInfo", "WinVerifyTrust", "StartupFileInspector", "SignatureInspector",
            "File.Write", "File.Move", "File.Delete", "ControlledInstallerHandoffService",
            "HttpClient", "WebClient", "Socket", "IEvidenceRepository", "JsonEvidenceRepository",
            "EvidenceService", "ForgeReportService", "ForgePlanItem", "ForgePlanResult",
            "OptimizationRecommendation", "new Recommendation"
        ];
        foreach (string token in forbidden)
            Assert.IsFalse(source.Contains(token, StringComparison.Ordinal), token);
    }

    [TestMethod]
    public void EngineDoesNotExposeEvidenceWriteMethods()
    {
        string[] methods = typeof(EvidenceCorrelationEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .ToArray();
        CollectionAssert.AreEquivalent(new[] { "Correlate" }, methods);
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
