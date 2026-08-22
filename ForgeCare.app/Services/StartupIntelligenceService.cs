using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class StartupIntelligenceService
{
    private readonly StartupCommandParser _commandParser;
    private readonly IStartupFileInspector _fileInspector;
    private readonly IStartupSignatureInspector _signatureInspector;
    private readonly StartupClassificationPolicy _classificationPolicy;

    public StartupIntelligenceService(
        StartupCommandParser commandParser,
        IStartupFileInspector fileInspector,
        IStartupSignatureInspector signatureInspector,
        StartupClassificationPolicy classificationPolicy)
    {
        _commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
        _fileInspector = fileInspector ?? throw new ArgumentNullException(nameof(fileInspector));
        _signatureInspector = signatureInspector ?? throw new ArgumentNullException(nameof(signatureInspector));
        _classificationPolicy = classificationPolicy ?? throw new ArgumentNullException(nameof(classificationPolicy));
    }

    public async Task<StartupIntelligenceResult> AnalyzeAsync(
        IEnumerable<StartupItem> startupItems,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(startupItems);

        var entries = new List<StartupIntelligenceEntry>();
        var warnings = new List<string>();
        var errors = new List<string>();
        var inspectionCache = new Dictionary<string, InspectionPair>(StringComparer.OrdinalIgnoreCase);

        foreach (StartupItem? input in startupItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (input == null)
            {
                errors.Add("A null startup entry could not be inspected.");
                continue;
            }

            string name = input.Name ?? string.Empty;
            string command = input.Command ?? string.Empty;
            string source = input.Source ?? string.Empty;
            StartupSourceKind sourceKind = StartupCommandParser.MapSource(source);
            StartupCommandResolution resolution = _commandParser.Parse(command, sourceKind);
            var entryWarnings = new List<string>();

            StartupFileInspection fileInspection = StartupFileInspection.NotChecked();
            StartupSignatureInfo signature = StartupSignatureInfo.NotChecked();

            if (resolution.HasConfidentDirectPath)
            {
                string path = resolution.ResolvedPath!;
                if (!inspectionCache.TryGetValue(path, out InspectionPair inspected))
                {
                    inspected = await InspectPathAsync(path, entryWarnings, cancellationToken);
                    inspectionCache[path] = inspected;
                }

                fileInspection = inspected.File;
                signature = inspected.Signature;
            }

            if (!string.IsNullOrWhiteSpace(fileInspection.Warning))
                entryWarnings.Add(fileInspection.Warning);
            if (!string.IsNullOrWhiteSpace(signature.Warning))
                entryWarnings.Add(signature.Warning);

            (StartupClassification classification,
                EvidenceConfidence confidence,
                string rationale) = _classificationPolicy.Classify(
                    resolution,
                    fileInspection,
                    signature);

            var entry = new StartupIntelligenceEntry
            {
                Name = name,
                OriginalSource = source,
                SourceKind = sourceKind,
                CommandResolution = Clone(resolution),
                FileInspection = fileInspection,
                Signature = signature,
                Classification = classification,
                Confidence = confidence,
                Rationale = rationale,
                Warnings = new ReadOnlyCollection<string>(entryWarnings.Distinct().ToList())
            };

            entries.Add(entry);
            warnings.AddRange(entryWarnings.Select(warning => $"{DisplayName(name)}: {warning}"));
        }

        return new StartupIntelligenceResult(entries, warnings.Distinct(), errors);
    }

    private async Task<InspectionPair> InspectPathAsync(
        string path,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        StartupFileInspection file;
        try
        {
            file = await _fileInspector.InspectAsync(path, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            string warning = $"File inspection failed ({ex.GetType().Name}).";
            warnings.Add(warning);
            file = new StartupFileInspection
            {
                Status = StartupFileInspectionStatus.InspectionFailure,
                NormalizedPath = path,
                Exists = null,
                Warning = warning
            };
        }

        if (file.Status == StartupFileInspectionStatus.Missing)
        {
            return new InspectionPair(
                file,
                new StartupSignatureInfo
                {
                    Status = StartupSignatureStatus.FileMissing
                });
        }

        if (file.Status != StartupFileInspectionStatus.Available)
        {
            return new InspectionPair(
                file,
                StartupSignatureInfo.NotChecked());
        }

        try
        {
            StartupSignatureInfo signature = await _signatureInspector.InspectAsync(path, cancellationToken);
            return new InspectionPair(file, signature);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            string warning = $"Authenticode inspection failed ({ex.GetType().Name}).";
            warnings.Add(warning);
            return new InspectionPair(
                file,
                new StartupSignatureInfo
                {
                    Status = StartupSignatureStatus.InspectionFailure,
                    Warning = warning
                });
        }
    }

    private static StartupCommandResolution Clone(StartupCommandResolution source) =>
        new()
        {
            OriginalCommand = source.OriginalCommand,
            Status = source.Status,
            ResolvedPath = source.ResolvedPath,
            Arguments = source.Arguments,
            LauncherName = source.LauncherName,
            EnvironmentExpansionApplied = source.EnvironmentExpansionApplied,
            Rationale = source.Rationale
        };

    private static string DisplayName(string name) =>
        string.IsNullOrWhiteSpace(name) ? "Unnamed startup entry" : name;

    private readonly record struct InspectionPair(
        StartupFileInspection File,
        StartupSignatureInfo Signature);
}
