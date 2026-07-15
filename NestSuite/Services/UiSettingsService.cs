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

    /// <summary>
    /// L21 で追加した NoteNest 限定のフォント種類設定。L22 で <see cref="WorkspaceEditorFontFamily"/>
    /// へ発展的に移行したため新規の読み書きはしないが、既存 ui-settings.json との後方互換のため
    /// フィールド自体とその既定値は維持する（<see cref="UiSettingsService.ResolveWorkspaceEditorFontFamily"/> 参照）。
    /// </summary>
    public string NoteNestEditorFontFamily { get; set; } = "Yu Gothic UI";

    /// <summary>
    /// L22: NoteNest / IdeaNest / ChatNest / TempNest 共通の本文・編集領域フォント種類設定。
    /// Workspace ファイル本体には保存せず、この ui-settings.json 上でのみ管理する。
    /// 未設定（null）の場合は <see cref="NoteNestEditorFontFamily"/>（L21 の旧設定）を移行元として使う。
    /// </summary>
    public string? WorkspaceEditorFontFamily { get; set; }

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

    /// <summary>既定値。NestSuite 全体の UI フォント（メニュー・タブ・ボタン・ダイアログ）には適用しない。</summary>
    public const string DefaultWorkspaceEditorFontFamily = "Yu Gothic UI";

    /// <summary>
    /// L22: NoteNest / IdeaNest / ChatNest / TempNest の本文・編集領域で選択可能なフォント種類
    /// （端末非依存の主要候補に限定）。L21 の <see cref="ValidNoteNestEditorFontFamilies"/> に
    /// BIZ UDMincho / UD Digi Kyokasho N-R を加えた Workspace 共通版。
    /// </summary>
    public static readonly IReadOnlyList<string> ValidWorkspaceEditorFontFamilies =
    [
        DefaultWorkspaceEditorFontFamily,
        "Meiryo UI",
        "MS Gothic",
        "BIZ UDGothic",
        "BIZ UDMincho",
        "UD Digi Kyokasho N-R",
        "Consolas",
    ];

    /// <summary>
    /// L22: 未設定・空文字・候補外（削除されたフォント名の残存等）の場合は既定へフォールバックする。
    /// これにより ui-settings.json に不正なフォント名が残っていても起動・各 Workspace 表示が壊れない。
    /// </summary>
    public static string ValidateWorkspaceEditorFontFamily(string? family) =>
        !string.IsNullOrWhiteSpace(family) && ValidWorkspaceEditorFontFamilies.Contains(family)
            ? family
            : DefaultWorkspaceEditorFontFamily;

    /// <summary>
    /// L22: 実際に適用する値を解決する。優先順位は次のとおり。
    /// 1. <see cref="UiSettings.WorkspaceEditorFontFamily"/>（新設定）が候補内なら、それを使う。
    /// 2. なければ <see cref="UiSettings.NoteNestEditorFontFamily"/>（L21 の旧設定）を移行元として使う。
    /// 3. どちらも無効・未設定なら既定 <see cref="DefaultWorkspaceEditorFontFamily"/> を使う。
    /// 保存は常に新設定名（<see cref="UiSettings.WorkspaceEditorFontFamily"/>）へ行う（呼び出し側の責務）。
    /// </summary>
    public static string ResolveWorkspaceEditorFontFamily(UiSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.WorkspaceEditorFontFamily) &&
            ValidWorkspaceEditorFontFamilies.Contains(settings.WorkspaceEditorFontFamily))
            return settings.WorkspaceEditorFontFamily;

        if (!string.IsNullOrWhiteSpace(settings.NoteNestEditorFontFamily) &&
            ValidWorkspaceEditorFontFamilies.Contains(settings.NoteNestEditorFontFamily))
            return settings.NoteNestEditorFontFamily;

        return DefaultWorkspaceEditorFontFamily;
    }

    public static AppTheme NormalizeTheme(AppTheme theme) =>
        Enum.IsDefined(typeof(AppTheme), theme) ? theme : AppTheme.Light;

    private static readonly string DefaultDataPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "NoteNest", "ui-settings.json");

    private readonly string _dataPath;

    /// <summary>
    /// M19: <paramref name="dataPath"/> は読込失敗時の破損ファイル退避テスト用。
    /// 省略時は従来どおり <c>%APPDATA%\NoteNest\ui-settings.json</c> を使う（挙動不変）。
    /// </summary>
    public UiSettingsService(string? dataPath = null)
    {
        _dataPath = dataPath ?? DefaultDataPath;
    }

    public UiSettings Load() => LoadWithRecovery().Settings;

    /// <summary>
    /// M19: 読込結果に加え、破損ファイルの退避結果（発生した場合のみ）を返す。
    /// ファイル不存在は正常な初回起動として扱い、<see cref="UiSettingsLoadResult.Recovery"/> は null のまま。
    /// 呼び出し側（Shell 起動処理）はこれを見て、利用者への一時通知を判断する。
    /// </summary>
    public UiSettingsLoadResult LoadWithRecovery()
    {
        if (!File.Exists(_dataPath)) return new UiSettingsLoadResult(new UiSettings(), null);

        try
        {
            var settings = JsonSerializer.Deserialize<UiSettings>(File.ReadAllText(_dataPath)) ?? new UiSettings();
            settings.Theme = NormalizeTheme(settings.Theme);
            return new UiSettingsLoadResult(settings, null);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("UiSettingsLoad", ex, filePath: _dataPath);
            var recovery = FileRecoveryHelper.QuarantineCorruptFile(_dataPath);
            if (!recovery.Succeeded && recovery.Exception != null)
                ErrorLogService.Log("UiSettingsCorruptFileBackup", recovery.Exception, filePath: _dataPath);
            return new UiSettingsLoadResult(new UiSettings(), recovery);
        }
    }

    public void Save(UiSettings settings)
    {
        try
        {
            // v2.14.10 TD-60: tmp 経由の atomic write 化。File.WriteAllText の既定エンコーディング
            // （BOM なし UTF-8）を維持するため Encoding.UTF8（BOM あり）ではなく明示的に指定する。
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = false });
            AtomicFileWriter.WriteAllText(_dataPath, json, new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("UiSettingsSave", ex, filePath: _dataPath);
        }
    }
}

/// <summary>M19: <see cref="UiSettingsService.LoadWithRecovery"/> の結果。</summary>
/// <param name="Settings">読込に成功した設定、または失敗時の既定設定。</param>
/// <param name="Recovery">読込に失敗し破損ファイル退避を試みた場合のみ設定される。正常時・ファイル不存在時は null。</param>
public sealed record UiSettingsLoadResult(UiSettings Settings, CorruptFileRecoveryResult? Recovery);
