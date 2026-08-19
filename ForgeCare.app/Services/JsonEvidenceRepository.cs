using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class JsonEvidenceRepository : IEvidenceRepository
{
    private readonly string _storageRoot;
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions =
        new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

    public JsonEvidenceRepository(string? storageRoot = null)
    {
        _storageRoot = storageRoot ??
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ForgeCare",
                "Evidence");
    }

    public string StorageRoot => _storageRoot;

    public Task AddAsync(
        EvidenceRecord record,
        CancellationToken cancellationToken = default) =>
        AddRangeAsync(new[] { record }, cancellationToken);

    public async Task AddRangeAsync(
        IReadOnlyCollection<EvidenceRecord> records,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);

        if (records.Count == 0)
            return;

        string sessionId = records.First().SessionId;
        ValidateSessionId(sessionId);

        if (records.Any(record =>
                !string.Equals(record.SessionId, sessionId, StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "All evidence records in a batch must belong to the same session.",
                nameof(records));
        }

        foreach (EvidenceRecord record in records)
        {
            IReadOnlyList<string> errors = record.Validate();
            if (errors.Count > 0)
            {
                throw new EvidenceValidationException(
                    string.Join(" ", errors));
            }
        }

        await _sync.WaitAsync(cancellationToken);
        try
        {
            EvidenceDocument document = await LoadDocumentAsync(
                sessionId,
                cancellationToken);

            document.Evidence.AddRange(records);
            document.Evidence = Order(document.Evidence).ToList();

            await SaveDocumentAsync(document, cancellationToken);
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<EvidenceRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EvidenceRecord> records = await LoadAllAsync(cancellationToken);
        return records.FirstOrDefault(record => record.Id == id);
    }

    public async Task<IReadOnlyList<EvidenceRecord>> GetBySessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ValidateSessionId(sessionId);

        await _sync.WaitAsync(cancellationToken);
        try
        {
            EvidenceDocument document = await LoadDocumentAsync(
                sessionId,
                cancellationToken);

            return Order(document.Evidence).ToList();
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<IReadOnlyList<EvidenceRecord>> GetByCategoryAsync(
        EvidenceCategory category,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(category))
            throw new ArgumentOutOfRangeException(nameof(category));

        IReadOnlyList<EvidenceRecord> records = await LoadAllAsync(cancellationToken);
        return Order(records.Where(record => record.Category == category)).ToList();
    }

    public async Task<IReadOnlyList<EvidenceRecord>> GetByCorrelationKeyAsync(
        string correlationKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(correlationKey))
            throw new ArgumentException("Correlation key must not be empty.", nameof(correlationKey));

        IReadOnlyList<EvidenceRecord> records = await LoadAllAsync(cancellationToken);
        return Order(
                records.Where(record =>
                    string.Equals(
                        record.CorrelationKey,
                        correlationKey,
                        StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private async Task<IReadOnlyList<EvidenceRecord>> LoadAllAsync(
        CancellationToken cancellationToken)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (!Directory.Exists(_storageRoot))
                return Array.Empty<EvidenceRecord>();

            var records = new List<EvidenceRecord>();
            foreach (string path in Directory.EnumerateFiles(_storageRoot, "*.json"))
            {
                EvidenceDocument document = await LoadDocumentFromPathAsync(
                    path,
                    cancellationToken);

                records.AddRange(document.Evidence);
            }

            return Order(records).ToList();
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task<EvidenceDocument> LoadDocumentAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        string path = GetSessionPath(sessionId);
        if (!File.Exists(path))
        {
            return new EvidenceDocument
            {
                SessionId = sessionId
            };
        }

        EvidenceDocument document = await LoadDocumentFromPathAsync(
            path,
            cancellationToken);

        if (!string.Equals(document.SessionId, sessionId, StringComparison.Ordinal))
        {
            throw new EvidencePersistenceException(
                $"Evidence document session '{document.SessionId}' does not match '{sessionId}'.");
        }

        return document;
    }

    private async Task<EvidenceDocument> LoadDocumentFromPathAsync(
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            string json = await File.ReadAllTextAsync(path, cancellationToken);
            EvidenceDocument document = JsonSerializer.Deserialize<EvidenceDocument>(
                                            json,
                                            _jsonOptions)
                                        ?? throw new JsonException("Evidence document was empty.");

            if (document.SchemaVersion != EvidenceDocument.CurrentSchemaVersion)
            {
                throw new UnsupportedEvidenceSchemaException(
                    document.SchemaVersion,
                    EvidenceDocument.CurrentSchemaVersion);
            }

            document.Evidence ??= new List<EvidenceRecord>();
            return document;
        }
        catch (UnsupportedEvidenceSchemaException)
        {
            throw;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new MalformedEvidenceDocumentException(path, ex);
        }
    }

    private async Task SaveDocumentAsync(
        EvidenceDocument document,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_storageRoot);

        string path = GetSessionPath(document.SessionId);
        string tempPath = path + ".tmp";
        string json = JsonSerializer.Serialize(document, _jsonOptions);

        try
        {
            await File.WriteAllTextAsync(tempPath, json, cancellationToken);
            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // Cleanup must not hide the original persistence failure.
            }

            throw;
        }
    }

    private string GetSessionPath(string sessionId)
    {
        ValidateSessionId(sessionId);
        return Path.Combine(_storageRoot, sessionId + ".json");
    }

    private static void ValidateSessionId(string sessionId)
    {
        if (!Guid.TryParseExact(sessionId, "N", out _))
        {
            throw new ArgumentException(
                "Evidence session IDs must be GUIDs in N format.",
                nameof(sessionId));
        }
    }

    private static IOrderedEnumerable<EvidenceRecord> Order(
        IEnumerable<EvidenceRecord> records) =>
        records
            .OrderByDescending(record => record.TimestampUtc)
            .ThenBy(record => record.Id);
}

public sealed class EvidenceValidationException : Exception
{
    public EvidenceValidationException(string message) : base(message)
    {
    }
}

public sealed class EvidencePersistenceException : Exception
{
    public EvidencePersistenceException(string message) : base(message)
    {
    }
}

public sealed class MalformedEvidenceDocumentException : Exception
{
    public MalformedEvidenceDocumentException(string path, Exception innerException)
        : base($"Evidence document is malformed: {path}", innerException)
    {
    }
}

public sealed class UnsupportedEvidenceSchemaException : Exception
{
    public UnsupportedEvidenceSchemaException(int actualVersion, int supportedVersion)
        : base($"Evidence schema version {actualVersion} is unsupported. Supported version: {supportedVersion}.")
    {
        ActualVersion = actualVersion;
        SupportedVersion = supportedVersion;
    }

    public int ActualVersion { get; }

    public int SupportedVersion { get; }
}
