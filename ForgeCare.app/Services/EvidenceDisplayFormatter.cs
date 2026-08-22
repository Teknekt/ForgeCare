using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public static partial class EvidenceDisplayFormatter
{
    private static readonly HashSet<string> OneDecimalUnits =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "%", "percent", "gb", "mb"
        };

    private static readonly HashSet<string> IntegerUnits =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "count", "counts", "entry", "entries", "item", "items",
            "process", "processes"
        };

    private static readonly IReadOnlyDictionary<string, string> Acronyms =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cpu"] = "CPU",
            ["os"] = "OS",
            ["id"] = "ID",
            ["pid"] = "PID",
            ["gb"] = "GB",
            ["mb"] = "MB"
        };

    public static string FormatSubject(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return string.Empty;

        string value = subject.Trim();
        int separator = value.IndexOf(':');
        if (separator >= 0)
        {
            string prefix = FormatWords(value[..separator], uppercase: true);
            string suffix = FormatWords(value[(separator + 1)..], uppercase: true);
            return string.IsNullOrEmpty(suffix) ? prefix + ":" : prefix + ": " + suffix;
        }

        return FormatWords(value, uppercase: true);
    }

    public static string FormatCategory(EvidenceCategory category) =>
        FormatWords(category.ToString(), uppercase: false);

    public static string FormatSource(EvidenceSource source) =>
        FormatWords(source.ToString(), uppercase: false);

    public static string FormatSeverity(EvidenceSeverity severity) =>
        FormatWords(severity.ToString(), uppercase: true);

    public static string FormatConfidence(EvidenceConfidence confidence) =>
        FormatWords(confidence.ToString(), uppercase: true);

    public static string FormatMetadataKey(string? key) =>
        FormatWords(key, uppercase: false);

    public static string? FormatValue(double? value, string? unit)
    {
        if (value == null)
            return null;

        string normalizedUnit = unit?.Trim() ?? string.Empty;
        string format;

        if (IntegerUnits.Contains(normalizedUnit) && IsWholeNumber(value.Value))
            format = "0";
        else if (OneDecimalUnits.Contains(normalizedUnit))
            format = "0.#";
        else
            format = "0.##";

        string number = value.Value.ToString(format, CultureInfo.InvariantCulture);
        return normalizedUnit.Length == 0 ? number : number + " " + normalizedUnit;
    }

    public static string FormatTimestamp(DateTime timestampUtc) =>
        timestampUtc.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);

    private static string FormatWords(string? value, bool uppercase)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string separated = SeparatorsRegex().Replace(value.Trim(), " ");
        separated = AcronymBoundaryRegex().Replace(separated, "$1 $2");
        separated = WordBoundaryRegex().Replace(separated, "$1 $2");

        IEnumerable<string> words = separated
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(FormatToken);

        string formatted = string.Join(" ", words);
        return uppercase ? formatted.ToUpperInvariant() : formatted;
    }

    private static string FormatToken(string token)
    {
        if (Acronyms.TryGetValue(token, out string? acronym))
            return acronym;

        if (token.Length == 0)
            return token;

        string lower = token.ToLowerInvariant();
        return char.ToUpperInvariant(lower[0]) + lower[1..];
    }

    private static bool IsWholeNumber(double value) =>
        Math.Abs(value - Math.Round(value)) < 0.0000001;

    [GeneratedRegex("[-_]+")]
    private static partial Regex SeparatorsRegex();

    [GeneratedRegex("([A-Z]+)([A-Z][a-z])")]
    private static partial Regex AcronymBoundaryRegex();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex WordBoundaryRegex();
}
