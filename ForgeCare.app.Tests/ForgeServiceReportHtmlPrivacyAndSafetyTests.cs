using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ForgeServiceReportHtmlPrivacyAndSafetyTests
{
    [TestMethod]
    public void PhaseBPrivacyProjectionRemainsPrivateWhenRendered()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        session.Actions.Add(new ForgeReportAction
        {
            Category = "STARTUP",
            Title = @"C:\Users\Alice\agent.exe --token=SUPER_SECRET_VALUE",
            Detail = "API_KEY_TEST_VALUE raw startup command raw registry value disabled-storage path"
        });
        StartupVerificationTargetIdentity target = new(
            StartupVerificationTargetKind.HkcuRun,
            @"C:\Users\Alice\agent.exe --token=SUPER_SECRET_VALUE API_KEY_TEST_VALUE",
            "hkcu-run",
            ForgeServiceReportTestFactory.Hash("raw registry value"),
            ForgeServiceReportTestFactory.Hash("target"),
            ForgeServiceReportTestFactory.Hash("configuration fingerprint source material"));
        StartupActionReceipt receipt = ForgeServiceReportTestFactory.Receipt(session.SessionId, target: target);
        session.StartupActionReceipts.Add(receipt);
        EvidenceRecord evidence = ForgeServiceReportTestFactory.Evidence(session.SessionId);
        evidence.Observation = "raw startup command --token=SUPER_SECRET_VALUE";
        evidence.Metadata["path"] = @"C:\Users\Alice\disabled-storage\agent.lnk";

        ForgeServiceReportModel projection = new ForgeServiceReportBuilder().Build(session, [evidence]);
        string html = new ForgeServiceReportHtmlRenderer().Render(projection);

        foreach (string forbidden in new[]
        {
            @"C:\Users\Alice", "Alice", "--token", "SUPER_SECRET_VALUE", "API_KEY_TEST_VALUE",
            "raw startup command", "raw registry value", "disabled-storage path",
            "configuration fingerprint source material", target.ConfigurationFingerprint!, target.LocatorId
        })
            Assert.IsFalse(html.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
    }

    [TestMethod]
    public void RendererDoesNotExposeFullInternalActionOrEvidenceIdentifiers()
    {
        ServiceReportAction action = HtmlReportTestFactory.Action("Agent", 42);
        ServiceReportEvidenceReference evidence = HtmlReportTestFactory.EvidenceReference(42);
        string html = new ForgeServiceReportHtmlRenderer().Render(
            HtmlReportTestFactory.Model(actions: [action], evidenceReferences: [evidence]));

        Assert.IsFalse(html.Contains(action.ReceiptId, StringComparison.Ordinal));
        Assert.IsFalse(html.Contains(action.TargetId, StringComparison.Ordinal));
        Assert.IsFalse(html.Contains(evidence.EvidenceId.ToString(), StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(html, action.ReceiptId[..10]);
        StringAssert.Contains(html, evidence.Reference);
    }

    [TestMethod]
    public void RendererContainsNoForbiddenCausalPerformanceOrSafetyClaims()
    {
        string html = new ForgeServiceReportHtmlRenderer().Render(HtmlReportTestFactory.PopulatedModel());

        foreach (string forbidden in new[]
        {
            "improved performance", "faster startup", "reduced resource usage", "fixed the issue",
            "optimized the system", "made the application safe", "unnecessary startup item",
            "caused system pressure", "rollback guaranteed", "fully reversible"
        })
            Assert.IsFalse(html.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
    }

    [TestMethod]
    public void RendererIsPureTextProjectionWithoutRepositoryMutationOrShellDependencies()
    {
        string root = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            root,
            "ForgeCare.app",
            "Services",
            "ForgeServiceReportHtmlRenderer.cs"));
        string[] forbidden =
        [
            "ForgeReportService", "JsonEvidenceRepository", "EvidenceService", "EvidenceCorrelationEngine",
            "StartupManagerService", "SystemScanner", "StartupScanner", "MainWindow", "System.Windows",
            "Registry", "Process.Start", "Process.Kill", "File.Write", "File.Move", "File.Delete",
            "HttpClient", "WebClient", "BuildHtml", "ExportHtml"
        ];

        foreach (string token in forbidden)
            Assert.IsFalse(source.Contains(token, StringComparison.Ordinal), token);
    }

    [TestMethod]
    public void ReportIsPrintableAccessibleAndIndependentOfRemoteAssets()
    {
        string html = new ForgeServiceReportHtmlRenderer().Render(HtmlReportTestFactory.PopulatedModel());

        StringAssert.Contains(html, "@media print");
        StringAssert.Contains(html, "<header>");
        StringAssert.Contains(html, "<main>");
        StringAssert.Contains(html, "<section");
        StringAssert.Contains(html, "<h1>");
        StringAssert.Contains(html, "<h2>");
        StringAssert.Contains(html, "<table>");
        StringAssert.Contains(html, "scope=\"col\"");
        StringAssert.Contains(html, "Verified");
        Assert.IsFalse(html.Contains("cdn", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("@import", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
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
