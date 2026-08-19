using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class EvidenceInspectionService
{
    private readonly string _storageRoot;
    private readonly JsonSerializerOptions _jsonOptions =
        new()
        {
            Converters = { new JsonStringEnumConverter() }
        };

    public EvidenceInspectionService(string? storageRoot = null)
    {
        _storageRoot = storageRoot ?? DefaultStorageRoot;
    }

    public static string DefaultStorageRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ForgeCare",
            "Evidence");

    public string StorageRoot => _storageRoot;

    public EvidenceHealthResult Inspect()
    {
        var result = new EvidenceHealthResult
        {
            StorageRoot = _storageRoot,
            DirectoryExists = Directory.Exists(_storageRoot)
        };

        if (!result.DirectoryExists)
            return result;

        string[] paths;
        try
        {
            paths = Directory.GetFiles(_storageRoot, "*.json");
        }
        catch (Exception ex)
        {
            result.Errors.Add(
                $"Evidence directory could not be enumerated: {ex.Message}");
            return result;
        }

        result.DocumentCount = paths.Length;

        foreach (string path in paths.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            InspectDocument(path, result);

        return result;
    }

    private void InspectDocument(
        string path,
        EvidenceHealthResult result)
    {
        string fileName = Path.GetFileName(path);
        string fileSessionId = Path.GetFileNameWithoutExtension(path);
        bool documentIsValid = true;

        if (!IsValidSessionId(fileSessionId))
        {
            result.Warnings.Add(
                $"{fileName}: filename is not a session GUID in N format.");
            documentIsValid = false;
        }

        EvidenceDocument document;
        try
        {
            string json = File.ReadAllText(path);
            document = JsonSerializer.Deserialize<EvidenceDocument>(json, _jsonOptions)
                       ?? throw new JsonException("Evidence document was empty.");
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or IOException or UnauthorizedAccessException)
        {
            result.MalformedDocumentCount++;
            result.Errors.Add(
                $"{fileName}: document could not be read: {ex.Message}");
            return;
        }

        if (document.SchemaVersion != EvidenceDocument.CurrentSchemaVersion)
        {
            result.UnsupportedSchemaCount++;
            result.Warnings.Add(
                $"{fileName}: schema {document.SchemaVersion} is unsupported; expected {EvidenceDocument.CurrentSchemaVersion}.");
            return;
        }

        if (!IsValidSessionId(document.SessionId))
        {
            result.Errors.Add(
                $"{fileName}: top-level SessionId is invalid.");
            documentIsValid = false;
        }
        else if (!string.Equals(
                     document.SessionId,
                     fileSessionId,
                     StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add(
                $"{fileName}: filename and top-level SessionId do not match.");
            documentIsValid = false;
        }

        if (document.Evidence == null)
        {
            result.Errors.Add(
                $"{fileName}: Evidence collection is missing.");
            documentIsValid = false;
        }
        else
        {
            result.TotalRecordCount += document.Evidence.Count;

            foreach (EvidenceRecord? record in document.Evidence)
            {
                if (record == null)
                {
                    result.Errors.Add(
                        $"{fileName}: document contains a null Evidence record.");
                    documentIsValid = false;
                    continue;
                }

                if (!string.Equals(
                        record.SessionId,
                        document.SessionId,
                        StringComparison.Ordinal))
                {
                    result.Errors.Add(
                        $"{fileName}: Evidence record {record.Id} has a mismatched SessionId.");
                    documentIsValid = false;
                }

                IReadOnlyList<string> validationErrors = record.Validate();
                if (validationErrors.Count > 0)
                {
                    result.Errors.Add(
                        $"{fileName}: Evidence record {record.Id} is invalid: " +
                        string.Join(" ", validationErrors));
                    documentIsValid = false;
                }

                if (record.TimestampUtc.Kind == DateTimeKind.Utc &&
                    (result.LatestTimestampUtc == null ||
                     record.TimestampUtc > result.LatestTimestampUtc.Value))
                {
                    result.LatestTimestampUtc = record.TimestampUtc;
                }
            }
        }

        if (documentIsValid)
            result.ValidDocumentCount++;
        else
            result.InvalidDocumentCount++;
    }

    private static bool IsValidSessionId(string? sessionId) =>
        Guid.TryParseExact(sessionId, "N", out _);
}
