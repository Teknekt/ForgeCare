using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class EvidenceDisplayFormatterTests
{
    [TestMethod]
    [DataRow("cpu-pressure", "CPU PRESSURE")]
    [DataRow("memory-pressure", "MEMORY PRESSURE")]
    [DataRow("process-count", "PROCESS COUNT")]
    [DataRow("overall-resource-pressure", "OVERALL RESOURCE PRESSURE")]
    [DataRow("operating-system", "OPERATING SYSTEM")]
    [DataRow("processor", "PROCESSOR")]
    [DataRow("physical-memory", "PHYSICAL MEMORY")]
    [DataRow("available-memory", "AVAILABLE MEMORY")]
    [DataRow("system-drive", "SYSTEM DRIVE")]
    [DataRow("startup-entries", "STARTUP ENTRIES")]
    [DataRow("process:chrome-helper", "PROCESS: CHROME HELPER")]
    [DataRow("future_gpuMetric", "FUTURE GPU METRIC")]
    public void SubjectsAreReadable(string input, string expected)
    {
        Assert.AreEqual(expected, EvidenceDisplayFormatter.FormatSubject(input));
    }

    [TestMethod]
    public void AcronymsAndGenericWordBoundariesArePreserved()
    {
        Assert.AreEqual("CPU Process ID", EvidenceDisplayFormatter.FormatMetadataKey("cpuProcessId"));
        Assert.AreEqual("OS PID", EvidenceDisplayFormatter.FormatMetadataKey("os_pid"));
        Assert.AreEqual("Memory GB", EvidenceDisplayFormatter.FormatMetadataKey("memoryGb"));
        Assert.AreEqual("Working Set MB", EvidenceDisplayFormatter.FormatMetadataKey("WorkingSetMB"));
    }

    [TestMethod]
    public void CategoriesSourcesAndAssessmentsAreFormatted()
    {
        Assert.AreEqual("Operating System", EvidenceDisplayFormatter.FormatCategory(EvidenceCategory.OperatingSystem));
        Assert.AreEqual("CPU", EvidenceDisplayFormatter.FormatCategory(EvidenceCategory.Cpu));
        Assert.AreEqual("System Scan", EvidenceDisplayFormatter.FormatSource(EvidenceSource.SystemScan));
        Assert.AreEqual("Deep Analysis", EvidenceDisplayFormatter.FormatSource(EvidenceSource.DeepAnalysis));
        Assert.AreEqual("INFORMATIONAL", EvidenceDisplayFormatter.FormatSeverity(EvidenceSeverity.Informational));
        Assert.AreEqual("HIGH", EvidenceDisplayFormatter.FormatConfidence(EvidenceConfidence.High));
    }

    [TestMethod]
    [DataRow(18.4, "%", "18.4 %")]
    [DataRow(51.0, "GB", "51 GB")]
    [DataRow(634.0, "MB", "634 MB")]
    [DataRow(247.0, "processes", "247 processes")]
    [DataRow(12.345, "score", "12.35 score")]
    [DataRow(4.0, null, "4")]
    public void ValuesUseStructuredNumericFormatting(double value, string? unit, string expected)
    {
        Assert.AreEqual(expected, EvidenceDisplayFormatter.FormatValue(value, unit));
    }

    [TestMethod]
    public void NullValueAndUtcTimestampAreExplicit()
    {
        Assert.IsNull(EvidenceDisplayFormatter.FormatValue(null, "GB"));

        var timestamp = new DateTime(2026, 8, 19, 13, 45, 59, DateTimeKind.Utc);
        Assert.AreEqual("2026-08-19 13:45 UTC", EvidenceDisplayFormatter.FormatTimestamp(timestamp));
    }
}
