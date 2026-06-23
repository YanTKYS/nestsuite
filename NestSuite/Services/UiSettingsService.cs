using System.IO;
using System.Text.Json;
using NestSuite.Models;

namespace NestSuite.Services;

public class UiSettings
{
    public string LastSearchText { get; set; } = "";
    public string LastReplaceText { get; set; } = "";
    public double? FindReplaceLeft { get; set; }
    public double? FindReplaceTop { get; set; }
    public bool ShowLineNumbers { get; set; } = false;
    public AppTheme Theme { get; set; } = AppTheme.Light;
    public int MarkerSortOrderIndex { get; set; } = 0;
    public double WindowWidth { get; set; } = 1100;
    public double WindowHeight { get; set; } = 720;
    public bool IsWindowMaximized { get; set; } = false;
    public double LeftPaneWidth { get; set; } = 220;
    public double RightPaneWidth { get; set; } = 280;
    public bool IsRightPaneCollapsed { get; set; } = false;
    public bool IsAutoSaveEnabled { get; set; } = false;
    public double NestSuiteWindowWidth { get; set; } = 1280;
    public double NestSuiteWindowHeight { get; set; } = 720;
    public bool NestSuiteIsWindowMaximized { get; set; } = false;
    public double? NestSuiteWindowLeft { get; set; }
    public double? NestSuiteWindowTop { get; set; }
    public double NoteNestEditorFontSize { get; set; } = 14;
    public double? PreviewIdeaWindowWidth { get; set; }
    public double? PreviewIdeaWindowHeight { get; set; }
    public double? PreviewIdeaWindowLeft { get; set; }
    public double? PreviewIdeaWindowTop { get; set; }
}

public class UiSettingsService
{
    public static double ValidateNoteNestEditorFontSize(double size) =>
        size is 12 or 14 or 16 or 18 or 20 ? size : 14;

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
