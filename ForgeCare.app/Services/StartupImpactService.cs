using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public class StartupImpactService
{
    public StartupImpactResult Analyze(
        IEnumerable<StartupItem> startupItems)
    {
        var result =
            new StartupImpactResult();

        foreach (var item in startupItems)
        {
            result.Items.Add(
                AnalyzeItem(item));
        }

        result.Items =
            result.Items
                .OrderByDescending(item =>
                    item.ImpactScore)
                .ThenBy(item =>
                    item.Name)
                .ToList();

        return result;
    }

    private StartupImpactItem AnalyzeItem(
        StartupItem item)
    {
        string combined =
            $"{item.Name} {item.Command}"
                .ToLowerInvariant();

        string executablePath =
            TryExtractExecutablePath(
                item.Command);

        if (LooksSystemCritical(
                combined,
                executablePath))
        {
            return Build(
                item,
                category: "SYSTEM / DRIVER",
                impact: 15,
                recommendation: "KEEP",
                reason:
                    "Looks like a Windows, security, hardware or driver component. " +
                    "ForgeCare will not recommend disabling it automatically.",
                confidence: "HIGH");
        }

        if (ContainsAny(
                combined,
                "defender",
                "securityhealth",
                "antivirus",
                "endpoint",
                "crowdstrike",
                "sentinel",
                "sophos",
                "malwarebytes"))
        {
            return Build(
                item,
                category: "SECURITY",
                impact: 10,
                recommendation: "KEEP",
                reason:
                    "Security software should normally remain available at startup.",
                confidence: "HIGH");
        }

        if (ContainsAny(
                combined,
                "onedrive",
                "dropbox",
                "google drive",
                "googledrive",
                "icloud"))
        {
            return Build(
                item,
                category: "SYNC",
                impact: 48,
                recommendation: "REVIEW",
                reason:
                    "Cloud-sync software can add background load, but disabling startup " +
                    "may delay file synchronization until the app is opened manually.",
                confidence: "HIGH");
        }

        if (ContainsAny(
                combined,
                "discord",
                "slack",
                "spotify",
                "steam",
                "epicgames",
                "epic games",
                "battle.net",
                "battlenet",
                "whatsapp",
                "telegram",
                "teams",
                "zoom"))
        {
            return Build(
                item,
                category: "USER APP",
                impact: 74,
                recommendation: "GOOD CANDIDATE",
                reason:
                    "This type of user application often does not need to launch with Windows. " +
                    "Starting it manually can reduce login-time background load.",
                confidence: "HIGH");
        }

        if (ContainsAny(
                combined,
                "adobe",
                "creative cloud",
                "ccxprocess",
                "acrotray"))
        {
            return Build(
                item,
                category: "CREATIVE / HELPER",
                impact: 68,
                recommendation: "GOOD CANDIDATE",
                reason:
                    "Creative-suite helpers and launchers can add background processes. " +
                    "Core applications can usually still be opened manually.",
                confidence: "MEDIUM");
        }

        if (ContainsAny(
                combined,
                "updater",
                "update",
                "launcher",
                "helper",
                "assistant",
                "tray",
                "quickstart",
                "quick start"))
        {
            return Build(
                item,
                category: "HELPER / UPDATER",
                impact: 58,
                recommendation: "REVIEW",
                reason:
                    "The entry looks like a helper, tray app, launcher or updater. " +
                    "Review whether it needs to run immediately after sign-in.",
                confidence: "MEDIUM");
        }

        if (item.Source.Contains(
                "Startup Folder",
                StringComparison.OrdinalIgnoreCase))
        {
            return Build(
                item,
                category: "STARTUP FOLDER",
                impact: 52,
                recommendation: "REVIEW",
                reason:
                    "The application is explicitly placed in a Windows Startup folder. " +
                    "Review whether automatic launch is still useful.",
                confidence: "MEDIUM");
        }

        if (item.Source.Contains(
                "Local Machine",
                StringComparison.OrdinalIgnoreCase))
        {
            return Build(
                item,
                category: "MACHINE STARTUP",
                impact: 30,
                recommendation: "REVIEW",
                reason:
                    "This entry applies at machine level. ForgeCare treats machine-wide " +
                    "startup entries more cautiously because they may support hardware or shared software.",
                confidence: "LOW");
        }

        return Build(
            item,
            category: "UNCLASSIFIED",
            impact: 38,
            recommendation: "REVIEW",
            reason:
                "ForgeCare does not have enough evidence to classify this entry confidently. " +
                "Inspect the command and application purpose before changing it.",
            confidence: "LOW");
    }

    private static StartupImpactItem Build(
        StartupItem item,
        string category,
        int impact,
        string recommendation,
        string reason,
        string confidence)
    {
        return new StartupImpactItem
        {
            Name = item.Name,
            Source = item.Source,
            Command = item.Command,
            Category = category,
            ImpactScore = impact,
            ImpactLevel = impact switch
            {
                >= 70 => "HIGH",
                >= 45 => "MEDIUM",
                >= 20 => "LOW",
                _ => "MINIMAL"
            },
            Recommendation = recommendation,
            Reason = reason,
            Confidence = confidence
        };
    }

    private static bool LooksSystemCritical(
        string combined,
        string executablePath)
    {
        if (ContainsAny(
                combined,
                "windows defender",
                "securityhealth",
                "realtek",
                "synaptics",
                "touchpad",
                "intel graphics",
                "nvidia container",
                "amd software",
                "audio driver",
                "bluetooth"))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(
                executablePath))
        {
            return false;
        }

        string windows =
            Environment.GetFolderPath(
                Environment.SpecialFolder.Windows);

        string system =
            Environment.GetFolderPath(
                Environment.SpecialFolder.System);

        try
        {
            string full =
                Path.GetFullPath(
                    Environment.ExpandEnvironmentVariables(
                        executablePath));

            if (!string.IsNullOrWhiteSpace(system) &&
                IsInside(full, system))
            {
                return true;
            }

            // Do NOT treat all of C:\Windows as critical, only the system
            // locations and entries with explicit driver/security signals.
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsInside(
        string path,
        string root)
    {
        string normalizedPath =
            Path.GetFullPath(path)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);

        string normalizedRoot =
            Path.GetFullPath(root)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);

        return normalizedPath.Equals(
                   normalizedRoot,
                   StringComparison.OrdinalIgnoreCase) ||
               normalizedPath.StartsWith(
                   normalizedRoot +
                   Path.DirectorySeparatorChar,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string TryExtractExecutablePath(
        string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return string.Empty;
        }

        string expanded =
            Environment.ExpandEnvironmentVariables(
                command.Trim());

        if (expanded.StartsWith("\""))
        {
            int closingQuote =
                expanded.IndexOf(
                    '"',
                    1);

            if (closingQuote > 1)
            {
                return expanded.Substring(
                    1,
                    closingQuote - 1);
            }
        }

        int exeIndex =
            expanded.IndexOf(
                ".exe",
                StringComparison.OrdinalIgnoreCase);

        if (exeIndex >= 0)
        {
            return expanded.Substring(
                0,
                exeIndex + 4)
                .Trim()
                .Trim('"');
        }

        return string.Empty;
    }

    private static bool ContainsAny(
        string value,
        params string[] needles)
    {
        return needles.Any(
            needle =>
                value.Contains(
                    needle,
                    StringComparison.OrdinalIgnoreCase));
    }
}