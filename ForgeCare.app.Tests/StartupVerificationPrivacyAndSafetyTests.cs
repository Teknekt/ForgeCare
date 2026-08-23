using System.Reflection;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public class StartupVerificationPrivacyAndSafetyTests
{
    [TestMethod]
    public void DurableModelsAndRationales_DoNotContainTransientSecrets()
    {
        const string raw = @"C:\Users\Alice\agent.exe --token=SUPER_SECRET_VALUE API_KEY_TEST_VALUE";
        var builder = new StartupVerificationIdentityBuilder();
        StartupVerificationTargetIdentity target = builder.BuildHkcuRun("Agent", raw);
        StartupActionReceipt receipt = StartupVerificationTestFactory.Receipt(
            StartupVerificationOperation.Restore, StartupExecutionOutcome.Executed, target);
        StartupObservationSnapshot observation = StartupVerificationTestFactory.Observation(
            StartupVerificationTargetKind.HkcuRun, targets: new[] { target });
        StartupVerificationResult result = StartupVerificationTestFactory.Evaluate(receipt, observation);

        string durable = string.Join('|',
            target.DisplayName, target.SourceLocator, target.LocatorId, target.TargetId,
            target.ConfigurationFingerprint, receipt.Items[0].ExpectedState.Summary,
            result.Rationale, result.ExpectedStateSummary);
        foreach (string forbidden in new[] { @"C:\Users", "Alice", "SUPER_SECRET_VALUE", "API_KEY_TEST_VALUE", "--token" })
            Assert.IsFalse(durable.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
    }

    [TestMethod]
    public void Wording_RemainsFactualAndAvoidsCausalityClaims()
    {
        var receipt = StartupVerificationTestFactory.Receipt(
            StartupVerificationOperation.Disable, StartupExecutionOutcome.Executed);
        var observation = StartupVerificationTestFactory.Observation(
            StartupVerificationTargetKind.HkcuRun);
        StartupVerificationResult result = StartupVerificationTestFactory.Evaluate(receipt, observation);
        string wording = result.Rationale + " " + result.ExpectedStateSummary;

        foreach (string forbidden in new[]
        {
            "improved", "faster", "optimized", "safe", "unsafe", "unnecessary",
            "caused", "reduced cpu", "reduced memory", "prevented the application from running"
        })
        {
            Assert.IsFalse(wording.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        }
    }

    [TestMethod]
    public void PhaseBProductionFiles_HaveNoForbiddenDependenciesOrMutationCalls()
    {
        string root = FindRepositoryRoot();
        string[] files =
        {
            "StartupVerificationTargetIdentity.cs", "StartupActionReceipt.cs",
            "StartupExpectedState.cs", "StartupObservationSnapshot.cs",
            "StartupVerificationResult.cs", "StartupVerificationIdentityBuilder.cs",
            "StartupVerificationEvaluator.cs"
        };
        string source = string.Join('\n', files.Select(file =>
        {
            string folder = file is "StartupVerificationIdentityBuilder.cs" or "StartupVerificationEvaluator.cs"
                ? "Services"
                : "Models";
            return File.ReadAllText(Path.Combine(root, "ForgeCare.app", folder, file));
        }));

        foreach (string forbidden in new[]
        {
            "StartupManagerService", "StartupScanner", "SystemScanner", "Microsoft.Win32",
            "Registry.", "File.Move", "File.Delete", "File.Write", "Process.Start", "Process.Kill",
            "ServiceController", "EvidenceService", "JsonEvidenceRepository", "ForgeReportService",
            "MainWindow", "System.Windows", "HttpClient", "WebClient", "ControlledInstallerHandoffService"
        })
        {
            Assert.IsFalse(source.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }
    }

    [TestMethod]
    public void PublicDomainCollections_AreReadOnlyContracts()
    {
        Assert.AreEqual(typeof(IReadOnlyList<StartupActionReceiptItem>),
            typeof(StartupActionReceipt).GetProperty(nameof(StartupActionReceipt.Items))!.PropertyType);
        Assert.AreEqual(typeof(IReadOnlyList<StartupSourceObservation>),
            typeof(StartupObservationSnapshot).GetProperty(nameof(StartupObservationSnapshot.Sources))!.PropertyType);
        Assert.AreEqual(typeof(IReadOnlyList<StartupVerificationTargetIdentity>),
            typeof(StartupSourceObservation).GetProperty(nameof(StartupSourceObservation.Targets))!.PropertyType);
    }

    private static string FindRepositoryRoot()
    {
        string? current = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current, "ForgeCare.app")) &&
                Directory.Exists(Path.Combine(current, "ForgeCare.app.Tests")))
                return current;
            current = Directory.GetParent(current)?.FullName;
        }
        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
