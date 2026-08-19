using System;

namespace ForgeCare.App.Models;

public enum StartupSignatureStatus
{
    Valid,
    NotSigned,
    HashMismatch,
    Untrusted,
    Invalid,
    InspectionFailure,
    FileMissing,
    NotChecked,
    Unsupported
}

public sealed class StartupSignatureInfo
{
    public required StartupSignatureStatus Status { get; init; }

    public int? NativeStatusCode { get; init; }

    public string? SignerName { get; init; }

    public string? Issuer { get; init; }

    public DateTime? CertificateNotBefore { get; init; }

    public DateTime? CertificateNotAfter { get; init; }

    public string? Warning { get; init; }

    public static StartupSignatureInfo NotChecked(string? warning = null) =>
        new()
        {
            Status = StartupSignatureStatus.NotChecked,
            Warning = warning
        };
}
