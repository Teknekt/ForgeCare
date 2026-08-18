using System;
using System.IO;
using System.Text;

namespace ForgeCare.App.Services;

public static class AppLifecycleRecoveryService
{
    private static readonly string Root =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ForgeCare",
            "Recovery");

    private static readonly string RunningMarker =
        Path.Combine(Root, "running.marker");

    private static readonly string LastCleanShutdown =
        Path.Combine(Root, "last-clean-shutdown.txt");

    public static bool PreviousSessionWasUnclean { get; private set; }

    public static string RecoveryRoot => Root;

    public static void BeginSession()
    {
        try
        {
            Directory.CreateDirectory(Root);

            PreviousSessionWasUnclean =
                File.Exists(RunningMarker);

            File.WriteAllText(
                RunningMarker,
                $"ForgeCare session started {DateTime.Now:O}{Environment.NewLine}" +
                $"Machine={Environment.MachineName}{Environment.NewLine}" +
                $"Process={Environment.ProcessId}",
                Encoding.UTF8);
        }
        catch
        {
            // Recovery telemetry must never block app startup.
        }
    }

    public static void MarkCleanShutdown()
    {
        try
        {
            Directory.CreateDirectory(Root);

            File.WriteAllText(
                LastCleanShutdown,
                DateTime.Now.ToString("O"),
                Encoding.UTF8);

            if (File.Exists(RunningMarker))
                File.Delete(RunningMarker);
        }
        catch
        {
            // Shutdown should not fail because recovery metadata could not be written.
        }
    }
}
