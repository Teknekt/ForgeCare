using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class ProcessIntelligenceService
{
    public const int MaximumConcurrency = 4;
    public const int MaximumMessages = 32;

    private readonly IProcessExecutableInspector _inspector;
    private readonly ProcessClassificationPolicy _classificationPolicy;

    public ProcessIntelligenceService(
        IProcessExecutableInspector inspector,
        ProcessClassificationPolicy? classificationPolicy = null)
    {
        _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
        _classificationPolicy = classificationPolicy ?? new ProcessClassificationPolicy();
    }

    public async Task<ProcessIntelligenceResult> AnalyzeAsync(
        IEnumerable<ProcessInstanceObservation> observations,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observations);
        cancellationToken.ThrowIfCancellationRequested();

        ProcessInstanceObservation[] snapshot = observations.ToArray();
        var warnings = new List<string>();
        var errors = new List<string>();
        var candidates = new List<Candidate>(snapshot.Length);

        for (int index = 0; index < snapshot.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProcessInstanceObservation observation = snapshot[index] ??
                throw new ArgumentException("Process observations must not contain null entries.", nameof(observations));

            if (TryCanonicalize(observation.ExecutablePath, out string? canonicalPath))
            {
                candidates.Add(new Candidate(index, observation, canonicalPath, null));
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(observation.ExecutablePath))
                    AddBounded(warnings, "A process executable path was malformed; the observation remains provisional.");
                candidates.Add(new Candidate(index, observation, null, $"provisional:{index}:{observation.ProcessId}:{observation.StartTimeUtc?.Ticks ?? 0}"));
            }
        }

        string[] uniquePaths = candidates
            .Where(candidate => candidate.CanonicalPath != null)
            .Select(candidate => candidate.CanonicalPath!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var inspections = new Dictionary<string, ProcessExecutableInspection>(StringComparer.OrdinalIgnoreCase);
        using var gate = new SemaphoreSlim(MaximumConcurrency, MaximumConcurrency);
        object sync = new();

        Task[] inspectionTasks = uniquePaths.Select(async path =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessExecutableInspection inspection;
                try
                {
                    inspection = await _inspector.InspectAsync(path, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    inspection = FailureInspection(path);
                    lock (sync)
                        AddBounded(errors, $"Executable inspection failed ({ex.GetType().Name}).");
                }

                lock (sync)
                {
                    ProcessExecutableInspection sanitizedInspection = Sanitize(inspection);
                    inspections[path] = sanitizedInspection;
                    foreach (string warning in sanitizedInspection.Warnings)
                        AddBounded(warnings, warning);
                }
            }
            finally
            {
                gate.Release();
            }
        }).ToArray();

        await Task.WhenAll(inspectionTasks).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        ProcessIntelligenceEntry[] entries = candidates.Select(candidate =>
        {
            ProcessIdentityStrength strength = candidate.CanonicalPath == null
                ? ProcessIdentityStrength.Provisional
                : ProcessIdentityStrength.Strong;
            ProcessExecutableInspection? inspection = candidate.CanonicalPath == null
                ? null
                : inspections[candidate.CanonicalPath];
            ProcessClassificationDecision decision = _classificationPolicy.Classify(strength, inspection);

            return new ProcessIntelligenceEntry(
                candidate.Observation,
                strength,
                candidate.CanonicalPath ?? candidate.ProvisionalIdentity!,
                candidate.CanonicalPath,
                inspection,
                decision.Classification,
                decision.Confidence,
                decision.Rationale);
        }).ToArray();

        ProcessApplicationGroup[] groups = BuildGroups(entries);
        return new ProcessIntelligenceResult(
            ReadOnly(entries),
            ReadOnly(groups),
            ReadOnly(warnings.ToArray()),
            ReadOnly(errors.ToArray()));
    }

    private static ProcessApplicationGroup[] BuildGroups(ProcessIntelligenceEntry[] entries)
    {
        return entries
            .GroupBy(entry => entry.TransientIdentity, StringComparer.OrdinalIgnoreCase)
            .Select(group => CreateGroup(group.Key, group))
            .OrderByDescending(group => group.MaximumPressureScore)
            .ThenByDescending(group => group.TotalCpuPercent)
            .ThenByDescending(group => group.TotalMemoryMb)
            .ThenBy(group => group.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.TransientGroupIdentity, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ProcessApplicationGroup CreateGroup(
        string identity,
        IEnumerable<ProcessIntelligenceEntry> source)
    {
        ProcessIntelligenceEntry[] members = source
            .OrderBy(entry => entry.Observation.ProcessId)
            .ThenBy(entry => entry.Observation.StartTimeUtc)
            .ToArray();
        ProcessIntelligenceEntry representative = members[0];
        string strongestPressure = members
            .OrderByDescending(entry => PressureRank(entry.Observation.PressureLevel))
            .ThenBy(entry => entry.Observation.PressureLevel, StringComparer.OrdinalIgnoreCase)
            .First().Observation.PressureLevel;

        return new ProcessApplicationGroup(
            identity,
            representative.ExecutableInspection?.ProductName ?? representative.Observation.Name,
            representative.IdentityStrength,
            representative.CanonicalExecutablePath,
            ReadOnly(members),
            members.Sum(entry => entry.Observation.CpuPercent),
            members.Sum(entry => entry.Observation.MemoryMb),
            members.Max(entry => entry.Observation.CpuPercent),
            members.Max(entry => entry.Observation.MemoryMb),
            members.Max(entry => entry.Observation.PressureScore),
            strongestPressure,
            representative.Classification,
            representative.Confidence,
            representative.ExecutableInspection,
            representative.Rationale);
    }

    private static bool TryCanonicalize(string? path, out string? canonicalPath)
    {
        canonicalPath = null;
        if (string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            string candidate = path.Trim().Trim('"');
            if (candidate.IndexOf('\0') >= 0 || !Path.IsPathFullyQualified(candidate)) return false;
            canonicalPath = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return !string.IsNullOrWhiteSpace(canonicalPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static ProcessExecutableInspection FailureInspection(string path) =>
        new(path, ProcessFileInspectionStatus.InspectionFailure, null, Path.GetFileName(path), Path.GetExtension(path),
            null, null, null, null, null, null, ProcessSignatureStatus.InspectionFailure, null,
            Array.AsReadOnly(new[] { "Executable inspection did not complete." }));

    private static ProcessExecutableInspection Sanitize(ProcessExecutableInspection inspection) =>
        new(
            inspection.CanonicalPath, inspection.FileStatus, inspection.Exists, inspection.FileName,
            inspection.Extension, inspection.FileDescription, inspection.ProductName, inspection.CompanyName,
            inspection.FileVersion, inspection.ProductVersion, inspection.OriginalFilename,
            inspection.SignatureStatus, inspection.SignerName,
            inspection.Warnings.Take(8).Select(_ => "Executable inspection reported a bounded warning."));

    private static int PressureRank(string value) => value.ToUpperInvariant() switch
    {
        "HIGH" or "CRITICAL" => 4,
        "MODERATE" or "ELEVATED" => 3,
        "LOW" => 2,
        "MINIMAL" or "NORMAL" => 1,
        _ => 0
    };

    private static void AddBounded(List<string> values, string value)
    {
        if (values.Count < MaximumMessages && !string.IsNullOrWhiteSpace(value)) values.Add(value);
    }

    private static IReadOnlyList<T> ReadOnly<T>(T[] values) => new ReadOnlyCollection<T>(values);

    private sealed record Candidate(
        int Index,
        ProcessInstanceObservation Observation,
        string? CanonicalPath,
        string? ProvisionalIdentity);
}
