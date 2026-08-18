using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ForgeCare.App.Services;

public sealed class UxStateService
{
    private readonly string _path;

    public UxStateService()
    {
        _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ForgeCare", "Settings", "ux-state.json");
    }

    public string LoadLastTab()
    {
        try
        {
            if (!File.Exists(_path))
                return "DASHBOARD";

            Dictionary<string, string>? state =
                JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(_path));

            return state != null &&
                   state.TryGetValue("lastTab", out string? tab) &&
                   !string.IsNullOrWhiteSpace(tab)
                ? tab
                : "DASHBOARD";
        }
        catch
        {
            return "DASHBOARD";
        }
    }

    public void SaveLastTab(string header)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(
                _path,
                JsonSerializer.Serialize(
                    new Dictionary<string, string> { ["lastTab"] = header },
                    new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
        }
    }
}
