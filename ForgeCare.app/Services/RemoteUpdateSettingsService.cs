using System;
using System.IO;
using System.Text;
using System.Text.Json;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class RemoteUpdateSettingsService
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _json =
        new()
        {
            WriteIndented = true
        };

    public RemoteUpdateSettingsService()
    {
        string root =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "ForgeCare",
                "Settings");

        _settingsPath =
            Path.Combine(
                root,
                "remote-update.json");
    }

    public RemoteUpdateSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return new RemoteUpdateSettings();

            return JsonSerializer.Deserialize<RemoteUpdateSettings>(
                       File.ReadAllText(_settingsPath),
                       _json)
                   ?? new RemoteUpdateSettings();
        }
        catch
        {
            return new RemoteUpdateSettings();
        }
    }

    public void Save(
        RemoteUpdateSettings settings)
    {
        string? directory =
            Path.GetDirectoryName(
                _settingsPath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(
            _settingsPath,
            JsonSerializer.Serialize(
                settings,
                _json),
            Encoding.UTF8);
    }
}
