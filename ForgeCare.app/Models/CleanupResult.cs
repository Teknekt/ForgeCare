using System.Collections.Generic;
using System.Linq;

namespace ForgeCare.App.Models;

public class CleanupResult
{
    public List<CleanupItem> Items { get; set; } =
        new();

    public long TotalBytes =>
        Items.Sum(x => x.SizeBytes);

    public int TotalFiles =>
        Items.Sum(x => x.FileCount);

    public double TotalGb =>
        TotalBytes / 1024d / 1024d / 1024d;

    public string DisplayTotalSize
    {
        get
        {
            double mb =
                TotalBytes / 1024d / 1024d;

            if (TotalGb >= 1)
            {
                return $"{TotalGb:0.00} GB";
            }

            return $"{mb:0.0} MB";
        }
    }
}