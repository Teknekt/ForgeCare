using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public class DuplicateScannerService
{
    private const long MinimumFileSize = 10L * 1024L * 1024L;
    private const int MaxFilesToInspect = 80_000;
    private const long MaxBytesToHash = 120L * 1024L * 1024L * 1024L;
    private const int FingerprintBlockSize = 64 * 1024;

    public Task<DuplicateScanResult> ScanAsync(
        IProgress<DuplicateScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () => Scan(progress, cancellationToken),
            cancellationToken);
    }

    private DuplicateScanResult Scan(
        IProgress<DuplicateScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        var result = new DuplicateScanResult { ScanTime = DateTime.Now };
        var candidates = new List<FileCandidate>();
        int inspected = 0;

        Report(progress, "Discovering files", inspected, candidates.Count, 0, 0, 0, 0, string.Empty);

        foreach (var target in BuildTargets())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (inspected >= MaxFilesToInspect)
            {
                result.HitFileLimit = true;
                break;
            }

            ScanTarget(
                target.Name,
                target.Path,
                candidates,
                result,
                ref inspected,
                progress,
                cancellationToken);
        }

        result.InspectedFiles = inspected;

        var sameSizeGroups = candidates
            .GroupBy(x => x.SizeBytes)
            .Where(g => g.Count() > 1)
            .OrderByDescending(g => g.Key)
            .ToList();

        var fingerprintGroups = new List<List<FileCandidate>>();

        foreach (var sizeGroup in sameSizeGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var quickGroups = new Dictionary<string, List<FileCandidate>>(StringComparer.OrdinalIgnoreCase);

            foreach (var candidate in sizeGroup)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    string fingerprint = ComputeQuickFingerprint(candidate.FullPath, candidate.SizeBytes);
                    if (!quickGroups.TryGetValue(fingerprint, out var group))
                    {
                        group = new List<FileCandidate>();
                        quickGroups[fingerprint] = group;
                    }
                    group.Add(candidate);
                }
                catch
                {
                    result.SkippedFiles++;
                }
            }

            fingerprintGroups.AddRange(
                quickGroups.Values.Where(g => g.Count > 1));
        }

        long plannedHashBytes = fingerprintGroups
            .SelectMany(g => g)
            .Sum(x => x.SizeBytes);

        if (plannedHashBytes > MaxBytesToHash)
            plannedHashBytes = MaxBytesToHash;

        long hashedBytes = 0;
        var hashGroups = new Dictionary<string, List<FileCandidate>>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in fingerprintGroups)
        {
            foreach (var candidate in group)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (hashedBytes + candidate.SizeBytes > MaxBytesToHash)
                {
                    result.HitHashByteLimit = true;
                    break;
                }

                try
                {
                    Report(
                        progress,
                        "Verifying exact matches",
                        inspected,
                        candidates.Count,
                        result.HashedFiles,
                        hashedBytes,
                        plannedHashBytes,
                        Percent(hashedBytes, plannedHashBytes),
                        candidate.FullPath);

                    string hash = ComputeSha256(candidate.FullPath, cancellationToken);
                    hashedBytes += candidate.SizeBytes;
                    result.HashedFiles++;

                    string key = $"{candidate.SizeBytes}:{hash}";
                    if (!hashGroups.TryGetValue(key, out var exact))
                    {
                        exact = new List<FileCandidate>();
                        hashGroups[key] = exact;
                    }
                    exact.Add(candidate);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    result.SkippedFiles++;
                }
            }

            if (result.HitHashByteLimit)
                break;
        }

        result.HashedBytes = hashedBytes;
        int groupNumber = 1;

        result.Groups = hashGroups
            .Where(pair => pair.Value.Count > 1)
            .Select(pair =>
            {
                string groupId = $"DUP-{groupNumber:000}";
                var files = pair.Value
                    .OrderByDescending(file => file.LastWriteTime)
                    .Select(file => new DuplicateFileInfo
                    {
                        GroupId = groupId,
                        Name = Path.GetFileName(file.FullPath),
                        FullPath = file.FullPath,
                        Location = file.Location,
                        SizeBytes = file.SizeBytes,
                        LastWriteTime = file.LastWriteTime
                    })
                    .ToList();

                groupNumber++;
                string hash = pair.Key[(pair.Key.IndexOf(':') + 1)..];

                return new DuplicateGroup
                {
                    GroupId = groupId,
                    Hash = hash,
                    FileSizeBytes = pair.Value[0].SizeBytes,
                    Files = files
                };
            })
            .OrderByDescending(group => group.ReclaimableBytes)
            .ToList();

        Report(progress, "Complete", inspected, candidates.Count, result.HashedFiles,
            hashedBytes, plannedHashBytes, 100, string.Empty);

        return result;
    }

    private static void ScanTarget(
        string location,
        string path,
        List<FileCandidate> candidates,
        DuplicateScanResult result,
        ref int inspected,
        IProgress<DuplicateScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return;

        var pending = new Stack<string>();
        pending.Push(path);

        while (pending.Count > 0 && inspected < MaxFilesToInspect)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string current = pending.Pop();

            try
            {
                var directoryInfo = new DirectoryInfo(current);
                if (directoryInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    continue;

                foreach (var filePath in Directory.EnumerateFiles(current))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (inspected >= MaxFilesToInspect)
                    {
                        result.HitFileLimit = true;
                        return;
                    }

                    inspected++;

                    try
                    {
                        var file = new FileInfo(filePath);
                        if (file.Length >= MinimumFileSize)
                        {
                            candidates.Add(new FileCandidate
                            {
                                FullPath = file.FullName,
                                Location = location,
                                SizeBytes = file.Length,
                                LastWriteTime = file.LastWriteTime
                            });
                        }
                    }
                    catch
                    {
                        result.SkippedFiles++;
                    }

                    if (inspected % 250 == 0)
                    {
                        Report(progress, "Discovering files", inspected, candidates.Count,
                            0, 0, 0, 0, filePath);
                    }
                }

                foreach (var child in Directory.EnumerateDirectories(current))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var childInfo = new DirectoryInfo(child);
                        if (!childInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                            pending.Push(child);
                    }
                    catch
                    {
                        result.SkippedFiles++;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                result.SkippedFiles++;
            }
        }
    }

    private static string ComputeQuickFingerprint(string filePath, long length)
    {
        using var stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: FingerprintBlockSize,
            options: FileOptions.SequentialScan);

        using var sha = SHA256.Create();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(FingerprintBlockSize);

        try
        {
            int firstRead = stream.Read(buffer, 0, FingerprintBlockSize);
            sha.TransformBlock(buffer, 0, firstRead, null, 0);

            if (length > FingerprintBlockSize)
            {
                stream.Seek(Math.Max(0, length - FingerprintBlockSize), SeekOrigin.Begin);
                int lastRead = stream.Read(buffer, 0, FingerprintBlockSize);
                sha.TransformBlock(buffer, 0, lastRead, null, 0);
            }

            byte[] sizeBytes = BitConverter.GetBytes(length);
            sha.TransformFinalBlock(sizeBytes, 0, sizeBytes.Length);
            return Convert.ToHexString(sha.Hash!);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static string ComputeSha256(string filePath, CancellationToken cancellationToken)
    {
        using var stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 1024 * 1024,
            options: FileOptions.SequentialScan);

        using var sha = SHA256.Create();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);

        try
        {
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                sha.TransformBlock(buffer, 0, read, null, 0);
            }

            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return Convert.ToHexString(sha.Hash!);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static double Percent(long current, long total) =>
        total <= 0 ? 0 : Math.Min(100d, (double)current / total * 100d);

    private static void Report(
        IProgress<DuplicateScanProgress>? progress,
        string stage,
        int inspected,
        int candidates,
        int hashed,
        long hashedBytes,
        long plannedHashBytes,
        double percent,
        string currentFile)
    {
        progress?.Report(new DuplicateScanProgress
        {
            Stage = stage,
            InspectedFiles = inspected,
            CandidateFiles = candidates,
            HashedFiles = hashed,
            HashedBytes = hashedBytes,
            PlannedHashBytes = plannedHashBytes,
            Percent = percent,
            CurrentFile = currentFile
        });
    }

    private static List<(string Name, string Path)> BuildTargets()
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return new List<(string, string)>
        {
            ("Downloads", Path.Combine(userProfile, "Downloads")),
            ("Desktop", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)),
            ("Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),
            ("Pictures", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)),
            ("Videos", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
        }
        .Where(target => !string.IsNullOrWhiteSpace(target.Item2))
        .GroupBy(target => target.Item2, StringComparer.OrdinalIgnoreCase)
        .Select(group => group.First())
        .ToList();
    }

    private sealed class FileCandidate
    {
        public string FullPath { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime LastWriteTime { get; set; }
    }
}
