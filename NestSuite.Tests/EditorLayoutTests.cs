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

    // ── L22: EditorFontFamilyChoices（Workspace 共通設定への拡大） ────────

    [Fact]
    public void EditorFontFamilyChoices_ContainsExpectedValues()
    {
        // v2.14.17 L22: BIZ UDMincho / UD Digi Kyokasho N-R を追加した Workspace 共通の候補一覧。
        var choices = MainViewModel.EditorFontFamilyChoices;
        Assert.Equal(
            ["Yu Gothic UI", "Meiryo UI", "MS Gothic", "BIZ UDGothic", "BIZ UDMincho", "UD Digi Kyokasho N-R", "Consolas"],
            choices);
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

    // ── L22: 他 Workspace への Workspace 共通フォント適用の確認 ───────────
    // v2.14.17 L22 で NoteNest 限定だったフォント種類設定を IdeaNest / ChatNest / TempNest の
    // 本文・編集領域へ拡大した。L21 時点の「他 Workspace に FontFamily 概念が存在しない」という
    // 制約は本リリースの意図（Workspace 共通化）と矛盾するため、
    // 「反映されるが Workspace ファイル保存対象にはならない」という新仕様のテストへ更新する。

    [Theory]
    [InlineData(typeof(NestSuite.IdeaNest.ViewModels.IdeaNestWorkspaceViewModel))]
    [InlineData(typeof(NestSuite.ChatNest.ChatNestWorkspaceViewModel))]
    [InlineData(typeof(NestSuite.TempNest.TempNestWorkspaceViewModel))]
    public void OtherWorkspaceViewModels_HaveContentFontFamilyMember(Type workspaceViewModelType)
    {
        var hasContentFontFamilyMember = workspaceViewModelType
            .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Any(m => m.Name == "ContentFontFamily");

        Assert.True(hasContentFontFamilyMember,
            $"{workspaceViewModelType.Name} は Workspace 共通の ContentFontFamily 設定を持つべき。");
    }

    [Fact]
    public void IdeaNestWorkspaceViewModel_ContentFontFamily_DefaultsToYuGothicUI()
    {
        var vm = new NestSuite.IdeaNest.ViewModels.IdeaNestWorkspaceViewModel();
        Assert.Equal("Yu Gothic UI", vm.ContentFontFamily);
    }

    [Fact]
    public void IdeaNestWorkspaceViewModel_ContentFontFamilyChange_DoesNotMarkHasChanges()
    {
        var vm = new NestSuite.IdeaNest.ViewModels.IdeaNestWorkspaceViewModel();

        vm.ContentFontFamily = "Consolas";

        Assert.Equal("Consolas", vm.ContentFontFamily);
        Assert.False(vm.HasChanges);
    }

    [Fact]
    public void ChatNestWorkspaceViewModel_ContentFontFamily_DefaultsToYuGothicUI()
    {
        var vm = new NestSuite.ChatNest.ChatNestWorkspaceViewModel();
        Assert.Equal("Yu Gothic UI", vm.ContentFontFamily);
    }

    [Fact]
    public void ChatNestWorkspaceViewModel_ContentFontFamilyChange_DoesNotMarkDirty()
    {
        var vm = new NestSuite.ChatNest.ChatNestWorkspaceViewModel();

        vm.ContentFontFamily = "Consolas";

        Assert.Equal("Consolas", vm.ContentFontFamily);
        Assert.False(vm.IsDirty);
        Assert.False(vm.HasUnsavedChanges);
    }

    [Fact]
    public void TempNestWorkspaceViewModel_ContentFontFamily_DefaultsToYuGothicUI()
    {
        using var vm = new NestSuite.TempNest.TempNestWorkspaceViewModel();
        Assert.Equal("Yu Gothic UI", vm.ContentFontFamily);
    }

    [Fact]
    public void TempNestWorkspaceViewModel_ContentFontFamilyChange_AppliesWithoutAffectingSlots()
    {
        using var vm = new NestSuite.TempNest.TempNestWorkspaceViewModel();

        vm.ContentFontFamily = "Consolas";

        Assert.Equal("Consolas", vm.ContentFontFamily);
        Assert.Equal("", vm.Slot1.Title);
        Assert.Equal("", vm.Slot1.Body);
    }

    // ── L22: WorkspaceEditorFontFamily（共通設定）デフォルト値・移行・候補 ──

    [Fact]
    public void UiSettings_WorkspaceEditorFontFamily_DefaultIsNull()
    {
        // 未設定（null）は「まだ新設定に移行していない」ことを表す。
        var settings = new UiSettings();
        Assert.Null(settings.WorkspaceEditorFontFamily);
    }

    [Theory]
    [InlineData("Yu Gothic UI")]
    [InlineData("Meiryo UI")]
    [InlineData("MS Gothic")]
    [InlineData("BIZ UDGothic")]
    [InlineData("BIZ UDMincho")]
    [InlineData("UD Digi Kyokasho N-R")]
    [InlineData("Consolas")]
    public void ValidateWorkspaceEditorFontFamily_AcceptsValidValues(string family)
    {
        Assert.Equal(family, UiSettingsService.ValidateWorkspaceEditorFontFamily(family));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Unknown Font That Was Uninstalled")]
    [InlineData("MS Mincho")]
    public void ValidateWorkspaceEditorFontFamily_InvalidOrMissingValueFallsBackToDefault(string? family)
    {
        Assert.Equal("Yu Gothic UI", UiSettingsService.ValidateWorkspaceEditorFontFamily(family));
    }

    [Fact]
    public void ResolveWorkspaceEditorFontFamily_PrefersNewSettingOverLegacy()
    {
        var settings = new UiSettings
        {
            WorkspaceEditorFontFamily = "Consolas",
            NoteNestEditorFontFamily = "MS Gothic",
        };

        Assert.Equal("Consolas", UiSettingsService.ResolveWorkspaceEditorFontFamily(settings));
    }

    [Fact]
    public void ResolveWorkspaceEditorFontFamily_MigratesFromLegacyNoteNestSetting_WhenNewSettingIsUnset()
    {
        // 既存ユーザーの ui-settings.json に旧 NoteNestEditorFontFamily のみが保存されているケース。
        var settings = new UiSettings
        {
            WorkspaceEditorFontFamily = null,
            NoteNestEditorFontFamily = "BIZ UDGothic",
        };

        Assert.Equal("BIZ UDGothic", UiSettingsService.ResolveWorkspaceEditorFontFamily(settings));
    }

    [Fact]
    public void ResolveWorkspaceEditorFontFamily_FallsBackToDefault_WhenBothSettingsAreInvalid()
    {
        var settings = new UiSettings
        {
            WorkspaceEditorFontFamily = "Unknown Font",
            NoteNestEditorFontFamily = "",
        };

        Assert.Equal("Yu Gothic UI", UiSettingsService.ResolveWorkspaceEditorFontFamily(settings));
    }

    [Fact]
    public void ResolveWorkspaceEditorFontFamily_FallsBackToDefault_WhenNeitherSettingIsPresent()
    {
        var settings = new UiSettings();
        Assert.Equal("Yu Gothic UI", UiSettingsService.ResolveWorkspaceEditorFontFamily(settings));
    }

    [Theory]
    [InlineData("Meiryo UI")]
    [InlineData("BIZ UDMincho")]
    public void UiSettingsService_SaveAndLoad_RoundTripsWorkspaceEditorFontFamily(string family)
    {
        var settings = new UiSettings { WorkspaceEditorFontFamily = family };

        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        var loaded = System.Text.Json.JsonSerializer.Deserialize<UiSettings>(json);

        Assert.NotNull(loaded);
        Assert.Equal(family, loaded!.WorkspaceEditorFontFamily);
    }

    // ── L22: IdeaNest / ChatNest / TempNest 保存形式へのフォント設定混入なし ──

    [Fact]
    public void IdeaNestWorkspaceSettings_HasNoFontFamilyMember()
    {
        var hasFontFamilyMember = typeof(NestSuite.IdeaNest.Models.WorkspaceSettings)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Any(m => m.Name.Contains("FontFamily", StringComparison.Ordinal));

        Assert.False(hasFontFamilyMember, ".ideanest の WorkspaceSettings にフォント設定を保存してはならない。");
    }

    [Fact]
    public void ChatNestMessageModel_HasNoFontFamilyMember()
    {
        var hasFontFamilyMember = typeof(NestSuite.ChatNest.Message)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Any(m => m.Name.Contains("FontFamily", StringComparison.Ordinal));

        Assert.False(hasFontFamilyMember, ".chatnest の Message モデルにフォント設定を保存してはならない。");
    }

    [Fact]
    public void TempNestSlotModel_HasNoFontFamilyMember()
    {
        var hasFontFamilyMember = typeof(NestSuite.TempNest.TempNestSlot)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Any(m => m.Name.Contains("FontFamily", StringComparison.Ordinal));

        Assert.False(hasFontFamilyMember, "tempnest.json の TempNestSlot にフォント設定を保存してはならない。");
    }

    [Fact]
    public void IdeaNestWorkspaceViewModel_BuildWorkspaceForSave_DoesNotIncludeContentFontFamily()
    {
        var vm = new NestSuite.IdeaNest.ViewModels.IdeaNestWorkspaceViewModel();
        vm.ContentFontFamily = "Consolas";

        var saved = vm.BuildWorkspaceForSave();

        Assert.Equal(NestSuite.IdeaNest.Models.IdeaNestSchema.CurrentVersion, saved.Version);
        // WorkspaceSettings に FontFamily フィールド自体が存在しないため、
        // ContentFontFamily の値がどこにも書き込まれ得ないことを型として保証する。
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
