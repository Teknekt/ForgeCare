using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class DeepAnalysisEvidenceAdapterTests
{
    [TestMethod]
    public void CollectProducesGlobalAndSelectedProcessEvidence()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        ResourceAnalysisResult analysis = CreateAnalysis();
        analysis.TopProcesses.AddRange(
            new[]
            {
                new ResourceProcessInfo
                {
                    ProcessId = 1234,
                    Name = "Chrome Helper",
                    CpuPercent = 18.4,
                    MemoryMb = 634,
                    PressureScore = 72,
                    PressureLevel = "HIGH",
                    PrimaryResource = "CPU"
                },
                new ResourceProcessInfo
                {
                    ProcessId = 42,
                    Name = "Memory.App",
                    CpuPercent = 1.5,
                    MemoryMb = 2048,
                    PressureScore = 58,
                    PressureLevel = "MODERATE",
                    PrimaryResource = "MEMORY"
                }
            });

        EvidenceCollectionResult result =
            new DeepAnalysisEvidenceAdapter().Collect(analysis, sessionId);

        Assert.IsTrue(result.Success);
        Assert.HasCount(6, result.Evidence);
        Assert.IsTrue(result.Evidence.All(record => record.SessionId == sessionId));
        Assert.IsTrue(result.Evidence.All(record => record.Source == EvidenceSource.DeepAnalysis));
        Assert.IsTrue(result.Evidence.All(record => record.TimestampUtc.Kind == DateTimeKind.Utc));
        Assert.IsTrue(result.Evidence.All(record => record.Confidence == EvidenceConfidence.High));

        AssertRecord(result, "cpu-pressure", EvidenceCategory.Cpu, 18.4, "%", "cpu:pressure", EvidenceSeverity.Medium);
        AssertRecord(result, "memory-pressure", EvidenceCategory.Memory, 31.4, "%", "memory:pressure", EvidenceSeverity.Informational);
        AssertRecord(result, "process-count", EvidenceCategory.Process, 247, "processes", "process:count", EvidenceSeverity.Informational);
        AssertRecord(result, "overall-resource-pressure", EvidenceCategory.System, null, null, "system:resource-pressure", EvidenceSeverity.Low);

        EvidenceRecord chrome = result.Evidence.Single(record => record.CorrelationKey == "process:chrome-helper:1234");
        Assert.AreEqual("process:chrome-helper", chrome.Subject);
        Assert.AreEqual(18.4, chrome.Value);
        Assert.AreEqual("%", chrome.Unit);
        Assert.AreEqual(EvidenceSeverity.High, chrome.Severity);
        Assert.AreEqual("634.0", chrome.Metadata["memoryMb"]);
        Assert.IsLessThanOrEqualTo(EvidenceRecord.MaxMetadataEntries, chrome.Metadata.Count);

        EvidenceRecord memory = result.Evidence.Single(record => record.CorrelationKey == "process:memory-app:42");
        Assert.AreEqual(2048, memory.Value);
        Assert.AreEqual("MB", memory.Unit);
        Assert.AreEqual(EvidenceSeverity.Medium, memory.Severity);

        string observations = string.Join(" ", result.Evidence.Select(record => record.Observation));
        Assert.IsFalse(observations.Contains("recommend", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(observations.Contains("should", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(observations.Contains("caused", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(observations.Contains("terminate", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(observations.Contains("disable", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void EmptyTopProcessesStillProducesGlobalEvidence()
    {
        ResourceAnalysisResult analysis = CreateAnalysis();

        EvidenceCollectionResult result = new DeepAnalysisEvidenceAdapter().Collect(
            analysis,
            Guid.NewGuid().ToString("N"));

        Assert.IsTrue(result.Success);
        Assert.HasCount(4, result.Evidence);
        Assert.IsFalse(result.Evidence.Any(record => record.Subject.StartsWith("process:") && record.Subject != "process-count"));
    }

    [TestMethod]
    public void MissingOverallPressureIsOmittedWithWarning()
    {
        ResourceAnalysisResult analysis = CreateAnalysis();
        analysis.OverallPressure = string.Empty;

        EvidenceCollectionResult result = new DeepAnalysisEvidenceAdapter().Collect(
            analysis,
            Guid.NewGuid().ToString("N"));

        Assert.IsTrue(result.Success);
        Assert.HasCount(3, result.Evidence);
        Assert.HasCount(1, result.Warnings);
        Assert.IsFalse(result.Evidence.Any(record => record.Subject == "overall-resource-pressure"));
    }

    [TestMethod]
    public void MalformedProcessDoesNotDiscardGlobalOrOtherProcessEvidence()
    {
        ResourceAnalysisResult analysis = CreateAnalysis();
        analysis.TopProcesses.Add(
            new ResourceProcessInfo
            {
                ProcessId = -1,
                Name = "Invalid process"
            });
        analysis.TopProcesses.Add(
            new ResourceProcessInfo
            {
                ProcessId = 77,
                Name = "Valid Process",
                CpuPercent = 2,
                MemoryMb = 100,
                PressureScore = 10,
                PressureLevel = "MINIMAL",
                PrimaryResource = "BALANCED"
            });

        EvidenceCollectionResult result = new DeepAnalysisEvidenceAdapter().Collect(
            analysis,
            Guid.NewGuid().ToString("N"));

        Assert.IsTrue(result.PartialSuccess);
        Assert.HasCount(5, result.Evidence);
        Assert.HasCount(1, result.Errors);
        Assert.IsTrue(result.Evidence.Any(record => record.CorrelationKey == "process:valid-process:77"));
    }

    [TestMethod]
    [DataRow("NORMAL", EvidenceSeverity.Informational)]
    [DataRow("MINIMAL", EvidenceSeverity.Informational)]
    [DataRow("LOW", EvidenceSeverity.Low)]
    [DataRow("MODERATE", EvidenceSeverity.Medium)]
    [DataRow("ELEVATED", EvidenceSeverity.Medium)]
    [DataRow("HIGH", EvidenceSeverity.High)]
    [DataRow("CRITICAL", EvidenceSeverity.Critical)]
    [DataRow("unsupported", EvidenceSeverity.Unknown)]
    [DataRow(null, EvidenceSeverity.Unknown)]
    public void SeverityMappingUsesExistingAnalyzerStatesOnly(
        string? status,
        EvidenceSeverity expected)
    {
        Assert.AreEqual(expected, DeepAnalysisEvidenceAdapter.MapSeverity(status));
    }

    private static ResourceAnalysisResult CreateAnalysis()
    {
        return new ResourceAnalysisResult
        {
            CpuUsagePercent = 18.4,
            MemoryUsedPercent = 31.4,
            UsedMemoryGb = 20.1,
            AvailableMemoryGb = 43.9,
            TotalMemoryGb = 64,
            ProcessCount = 247,
            CpuStatus = "MODERATE",
            MemoryStatus = "NORMAL",
            ProcessStatus = "NORMAL",
            OverallPressure = "LOW",
            AnalysisTime = new DateTime(2026, 8, 19, 15, 0, 0, DateTimeKind.Local)
        };
    }

    private static void AssertRecord(
        EvidenceCollectionResult result,
        string subject,
        EvidenceCategory category,
        double? value,
        string? unit,
        string correlationKey,
        EvidenceSeverity severity)
    {
        EvidenceRecord record = result.Evidence.Single(item => item.Subject == subject);
        Assert.AreEqual(category, record.Category);
        Assert.AreEqual(value, record.Value);
        Assert.AreEqual(unit, record.Unit);
        Assert.AreEqual(correlationKey, record.CorrelationKey);
        Assert.AreEqual(severity, record.Severity);
    }
}
