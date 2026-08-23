using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ForgeCare.App.Models;
using Microsoft.Win32;

namespace ForgeCare.App.Services;

public class StartupScanner
{
    public StartupScanResult Scan()
    {
        StartupScanSourceResult currentUserRegistry = ScanRegistryKey(
            Registry.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            "Current User Registry",
            StartupScanSourceKind.CurrentUserRegistry);

        StartupScanSourceResult localMachineRegistry = ScanRegistryKey(
            Registry.LocalMachine,
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            "Local Machine Registry",
            StartupScanSourceKind.LocalMachineRegistry);

        StartupScanSourceResult userStartupFolder = ScanStartupFolder(
            Environment.GetFolderPath(
                Environment.SpecialFolder.Startup),
            "User Startup Folder",
            StartupScanSourceKind.UserStartupFolder);

        StartupScanSourceResult commonStartupFolder = ScanStartupFolder(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonStartup),
            "Common Startup Folder",
            StartupScanSourceKind.CommonStartupFolder);

        StartupScanSourceResult[] sources =
        {
            currentUserRegistry,
            localMachineRegistry,
            userStartupFolder,
            commonStartupFolder
        };

        List<StartupItem> items = sources
            .SelectMany(source => source.Items)
            .GroupBy(item =>
                $"{item.Name}|{item.Command}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.Name)
            .ToList();

        return new StartupScanResult(items, sources);
    }

    private static StartupScanSourceResult ScanRegistryKey(
        RegistryKey root,
        string path,
        string source,
        StartupScanSourceKind sourceKind)
    {
        var items = new List<StartupItem>();
        try
        {
            using RegistryKey? key =
                root.OpenSubKey(path);

            if (key == null)
            {
                return new StartupScanSourceResult(
                    sourceKind,
                    StartupScanSourceStatus.Completed,
                    items);
            }

            foreach (string valueName in key.GetValueNames())
            {
                string command =
                    key.GetValue(valueName)?.ToString()
                    ?? string.Empty;

                items.Add(new StartupItem
                {
                    Name = string.IsNullOrWhiteSpace(valueName)
                        ? "Unnamed startup item"
                        : valueName,

                    Command = command,

                    Source = source
                });
            }

            return new StartupScanSourceResult(
                sourceKind,
                StartupScanSourceStatus.Completed,
                items);
        }
        catch (PlatformNotSupportedException)
        {
            return new StartupScanSourceResult(
                sourceKind,
                StartupScanSourceStatus.Unavailable,
                items);
        }
        catch
        {
            // Scanner must continue even if one source
            // cannot be read.
            return new StartupScanSourceResult(
                sourceKind,
                StartupScanSourceStatus.Failed,
                items);
        }
    }

    private static StartupScanSourceResult ScanStartupFolder(
        string folderPath,
        string source,
        StartupScanSourceKind sourceKind)
    {
        var items = new List<StartupItem>();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return new StartupScanSourceResult(
                sourceKind,
                StartupScanSourceStatus.Unavailable,
                items);
        }

        try
        {
            if (!Directory.Exists(folderPath))
            {
                return new StartupScanSourceResult(
                    sourceKind,
                    StartupScanSourceStatus.Completed,
                    items);
            }

            foreach (string file in
                     Directory.GetFiles(folderPath))
            {
                items.Add(new StartupItem
                {
                    Name =
                        Path.GetFileNameWithoutExtension(file),

                    Command = file,

                    Source = source
                });
            }

            return new StartupScanSourceResult(
                sourceKind,
                StartupScanSourceStatus.Completed,
                items);
        }
        catch (PlatformNotSupportedException)
        {
            return new StartupScanSourceResult(
                sourceKind,
                StartupScanSourceStatus.Unavailable,
                items);
        }
        catch
        {
            // Ignore inaccessible startup folders.
            return new StartupScanSourceResult(
                sourceKind,
                StartupScanSourceStatus.Failed,
                items);
        }
    }
}
