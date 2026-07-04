using System.IO;
using System.Text;
using System.Text.Json;
using NestSuite.Models;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.14.10 TD-60: UiSettingsService.Save の atomic write 化（tmp 経由 + UTF8Encoding(false)）の
/// 回帰テスト。
///
/// UiSettingsService.DataPath は private static readonly で `%APPDATA%\NoteNest\ui-settings.json`
/// に固定されており、テスト用に差し替え不可（このタスクのスコープ外の production 変更が必要）。
/// そのため実際の Save() 呼び出しではなく、Save() 内部と全く同じ処理列
/// （JsonSerializer.Serialize → AtomicFileWriter.WriteAllText(path, json, new UTF8Encoding(false))）
/// をテスト用の一時パスに対して再現し、atomic 上書き・no-BOM・tmp 未残留を固定する。
/// 既存の ThemeSettingsTests / EditorLayoutTests は UiSettings のメモリ上シリアライズのみを
/// 検証しており、実ファイル I/O は検証していなかった（本ファイルが補完する）。
/// </summary>
public class UiSettingsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _path;

    public UiSettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "UiSettingsServiceTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _path = Path.Combine(_tempDir, "ui-settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static void SaveViaProductionPattern(string path, UiSettings settings)
    {
        // UiSettingsService.Save() と同一のシリアライズ + 書き込み手順。
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = false });
        AtomicFileWriter.WriteAllText(path, json, new UTF8Encoding(false));
    }

    private static UiSettings LoadViaProductionPattern(string path)
    {
        // UiSettingsService.Load() と同一の読込手順。
        var settings = JsonSerializer.Deserialize<UiSettings>(File.ReadAllText(path)) ?? new();
        settings.Theme = UiSettingsService.NormalizeTheme(settings.Theme);
        return settings;
    }

    [Fact]
    public void Save_WithExistingFile_OverwritesAtomically_LatestValueWins()
    {
        SaveViaProductionPattern(_path, new UiSettings { LastSearchText = "first" });
        SaveViaProductionPattern(_path, new UiSettings { LastSearchText = "second" });

        var loaded = LoadViaProductionPattern(_path);

        Assert.Equal("second", loaded.LastSearchText);
    }

    [Fact]
    public void Save_ThenReadRaw_JsonRoundTrips_AndHasNoBomPrefix()
    {
        var settings = new UiSettings
        {
            LastSearchText = "テスト検索",
            Theme = AppTheme.Dark,
            WindowWidth = 1234,
            NoteNestEditorFontSize = 18,
        };

        SaveViaProductionPattern(_path, settings);

        var bytes = File.ReadAllBytes(_path);
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
            "File.WriteAllText の既定（BOM なし UTF-8）を維持しているはず");

        var loaded = LoadViaProductionPattern(_path);
        Assert.Equal(settings.LastSearchText, loaded.LastSearchText);
        Assert.Equal(settings.Theme, loaded.Theme);
        Assert.Equal(settings.WindowWidth, loaded.WindowWidth);
        Assert.Equal(settings.NoteNestEditorFontSize, loaded.NoteNestEditorFontSize);
    }

    [Fact]
    public void Save_NoLeftoverTmpFile()
    {
        SaveViaProductionPattern(_path, new UiSettings { LastSearchText = "x" });

        Assert.False(File.Exists(_path + ".tmp"));
    }
}
