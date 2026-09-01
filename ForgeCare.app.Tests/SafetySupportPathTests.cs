using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class SafetySupportPathTests
{
    [TestMethod]
    public void SafetyJournalExposesExistingAuthoritativeRoot()
    {
        string expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Mindforge Studio",
            "ForgeCare",
            "Safety");

        Assert.AreEqual(expected, SafetyJournalService.SafetyRoot);
    }

    [TestMethod]
    public void RegressionSuiteChecksInjectedAuthoritativeSafetyRoot()
    {
        using var temp = new TemporaryDirectory();
        string safetyRoot = Path.Combine(temp.Path, "ExistingSafetyRoot");
        var service = new RegressionSuiteService(safetyRoot: safetyRoot);

        RegressionSuiteResult result = service.Run();

        RegressionCheckResult check = result.Checks.Single(item =>
            item.Area == "Safety" && item.Check == "Safety directory");
        Assert.AreEqual("PASS", check.Status);
        Assert.AreEqual(safetyRoot, check.Detail);
        Assert.IsTrue(Directory.Exists(safetyRoot));
        Assert.AreEqual(
            "PASS",
            result.Checks.Single(item => item.Check == "Active beta identity").Status);
        Assert.AreEqual(
            "PASS",
            result.Checks.Single(item => item.Check == "Support source roots").Status);
        Assert.AreEqual(
            "PASS",
            result.Checks.Single(item => item.Check == "Professional report export root").Status);
    }
}
