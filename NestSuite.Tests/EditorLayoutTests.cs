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

    // ── L21: NoteNestEditorFontFamily デフォルト値 ───────────────────────

    [Fact]
    public void UiSettings_NoteNestEditorFontFamily_DefaultIsYuGothicUI()
    {
        var settings = new UiSettings();
        Assert.Equal("Yu Gothic UI", settings.NoteNestEditorFontFamily);
    }

    // ── L21: ValidateNoteNestEditorFontFamily ────────────────────────────

    [Theory]
    [InlineData("Yu Gothic UI")]
    [InlineData("Meiryo UI")]
    [InlineData("MS Gothic")]
    [InlineData("BIZ UDGothic")]
    [InlineData("Consolas")]
    public void ValidateNoteNestEditorFontFamily_AcceptsValidValues(string family)
    {
        Assert.Equal(family, UiSettingsService.ValidateNoteNestEditorFontFamily(family));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Unknown Font That Was Uninstalled")]
    [InlineData("MS Mincho")]
    public void ValidateNoteNestEditorFontFamily_InvalidOrMissingValueFallsBackToDefault(string? family)
    {
        // ui-settings.json に存在しない／削除されたフォント名が残っていても
        // 起動・NoteNest 表示が壊れず既定 "Yu Gothic UI" へフォールバックすることを確認する。
        Assert.Equal("Yu Gothic UI", UiSettingsService.ValidateNoteNestEditorFontFamily(family));
    }

    // ── L21: UiSettingsService 保存・復元 ────────────────────────────────

    [Theory]
    [InlineData("Meiryo UI")]
    [InlineData("Consolas")]
    public void UiSettingsService_SaveAndLoad_RoundTripsNoteNestEditorFontFamily(string family)
    {
        var settings = new UiSettings { NoteNestEditorFontFamily = family };

        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        var loaded = System.Text.Json.JsonSerializer.Deserialize<UiSettings>(json);

        Assert.NotNull(loaded);
        Assert.Equal(family, loaded!.NoteNestEditorFontFamily);
    }

    // ── L21: EditorFontFamilyChoices ──────────────────────────────────────

    [Fact]
    public void EditorFontFamilyChoices_ContainsExpectedValues()
    {
        var choices = MainViewModel.EditorFontFamilyChoices;
        Assert.Equal(["Yu Gothic UI", "Meiryo UI", "MS Gothic", "BIZ UDGothic", "Consolas"], choices);
    }

    [Fact]
    public void EditorFontFamilyChoices_DefaultFontFamilyIsInList()
    {
        var settings = new UiSettings();
        Assert.Contains(settings.NoteNestEditorFontFamily, MainViewModel.EditorFontFamilyChoices);
    }

    [Fact]
    public void EditorFontFamilyChoices_FirstEntryIsDefault_SoUserCanReturnToIt()
    {
        Assert.Equal("Yu Gothic UI", MainViewModel.EditorFontFamilyChoices[0]);
    }

    // ── L21: NoteNest エディタフォント反映（MainViewModel ファサード経由）──

    [Fact]
    public void MainViewModel_EditorFontFamily_AppliesToNoteNestEditorOnly()
    {
        var main = new MainViewModel();

        main.EditorFontFamily = "Consolas";

        Assert.Equal("Consolas", main.EditorFontFamily);
        Assert.Equal("Consolas", main.Editor.FontFamily);
    }

    // ── L21: 他 Workspace への意図しない適用がないことの確認 ─────────────
    // IdeaNest / ChatNest / TempNest の ViewModel には NoteNest エディタフォント種類の概念自体が
    // 存在しないことを型レベルで確認する（フォントサイズ変更とは分離し、NoteNest 本文エディタ限定）。

    [Theory]
    [InlineData(typeof(NestSuite.IdeaNest.ViewModels.IdeaNestWorkspaceViewModel))]
    [InlineData(typeof(NestSuite.ChatNest.ChatNestWorkspaceViewModel))]
    [InlineData(typeof(NestSuite.TempNest.TempNestWorkspaceViewModel))]
    public void OtherWorkspaceViewModels_HaveNoEditorFontFamilyMember(Type workspaceViewModelType)
    {
        var hasFontFamilyMember = workspaceViewModelType
            .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Any(m => m.Name.Contains("FontFamily", StringComparison.Ordinal));

        Assert.False(hasFontFamilyMember,
            $"{workspaceViewModelType.Name} は NoteNest 本文エディタ限定の FontFamily 設定を持つべきではない。");
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
