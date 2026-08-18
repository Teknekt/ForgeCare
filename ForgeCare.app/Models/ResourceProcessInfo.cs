namespace ForgeCare.App.Models;

public class ResourceProcessInfo
{
    public int ProcessId { get; set; }

    public string Name { get; set; } =
        string.Empty;

    public double CpuPercent { get; set; }

    public double MemoryMb { get; set; }

    public double MemoryPercent { get; set; }

    public int PressureScore { get; set; }

    public string PressureLevel { get; set; } =
        string.Empty;

    public string PrimaryResource { get; set; } =
        string.Empty;

    public string DisplayCpu =>
        $"{CpuPercent:0.0}%";

    public string DisplayMemory =>
        MemoryMb >= 1024
            ? $"{MemoryMb / 1024d:0.00} GB"
            : $"{MemoryMb:0} MB";
}