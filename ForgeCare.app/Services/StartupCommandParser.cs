using System;
using System.Collections.Generic;
using System.IO;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class StartupCommandParser
{
    private static readonly HashSet<string> Launchers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "cmd", "cmd.exe",
            "powershell", "powershell.exe",
            "pwsh", "pwsh.exe",
            "rundll32", "rundll32.exe",
            "regsvr32", "regsvr32.exe",
            "wscript", "wscript.exe",
            "cscript", "cscript.exe"
        };

    private static readonly HashSet<string> ExecutableExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".com"
        };

    private static readonly HashSet<string> DirectFileExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".dll", ".sys", ".cmd", ".bat", ".ps1", ".vbs", ".js"
        };

    private readonly Func<string, string> _expandEnvironmentVariables;

    public StartupCommandParser(
        Func<string, string>? expandEnvironmentVariables = null)
    {
        _expandEnvironmentVariables = expandEnvironmentVariables ??
            Environment.ExpandEnvironmentVariables;
    }

    public StartupCommandResolution Parse(
        string? command,
        StartupSourceKind sourceKind)
    {
        string original = command ?? string.Empty;
        if (string.IsNullOrWhiteSpace(original))
        {
            return Result(
                original,
                StartupCommandResolutionStatus.Empty,
                "The startup command was empty, so no target identity could be established.");
        }

        string trimmed = original.Trim();
        string expanded;
        try
        {
            expanded = _expandEnvironmentVariables(trimmed);
        }
        catch (Exception ex)
        {
            return Result(
                original,
                StartupCommandResolutionStatus.Ambiguous,
                $"Environment expansion could not be completed ({ex.GetType().Name}).");
        }

        bool expansionApplied = !string.Equals(trimmed, expanded, StringComparison.Ordinal);
        if (ContainsUnexpandedEnvironmentToken(expanded))
        {
            return Result(
                original,
                StartupCommandResolutionStatus.Ambiguous,
                "The command contains an environment variable that could not be resolved.",
                expansionApplied: expansionApplied);
        }

        if (expanded.StartsWith('"'))
            return ParseQuoted(original, expanded, sourceKind, expansionApplied);

        // StartupScanner supplies Startup-folder entries as the direct file path,
        // not as a registry-style command line. Preserve that stronger boundary.
        if (sourceKind is StartupSourceKind.UserStartupFolder or
                StartupSourceKind.CommonStartupFolder)
        {
            return ResolveCandidate(
                original,
                expanded,
                null,
                sourceKind,
                expansionApplied);
        }

        int whitespace = IndexOfWhitespace(expanded);
        string candidate = whitespace < 0 ? expanded : expanded[..whitespace];
        string arguments = whitespace < 0 ? string.Empty : expanded[whitespace..].Trim();

        if (IsLauncher(candidate))
        {
            return Result(
                original,
                StartupCommandResolutionStatus.LauncherMediated,
                $"The command uses {Path.GetFileName(candidate)} as a launcher; its payload was not resolved.",
                arguments: NullIfEmpty(arguments),
                launcher: Path.GetFileName(candidate),
                expansionApplied: expansionApplied);
        }

        if (whitespace >= 0)
        {
            return Result(
                original,
                StartupCommandResolutionStatus.Ambiguous,
                "The unquoted command contains spaces, so ForgeCare did not guess the executable boundary.",
                expansionApplied: expansionApplied);
        }

        return ResolveCandidate(
            original,
            expanded,
            null,
            sourceKind,
            expansionApplied);
    }

    private StartupCommandResolution ParseQuoted(
        string original,
        string expanded,
        StartupSourceKind sourceKind,
        bool expansionApplied)
    {
        int closingQuote = expanded.IndexOf('"', 1);
        if (closingQuote < 0)
        {
            return Result(
                original,
                StartupCommandResolutionStatus.Malformed,
                "The quoted startup target has no closing quote.",
                expansionApplied: expansionApplied);
        }

        string candidate = expanded[1..closingQuote];
        string remainder = expanded[(closingQuote + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return Result(
                original,
                StartupCommandResolutionStatus.Malformed,
                "The quoted startup target was empty.",
                expansionApplied: expansionApplied);
        }

        if (remainder.StartsWith('"'))
        {
            return Result(
                original,
                StartupCommandResolutionStatus.Malformed,
                "The command contains an unexpected quote after the target.",
                expansionApplied: expansionApplied);
        }

        if (IsLauncher(candidate))
        {
            return Result(
                original,
                StartupCommandResolutionStatus.LauncherMediated,
                $"The command uses {Path.GetFileName(candidate)} as a launcher; its payload was not resolved.",
                arguments: NullIfEmpty(remainder),
                launcher: Path.GetFileName(candidate),
                expansionApplied: expansionApplied);
        }

        return ResolveCandidate(
            original,
            candidate,
            NullIfEmpty(remainder),
            sourceKind,
            expansionApplied);
    }

    private static StartupCommandResolution ResolveCandidate(
        string original,
        string candidate,
        string? arguments,
        StartupSourceKind sourceKind,
        bool expansionApplied)
    {
        string extension = Path.GetExtension(candidate);
        if ((sourceKind is StartupSourceKind.UserStartupFolder or
                StartupSourceKind.CommonStartupFolder) &&
            extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            return Result(
                original,
                StartupCommandResolutionStatus.ShortcutNotResolved,
                "The Startup-folder entry is a shortcut. Phase B did not resolve its target.",
                arguments: arguments,
                expansionApplied: expansionApplied);
        }

        if (!Path.IsPathFullyQualified(candidate))
        {
            return Result(
                original,
                StartupCommandResolutionStatus.Ambiguous,
                "The command did not contain a fully qualified direct target, and PATH search was not used.",
                arguments: arguments,
                expansionApplied: expansionApplied);
        }

        try
        {
            string fullPath = Path.GetFullPath(candidate);
            if (ExecutableExtensions.Contains(extension))
            {
                return Result(
                    original,
                    StartupCommandResolutionStatus.DirectExecutable,
                    "A fully qualified direct executable target was resolved for inspection.",
                    fullPath,
                    arguments,
                    expansionApplied: expansionApplied);
            }

            if (DirectFileExtensions.Contains(extension))
            {
                return Result(
                    original,
                    StartupCommandResolutionStatus.DirectFile,
                    "A fully qualified direct file target was resolved for inspection.",
                    fullPath,
                    arguments,
                    expansionApplied: expansionApplied);
            }

            return Result(
                original,
                StartupCommandResolutionStatus.Unsupported,
                "The direct target uses a file type that Phase B does not inspect.",
                arguments: arguments,
                expansionApplied: expansionApplied);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return Result(
                original,
                StartupCommandResolutionStatus.Malformed,
                "The direct target path was malformed.",
                arguments: arguments,
                expansionApplied: expansionApplied);
        }
    }

    public static StartupSourceKind MapSource(string? source) =>
        source switch
        {
            "Current User Registry" => StartupSourceKind.CurrentUserRegistry,
            "Local Machine Registry" => StartupSourceKind.LocalMachineRegistry,
            "User Startup Folder" => StartupSourceKind.UserStartupFolder,
            "Common Startup Folder" => StartupSourceKind.CommonStartupFolder,
            _ => StartupSourceKind.Unknown
        };

    private static bool IsLauncher(string candidate) =>
        Launchers.Contains(candidate) ||
        Launchers.Contains(Path.GetFileName(candidate));

    private static bool ContainsUnexpandedEnvironmentToken(string value)
    {
        int opening = value.IndexOf('%');
        return opening >= 0 && value.IndexOf('%', opening + 1) > opening;
    }

    private static int IndexOfWhitespace(string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            if (char.IsWhiteSpace(value[index]))
                return index;
        }

        return -1;
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static StartupCommandResolution Result(
        string original,
        StartupCommandResolutionStatus status,
        string rationale,
        string? path = null,
        string? arguments = null,
        string? launcher = null,
        bool expansionApplied = false) =>
        new()
        {
            OriginalCommand = original,
            Status = status,
            ResolvedPath = path,
            Arguments = arguments,
            LauncherName = launcher,
            EnvironmentExpansionApplied = expansionApplied,
            Rationale = rationale
        };
}
