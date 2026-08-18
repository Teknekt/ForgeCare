using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ForgeCare.App.Services;

public sealed class BetaDiagnosticsService
{
    public string DataRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ForgeCare");

    public string GetEnvironmentSummary()
    {
        string version =
            Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
            ?? "unknown";

        return
            $"ForgeCare {version}{Environment.NewLine}" +
            $"Machine: {Environment.MachineName}{Environment.NewLine}" +
            $"Windows: {RuntimeInformation.OSDescription}{Environment.NewLine}" +
            $"Process: {RuntimeInformation.ProcessArchitecture}{Environment.NewLine}" +
            $"OS architecture: {RuntimeInformation.OSArchitecture}{Environment.NewLine}" +
            $".NET: {RuntimeInformation.FrameworkDescription}{Environment.NewLine}" +
            $"64-bit process: {Environment.Is64BitProcess}{Environment.NewLine}" +
            $"CPU count: {Environment.ProcessorCount}{Environment.NewLine}" +
            $"Working set: {Environment.WorkingSet / 1024d / 1024d:0.0} MB{Environment.NewLine}" +
            $"Data root: {DataRoot}";
    }

    public string ExportDebugBundle(string requestedZipPath)
    {
        string fullZip = Path.GetFullPath(requestedZipPath);
        string? destination = Path.GetDirectoryName(fullZip);

        if (string.IsNullOrWhiteSpace(destination))
            throw new InvalidOperationException("Could not resolve the debug bundle destination.");

        Directory.CreateDirectory(destination);
        Directory.CreateDirectory(CrashLogService.DiagnosticsRoot);

        string staging = Path.Combine(
            CrashLogService.DiagnosticsRoot,
            "bundle-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));

        if (Directory.Exists(staging))
            Directory.Delete(staging, true);

        Directory.CreateDirectory(staging);

        File.WriteAllText(
            Path.Combine(staging, "environment.txt"),
            GetEnvironmentSummary(),
            Encoding.UTF8);

        CopyIfExists(
            CrashLogService.CrashLogPath,
            Path.Combine(staging, "crash.log"));

        foreach (string folderName in new[] { "Settings", "Reports", "Safety" })
        {
            string source = Path.Combine(DataRoot, folderName);
            if (!Directory.Exists(source))
                continue;

            string target = Path.Combine(staging, folderName);
            CopyDirectoryBestEffort(source, target);
        }

        if (File.Exists(fullZip))
            File.Delete(fullZip);

        ZipFile.CreateFromDirectory(
            staging,
            fullZip,
            CompressionLevel.Optimal,
            includeBaseDirectory: false);

        Directory.Delete(staging, true);
        return fullZip;
    }

    private static void CopyDirectoryBestEffort(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            try
            {
                string relative = Path.GetRelativePath(source, file);
                string output = Path.Combine(target, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                File.Copy(file, output, true);
            }
            catch
            {
            }
        }
    }

    private static void CopyIfExists(string source, string target)
    {
        try
        {
            if (File.Exists(source))
                File.Copy(source, target, true);
        }
        catch
        {
        }
    }
}
