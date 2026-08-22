using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ForgeCare.App.Models;

public enum ProcessFileInspectionStatus { Available, Missing, AccessDenied, Unsupported, InspectionFailure, NotChecked }
public enum ProcessSignatureStatus { Valid, NotSigned, HashMismatch, Untrusted, Invalid, InspectionFailure, FileMissing, NotChecked, Unsupported }

public sealed class ProcessExecutableInspection
{
    public ProcessExecutableInspection(
        string canonicalPath, ProcessFileInspectionStatus fileStatus, bool? exists,
        string? fileName, string? extension, string? fileDescription, string? productName,
        string? companyName, string? fileVersion, string? productVersion, string? originalFilename,
        ProcessSignatureStatus signatureStatus, string? signerName, IEnumerable<string>? warnings = null)
    {
        CanonicalPath = canonicalPath;
        FileStatus = fileStatus;
        Exists = exists;
        FileName = fileName;
        Extension = extension;
        FileDescription = fileDescription;
        ProductName = productName;
        CompanyName = companyName;
        FileVersion = fileVersion;
        ProductVersion = productVersion;
        OriginalFilename = originalFilename;
        SignatureStatus = signatureStatus;
        SignerName = signerName;
        Warnings = new ReadOnlyCollection<string>(new List<string>(warnings ?? []));
    }

    public string CanonicalPath { get; }
    public ProcessFileInspectionStatus FileStatus { get; }
    public bool? Exists { get; }
    public string? FileName { get; }
    public string? Extension { get; }
    public string? FileDescription { get; }
    public string? ProductName { get; }
    public string? CompanyName { get; }
    public string? FileVersion { get; }
    public string? ProductVersion { get; }
    public string? OriginalFilename { get; }
    public ProcessSignatureStatus SignatureStatus { get; }
    public string? SignerName { get; }
    public IReadOnlyList<string> Warnings { get; }
}
