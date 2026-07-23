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

    // ── v2.19.3 L4: NoteNest 本文エディタの折り返し表示（NoteNestWordWrap） ────

    [Fact]
    public void NoteNestWordWrap_DefaultsToTrue_OnNewUiSettings()
    {
        Assert.True(new UiSettings().NoteNestWordWrap);
    }

    [Fact]
    public void Load_OldUiSettingsJsonWithoutWordWrapField_DefaultsToTrue()
    {
        // v2.19.3 以前の ui-settings.json（NoteNestWordWrap フィールドなし）を想定。
        File.WriteAllText(_path, "{\"LastSearchText\":\"既存\"}");
        var service = new UiSettingsService(_path);

        var settings = service.Load();

        Assert.True(settings.NoteNestWordWrap);
        Assert.Equal("既存", settings.LastSearchText);
    }

    [Fact]
    public void Save_WordWrapFalse_ThenReload_RestoresFalse()
    {
        var service = new UiSettingsService(_path);
        service.Save(new UiSettings { NoteNestWordWrap = false });

        var loaded = service.Load();

        Assert.False(loaded.NoteNestWordWrap);
    }

    [Fact]
    public void Save_WordWrapTrue_ThenReload_RestoresTrue()
    {
        var service = new UiSettingsService(_path);
        service.Save(new UiSettings { NoteNestWordWrap = true });

        var loaded = service.Load();

        Assert.True(loaded.NoteNestWordWrap);
    }

    // ── M19: 読込失敗時の復旧経路 ────────────────────────────────────────────

    [Fact]
    public void Load_NormalFile_ReadsAsExpected_LikeBefore()
    {
        var service = new UiSettingsService(_path);
        SaveViaProductionPattern(_path, new UiSettings { LastSearchText = "既存どおり", Theme = AppTheme.Dark });

        var settings = service.Load();

        Assert.Equal("既存どおり", settings.LastSearchText);
        Assert.Equal(AppTheme.Dark, settings.Theme);
    }

    [Fact]
    public void Load_FileDoesNotExist_ReturnsDefault_NoRecovery()
    {
        var service = new UiSettingsService(_path);

        var result = service.LoadWithRecovery();

        Assert.Null(result.Recovery);
        Assert.Equal(new UiSettings().LastSearchText, result.Settings.LastSearchText);
    }

    [Fact]
    public void Load_InvalidJson_ReturnsDefaultSettings()
    {
        var service = new UiSettingsService(_path);
        File.WriteAllText(_path, "{ not valid json");

        var settings = service.Load();

        Assert.Equal(new UiSettings().LastSearchText, settings.LastSearchText);
    }

    [Fact]
    public void Load_InvalidJson_QuarantinesOriginalFile_BackupFileExists()
    {
        var service = new UiSettingsService(_path);
        File.WriteAllText(_path, "{ not valid json");

        var result = service.LoadWithRecovery();

        Assert.NotNull(result.Recovery);
        Assert.True(result.Recovery!.Succeeded);
        Assert.False(File.Exists(_path));
        Assert.True(File.Exists(result.Recovery.BackupPath));
        Assert.Contains(".corrupt-", result.Recovery.BackupPath);
    }

    [Fact]
    public void Load_InvalidJson_DoesNotThrow_ReturnsRecoveryResult()
    {
        var service = new UiSettingsService(_path);
        File.WriteAllText(_path, "not json at all {{{");

        var exception = Record.Exception(() => service.LoadWithRecovery());

        Assert.Null(exception);
    }

    [Fact]
    public void Load_AfterQuarantine_CanSaveNormallyToOriginalPath()
    {
        var service = new UiSettingsService(_path);
        File.WriteAllText(_path, "{ not valid json");
        service.LoadWithRecovery();

        service.Save(new UiSettings { LastSearchText = "復旧後の保存" });

        var reloaded = service.Load();
        Assert.Equal("復旧後の保存", reloaded.LastSearchText);
    }

    [Fact]
    public void Load_NormalFile_IsNeverQuarantined()
    {
        var service = new UiSettingsService(_path);
        SaveViaProductionPattern(_path, new UiSettings { LastSearchText = "正常" });

        var result = service.LoadWithRecovery();

        Assert.Null(result.Recovery);
        Assert.True(File.Exists(_path));
        Assert.Empty(Directory.GetFiles(_tempDir, "*.corrupt-*"));
    }

    [Fact]
    public void Load_InvalidJson_DoesNotChangeJsonFormat_SaveStillProducesValidJson()
    {
        var service = new UiSettingsService(_path);
        File.WriteAllText(_path, "{ not valid json");
        service.LoadWithRecovery();

        service.Save(new UiSettings { LastSearchText = "x" });

        var reloadedRaw = File.ReadAllText(_path);
        var reloaded = JsonSerializer.Deserialize<UiSettings>(reloadedRaw);
        Assert.NotNull(reloaded);
        Assert.Equal("x", reloaded!.LastSearchText);
    }
}
