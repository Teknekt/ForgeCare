namespace ForgeCare.App.Models;

public class DuplicateScanProgress
{
    public string Stage { get; set; } = "Preparing";
    public int InspectedFiles { get; set; }
    public int CandidateFiles { get; set; }
    public int HashedFiles { get; set; }
    public long HashedBytes { get; set; }
    public long PlannedHashBytes { get; set; }
    public double Percent { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
}
