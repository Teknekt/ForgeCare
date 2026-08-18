using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class StabilityRecoveryService
{
    private readonly string _dataRoot;

    public StabilityRecoveryService()
    {
        _dataRoot =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ForgeCare");
    }

    public StabilityRecoveryResult Inspect()
    {
        var result =
            new StabilityRecoveryResult
            {
                PreviousSessionUnclean =
                    AppLifecycleRecoveryService.PreviousSessionWasUnclean
            };

        if (result.PreviousSessionUnclean)
        {
            result.Findings.Add(
                "Previous ForgeCare session did not record a clean shutdown. Review the crash log and last operation before continuing system-changing work.");
        }

        DateTime staleBefore =
            DateTime.Now.AddMinutes(-30);

        string updates =
            Path.Combine(_dataRoot, "Updates");

        if (Directory.Exists(updates))
        {
            foreach (string file in
                     Directory.EnumerateFiles(
                         updates,
                         "*.partial",
                         SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var info = new FileInfo(file);

                    if (info.LastWriteTime < staleBefore)
                    {
                        result.StalePartialFileCount++;
                        result.RecoverableTransientBytes += info.Length;
                    }
                }
                catch
                {
                }
            }
        }

        string diagnostics =
            Path.Combine(_dataRoot, "Diagnostics");

        if (Directory.Exists(diagnostics))
        {
            foreach (string directory in
                     Directory.EnumerateDirectories(
                         diagnostics,
                         "bundle-*",
                         SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var info = new DirectoryInfo(directory);

                    if (info.LastWriteTime < staleBefore)
                    {
                        result.StaleStagingDirectoryCount++;
                        result.RecoverableTransientBytes +=
                            DirectorySize(directory);
                    }
                }
                catch
                {
                }
            }
        }

        if (result.StalePartialFileCount > 0)
        {
            result.Findings.Add(
                $"{result.StalePartialFileCount} stale update .partial file(s) can be removed safely.");
        }

        if (result.StaleStagingDirectoryCount > 0)
        {
            result.Findings.Add(
                $"{result.StaleStagingDirectoryCount} stale diagnostic staging folder(s) can be removed safely.");
        }

        if (result.Findings.Count == 0)
        {
            result.Findings.Add(
                "No interrupted-session or stale transient-file recovery issue was detected.");
        }

        result.State =
            result.PreviousSessionUnclean
                ? "REVIEW REQUIRED"
                : result.StalePartialFileCount > 0 ||
                  result.StaleStagingDirectoryCount > 0
                    ? "RECOVERY AVAILABLE"
                    : "HEALTHY";

        return result;
    }

    public StabilityRecoveryResult CleanSafeTransientFiles()
    {
        DateTime staleBefore =
            DateTime.Now.AddMinutes(-30);

        string updates =
            Path.Combine(_dataRoot, "Updates");

        if (Directory.Exists(updates))
        {
            foreach (string file in
                     Directory.EnumerateFiles(
                         updates,
                         "*.partial",
                         SearchOption.TopDirectoryOnly))
            {
                try
                {
                    if (File.GetLastWriteTime(file) < staleBefore)
                        File.Delete(file);
                }
                catch
                {
                }
            }
        }

        string diagnostics =
            Path.Combine(_dataRoot, "Diagnostics");

        if (Directory.Exists(diagnostics))
        {
            foreach (string directory in
                     Directory.EnumerateDirectories(
                         diagnostics,
                         "bundle-*",
                         SearchOption.TopDirectoryOnly))
            {
                try
                {
                    if (Directory.GetLastWriteTime(directory) < staleBefore)
                        Directory.Delete(directory, true);
                }
                catch
                {
                }
            }
        }

        return Inspect();
    }

    private static long DirectorySize(string directory)
    {
        long total = 0;

        foreach (string file in
                 Directory.EnumerateFiles(
                     directory,
                     "*",
                     SearchOption.AllDirectories))
        {
            try
            {
                total +=
                    new FileInfo(file).Length;
            }
            catch
            {
            }
        }

        return total;
    }
}
