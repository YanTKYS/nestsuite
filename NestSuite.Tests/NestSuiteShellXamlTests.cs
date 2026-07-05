using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// Shell および関連 View の XAML 構造確認テスト。
/// NestSuiteShellWindow.xaml・NoteNestWorkspaceView.xaml・PreviewIdeaWindow.xaml が
/// 想定どおりの要素を持つ（または持たない）ことをファイル読み取りで静的に確認する。
/// </summary>
public class NestSuiteShellXamlTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    // ── SH-25: Shell 上部バー削除・メニュー導線整理 ──────────────────────

    [Fact]
    public void ShellXaml_DoesNotContain_TopBarLaunchButtons()
    {
        var src = ReadShellXaml();
        Assert.DoesNotContain("Shell.NoteNestLaunchButton", src);
        Assert.DoesNotContain("Shell.IdeaNestLaunchButton", src);
        Assert.DoesNotContain("Shell.ChatNestLaunchButton", src);
    }

    [Fact]
    public void ShellXaml_DoesNotContain_NoteExportMenuItems()
    {
        // SH-25: NoteNest エクスポートメニューは Shell File メニューから NoteNestWorkspaceView の右クリックへ移管した
        var src = ReadShellXaml();
        Assert.DoesNotContain("MenuExportNoteMarkdownCopy_Click", src);
        Assert.DoesNotContain("MenuExportNoteMarkdownSave_Click", src);
        Assert.DoesNotContain("MenuExportAllNotesMarkdownSave_Click", src);
    }

    [Fact]
    public void ShellXaml_ToolMenu_HasDescriptions()
    {
        // SH-25: ツールメニュー項目に説明文を追加した
        var src = ReadShellXaml();
        Assert.Contains("ノートをプロジェクト単位で管理", src);
        Assert.Contains("アイデアをカード形式で整理", src);
        Assert.Contains("チャット形式でブレスト記録", src);
    }

    [Fact]
    public void NoteNestWorkspaceViewXaml_Contains_ExportContextMenu()
    {
        // SH-25: NoteNestWorkspaceView に Markdown エクスポートの右クリックメニューが追加された
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml");
        var src = File.ReadAllText(path);
        Assert.Contains("ExportNoteMarkdownCopy_Click", src);
        Assert.Contains("ExportNoteMarkdownSave_Click", src);
        Assert.Contains("ExportAllNotesMarkdownSave_Click", src);
    }

    // ── v2.14.18 SH: Workspace共通フォント設定をメニューバーへ移動 ────────

    [Fact]
    public void ShellXaml_WorkspaceFontMenu_ContainsAllCandidates()
    {
        // メニュー表示対象が UiSettingsService.ValidWorkspaceEditorFontFamilies と一致することを固定する。
        var src = ReadShellXaml();
        Assert.Contains("Shell.WorkspaceFontMenu", src);
        foreach (var family in NestSuite.Services.UiSettingsService.ValidWorkspaceEditorFontFamilies)
            Assert.Contains($"Tag=\"{family}\"", src);
    }

    [Fact]
    public void ShellXaml_WorkspaceFontMenu_ItemsAreCheckableAndShareClickHandler()
    {
        var src = ReadShellXaml();
        var occurrences = System.Text.RegularExpressions.Regex.Matches(src, "Click=\"MenuWorkspaceFont_Click\"").Count;
        Assert.Equal(NestSuite.Services.UiSettingsService.ValidWorkspaceEditorFontFamilies.Count, occurrences);
    }

    [Fact]
    public void NoteNestWorkspaceViewXaml_DoesNotContain_EditorFontFamilyComboBox()
    {
        // v2.14.18 SH: NoteNest 上部ツールバーのフォント種類 ComboBox はメニューバーへ移動したため廃止した。
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml");
        var src = File.ReadAllText(path);
        Assert.DoesNotContain("EditorFontFamilyChoices", src);
        Assert.DoesNotContain("Binding EditorFontFamily,", src);
    }

    [Fact]
    public void NoteNestWorkspaceViewXaml_StillContains_EditorFontSizeComboBox()
    {
        // フォントサイズ ComboBox は今回の対象外。維持されていることを固定する。
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml");
        var src = File.ReadAllText(path);
        Assert.Contains("EditorFontSizeChoices", src);
        Assert.Contains("Binding EditorFontSize,", src);
    }

    // ── ID-14: IdeaNest 新規カードのサンプル表示削減 ──────────────────────

    [Fact]
    public void PreviewIdeaWindowXaml_DoesNotContain_TagExampleText()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "IdeaNest", "Views", "PreviewIdeaWindow.xaml");
        Assert.True(File.Exists(path), $"PreviewIdeaWindow.xaml not found: {path}");
        var src = File.ReadAllText(path);
        Assert.DoesNotContain("例: アイデア", src);
        Assert.DoesNotContain("タグをカンマ区切りで入力", src);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string ReadShellXaml()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.xaml");
        Assert.True(File.Exists(path), $"NestSuiteShellWindow.xaml not found: {path}");
        return File.ReadAllText(path);
    }
}
