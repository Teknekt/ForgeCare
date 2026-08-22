using System;
using System.Collections.Generic;

namespace ForgeCare.App.Models;

public sealed class EvidenceHealthResult
{
    public string StorageRoot { get; set; } = string.Empty;

    public bool DirectoryExists { get; set; }

    public int DocumentCount { get; set; }

    public int ValidDocumentCount { get; set; }

    public int TotalRecordCount { get; set; }

    public int MalformedDocumentCount { get; set; }

    public int UnsupportedSchemaCount { get; set; }

    public int InvalidDocumentCount { get; set; }

    public DateTime? LatestTimestampUtc { get; set; }

    public List<string> Warnings { get; set; } = new();

    public List<string> Errors { get; set; } = new();

    public bool HasWarnings => Warnings.Count > 0;

    public bool HasErrors => Errors.Count > 0;
}
