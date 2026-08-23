using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ForgeServiceReportExportIntegrationTests
{
    [TestMethod]
    public async Task ProfessionalHtmlUsesExistingWriterAndProfessionalHistoryKind()
    {
        using var temp = new TemporaryDirectory();
        var service = new ForgeReportService(temp.Path);
        string sessionId = service.Snapshot().SessionId;
        string requestedPath = Path.Combine(temp.Path, "professional-report");

        await service.ExportProfessionalHtmlAsync(
            requestedPath,
            "<!doctype html><html><body>Professional</body></html>",
            sessionId);

        string expectedPath = requestedPath + ".html";
        Assert.IsTrue(File.Exists(expectedPath));
        StringAssert.Contains(await File.ReadAllTextAsync(expectedPath), "Professional");
        ForgeReportArchiveEntry entry = service.GetArchive().Single();
        Assert.AreEqual("Professional", entry.ReportKind);
        Assert.AreEqual("PROFESSIONAL", entry.DisplayReportKind);
        Assert.AreEqual(expectedPath, entry.FilePath);
        Assert.AreEqual(sessionId, service.Snapshot().SessionId);
    }

    [TestMethod]
    public async Task LegacyExportRemainsAvailableAndIsDistinguishedInHistory()
    {
        using var temp = new TemporaryDirectory();
        var service = new ForgeReportService(temp.Path);
        string requestedPath = Path.Combine(temp.Path, "legacy-report");

        await service.ExportHtmlAsync(requestedPath);

        ForgeReportArchiveEntry entry = service.GetArchive().Single();
        Assert.AreEqual("Legacy", entry.ReportKind);
        Assert.AreEqual("LEGACY", entry.DisplayReportKind);
        Assert.IsTrue(File.Exists(requestedPath + ".html"));
    }

    [TestMethod]
    public async Task SessionChangeRejectsProfessionalArtifactWithoutCorruptingSessionOrHistory()
    {
        using var temp = new TemporaryDirectory();
        var service = new ForgeReportService(temp.Path);
        string oldSessionId = service.Snapshot().SessionId;
        string currentSessionId = service.StartNewSession().SessionId;
        string path = Path.Combine(temp.Path, "must-not-exist.html");

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.ExportProfessionalHtmlAsync(path, "<html></html>", oldSessionId));

        Assert.IsFalse(File.Exists(path));
        Assert.IsEmpty(service.GetArchive());
        Assert.AreEqual(currentSessionId, service.Snapshot().SessionId);
    }

    [TestMethod]
    public async Task WriteFailureDoesNotAddHistoryOrChangeSession()
    {
        using var temp = new TemporaryDirectory();
        var service = new ForgeReportService(temp.Path);
        string sessionId = service.Snapshot().SessionId;
        string directoryAsPath = Path.Combine(temp.Path, "occupied.html");
        Directory.CreateDirectory(directoryAsPath);

        Exception? failure = null;
        try
        {
            await service.ExportProfessionalHtmlAsync(directoryAsPath, "<html></html>", sessionId);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        Assert.IsNotNull(failure);
        Assert.IsEmpty(service.GetArchive());
        Assert.AreEqual(sessionId, service.Snapshot().SessionId);
    }

    [TestMethod]
    public void HistoricalArchiveWithoutKindReloadsAsLegacyPresentation()
    {
        using var temp = new TemporaryDirectory();
        string archivePath = Path.Combine(temp.Path, "report-history.json");
        File.WriteAllText(
            archivePath,
            """
            [
              {
                "ExportedAt": "2026-08-23T12:00:00",
                "JobId": "LEGACY-1",
                "FilePath": "legacy.html"
              }
            ]
            """);

        ForgeReportArchiveEntry entry = new ForgeReportService(temp.Path).GetArchive().Single();

        Assert.AreEqual("Legacy", entry.ReportKind);
        Assert.AreEqual("LEGACY", entry.DisplayReportKind);
    }
}
