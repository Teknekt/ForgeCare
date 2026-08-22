using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class BetaFieldTestEvidenceTests
{
    [TestMethod]
    public void NewFieldTestIncludesEvidencePersistenceRestartStep()
    {
        using var temp = new TemporaryDirectory();
        var service = new BetaFieldTestService(temp.Path);

        BetaFieldTestSession session = service.StartNew("Test technician");

        BetaFieldTestStep evidence = session.Steps.Single(step => step.Id == "evidence");
        StringAssert.Contains(evidence.Title, "Evidence");
        StringAssert.Contains(evidence.Detail, "System Scan");
        StringAssert.Contains(evidence.Detail, "Deep Analysis");
        StringAssert.Contains(evidence.Detail, "restart ForgeCare");
    }
}
