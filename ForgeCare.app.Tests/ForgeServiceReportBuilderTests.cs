using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ForgeServiceReportBuilderTests
{
    private readonly ForgeServiceReportBuilder _builder = new();

    [TestMethod]
    public void EmptySessionBuildsWithoutFabricatingActionsOrVerification()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();

        ForgeServiceReportModel report = _builder.Build(session);

        Assert.IsEmpty(report.Activities);
        Assert.IsEmpty(report.Actions);
        Assert.IsEmpty(report.EvidenceReferences);
        Assert.IsEmpty(report.UnresolvedItems);
        Assert.AreEqual(session.SessionId, report.SessionSummary.SessionId);
    }

    [TestMethod]
    public void OldDiagnosticSessionRemainsDiagnosticAndHasNoTypedVerification()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        session.Actions.Add(new ForgeReportAction
        {
            Timestamp = DateTime.Now,
            Category = "SYSTEM",
            Title = "Initial system profile captured",
            IsSuccess = true
        });

        ForgeServiceReportModel report = _builder.Build(session);

        Assert.HasCount(1, report.Activities);
        Assert.AreEqual(ServiceReportActivityKind.DiagnosticActivity, report.Activities[0].Kind);
        Assert.AreEqual("System scan", report.Activities[0].Title);
        Assert.IsEmpty(report.Actions);
        Assert.IsTrue(report.Warnings.Any(value => value.Contains("legacy session", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void LegacyClassificationUsesOnlyStableCategoryContracts()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        string[] categories = ["SYSTEM", "ANALYSIS", "SERVICES", "STORAGE", "DUPLICATES", "CLEANUP", "STORAGE CLEANUP", "RECOVERY", "STARTUP", "mystery"];
        for (int index = 0; index < categories.Length; index++)
        {
            session.Actions.Add(new ForgeReportAction
            {
                Timestamp = DateTime.Today.AddMinutes(index),
                Category = categories[index],
                Title = "Do not infer from this arbitrary title",
                Detail = "--token=SUPER_SECRET_VALUE",
                IsSuccess = true
            });
        }

        ForgeServiceReportModel report = _builder.Build(session);

        Assert.AreEqual(5, report.Activities.Count(value => value.Kind == ServiceReportActivityKind.DiagnosticActivity));
        Assert.AreEqual(2, report.Activities.Count(value => value.Kind == ServiceReportActivityKind.SystemChange));
        Assert.AreEqual(1, report.Activities.Count(value => value.Kind == ServiceReportActivityKind.Recovery));
        Assert.AreEqual(2, report.Activities.Count(value => value.Kind == ServiceReportActivityKind.LegacyActivity));
        Assert.IsFalse(report.Activities.Any(value => value.Title.Contains("arbitrary", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CheckpointsAreContextOnlyAndDeterministicallyOrdered()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        session.Checkpoints.Add(new ForgeReportCheckpoint { Timestamp = DateTime.Today.AddHours(2), HealthScore = 90, HealthRating = "Healthy", StartupCount = 2 });
        session.Checkpoints.Add(new ForgeReportCheckpoint { Timestamp = DateTime.Today, HealthScore = 70, HealthRating = "Good", StartupCount = 4 });

        ForgeServiceReportModel report = _builder.Build(session);

        Assert.AreEqual(70, report.InitialCheckpoint!.HealthScore);
        Assert.AreEqual(90, report.LatestCheckpoint!.HealthScore);
        Assert.AreEqual(report.LatestCheckpoint.Timestamp, report.SessionSummary.LatestCheckpointAt);
        Assert.IsFalse(typeof(ServiceReportCheckpoint).GetProperties().Any(property => property.Name.Contains("Cause", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void EvidenceIsSummarizedWithoutObservationMetadataOrCorrelationData()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        EvidenceRecord later = ForgeServiceReportTestFactory.Evidence(session.SessionId, 2, "memory-pressure");
        EvidenceRecord earlier = ForgeServiceReportTestFactory.Evidence(session.SessionId, 1, "cpu-pressure");

        ForgeServiceReportModel report = _builder.Build(session, [later, earlier]);

        Assert.HasCount(2, report.EvidenceReferences);
        Assert.AreEqual(earlier.Id, report.EvidenceReferences[0].EvidenceId);
        Assert.AreEqual("CPU PRESSURE", report.EvidenceReferences[0].SubjectDisplay);
        Assert.AreEqual(earlier.Id.ToString("N")[..10], report.EvidenceReferences[0].Reference);
        Assert.IsFalse(typeof(ServiceReportEvidenceReference).GetProperties().Any(property =>
            property.Name is "Observation" or "Metadata" or "CorrelationKey"));
    }

    [TestMethod]
    public void InvalidAndWrongSessionEvidenceAreSkippedWithBoundedWarning()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        EvidenceRecord valid = ForgeServiceReportTestFactory.Evidence(session.SessionId);
        EvidenceRecord invalid = ForgeServiceReportTestFactory.Evidence(session.SessionId, 1);
        invalid.TimestampUtc = DateTime.Now;
        EvidenceRecord wrongSession = ForgeServiceReportTestFactory.Evidence(Guid.NewGuid().ToString("N"), 2);

        ForgeServiceReportModel report = _builder.Build(session, [invalid, valid, wrongSession]);

        Assert.HasCount(1, report.EvidenceReferences);
        CollectionAssert.Contains(report.Warnings.ToArray(), "Some Evidence records could not be included.");
    }

    [TestMethod]
    public void SourceMutationsAfterBuildCannotChangeProjection()
    {
        ForgeReportSession session = ForgeServiceReportTestFactory.Session();
        var action = new ForgeReportAction { Category = "SYSTEM", Title = "Original", Timestamp = DateTime.Now };
        var checkpoint = new ForgeReportCheckpoint { Timestamp = DateTime.Now, HealthScore = 75, HealthRating = "Good" };
        EvidenceRecord evidence = ForgeServiceReportTestFactory.Evidence(session.SessionId);
        session.Actions.Add(action);
        session.Checkpoints.Add(checkpoint);
        StartupActionReceipt receipt = ForgeServiceReportTestFactory.Receipt(session.SessionId);
        session.StartupActionReceipts.Add(receipt);

        ForgeServiceReportModel report = _builder.Build(session, [evidence]);
        action.Category = "CLEANUP";
        checkpoint.HealthScore = 1;
        evidence.Subject = "changed";
        evidence.Metadata["new"] = "value";
        session.Actions.Clear();
        session.Checkpoints.Clear();
        session.StartupActionReceipts.Clear();

        Assert.AreEqual(ServiceReportActivityKind.DiagnosticActivity, report.Activities[0].Kind);
        Assert.AreEqual(75, report.Checkpoints[0].HealthScore);
        Assert.AreEqual("CPU PRESSURE", report.EvidenceReferences[0].SubjectDisplay);
        Assert.HasCount(1, report.Actions);
        Assert.ThrowsExactly<NotSupportedException>(() =>
            ((IList<ServiceReportAction>)report.Actions).Add(report.Actions[0]));
    }
}
