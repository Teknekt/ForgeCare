using System;
using System.Collections.Generic;

namespace ForgeCare.App.Models;

public class SystemSnapshot
{
    public string ComputerName { get; set; } = string.Empty;

    public string OperatingSystem { get; set; } = string.Empty;

    public string ProcessorName { get; set; } = string.Empty;

    public double TotalMemoryGb { get; set; }

    public double AvailableMemoryGb { get; set; }

    public double SystemDriveTotalGb { get; set; }

    public double SystemDriveFreeGb { get; set; }

    public List<StartupItem> StartupItems { get; set; } = new();

    public DateTime ScanTime { get; set; }
}