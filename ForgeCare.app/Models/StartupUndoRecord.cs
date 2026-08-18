namespace ForgeCare.App.Models;

public class StartupUndoRecord
{
    public string Name { get; set; } = string.Empty;
    public string HandlerType { get; set; } = string.Empty;

    public string RegistryPath { get; set; } = string.Empty;
    public string RegistryValueName { get; set; } = string.Empty;
    public string RegistryValueData { get; set; } = string.Empty;
    public int RegistryValueKind { get; set; }

    public string OriginalFilePath { get; set; } = string.Empty;
    public string DisabledFilePath { get; set; } = string.Empty;
    public string CreatedUtc { get; set; } = string.Empty;
}
