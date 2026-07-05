using System.IO;
using System.Linq;
using System.Text;
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
    public string NoteNestEditorFontFamily { get; set; } = "Yu Gothic UI";
    public double? PreviewIdeaWindowWidth { get; set; }
    public double? PreviewIdeaWindowHeight { get; set; }
    public double? PreviewIdeaWindowLeft { get; set; }
    public double? PreviewIdeaWindowTop { get; set; }
}

public class UiSettingsService
{
    public static double ValidateNoteNestEditorFontSize(double size) =>
        size is 12 or 14 or 16 or 18 or 20 ? size : 14;

    /// <summary>既定値。他 Workspace（IdeaNest/ChatNest/TempNest）や NestSuite 全体の UI フォントには適用しない。</summary>
    public const string DefaultNoteNestEditorFontFamily = "Yu Gothic UI";

    /// <summary>L21: NoteNest 本文エディタで選択可能なフォント種類（端末非依存の主要候補に限定）。</summary>
    public static readonly IReadOnlyList<string> ValidNoteNestEditorFontFamilies =
    [
        DefaultNoteNestEditorFontFamily,
        "Meiryo UI",
        "MS Gothic",
        "BIZ UDGothic",
        "Consolas",
    ];

    /// <summary>
    /// L21: 未設定・空文字・候補外（削除されたフォント名の残存等）の場合は既定へフォールバックする。
    /// これにより ui-settings.json に不正なフォント名が残っていても起動・表示が壊れない。
    /// </summary>
    public static string ValidateNoteNestEditorFontFamily(string? family) =>
        !string.IsNullOrWhiteSpace(family) && ValidNoteNestEditorFontFamilies.Contains(family)
            ? family
            : DefaultNoteNestEditorFontFamily;

    public static AppTheme NormalizeTheme(AppTheme theme) =>
        Enum.IsDefined(typeof(AppTheme), theme) ? theme : AppTheme.Light;

    private static readonly string DataPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "NoteNest", "ui-settings.json");

    public UiSettings Load()
    {
        try
        {
            if (!File.Exists(DataPath)) return new();
            var settings = JsonSerializer.Deserialize<UiSettings>(File.ReadAllText(DataPath)) ?? new();
            settings.Theme = NormalizeTheme(settings.Theme);
            return settings;
        }
        catch { return new(); }
    }

    public void Save(UiSettings settings)
    {
        try
        {
            // v2.14.10 TD-60: tmp 経由の atomic write 化。File.WriteAllText の既定エンコーディング
            // （BOM なし UTF-8）を維持するため Encoding.UTF8（BOM あり）ではなく明示的に指定する。
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = false });
            AtomicFileWriter.WriteAllText(DataPath, json, new UTF8Encoding(false));
        }
        catch { }
    }
}
