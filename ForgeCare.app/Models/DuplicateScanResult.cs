using System;
using System.Collections.Generic;
using System.Linq;

namespace ForgeCare.App.Models;

public class DuplicateScanResult
{
    public DateTime ScanTime { get; set; }

    public int InspectedFiles { get; set; }

    public int HashedFiles { get; set; }

    public long HashedBytes { get; set; }

    public int SkippedFiles { get; set; }

    public bool HitFileLimit { get; set; }

    public bool HitHashByteLimit { get; set; }

    public List<DuplicateGroup> Groups { get; set; } =
        new();

    public int DuplicateGroupCount =>
        Groups.Count;

    public int DuplicateFileCount =>
        Groups.Sum(group => group.Files.Count);

    public long ReclaimableBytes =>
        Groups.Sum(group => group.ReclaimableBytes);

    public string DisplayReclaimable =>
        FormatBytes(ReclaimableBytes);

    public string DisplayHashedBytes =>
        FormatBytes(HashedBytes);

    private static string FormatBytes(long bytes)
    {
        double gb =
            bytes / 1024d / 1024d / 1024d;

        if (gb >= 1)
            return $"{gb:0.00} GB";

        double mb =
            bytes / 1024d / 1024d;

        if (mb >= 1)
            return $"{mb:0.0} MB";

        return $"{bytes / 1024d:0.0} KB";
    }
}
