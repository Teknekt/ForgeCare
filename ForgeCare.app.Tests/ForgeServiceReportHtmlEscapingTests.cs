using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ForgeServiceReportHtmlEscapingTests
{
    [TestMethod]
    public void EveryDynamicReportSectionHtmlEncodesAdversarialText()
    {
        const string script = "<script>alert('x')</script>";
        const string image = "<img src=x onerror=alert(1)>";
        ServiceReportSessionSummary summary = new(
            script,
            DateTime.Today,
            DateTime.Today,
            null,
            1,
            0,
            1,
            1);
        ServiceReportActivity activity = new(
            DateTime.Today,
            ServiceReportActivityKind.LegacyActivity,
            "A&B",
            image,
            false);
        ServiceReportAction action = new(
            Guid.NewGuid().ToString("N"),
            ForgeServiceReportTestFactory.Hash("target"),
            StartupVerificationOperation.Disable,
            ForgeServiceReportTestFactory.ExecutedAt,
            script,
            StartupVerificationTargetKind.HkcuRun,
            image,
            ServiceReportExecutionStatus.Executed,
            "\"quoted\"",
            "<TEST>",
            true,
            false,
            null,
            null,
            ServiceReportVerificationStatus.Verified,
            ServiceReportTraceabilityStatus.ExecutedAndVerified,
            [new ServiceReportVerification(
                ServiceReportVerificationStatus.Verified,
                ForgeServiceReportTestFactory.ExecutedAt,
                ForgeServiceReportTestFactory.ExecutedAt,
                "'quoted'",
                image)]);
        ServiceReportEvidenceReference evidence = new(
            Guid.NewGuid(),
            script,
            ForgeServiceReportTestFactory.ExecutedAt,
            EvidenceCategory.Other,
            image,
            EvidenceSource.Manual,
            script,
            "A&B",
            EvidenceSeverity.Unknown,
            "<TEST>",
            EvidenceConfidence.Unknown,
            "\"quoted\"");
        ServiceReportCheckpoint checkpoint = new(
            DateTime.Today, 50, image, 1, 2, 3, 4, 5);
        ServiceReportUnresolvedItem unresolved = new(
            ServiceReportUnresolvedKind.IncompleteTraceability,
            DateTime.Today,
            script,
            null,
            null);
        ForgeServiceReportModel model = new(
            summary,
            [activity],
            [action],
            [evidence],
            [checkpoint],
            [unresolved],
            [image]);

        string html = new ForgeServiceReportHtmlRenderer().Render(model);

        Assert.IsFalse(html.Contains(script, StringComparison.Ordinal));
        Assert.IsFalse(html.Contains(image, StringComparison.Ordinal));
        StringAssert.Contains(html, "&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt;");
        StringAssert.Contains(html, "&lt;img src=x onerror=alert(1)&gt;");
        StringAssert.Contains(html, "A&amp;B");
        StringAssert.Contains(html, "&lt;TEST&gt;");
        StringAssert.Contains(html, "&quot;quoted&quot;");
        StringAssert.Contains(html, "&#39;quoted&#39;");
    }

    [TestMethod]
    public void DynamicValuesAreNeverInsertedIntoStyleScriptUrlOrRendererOwnedAttributes()
    {
        const string payload = "javascript:alert(1);background:url(https://example.invalid/x)";
        ForgeServiceReportModel model = HtmlReportTestFactory.Model(warnings: [payload]);

        string html = new ForgeServiceReportHtmlRenderer().Render(model);

        StringAssert.Contains(html, payload);
        Assert.AreEqual(1, Count(html, payload));
        int payloadIndex = html.IndexOf(payload, StringComparison.Ordinal);
        int listItemStart = html.LastIndexOf("<li>", payloadIndex, StringComparison.Ordinal);
        int listItemEnd = html.IndexOf("</li>", payloadIndex, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, listItemStart);
        Assert.IsGreaterThan(payloadIndex, listItemEnd);
    }

    private static int Count(string source, string value) =>
        source.Split(value, StringSplitOptions.None).Length - 1;
}
