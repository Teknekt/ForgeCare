using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class WindowsProcessExecutableInspector : IProcessExecutableInspector
{
    private const int MaximumWarnings = 8;
    private readonly IStartupFileInspector _fileInspector;
    private readonly IStartupSignatureInspector _signatureInspector;

    public WindowsProcessExecutableInspector()
        : this(new WindowsStartupFileInspector(), new WinVerifyTrustStartupSignatureInspector())
    {
    }

    public WindowsProcessExecutableInspector(
        IStartupFileInspector fileInspector,
        IStartupSignatureInspector signatureInspector)
    {
        _fileInspector = fileInspector ?? throw new ArgumentNullException(nameof(fileInspector));
        _signatureInspector = signatureInspector ?? throw new ArgumentNullException(nameof(signatureInspector));
    }

    public async Task<ProcessExecutableInspection> InspectAsync(
        string canonicalExecutablePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalExecutablePath);
        cancellationToken.ThrowIfCancellationRequested();

        StartupFileInspection file = await _fileInspector
            .InspectAsync(canonicalExecutablePath, cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        StartupSignatureInfo signature = await _signatureInspector
            .InspectAsync(canonicalExecutablePath, cancellationToken)
            .ConfigureAwait(false);

        string[] warnings = new[] { file.Warning, signature.Warning }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Take(MaximumWarnings)
            .ToArray();

        return new ProcessExecutableInspection(
            canonicalExecutablePath,
            Map(file.Status),
            file.Exists,
            file.FileName,
            file.Extension,
            file.FileDescription,
            file.ProductName,
            file.CompanyName,
            file.FileVersion,
            file.ProductVersion,
            file.OriginalFilename,
            Map(signature.Status),
            signature.SignerName,
            Array.AsReadOnly(warnings));
    }

    private static ProcessFileInspectionStatus Map(StartupFileInspectionStatus status) =>
        status switch
        {
            StartupFileInspectionStatus.Available => ProcessFileInspectionStatus.Available,
            StartupFileInspectionStatus.Missing => ProcessFileInspectionStatus.Missing,
            StartupFileInspectionStatus.AccessDenied => ProcessFileInspectionStatus.AccessDenied,
            StartupFileInspectionStatus.Unsupported => ProcessFileInspectionStatus.Unsupported,
            StartupFileInspectionStatus.InspectionFailure => ProcessFileInspectionStatus.InspectionFailure,
            _ => ProcessFileInspectionStatus.NotChecked
        };

    private static ProcessSignatureStatus Map(StartupSignatureStatus status) =>
        status switch
        {
            StartupSignatureStatus.Valid => ProcessSignatureStatus.Valid,
            StartupSignatureStatus.NotSigned => ProcessSignatureStatus.NotSigned,
            StartupSignatureStatus.HashMismatch => ProcessSignatureStatus.HashMismatch,
            StartupSignatureStatus.Untrusted => ProcessSignatureStatus.Untrusted,
            StartupSignatureStatus.Invalid => ProcessSignatureStatus.Invalid,
            StartupSignatureStatus.InspectionFailure => ProcessSignatureStatus.InspectionFailure,
            StartupSignatureStatus.FileMissing => ProcessSignatureStatus.FileMissing,
            StartupSignatureStatus.Unsupported => ProcessSignatureStatus.Unsupported,
            _ => ProcessSignatureStatus.NotChecked
        };
}
