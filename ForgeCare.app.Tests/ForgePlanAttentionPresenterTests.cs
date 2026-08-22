using System.Reflection;
using ForgeCare.App.Models;
using ForgeCare.App.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForgeCare.app.Tests;

[TestClass]
public sealed class ForgePlanAttentionPresenterTests
{
    [TestMethod]
    public void ProjectionPreservesIdentityTraceabilityAndFormatsDisplayFields()
    {
        EvidenceRecord startup = EvidenceCorrelationTestFactory.Startup();
        EvidenceRecord process = EvidenceCorrelationTestFactory.Process();
        TechnicianAttentionItem attention = new EvidenceCorrelationEngine()
            .Correlate([startup, process], EvidenceCorrelationTestFactory.SessionId).Items.Single();

        ForgePlanAttentionItem item = new ForgePlanAttentionPresenter()
            .Present([attention], [startup, process]).Single();

        Assert.AreEqual("plan-" + attention.Id, item.Id);
        Assert.AreEqual(attention.Id, item.AttentionId);
        Assert.AreEqual(attention.RuleId, item.RuleId);
        Assert.AreEqual("Application", item.CategoryDisplay);
        Assert.AreEqual("Medium", item.PriorityDisplay);
        Assert.AreEqual("HIGH", item.ConfidenceDisplay);
        Assert.AreEqual("2026-08-23 10:01 UTC", item.TimestampDisplay);
        Assert.AreEqual(2, item.SupportingEvidenceCount);
        Assert.IsTrue(item.HasMultipleEvidenceSources);
        CollectionAssert.AreEqual(
            new[] { "Process Intelligence", "Startup Intelligence" },
            item.SourceDisplays.ToArray());
        CollectionAssert.AreEqual(attention.EvidenceIds.ToArray(), item.EvidenceIds.ToArray());
    }

    [TestMethod]
    public void ProjectionCollectionsAreImmutableAndInputsRemainUnchanged()
    {
        TechnicianAttentionItem attention = EvidenceCorrelationTestFactory.Attention();
        ForgePlanAttentionItem item = new ForgePlanAttentionPresenter().Present([attention]).Single();

        Assert.ThrowsExactly<NotSupportedException>(() =>
            ((IList<Guid>)item.EvidenceIds).Add(Guid.NewGuid()));
        Assert.ThrowsExactly<NotSupportedException>(() =>
            ((IList<string>)item.SourceDisplays).Add("Other"));
        Assert.HasCount(1, attention.EvidenceIds);
        Assert.IsEmpty(item.SourceDisplays);
        Assert.AreEqual("1 supporting Evidence record", item.SupportingEvidenceSummary);
        Assert.IsFalse(item.HasMultipleEvidenceSources);
    }

    [TestMethod]
    public void PresenterOrderingIsDeterministicAndIgnoresCallerOrder()
    {
        TechnicianAttentionItem low = EvidenceCorrelationTestFactory.Attention(
            "attention:0000000000000001", TechnicianAttentionPriority.Low,
            EvidenceCorrelationTestFactory.Time.AddMinutes(5), entityKey: "app:0000000000000001");
        TechnicianAttentionItem mediumOlder = EvidenceCorrelationTestFactory.Attention(
            "attention:0000000000000002", TechnicianAttentionPriority.Medium,
            EvidenceCorrelationTestFactory.Time, entityKey: "app:0000000000000002",
            evidenceId: Guid.Parse("00000000-0000-0000-0000-000000000002"));
        TechnicianAttentionItem mediumNewer = EvidenceCorrelationTestFactory.Attention(
            "attention:0000000000000003", TechnicianAttentionPriority.Medium,
            EvidenceCorrelationTestFactory.Time.AddMinutes(1), entityKey: "app:0000000000000003",
            evidenceId: Guid.Parse("00000000-0000-0000-0000-000000000003"));
        var presenter = new ForgePlanAttentionPresenter();

        string[] first = presenter.Present([low, mediumOlder, mediumNewer]).Select(item => item.AttentionId).ToArray();
        string[] second = presenter.Present([mediumNewer, low, mediumOlder]).Select(item => item.AttentionId).ToArray();

        CollectionAssert.AreEqual(first, second);
        CollectionAssert.AreEqual(
            new[] { mediumNewer.Id, mediumOlder.Id, low.Id }, first);
    }

    [TestMethod]
    public void PresentationDomainHasNoExecutionOrRoutingFields()
    {
        string[] properties = typeof(ForgePlanAttentionItem)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToArray();
        foreach (string forbidden in new[]
                 { "CanExecute", "Route", "IsSelected", "ActionLabel", "Risk", "Reversibility", "Command" })
            CollectionAssert.DoesNotContain(properties, forbidden);
    }
}
