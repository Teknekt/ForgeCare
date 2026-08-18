using System.Collections.Generic;
using System.Linq;

namespace ForgeCare.App.Models;

public class DuplicateGroup
{
    public string GroupId { get; set; } =
        string.Empty;

    public string Hash { get; set; } =
        string.Empty;

    public long FileSizeBytes { get; set; }

    public List<DuplicateFileInfo> Files { get; set; } =
        new();

    public int CopyCount =>
        Files.Count;

    public long ReclaimableBytes =>
        Files.Count <= 1
            ? 0
            : FileSizeBytes * (Files.Count - 1);

    public string DisplayFileSize =>
        FormatBytes(FileSizeBytes);

    public string DisplayReclaimable =>
        FormatBytes(ReclaimableBytes);

    public string Locations =>
        string.Join(
            " · ",
            Files
                .Select(file => file.Location)
                .Distinct());

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
