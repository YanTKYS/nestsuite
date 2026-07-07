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
    public void ShellXaml_NewMenu_HasDescriptions()
    {
        // SH-25 で追加した説明文は、v2.15.1 でツールメニューからファイル > 新規作成へ移動した。
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

    // ── v2.15.0 SH: Shell横断検索の最小実装 ──────────────────────────────

    [Fact]
    public void ShellXaml_ContainsCrossSearchMenuItem_WithShortcutText()
    {
        var src = ReadShellXaml();
        Assert.Contains("Shell.CrossSearchMenuItem", src);
        Assert.Contains("Ctrl+Shift+F", src);
        Assert.Contains("CrossSearchCommand", src);
    }

    [Fact]
    public void ShellXaml_ContainsCrossSearchPanel_UsingThemeBrushes()
    {
        var src = ReadShellXaml();
        Assert.Contains("CrossSearchPanel", src);
        Assert.Contains("Shell.CrossSearchBox", src);
        Assert.Contains("Shell.CrossSearchResultsList", src);
        // ハードコードされた色ではなく既存テーマブラシを再利用すること
        Assert.Contains("{DynamicResource SidebarBg}", src);
        Assert.Contains("{DynamicResource PrimaryTextBrush}", src);
        Assert.Contains("{DynamicResource InputBackgroundBrush}", src);
    }

    [Fact]
    public void ShellXaml_DoesNotIntroduce_SearchNestWorkspace()
    {
        // v2.15.0 SH: 横断検索は Shell の補助機能であり、新規 SearchNest Workspace ではない
        var src = ReadShellXaml();
        Assert.DoesNotContain("SearchNestWorkspaceView", src);
    }

    // ── v2.15.1 SH: 横断検索導線・メニュー整理・タブ移動ショートカット調整 ─

    [Fact]
    public void ShellXaml_ViewMenu_NoLongerContainsCrossSearchMenuItem()
    {
        // 横断検索は「表示」（表示切替）ではなく「ツール」（作業補助）配下へ移動した。
        var src = ReadShellXaml();
        var viewMenuStart = src.IndexOf("Header=\"表示(_V)\"", StringComparison.Ordinal);
        var toolMenuStart = src.IndexOf("Header=\"ツール(_T)\"", StringComparison.Ordinal);
        Assert.True(viewMenuStart >= 0, "表示メニューが見つからない");
        Assert.True(toolMenuStart >= 0, "ツールメニューが見つからない");
        Assert.True(toolMenuStart < viewMenuStart, "ツールメニューは表示メニューより前に定義されている想定");

        // 表示メニューの範囲（ツールメニュー終了〜表示メニュー開始の次の閉じタグまで）に
        // 横断検索メニュー項目が含まれないことを確認する。
        var viewMenuSection = src.Substring(viewMenuStart);
        var viewMenuEnd = viewMenuSection.IndexOf("<MenuItem Header=\"ヘルプ(_H)\"", StringComparison.Ordinal);
        Assert.True(viewMenuEnd >= 0, "ヘルプメニューが見つからない（表示メニューの終端検出に使用）");
        var viewMenuOnly = viewMenuSection.Substring(0, viewMenuEnd);
        Assert.DoesNotContain("Shell.CrossSearchMenuItem", viewMenuOnly);
    }

    [Fact]
    public void ShellXaml_ToolMenu_ContainsCrossSearchMenuItem()
    {
        var src = ReadShellXaml();
        var toolMenuStart = src.IndexOf("Header=\"ツール(_T)\"", StringComparison.Ordinal);
        var viewMenuStart = src.IndexOf("Header=\"表示(_V)\"", StringComparison.Ordinal);
        Assert.True(toolMenuStart >= 0 && viewMenuStart > toolMenuStart);
        var toolMenuSection = src.Substring(toolMenuStart, viewMenuStart - toolMenuStart);
        Assert.Contains("Shell.CrossSearchMenuItem", toolMenuSection);
        Assert.Contains("Ctrl+Shift+F", toolMenuSection);
    }

    [Fact]
    public void ShellXaml_ToolMenu_ContainsMigrationPackMenuItems()
    {
        // v2.15.3 SH: デバイス移行パックは新 Workspace ではなく Shell 補助機能としてツールメニューへ配置する。
        var src = ReadShellXaml();
        var toolMenuStart = src.IndexOf("Header=\"ツール(_T)\"", StringComparison.Ordinal);
        var viewMenuStart = src.IndexOf("Header=\"表示(_V)\"", StringComparison.Ordinal);
        Assert.True(toolMenuStart >= 0 && viewMenuStart > toolMenuStart);
        var toolMenuSection = src.Substring(toolMenuStart, viewMenuStart - toolMenuStart);
        Assert.Contains("Shell.ExportMigrationPackMenuItem", toolMenuSection);
        Assert.Contains("Shell.ImportMigrationPackMenuItem", toolMenuSection);
        Assert.Contains("デバイス移行パックをエクスポート", toolMenuSection);
        Assert.Contains("デバイス移行パックをインポート", toolMenuSection);
    }

    [Fact]
    public void ShellXaml_ToolMenu_NoLongerContainsPerNestLaunchItems()
    {
        // v2.15.1 SH: 各 Nest の新規作成・起動項目はツールメニューから削除し、
        // ファイル > 新規作成 とタブバー ＋ ボタンへ集約した。
        var src = ReadShellXaml();
        var toolMenuStart = src.IndexOf("Header=\"ツール(_T)\"", StringComparison.Ordinal);
        var viewMenuStart = src.IndexOf("Header=\"表示(_V)\"", StringComparison.Ordinal);
        Assert.True(toolMenuStart >= 0 && viewMenuStart > toolMenuStart);
        var toolMenuSection = src.Substring(toolMenuStart, viewMenuStart - toolMenuStart);
        Assert.DoesNotContain("Shell.MenuToolNoteNest", toolMenuSection);
        Assert.DoesNotContain("Shell.MenuToolIdeaNest", toolMenuSection);
        Assert.DoesNotContain("Shell.MenuToolChatNest", toolMenuSection);
        Assert.DoesNotContain("MenuTool_Click", toolMenuSection);
    }

    [Fact]
    public void ShellXaml_FileNewMenu_ContainsPerNestDescriptiveLabelsAndAutomationIds()
    {
        var src = ReadShellXaml();
        Assert.Contains("Shell.FileMenu", src);
        Assert.Contains("Shell.NewMenu", src);
        Assert.Contains("Shell.MenuNewNoteNest", src);
        Assert.Contains("Shell.MenuNewIdeaNest", src);
        Assert.Contains("Shell.MenuNewChatNest", src);
        Assert.Contains("新規 NoteNest — ノートをプロジェクト単位で管理", src);
        Assert.Contains("新規 IdeaNest — アイデアをカード形式で整理", src);
        Assert.Contains("新規 ChatNest — チャット形式でブレスト記録", src);
    }

    [Fact]
    public void ShellXaml_TabAddButtonMenu_ContainsPerNestDescriptiveLabels()
    {
        var src = ReadShellXaml();
        Assert.Contains("Shell.TabAddMenuNoteNest", src);
        Assert.Contains("Shell.TabAddMenuIdeaNest", src);
        Assert.Contains("Shell.TabAddMenuChatNest", src);
        // タブバー新規メニューも ファイル > 新規作成 と同じ説明文を再利用する。
        var occurrences = System.Text.RegularExpressions.Regex.Matches(
            src, System.Text.RegularExpressions.Regex.Escape("ノートをプロジェクト単位で管理")).Count;
        Assert.Equal(2, occurrences); // ファイル > 新規作成 とタブバー新規メニューの計2箇所
    }

    [Fact]
    public void ShellXaml_CrossSearchPanelCloseButton_IsPinnedToGridEdgeColumn()
    {
        // v2.15.1 SH: 閉じるボタンが中央寄りに見えていた不具合を修正。
        // ヘッダーを Grid 化し、閉じるボタンを Auto 幅の右端カラム（Grid.Column="1"）へ固定した。
        var src = ReadShellXaml();
        var buttonIndex = src.IndexOf("CrossSearchCloseButton_Click", StringComparison.Ordinal);
        Assert.True(buttonIndex >= 0, "CrossSearchCloseButton_Click が見つからない");
        var precedingButtonTagStart = src.LastIndexOf("<Button", buttonIndex, StringComparison.Ordinal);
        Assert.True(precedingButtonTagStart >= 0);
        var buttonTag = src.Substring(precedingButtonTagStart, buttonIndex - precedingButtonTagStart);
        Assert.Contains("Grid.Column=\"1\"", buttonTag);
        // 旧実装（DockPanel.Dock="Right" が LastChildFill に無視される構成）には戻っていないこと。
        Assert.DoesNotContain("DockPanel.Dock=\"Right\"", buttonTag);
    }


    // ── v2.16.4 SH-19: キーボードショートカット一覧 ─────────────────────

    [Fact]
    public void ShellXaml_HelpMenu_ContainsKeyboardShortcutsMenuItem()
    {
        var src = ReadShellXaml();
        Assert.Contains("キーボードショートカット(_K)", src);
        Assert.Contains("Shell.KeyboardShortcutsMenuItem", src);
        Assert.Contains("MenuKeyboardShortcuts_Click", src);
    }

    [Fact]
    public void ShortcutHelpDialog_Title_IsKeyboardShortcuts()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "Dialogs", "ShortcutHelpDialog.xaml");
        Assert.True(File.Exists(path), $"ShortcutHelpDialog.xaml not found: {path}");
        var src = File.ReadAllText(path);
        Assert.Contains("Title=\"キーボードショートカット\"", src);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string ReadShellXaml()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.xaml");
        Assert.True(File.Exists(path), $"NestSuiteShellWindow.xaml not found: {path}");
        return File.ReadAllText(path);
    }
}
