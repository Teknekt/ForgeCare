using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ForgeCare.App.Models;
using Microsoft.Win32;

namespace ForgeCare.App.Services;

public class ServiceAnalyzerService
{
    private const uint ScManagerEnumerateService =
        0x0004;

    private const uint ServiceWin32 =
        0x00000030;

    private const uint ServiceStateAll =
        0x00000003;

    private const int ScEnumProcessInfo =
        0;

    private static readonly HashSet<string>
        CriticalServices =
            new(
                StringComparer.OrdinalIgnoreCase)
            {
                "RpcSs",
                "DcomLaunch",
                "EventLog",
                "PlugPlay",
                "Power",
                "ProfSvc",
                "SamSs",
                "Schedule",
                "Winmgmt",
                "LSM",
                "nsi",
                "Dhcp",
                "Dnscache",
                "LanmanWorkstation",
                "CryptSvc"
            };

    private static readonly string[]
        SecuritySignals =
        {
            "defender",
            "security",
            "antivirus",
            "firewall",
            "endpoint",
            "crowdstrike",
            "sentinel",
            "sophos",
            "malwarebytes",
            "carbonblack",
            "cylance"
        };

    private static readonly string[]
        DriverSignals =
        {
            "nvidia",
            "amd",
            "intel",
            "realtek",
            "bluetooth",
            "audio",
            "synaptics",
            "touchpad",
            "logitech",
            "razer",
            "corsair",
            "steelseries",
            "displaylink"
        };

    private static readonly string[]
        OptionalSignals =
        {
            "xbox",
            "xbl",
            "mapsbroker",
            "fax",
            "mixed reality",
            "retaildemo",
            "walletservice",
            "phone service"
        };

    public Task<ServiceAnalysisResult>
        AnalyzeAsync()
    {
        return Task.Run(
            Analyze);
    }

    private ServiceAnalysisResult Analyze()
    {
        var services =
            EnumerateServices()
                .Select(
                    EnrichAndClassify)
                .OrderBy(
                    service =>
                        RecommendationRank(
                            service.Recommendation))
                .ThenByDescending(
                    service =>
                        service.IsRunning)
                .ThenBy(
                    service =>
                        service.DisplayName)
                .ToList();

        var result =
            new ServiceAnalysisResult
            {
                Services =
                    services,

                AnalysisTime =
                    DateTime.Now
            };

        result.Insights =
            BuildInsights(
                result);

        return result;
    }

    private static List<ServiceInfo>
        EnumerateServices()
    {
        IntPtr scm =
            OpenSCManager(
                null,
                null,
                ScManagerEnumerateService);

        if (scm == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "ForgeCare could not open the Windows Service Control Manager.");
        }

        try
        {
            uint bytesNeeded =
                0;

            uint servicesReturned =
                0;

            uint resumeHandle =
                0;

            EnumServicesStatusEx(
                scm,
                ScEnumProcessInfo,
                ServiceWin32,
                ServiceStateAll,
                IntPtr.Zero,
                0,
                out bytesNeeded,
                out servicesReturned,
                ref resumeHandle,
                null);

            if (bytesNeeded == 0)
            {
                return new List<ServiceInfo>();
            }

            IntPtr buffer =
                Marshal.AllocHGlobal(
                    checked((int)bytesNeeded));

            try
            {
                resumeHandle =
                    0;

                bool success =
                    EnumServicesStatusEx(
                        scm,
                        ScEnumProcessInfo,
                        ServiceWin32,
                        ServiceStateAll,
                        buffer,
                        bytesNeeded,
                        out bytesNeeded,
                        out servicesReturned,
                        ref resumeHandle,
                        null);

                if (!success)
                {
                    throw new InvalidOperationException(
                        $"Windows service enumeration failed with error {Marshal.GetLastWin32Error()}.");
                }

                var result =
                    new List<ServiceInfo>(
                        checked((int)servicesReturned));

                int structSize =
                    Marshal.SizeOf<
                        EnumServiceStatusProcess>();

                for (int i = 0;
                     i < servicesReturned;
                     i++)
                {
                    IntPtr current =
                        IntPtr.Add(
                            buffer,
                            i * structSize);

                    var native =
                        Marshal.PtrToStructure<
                            EnumServiceStatusProcess>(
                                current);

                    string name =
                        Marshal.PtrToStringUni(
                            native.ServiceName)
                        ?? string.Empty;

                    string displayName =
                        Marshal.PtrToStringUni(
                            native.DisplayName)
                        ?? name;

                    result.Add(
                        new ServiceInfo
                        {
                            Name =
                                name,

                            DisplayName =
                                string.IsNullOrWhiteSpace(
                                    displayName)
                                    ? name
                                    : displayName,

                            Status =
                                MapServiceState(
                                    native.Status.CurrentState)
                        });
                }

                return result;
            }
            finally
            {
                Marshal.FreeHGlobal(
                    buffer);
            }
        }
        finally
        {
            CloseServiceHandle(
                scm);
        }
    }

    private static ServiceInfo
        EnrichAndClassify(
            ServiceInfo service)
    {
        try
        {
            using var key =
                Registry.LocalMachine
                    .OpenSubKey(
                        $@"SYSTEM\CurrentControlSet\Services\{service.Name}");

            if (key != null)
            {
                int startValue =
                    Convert.ToInt32(
                        key.GetValue(
                            "Start",
                            3));

                int delayed =
                    Convert.ToInt32(
                        key.GetValue(
                            "DelayedAutoStart",
                            0));

                service.StartupType =
                    MapStartupType(
                        startValue,
                        delayed);

                service.ImagePath =
                    key.GetValue(
                        "ImagePath")
                        ?.ToString()
                    ?? string.Empty;

                service.Account =
                    key.GetValue(
                        "ObjectName")
                        ?.ToString()
                    ?? string.Empty;
            }
        }
        catch
        {
            service.StartupType =
                "Unknown";
        }

        string combined =
            $"{service.Name} " +
            $"{service.DisplayName} " +
            $"{service.ImagePath}"
                .ToLowerInvariant();

        if (CriticalServices.Contains(
                service.Name))
        {
            service.Category =
                "SYSTEM CRITICAL";

            service.Recommendation =
                "LEAVE UNCHANGED";

            service.RiskLevel =
                "HIGH";

            service.Reason =
                "Core Windows infrastructure or networking service. " +
                "ForgeCare will not recommend disabling it.";

            return service;
        }

        if (ContainsAny(
                combined,
                SecuritySignals))
        {
            service.Category =
                "SECURITY";

            service.Recommendation =
                "LEAVE UNCHANGED";

            service.RiskLevel =
                "HIGH";

            service.Reason =
                "Looks related to endpoint security, antivirus or firewall functionality.";

            return service;
        }

        if (ContainsAny(
                combined,
                DriverSignals))
        {
            service.Category =
                "DRIVER / HARDWARE";

            service.Recommendation =
                "REVIEW CAREFULLY";

            service.RiskLevel =
                "MEDIUM";

            service.Reason =
                "Looks related to a hardware vendor, device driver or peripheral utility. " +
                "Disabling may remove device features.";

            return service;
        }

        if (ContainsAny(
                combined,
                OptionalSignals))
        {
            service.Category =
                "OPTIONAL FEATURE";

            service.Recommendation =
                "POTENTIALLY OPTIONAL";

            service.RiskLevel =
                "MEDIUM";

            service.Reason =
                "Associated with a Windows feature that may be unnecessary for some systems. " +
                "ForgeCare still requires user-context before recommending a change.";

            return service;
        }

        if (LooksThirdParty(
                service.ImagePath))
        {
            service.Category =
                "APPLICATION";

            service.Recommendation =
                "REVIEW IF UNUSED";

            service.RiskLevel =
                "LOW";

            service.Reason =
                "Looks like a third-party application service. " +
                "It may be worth reviewing if the related software is no longer used.";

            return service;
        }

        if (service.ImagePath.Contains(
                "Windows",
                StringComparison.OrdinalIgnoreCase) ||
            service.ImagePath.Contains(
                "System32",
                StringComparison.OrdinalIgnoreCase))
        {
            service.Category =
                "WINDOWS SERVICE";

            service.Recommendation =
                "LEAVE UNCHANGED";

            service.RiskLevel =
                "MEDIUM";

            service.Reason =
                "Windows-owned service without enough context for a safe optimization recommendation.";

            return service;
        }

        service.Category =
            "UNKNOWN";

        service.Recommendation =
            "UNKNOWN";

        service.RiskLevel =
            "UNKNOWN";

        service.Reason =
            "ForgeCare does not have enough evidence to classify this service safely.";

        return service;
    }

    private static bool LooksThirdParty(
        string imagePath)
    {
        if (string.IsNullOrWhiteSpace(
                imagePath))
        {
            return false;
        }

        string expanded =
            Environment.ExpandEnvironmentVariables(
                imagePath);

        return expanded.Contains(
                   "Program Files",
                   StringComparison.OrdinalIgnoreCase) ||
               expanded.Contains(
                   "ProgramData",
                   StringComparison.OrdinalIgnoreCase) ||
               expanded.Contains(
                   @"AppData\Local",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string MapStartupType(
        int start,
        int delayed)
    {
        return start switch
        {
            0 => "Boot",
            1 => "System",
            2 when delayed == 1 =>
                "Automatic (Delayed)",
            2 => "Automatic",
            3 => "Manual",
            4 => "Disabled",
            _ => "Unknown"
        };
    }

    private static string MapServiceState(
        uint state)
    {
        return state switch
        {
            1 => "STOPPED",
            2 => "START PENDING",
            3 => "STOP PENDING",
            4 => "RUNNING",
            5 => "CONTINUE PENDING",
            6 => "PAUSE PENDING",
            7 => "PAUSED",
            _ => "UNKNOWN"
        };
    }

    private static int RecommendationRank(
        string recommendation)
    {
        return recommendation switch
        {
            "POTENTIALLY OPTIONAL" => 0,
            "REVIEW IF UNUSED" => 1,
            "REVIEW CAREFULLY" => 2,
            "UNKNOWN" => 3,
            "LEAVE UNCHANGED" => 4,
            _ => 5
        };
    }

    private static bool ContainsAny(
        string value,
        IEnumerable<string> signals)
    {
        return signals.Any(
            signal =>
                value.Contains(
                    signal,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static List<ServiceInsight>
        BuildInsights(
            ServiceAnalysisResult result)
    {
        var insights =
            new List<ServiceInsight>();

        insights.Add(
            new ServiceInsight
            {
                Title =
                    $"{result.RunningCount} services are currently running",

                Description =
                    $"ForgeCare found {result.TotalCount} Win32 services: " +
                    $"{result.RunningCount} running and {result.StoppedCount} stopped.",

                Severity =
                    "INFO"
            });

        if (result.DisabledCount > 0)
        {
            insights.Add(
                new ServiceInsight
                {
                    Title =
                        $"{result.DisabledCount} services are already disabled",

                    Description =
                        "Disabled does not automatically mean optimized. " +
                        "ForgeCare records the state but does not assume it should be changed.",

                    Severity =
                        "INFO"
                });
        }

        if (result.ReviewCount > 0)
        {
            insights.Add(
                new ServiceInsight
                {
                    Title =
                        $"{result.ReviewCount} services may deserve contextual review",

                    Description =
                        "These are third-party, hardware-related or optional-feature services. " +
                        "They are candidates for investigation, not automatic disabling.",

                    Severity =
                        "ATTENTION"
                });
        }

        insights.Add(
            new ServiceInsight
            {
                Title =
                    "Unknown is safer than guessing",

                Description =
                    "ForgeCare intentionally labels ambiguous services as UNKNOWN rather than " +
                    "inventing a recommendation from incomplete evidence.",

                Severity =
                    "HEALTHY"
            });

        return insights;
    }

    [StructLayout(
        LayoutKind.Sequential)]
    private struct ServiceStatusProcess
    {
        public uint ServiceType;
        public uint CurrentState;
        public uint ControlsAccepted;
        public uint Win32ExitCode;
        public uint ServiceSpecificExitCode;
        public uint CheckPoint;
        public uint WaitHint;
        public uint ProcessId;
        public uint ServiceFlags;
    }

    [StructLayout(
        LayoutKind.Sequential,
        CharSet = CharSet.Unicode)]
    private struct EnumServiceStatusProcess
    {
        public IntPtr ServiceName;
        public IntPtr DisplayName;
        public ServiceStatusProcess Status;
    }

    [DllImport(
        "advapi32.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    private static extern IntPtr OpenSCManager(
        string? machineName,
        string? databaseName,
        uint desiredAccess);

    [DllImport(
        "advapi32.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    [return: MarshalAs(
        UnmanagedType.Bool)]
    private static extern bool EnumServicesStatusEx(
        IntPtr serviceManager,
        int infoLevel,
        uint serviceType,
        uint serviceState,
        IntPtr services,
        uint bufferSize,
        out uint bytesNeeded,
        out uint servicesReturned,
        ref uint resumeHandle,
        string? groupName);

    [DllImport(
        "advapi32.dll",
        SetLastError = true)]
    [return: MarshalAs(
        UnmanagedType.Bool)]
    private static extern bool CloseServiceHandle(
        IntPtr handle);
}