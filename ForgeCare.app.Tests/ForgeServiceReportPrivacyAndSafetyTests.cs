using System.Reflection;
using System.Text.Json;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ForgeServiceReportPrivacyAndSafetyTests
{
    [TestMethod]
    public void ProjectionDoesNotExposeAdversarialCommandsPathsSecretsOrInternalMaterial()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        session.Actions.Add(new ForgeReportAction
        {
            Category = "STARTUP",
            Title = @"C:\Users\Alice\agent.exe --token=SUPER_SECRET_VALUE",
            Detail = "API_KEY_TEST_VALUE",
            Metric = @"C:\Users\Alice\disabled-storage\agent.lnk"
        });
        StartupVerificationTargetIdentity target = new(
            StartupVerificationTargetKind.HkcuRun,
            @"C:\Users\Alice\agent.exe --token=SUPER_SECRET_VALUE API_KEY_TEST_VALUE",
            "hkcu-run",
            ForgeServiceReportTestFactory.Hash("registry-value-name"),
            ForgeServiceReportTestFactory.Hash("target"),
            ForgeServiceReportTestFactory.Hash("raw configuration fingerprint input"));
        StartupActionReceipt receipt = ForgeServiceReportTestFactory.Receipt(session.SessionId, target: target);
        session.StartupActionReceipts.Add(receipt);
        session.StartupVerificationResults.Add(ForgeServiceReportTestFactory.Verification(
            receipt,
            rationale: @"C:\Users\Alice\private --token=SUPER_SECRET_VALUE API_KEY_TEST_VALUE"));
        EvidenceRecord evidence = ForgeServiceReportTestFactory.Evidence(
            session.SessionId,
            subject: @"C:\Users\Alice\secret.exe");
        evidence.Observation = "--token=SUPER_SECRET_VALUE API_KEY_TEST_VALUE";
        evidence.Metadata["command"] = @"C:\Users\Alice\agent.exe";

        ForgeServiceReportModel report = new ForgeServiceReportBuilder().Build(session, [evidence]);
        string serialized = JsonSerializer.Serialize(report);

        foreach (string forbidden in new[]
        {
            @"C:\Users\Alice", "Alice", "--token", "SUPER_SECRET_VALUE", "API_KEY_TEST_VALUE",
            "disabled-storage", "registry-value-name", "raw configuration fingerprint input",
            target.ConfigurationFingerprint!, target.LocatorId
        })
            Assert.IsFalse(serialized.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        Assert.AreEqual("Startup target", report.Actions.Single().TargetDisplayName);
        Assert.AreEqual("EVIDENCE OBSERVATION", report.EvidenceReferences.Single().SubjectDisplay);
    }

    [TestMethod]
    public void ProfessionalWordingContainsNoCausalityPerformanceOrSafetyClaims()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        StartupActionReceipt receipt = ForgeServiceReportTestFactory.Receipt(session.SessionId);
        session.StartupActionReceipts.Add(receipt);
        session.StartupVerificationResults.Add(ForgeServiceReportTestFactory.Verification(
            receipt,
            StartupVerificationStatus.ExpectedOutcomeNotObserved));

        string serialized = JsonSerializer.Serialize(new ForgeServiceReportBuilder().Build(session));

        foreach (string forbidden in new[]
        {
            "improved startup performance", "faster boot", "reduced resource usage",
            "fixed the issue", "optimized the system", "made the application safe",
            "unnecessary startup item", "caused system pressure"
        })
            Assert.IsFalse(serialized.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        StringAssert.Contains(serialized, "Not Verified");
    }

    [TestMethod]
    public void PhaseBProductionTypesArePureProjectionOnly()
    {
        string root = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(root, "ForgeCare.app", "Services", "ForgeServiceReportBuilder.cs")) +
            File.ReadAllText(Path.Combine(root, "ForgeCare.app", "Models", "ForgeServiceReportModel.cs"));
        string[] forbidden =
        [
            "JsonEvidenceRepository", "EvidenceService", "AddAsync(", "AddRangeAsync(",
            "ForgeReportService", "StartupManagerService", "StartupScanner", "SystemScanner",
            "ResourceAnalyzerService", "System.Diagnostics.Process", "Microsoft.Win32.Registry",
            "File.Write", "File.Move", "File.Delete", "ServiceController", "HttpClient",
            "MainWindow", "System.Windows", "BuildHtml", "ExportHtml"
        ];

        foreach (string token in forbidden)
            Assert.IsFalse(source.Contains(token, StringComparison.Ordinal), token);

        ConstructorInfo[] constructors = typeof(ForgeServiceReportBuilder).GetConstructors();
        Assert.HasCount(1, constructors);
        Assert.IsEmpty(constructors[0].GetParameters());
    }

    [TestMethod]
    public void InvalidOptionalCollectionsProducePartialReportWithoutRawFailureDetails()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        session.Actions = null!;
        session.Checkpoints = null!;
        session.StartupActionReceipts = null!;
        session.StartupVerificationResults = null!;

        ForgeServiceReportModel report = new ForgeServiceReportBuilder().Build(session);

        Assert.IsEmpty(report.Actions);
        Assert.IsEmpty(report.Activities);
        Assert.IsEmpty(report.Checkpoints);
        Assert.IsGreaterThanOrEqualTo(4, report.Warnings.Count);
        Assert.IsFalse(report.Warnings.Any(value => value.Contains("Exception", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void InvalidTypedRecordsAreSkippedWhileValidSectionsRemain()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        session.Actions.Add(new ForgeReportAction { Category = "SYSTEM", Timestamp = DateTime.Now });
        StartupActionReceipt invalid = ForgeServiceReportTestFactory.Receipt(
            session.SessionId,
            outcome: (StartupExecutionOutcome)999);
        session.StartupActionReceipts.Add(invalid);

        ForgeServiceReportModel report = new ForgeServiceReportBuilder().Build(session);

        Assert.HasCount(1, report.Activities);
        Assert.IsEmpty(report.Actions);
        CollectionAssert.Contains(report.Warnings.ToArray(), "Some typed startup action data could not be included.");
    }

    [TestMethod]
    public void ActionProjectionHasNoEvidenceOrAttentionLinkageSurface()
    {
        string[] propertyNames = typeof(ServiceReportAction)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        CollectionAssert.DoesNotContain(propertyNames, "EvidenceIds");
        CollectionAssert.DoesNotContain(propertyNames, "AttentionId");
        CollectionAssert.DoesNotContain(propertyNames, "CorrelationKey");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ForgeCare.slnx")))
                return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
