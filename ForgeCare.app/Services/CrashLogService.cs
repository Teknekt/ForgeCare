using System;
using System.IO;

namespace ForgeCare.App.Services;

public static class CrashLogService
{
    private static readonly object Sync = new();

    public static string DiagnosticsRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ForgeCare", "Diagnostics");

    public static string CrashLogPath =>
        Path.Combine(DiagnosticsRoot, "crash.log");

    public static void Record(Exception exception, string context)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(DiagnosticsRoot);

                File.AppendAllText(
                    CrashLogPath,
                    PrivacySafeDiagnosticFormatter.FormatCrashEntry(
                        exception,
                        context,
                        DateTime.UtcNow),
                    System.Text.Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never cause a secondary crash.
        }
    }

    public static void RecordPrivacySafe(Exception exception, string context)
        => Record(exception, context);
}
