using System.IO;
using System.Text.Json;
using NoteNest.Models;

namespace NoteNest.Services;

public class UiSettings
{
    public string LastSearchText { get; set; } = "";
    public string LastReplaceText { get; set; } = "";
    public double? FindReplaceLeft { get; set; }
    public double? FindReplaceTop { get; set; }
    public bool ShowLineNumbers { get; set; } = false;
    public AppTheme Theme { get; set; } = AppTheme.Light;
}

public class UiSettingsService
{
    private static readonly string DataPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "NoteNest", "ui-settings.json");

    public UiSettings Load()
    {
        try
        {
            if (!File.Exists(DataPath)) return new();
            return JsonSerializer.Deserialize<UiSettings>(File.ReadAllText(DataPath)) ?? new();
        }
        catch { return new(); }
    }

    public void Save(UiSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DataPath)!);
            File.WriteAllText(DataPath, JsonSerializer.Serialize(settings,
                new JsonSerializerOptions { WriteIndented = false }));
        }
        catch { }
    }
}
