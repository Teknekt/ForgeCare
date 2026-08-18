using System;
using System.IO;
using System.Text.Json;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class ForgeCareSettingsService
{
    private readonly string _directory;
    private readonly string _settingsFile;

    private readonly JsonSerializerOptions _json =
        new()
        {
            WriteIndented = true
        };

    public ForgeCareSettingsService()
    {
        _directory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "ForgeCare",
                "Settings");

        _settingsFile =
            Path.Combine(
                _directory,
                "settings.json");
    }

    public string SettingsFilePath =>
        _settingsFile;

    public string DataRoot =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "ForgeCare");

    public ForgeCareSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsFile))
                return new ForgeCareSettings();

            string json =
                File.ReadAllText(
                    _settingsFile);

            return
                JsonSerializer.Deserialize<ForgeCareSettings>(
                    json,
                    _json)
                ?? new ForgeCareSettings();
        }
        catch
        {
            return new ForgeCareSettings();
        }
    }

    public void Save(
        ForgeCareSettings settings)
    {
        Directory.CreateDirectory(
            _directory);

        string json =
            JsonSerializer.Serialize(
                settings,
                _json);

        File.WriteAllText(
            _settingsFile,
            json);
    }

    public void Reset()
    {
        if (File.Exists(_settingsFile))
            File.Delete(_settingsFile);
    }
}
