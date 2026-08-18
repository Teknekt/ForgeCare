using System.Collections.Generic;

namespace ForgeCare.App.Models;

public sealed class StabilityRecoveryResult
{
    public string State { get; set; } = "READY";
    public bool PreviousSessionUnclean { get; set; }
    public int StalePartialFileCount { get; set; }
    public int StaleStagingDirectoryCount { get; set; }
    public long RecoverableTransientBytes { get; set; }
    public List<string> Findings { get; set; } = new();

    public string RecoverableSizeText =>
        RecoverableTransientBytes >= 1024L * 1024L
            ? $"{RecoverableTransientBytes / 1024d / 1024d:0.0} MB"
            : $"{RecoverableTransientBytes / 1024d:0.0} KB";
}
