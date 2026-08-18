using System;
using System.Collections.Generic;

namespace ForgeCare.App.Models;

public class SafetySnapshot
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string ComputerName { get; set; } = Environment.MachineName;
    public string Reason { get; set; } = string.Empty;
    public List<StartupUndoRecord> StartupUndoRecords { get; set; } = new();
    public int StartupUndoCount => StartupUndoRecords.Count;
    public string DisplayTime => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
}
