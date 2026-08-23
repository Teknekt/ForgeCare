namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ForgePlanAttentionLiveSafetyTests
{
    [TestMethod]
    public void LiveServiceIsReadOnlyAndHasNoExecutionSurface()
    {
        string source = Read("ForgeCare.app", "Services", "ForgePlanAttentionLiveService.cs");
        string[] forbidden =
        {
            "AddAsync", "AddRangeAsync", "EvidenceService", "CanExecute", "Route =",
            "Process.Start", "Process.Kill", "Registry", "ServiceController", "File.Move",
            "File.Delete", "SafetyJournalService", "StartupManagerService"
        };

        foreach (string value in forbidden)
            Assert.IsFalse(source.Contains(value, StringComparison.Ordinal), value);
    }

    [TestMethod]
    public void SupportingEvidenceHandlerOnlyLoadsSelectsAndNavigates()
    {
        string source = Read("ForgeCare.app", "MainWindow.xaml.cs");
        string helper = Slice(
            source,
            "private async void ViewSupportingEvidenceButton_Click(",
            "private void ForgePlanSelection_Click(");
        string[] forbidden =
        {
            "AddAsync", "AddRangeAsync", "EvidenceService", "CanExecute", "Route =",
            "Process.Start", "Process.Kill", "Registry", "ServiceController", "File.Move",
            "File.Delete", "SafetyJournalService"
        };

        StringAssert.Contains(helper, "SelectEvidence");
        StringAssert.Contains(helper, "SelectMainTab(\"EVIDENCE\")");
        foreach (string value in forbidden)
            Assert.IsFalse(helper.Contains(value, StringComparison.Ordinal), value);
    }

    private static string Slice(string source, string start, string end)
    {
        int startIndex = source.IndexOf(start, StringComparison.Ordinal);
        int endIndex = source.IndexOf(end, startIndex, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, startIndex);
        Assert.IsGreaterThan(startIndex, endIndex);
        return source[startIndex..endIndex];
    }

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { FindRepositoryRoot() }.Concat(parts).ToArray()));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ForgeCare.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
