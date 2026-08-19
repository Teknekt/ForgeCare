using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

internal static class StartupEvidenceTestFactory
{
    public static StartupIntelligenceEntry Entry(
        string name = "Example",
        string command = @"C:\Users\Alice\AppData\Local\Vendor\App.exe",
        string? resolvedPath = @"C:\Users\Alice\AppData\Local\Vendor\App.exe",
        StartupSourceKind source = StartupSourceKind.CurrentUserRegistry,
        StartupCommandResolutionStatus resolution = StartupCommandResolutionStatus.DirectExecutable,
        StartupFileInspectionStatus fileStatus = StartupFileInspectionStatus.Available,
        bool? fileExists = true,
        StartupSignatureStatus signature = StartupSignatureStatus.Valid,
        StartupClassification classification = StartupClassification.Verified,
        EvidenceConfidence confidence = EvidenceConfidence.High,
        string? arguments = null,
        string? launcher = null) =>
        new()
        {
            Name = name,
            OriginalSource = source.ToString(),
            SourceKind = source,
            CommandResolution = new StartupCommandResolution
            {
                OriginalCommand = command,
                Status = resolution,
                ResolvedPath = resolvedPath,
                Arguments = arguments,
                LauncherName = launcher,
                Rationale = "Transient Phase B rationale."
            },
            FileInspection = new StartupFileInspection
            {
                Status = fileStatus,
                NormalizedPath = resolvedPath,
                Exists = fileExists,
                FileName = resolvedPath == null ? null : Path.GetFileName(resolvedPath),
                FileDescription = "Example background component",
                ProductName = "Example Product",
                CompanyName = "Example Corp",
                FileVersion = "1.2.3.4",
                ProductVersion = "1.2.3",
                OriginalFilename = "App.exe"
            },
            Signature = new StartupSignatureInfo
            {
                Status = signature,
                SignerName = signature == StartupSignatureStatus.Valid ? "Example Corporation LLC" : null
            },
            Classification = classification,
            Confidence = confidence,
            Rationale = "Transient Phase B rationale."
        };

    public static StartupEvidencePathNormalizer Normalizer() =>
        new(
            userProfile: @"C:\Users\Alice",
            localApplicationData: @"C:\Users\Alice\AppData\Local",
            applicationData: @"C:\Users\Alice\AppData\Roaming",
            programFiles: @"C:\Program Files",
            programFilesX86: @"C:\Program Files (x86)",
            programData: @"C:\ProgramData",
            windows: @"C:\Windows");

    public static StartupIntelligenceEvidenceAdapter Adapter() =>
        new(Normalizer(), new StartupEvidenceCorrelationKeyBuilder());
}
