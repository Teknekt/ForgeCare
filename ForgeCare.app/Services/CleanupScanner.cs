using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public class CleanupScanner
{
    public Task<CleanupResult> ScanAsync()
    {
        return Task.Run(Scan);
    }

    private CleanupResult Scan()
    {
        var result =
            new CleanupResult();

        // =========================
        // USER TEMP
        // =========================

        string userTemp =
            Path.GetTempPath();

        result.Items.Add(
            ScanDirectory(
                "User Temporary Files",
                userTemp));


        // =========================
        // WINDOWS TEMP
        // =========================

        string windowsFolder =
            Environment.GetFolderPath(
                Environment.SpecialFolder.Windows);

        string windowsTemp =
            Path.Combine(
                windowsFolder,
                "Temp");

        result.Items.Add(
            ScanDirectory(
                "Windows Temporary Files",
                windowsTemp));


        // =========================
        // RECYCLE BIN
        // =========================

        result.Items.Add(
            ScanRecycleBin());


        return result;
    }

    private CleanupItem ScanDirectory(
        string name,
        string path)
    {
        var item =
            new CleanupItem
            {
                Name = name,
                Path = path,
                IsSelected = true
            };

        if (!Directory.Exists(path))
        {
            item.Status =
                "Not found";

            return item;
        }

        long totalBytes = 0;
        int fileCount = 0;

        try
        {
            ScanDirectoryRecursive(
                path,
                ref totalBytes,
                ref fileCount);

            item.Status =
                "Ready";
        }
        catch
        {
            item.Status =
                "Partial access";
        }

        item.SizeBytes =
            totalBytes;

        item.FileCount =
            fileCount;

        return item;
    }

    private void ScanDirectoryRecursive(
        string path,
        ref long totalBytes,
        ref int fileCount)
    {
        try
        {
            foreach (string file in
                     Directory.EnumerateFiles(path))
            {
                try
                {
                    var info =
                        new FileInfo(file);

                    totalBytes +=
                        info.Length;

                    fileCount++;
                }
                catch
                {
                    // Locked/inaccessible file.
                    // Skip safely.
                }
            }

            foreach (string directory in
                     Directory.EnumerateDirectories(path))
            {
                ScanDirectoryRecursive(
                    directory,
                    ref totalBytes,
                    ref fileCount);
            }
        }
        catch
        {
            // Folder inaccessible.
            // Continue scanning everything else.
        }
    }

    private CleanupItem ScanRecycleBin()
    {
        var item =
            new CleanupItem
            {
                Name = "Recycle Bin",
                Path = "$Recycle.Bin",
                IsSelected = false
            };

        long totalBytes = 0;
        int fileCount = 0;

        try
        {
            foreach (DriveInfo drive in
                     DriveInfo.GetDrives())
            {
                if (!drive.IsReady)
                {
                    continue;
                }

                string recyclePath =
                    Path.Combine(
                        drive.RootDirectory.FullName,
                        "$Recycle.Bin");

                if (!Directory.Exists(recyclePath))
                {
                    continue;
                }

                ScanDirectoryRecursive(
                    recyclePath,
                    ref totalBytes,
                    ref fileCount);
            }

            item.Status =
                "Ready";
        }
        catch
        {
            item.Status =
                "Partial access";
        }

        item.SizeBytes =
            totalBytes;

        item.FileCount =
            fileCount;

        return item;
    }
}