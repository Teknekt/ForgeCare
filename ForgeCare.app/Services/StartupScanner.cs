using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ForgeCare.App.Models;
using Microsoft.Win32;

namespace ForgeCare.App.Services;

public class StartupScanner
{
    public List<StartupItem> Scan()
    {
        var items = new List<StartupItem>();

        ScanRegistryKey(
            Registry.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            "Current User Registry",
            items);

        ScanRegistryKey(
            Registry.LocalMachine,
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            "Local Machine Registry",
            items);

        ScanStartupFolder(
            Environment.GetFolderPath(
                Environment.SpecialFolder.Startup),
            "User Startup Folder",
            items);

        ScanStartupFolder(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonStartup),
            "Common Startup Folder",
            items);

        return items
            .GroupBy(item =>
                $"{item.Name}|{item.Command}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.Name)
            .ToList();
    }

    private static void ScanRegistryKey(
        RegistryKey root,
        string path,
        string source,
        List<StartupItem> items)
    {
        try
        {
            using RegistryKey? key =
                root.OpenSubKey(path);

            if (key == null)
            {
                return;
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
        }
        catch
        {
            // Scanner must continue even if one source
            // cannot be read.
        }
    }

    private static void ScanStartupFolder(
        string folderPath,
        string source,
        List<StartupItem> items)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                return;
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
        }
        catch
        {
            // Ignore inaccessible startup folders.
        }
    }
}