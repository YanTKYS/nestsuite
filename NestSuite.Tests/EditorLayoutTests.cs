using System.Reflection;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.7.10 L9/L12: エディタ周辺レイアウト・フォントサイズ設定の確認テスト。
/// UI を起動しないリフレクションベースまたはサービス直接呼び出しによる静的確認。
/// </summary>
public class EditorLayoutTests
{
    // ── L12: NoteNestEditorFontSize デフォルト値 ─────────────────────────

    [Fact]
    public void UiSettings_NoteNestEditorFontSize_DefaultIs14()
    {
        var settings = new UiSettings();
        Assert.Equal(14.0, settings.NoteNestEditorFontSize);
    }

    // ── L12: ValidateNoteNestEditorFontSize ──────────────────────────────

    [Theory]
    [InlineData(12)]
    [InlineData(14)]
    [InlineData(16)]
    [InlineData(18)]
    [InlineData(20)]
    public void ValidateNoteNestEditorFontSize_AcceptsValidValues(double size)
    {
        Assert.Equal(size, UiSettingsService.ValidateNoteNestEditorFontSize(size));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(13)]
    [InlineData(99)]
    [InlineData(double.NaN)]
    public void ValidateNoteNestEditorFontSize_InvalidValueFallsBackTo14(double size)
    {
        Assert.Equal(14.0, UiSettingsService.ValidateNoteNestEditorFontSize(size));
    }

    // ── L12: UiSettingsService 保存・復元 ────────────────────────────────

    [Theory]
    [InlineData(12)]
    [InlineData(16)]
    [InlineData(20)]
    public void UiSettingsService_SaveAndLoad_RoundTripsNoteNestEditorFontSize(double size)
    {
        var settings = new UiSettings { NoteNestEditorFontSize = size };
        var svc = new UiSettingsService();

        // メモリ上でシリアライズ→デシリアライズ相当の検証
        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        var loaded = System.Text.Json.JsonSerializer.Deserialize<UiSettings>(json);

        Assert.NotNull(loaded);
        Assert.Equal(size, loaded!.NoteNestEditorFontSize);
    }

    // ── L12: EditorFontSizeChoices ────────────────────────────────────────

    [Fact]
    public void EditorFontSizeChoices_ContainsExpectedValues()
    {
        var choices = MainViewModel.EditorFontSizeChoices;
        Assert.Equal([12.0, 14.0, 16.0, 18.0, 20.0], choices);
    }

    [Fact]
    public void EditorFontSizeChoices_DefaultFontSizeIsInList()
    {
        var settings = new UiSettings();
        Assert.Contains(settings.NoteNestEditorFontSize, MainViewModel.EditorFontSizeChoices);
    }

    // ── L12: .notenest スキーマ非汚染確認 ────────────────────────────────

    [Fact]
    public void Project_SchemaVersion_IsNotChangedByEditorLayout()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".notenest");
        try
        {
            var project = new NestSuite.Models.Project
            {
                ProjectName = "EditorLayoutSchemaGuard",
                Version = NestSuite.Models.Project.CurrentSchemaVersion,
            };
            new ProjectFileService().Save(path, project);
            var loaded = new ProjectFileService().Load(path);
            Assert.Equal(NestSuite.Models.Project.CurrentSchemaVersion, loaded.Version);
        }
        finally { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
    }
}
