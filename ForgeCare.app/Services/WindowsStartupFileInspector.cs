using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class WindowsStartupFileInspector : IStartupFileInspector
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".com", ".dll", ".sys"
        };

    public Task<StartupFileInspection> InspectAsync(
        string resolvedPath,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => Inspect(resolvedPath, cancellationToken), cancellationToken);

    private static StartupFileInspection Inspect(
        string resolvedPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string fullPath = Path.GetFullPath(resolvedPath);
            if (Directory.Exists(fullPath))
            {
                return Basic(
                    StartupFileInspectionStatus.Unsupported,
                    fullPath,
                    true,
                    "The resolved target is a directory, not an inspectable file.");
            }

            if (!File.Exists(fullPath))
            {
                return Basic(
                    StartupFileInspectionStatus.Missing,
                    fullPath,
                    false,
                    null);
            }

            string extension = Path.GetExtension(fullPath);
            if (!SupportedExtensions.Contains(extension))
            {
                return Basic(
                    StartupFileInspectionStatus.Unsupported,
                    fullPath,
                    true,
                    "The file type is not supported for executable metadata inspection.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            FileVersionInfo version = FileVersionInfo.GetVersionInfo(fullPath);

            return new StartupFileInspection
            {
                Status = StartupFileInspectionStatus.Available,
                NormalizedPath = fullPath,
                Exists = true,
                FileName = Path.GetFileName(fullPath),
                Extension = extension,
                FileDescription = NullIfWhiteSpace(version.FileDescription),
                ProductName = NullIfWhiteSpace(version.ProductName),
                CompanyName = NullIfWhiteSpace(version.CompanyName),
                FileVersion = NullIfWhiteSpace(version.FileVersion),
                ProductVersion = NullIfWhiteSpace(version.ProductVersion),
                OriginalFilename = NullIfWhiteSpace(version.OriginalFilename)
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            return Basic(
                StartupFileInspectionStatus.AccessDenied,
                resolvedPath,
                null,
                $"File inspection was denied ({ex.GetType().Name}).");
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or
                                   NotSupportedException or Win32Exception)
        {
            return Basic(
                StartupFileInspectionStatus.InspectionFailure,
                resolvedPath,
                null,
                $"File inspection failed ({ex.GetType().Name}).");
        }
    }

    private static StartupFileInspection Basic(
        StartupFileInspectionStatus status,
        string path,
        bool? exists,
        string? warning) =>
        new()
        {
            Status = status,
            NormalizedPath = path,
            Exists = exists,
            FileName = Path.GetFileName(path),
            Extension = Path.GetExtension(path),
            Warning = warning
        };

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
