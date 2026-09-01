using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ForgeCare.App.Services;

public static partial class CrashLogBundleProjector
{
    private const long MaximumInputBytes = 4 * 1024 * 1024;
    private const int MaximumRecords = 500;

    public static void Project(string sourcePath, string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        using var input = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(
            input,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
            detectEncodingFromByteOrderMarks: true);
        using var output = new StreamWriter(
            destinationPath,
            append: false,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        output.WriteLine("ForgeCare privacy-safe crash diagnostic projection");
        output.WriteLine("Raw messages, identities, paths, and stack traces are omitted.");
        output.WriteLine();

        var record = new ProjectedRecord();
        int recordsWritten = 0;
        bool sawInput = false;
        bool truncated = false;

        while (recordsWritten < MaximumRecords && reader.ReadLine() is { } line)
        {
            sawInput = true;

            if (IsSeparator(line))
            {
                if (record.HasContent)
                {
                    WriteRecord(output, record);
                    recordsWritten++;
                    record = new ProjectedRecord();
                }
            }
            else
            {
                CaptureSafeField(record, line);
            }

            if (input.Position >= MaximumInputBytes)
            {
                truncated = input.Length > MaximumInputBytes;
                break;
            }
        }

        if (recordsWritten < MaximumRecords && record.HasContent)
        {
            WriteRecord(output, record);
            recordsWritten++;
        }

        if (recordsWritten == 0)
        {
            output.WriteLine(sawInput
                ? "Unstructured crash diagnostic content was omitted."
                : "No crash diagnostics were recorded.");
        }

        if (truncated || recordsWritten >= MaximumRecords)
        {
            output.WriteLine();
            output.WriteLine("Additional historical crash diagnostics were omitted by the bundle size limit.");
        }
    }

    private static void CaptureSafeField(ProjectedRecord record, string line)
    {
        string candidate = line.Trim();
        if (candidate.Length == 0)
            return;

        if (TryReadValue(candidate, "Context:", out string context))
        {
            record.Context = NormalizeContext(context);
            return;
        }

        if (TryReadValue(candidate, "Version:", out string version))
        {
            record.Version = SafeVersionPattern().IsMatch(version) && version.Length <= 80
                ? version
                : null;
            return;
        }

        if (TryReadValue(candidate, "Failure type:", out string failureType))
        {
            record.ExceptionType = NormalizeExceptionType(failureType);
            return;
        }

        Match timestamp = TimestampHeaderPattern().Match(candidate);
        if (timestamp.Success)
        {
            record.Timestamp = NormalizeTimestamp(timestamp.Groups[1].Value);
            return;
        }

        Match legacyException = LegacyExceptionPattern().Match(candidate);
        if (legacyException.Success)
            record.ExceptionType = NormalizeExceptionType(legacyException.Groups[1].Value);
    }

    private static string? NormalizeTimestamp(string value)
    {
        string candidate = value.Trim();
        return candidate.Length <= 48 && SafeTimestampPattern().IsMatch(candidate)
            ? candidate
            : null;
    }

    private static string NormalizeContext(string value)
    {
        string candidate = value.Trim();
        return candidate.Length is > 0 and <= 80 &&
               SafeContextPattern().IsMatch(candidate) &&
               !SensitiveTokenPattern().IsMatch(candidate)
            ? candidate
            : "Unspecified subsystem";
    }

    private static string? NormalizeExceptionType(string value)
    {
        string candidate = value.Trim();
        return candidate.Length <= 160 && ExceptionTypePattern().IsMatch(candidate)
            ? candidate
            : null;
    }

    private static bool TryReadValue(string line, string prefix, out string value)
    {
        if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = line[prefix.Length..].Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static void WriteRecord(TextWriter output, ProjectedRecord record)
    {
        output.WriteLine("============================================================");
        if (record.Timestamp is not null)
            output.WriteLine($"Timestamp: {record.Timestamp}");
        if (record.Version is not null)
            output.WriteLine($"Version: {record.Version}");
        output.WriteLine($"Context: {record.Context ?? "Unspecified subsystem"}");
        output.WriteLine($"Failure type: {record.ExceptionType ?? "UnknownException"}");
        output.WriteLine();
    }

    private static bool IsSeparator(string line) =>
        line.Length >= 20 && line.All(character => character == '=');

    private sealed class ProjectedRecord
    {
        public string? Timestamp { get; set; }
        public string? Version { get; set; }
        public string? Context { get; set; }
        public string? ExceptionType { get; set; }

        public bool HasContent =>
            Timestamp is not null ||
            Version is not null ||
            Context is not null ||
            ExceptionType is not null;
    }

    [GeneratedRegex("^(?:ForgeCare (?:exception|diagnostic issue) ·|Time:)\\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TimestampHeaderPattern();

    [GeneratedRegex("^([A-Za-z_][A-Za-z0-9_.+`]*(?:Exception|Error))(?::|$)", RegexOptions.CultureInvariant)]
    private static partial Regex LegacyExceptionPattern();

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_.+`]*(?:Exception|Error)$", RegexOptions.CultureInvariant)]
    private static partial Regex ExceptionTypePattern();

    [GeneratedRegex("^[0-9A-Za-z .:+TZ/-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeTimestampPattern();

    [GeneratedRegex("^[0-9A-Za-z.+_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeVersionPattern();

    [GeneratedRegex("^[A-Za-z0-9 ._/-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeContextPattern();

    [GeneratedRegex("(?i)(bearer|token|secret|password|api[-_ ]?key|sk-[a-z0-9])", RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveTokenPattern();
}
