using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ProcessCpuSampleCalculatorTests
{
    private static readonly DateTime Start = new(2026, 8, 22, 10, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void MatchingPidAndStartTimeCalculatesNormalizedCpuDelta()
    {
        var baseline = new ProcessCpuSample(42, TimeSpan.FromMilliseconds(1000), Start);
        var current = new ProcessCpuSample(42, TimeSpan.FromMilliseconds(1400), Start);

        double result = ProcessCpuSampleCalculator.CalculatePercent(baseline, current, 1000, 4);

        Assert.AreEqual(10d, result);
    }

    [TestMethod]
    public void MissingBaselineDoesNotUseLifetimeCpuAgainstZero()
    {
        var current = new ProcessCpuSample(42, TimeSpan.FromHours(2), Start);
        Assert.AreEqual(0d, ProcessCpuSampleCalculator.CalculatePercent(null, current, 900, 8));
    }

    [TestMethod]
    public void ReusedPidWithDifferentStartTimeRejectsOldBaseline()
    {
        var baseline = new ProcessCpuSample(42, TimeSpan.FromHours(1), Start);
        var current = new ProcessCpuSample(42, TimeSpan.FromMilliseconds(100), Start.AddMinutes(1));
        Assert.AreEqual(0d, ProcessCpuSampleCalculator.CalculatePercent(baseline, current, 900, 8));
    }

    [TestMethod]
    public void UnavailableStartTimeUsesConservativeZeroCpuPolicy()
    {
        var noBaselineStart = new ProcessCpuSample(42, TimeSpan.FromMilliseconds(100), null);
        var noCurrentStart = new ProcessCpuSample(42, TimeSpan.FromMilliseconds(200), null);
        var knownBaseline = new ProcessCpuSample(42, TimeSpan.FromMilliseconds(100), Start);

        Assert.AreEqual(0d, ProcessCpuSampleCalculator.CalculatePercent(noBaselineStart, noCurrentStart, 900, 8));
        Assert.AreEqual(0d, ProcessCpuSampleCalculator.CalculatePercent(knownBaseline, noCurrentStart, 900, 8));
    }

    [TestMethod]
    public void ValidCpuIsClampedAndSupportsExistingOneDecimalRounding()
    {
        var baseline = new ProcessCpuSample(1, TimeSpan.Zero, Start);
        var current = new ProcessCpuSample(1, TimeSpan.FromSeconds(10), Start);
        double clamped = ProcessCpuSampleCalculator.CalculatePercent(baseline, current, 900, 1);

        Assert.AreEqual(100d, clamped);

        current = new ProcessCpuSample(1, TimeSpan.FromMilliseconds(111), Start);
        double rounded = Math.Round(ProcessCpuSampleCalculator.CalculatePercent(baseline, current, 900, 1), 1);
        Assert.AreEqual(12.3d, rounded);
    }
}
