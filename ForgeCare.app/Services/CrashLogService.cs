using System;
using System.IO;
using System.Text;

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

                var text = new StringBuilder();
                text.AppendLine("============================================================");
                text.AppendLine($"ForgeCare exception · {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                text.AppendLine($"Context: {context}");
                text.AppendLine($"Machine: {Environment.MachineName}");
                text.AppendLine($"User: {Environment.UserName}");
                text.AppendLine($"OS: {Environment.OSVersion}");
                text.AppendLine($".NET: {Environment.Version}");
                text.AppendLine();
                text.AppendLine(exception.ToString());
                text.AppendLine();

                File.AppendAllText(CrashLogPath, text.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never cause a secondary crash.
        }
    }
}
