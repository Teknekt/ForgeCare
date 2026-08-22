using System.Collections.Concurrent;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

internal static class ProcessIntelligenceTestFactory
{
    public static ProcessInstanceObservation Observation(
        int id = 1,
        string name = "app",
        string? path = @"C:\Apps\app.exe",
        double cpu = 10,
        double memory = 100,
        double pressure = 20,
        string pressureLevel = "LOW",
        DateTime? startTimeUtc = null) =>
        new(id, name, startTimeUtc, path, cpu, memory, 1, pressure, pressureLevel, "CPU");

    public static ProcessExecutableInspection Inspection(
        string path,
        ProcessSignatureStatus signature = ProcessSignatureStatus.Valid,
        ProcessFileInspectionStatus fileStatus = ProcessFileInspectionStatus.Available,
        bool? exists = true,
        string? company = "Vendor",
        string? signer = "Signer",
        IReadOnlyList<string>? warnings = null) =>
        new(path, fileStatus, exists, Path.GetFileName(path), Path.GetExtension(path), "Description",
            "Product", company, "1.0", "1.0", Path.GetFileName(path), signature, signer,
            warnings ?? Array.Empty<string>());
}

internal sealed class FakeProcessExecutableInspector : IProcessExecutableInspector
{
    private readonly ConcurrentDictionary<string, int> _calls = new(StringComparer.OrdinalIgnoreCase);

    public Func<string, ProcessExecutableInspection>? Handler { get; init; }
    public Func<string, Exception?>? ExceptionFactory { get; init; }
    public IReadOnlyDictionary<string, int> Calls => _calls;
    public int TotalCalls => _calls.Values.Sum();
    public int MaximumActive { get; private set; }
    private int _active;

    public async Task<ProcessExecutableInspection> InspectAsync(
        string canonicalExecutablePath,
        CancellationToken cancellationToken = default)
    {
        _calls.AddOrUpdate(canonicalExecutablePath, 1, (_, value) => value + 1);
        int active = Interlocked.Increment(ref _active);
        MaximumActive = Math.Max(MaximumActive, active);
        try
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            Exception? exception = ExceptionFactory?.Invoke(canonicalExecutablePath);
            if (exception != null) throw exception;
            return Handler?.Invoke(canonicalExecutablePath) ??
                   ProcessIntelligenceTestFactory.Inspection(canonicalExecutablePath);
        }
        finally
        {
            Interlocked.Decrement(ref _active);
        }
    }
}
