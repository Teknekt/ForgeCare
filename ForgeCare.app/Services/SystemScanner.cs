using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ForgeCare.App.Models;
using Microsoft.Win32;

namespace ForgeCare.App.Services;

public class SystemScanner
{
    private readonly StartupScanner _startupScanner;

    public SystemScanner()
    {
        _startupScanner =
            new StartupScanner();
    }

    public Task<SystemSnapshot> ScanAsync()
    {
        return Task.Run(() =>
        {
            var memory =
                GetMemoryInfo();

            string systemRoot =
                Path.GetPathRoot(
                    Environment.SystemDirectory)
                ?? "C:\\";

            DriveInfo? systemDrive =
                DriveInfo.GetDrives()
                    .FirstOrDefault(drive =>
                        drive.IsReady &&
                        drive.Name.Equals(
                            systemRoot,
                            StringComparison.OrdinalIgnoreCase));

            var snapshot =
                new SystemSnapshot
                {
                    ComputerName =
                        Environment.MachineName,

                    OperatingSystem =
                        RuntimeInformation
                            .OSDescription,

                    ProcessorName =
                        GetProcessorName(),

                    TotalMemoryGb =
                        BytesToGb(
                            memory.TotalPhysicalMemory),

                    AvailableMemoryGb =
                        BytesToGb(
                            memory.AvailablePhysicalMemory),

                    SystemDriveTotalGb =
                        systemDrive != null
                            ? BytesToGb(
                                (ulong)
                                systemDrive.TotalSize)
                            : 0,

                    SystemDriveFreeGb =
                        systemDrive != null
                            ? BytesToGb(
                                (ulong)
                                systemDrive.AvailableFreeSpace)
                            : 0,

                    StartupItems =
                        _startupScanner.Scan(),

                    ScanTime =
                        DateTime.Now
                };

            return snapshot;
        });
    }

    private static string GetProcessorName()
    {
        try
        {
            using RegistryKey? key =
                Registry.LocalMachine.OpenSubKey(
                    @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");

            string? processorName =
                key?.GetValue(
                    "ProcessorNameString")
                    ?.ToString();

            return string.IsNullOrWhiteSpace(
                processorName)
                    ? "Unknown processor"
                    : processorName.Trim();
        }
        catch
        {
            return "Unknown processor";
        }
    }

    private static MemoryInfo GetMemoryInfo()
    {
        var status =
            new MemoryStatusEx();

        if (!GlobalMemoryStatusEx(status))
        {
            return new MemoryInfo(0, 0);
        }

        return new MemoryInfo(
            status.TotalPhysicalMemory,
            status.AvailablePhysicalMemory);
    }

    private static double BytesToGb(
        ulong bytes)
    {
        return Math.Round(
            bytes /
            1024d /
            1024d /
            1024d,
            1);
    }

    private readonly record struct MemoryInfo(
        ulong TotalPhysicalMemory,
        ulong AvailablePhysicalMemory);

    [StructLayout(
        LayoutKind.Sequential)]
    private class MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;

        public ulong TotalPhysicalMemory;
        public ulong AvailablePhysicalMemory;

        public ulong TotalPageFile;
        public ulong AvailablePageFile;

        public ulong TotalVirtual;
        public ulong AvailableVirtual;

        public ulong AvailableExtendedVirtual;

        public MemoryStatusEx()
        {
            Length =
                (uint)Marshal.SizeOf(this);
        }
    }

    [DllImport(
        "kernel32.dll",
        SetLastError = true,
        CharSet = CharSet.Auto)]
    [return:
        MarshalAs(UnmanagedType.Bool)]
    private static extern bool
        GlobalMemoryStatusEx(
            [In, Out]
            MemoryStatusEx buffer);
}