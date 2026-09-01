using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ForgeCare.App.Services;

public static partial class PrivacySafeDiagnosticFormatter
{
    public static string FormatCrashEntry(
        Exception exception,
        string context,
        DateTime timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(exception);

        string version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion?
            .Split('+')[0]
            ?? "unknown";

        var text = new StringBuilder();
        text.AppendLine("============================================================");
        text.AppendLine($"ForgeCare diagnostic issue · {timestampUtc.ToUniversalTime():yyyy-MM-dd HH:mm:ss} UTC");
        text.AppendLine($"Version: {version}");
        text.AppendLine($"Context: {NormalizeContext(context)}");
        text.AppendLine($"Failure type: {exception.GetType().Name}");
        text.AppendLine();
        return text.ToString();
    }

    public static string FormatSupportFailure(
        string category,
        string status,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"Category: {NormalizeLabel(category)} | Status: {NormalizeLabel(status)} | Reason: {exception.GetType().Name}";
    }

    private static string NormalizeContext(string value)
    {
        string candidate = (value ?? string.Empty).Trim();
        if (candidate.Length is 0 or > 80 ||
            candidate.Contains('\u005c') ||
            candidate.Contains(':') ||
            candidate.Contains('@') ||
            SensitiveTokenPattern().IsMatch(candidate))
        {
            return "Unspecified subsystem";
        }

        return SafeContextPattern().IsMatch(candidate)
            ? candidate
            : "Unspecified subsystem";
    }

    private static string NormalizeLabel(string value)
    {
        string candidate = (value ?? string.Empty).Trim();
        return candidate.Length is > 0 and <= 40 && SafeLabelPattern().IsMatch(candidate)
            ? candidate
            : "Unknown";
    }

    [GeneratedRegex("^[A-Za-z0-9 ._/-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeContextPattern();

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeLabelPattern();

    [GeneratedRegex("(?i)(bearer|token|secret|password|api[-_ ]?key|sk-[a-z0-9])", RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveTokenPattern();
}
