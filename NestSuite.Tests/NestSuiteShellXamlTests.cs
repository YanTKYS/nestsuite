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
    public void ShellXaml_NewMenu_HasDescriptions()
    {
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

    // ── Workspace 共通フォント設定（メニューバー配下） ───────────────────

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
        // フォント種類の選択導線はメニューバーに一本化する。NoteNest 上部ツールバーへ戻さない。
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml");
        var src = File.ReadAllText(path);
        Assert.DoesNotContain("EditorFontFamilyChoices", src);
        Assert.DoesNotContain("Binding EditorFontFamily,", src);
    }

    [Fact]
    public void NoteNestWorkspaceViewXaml_StillContains_EditorFontSizeComboBox()
    {
        // フォント種類と違い、フォントサイズ ComboBox は NoteNest ツールバーに残す。
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml");
        var src = File.ReadAllText(path);
        Assert.Contains("EditorFontSizeChoices", src);
        Assert.Contains("Binding EditorFontSize,", src);
    }

    // ── ID-14: IdeaNest 新規カードのサンプル表示削減 ──────────────────────

    // ── Shell 横断検索 ───────────────────────────────────────────────────

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
        // 横断検索は Shell の補助機能であり、新規 SearchNest Workspace ではない
        var src = ReadShellXaml();
        Assert.DoesNotContain("SearchNestWorkspaceView", src);
    }

    // ── 横断検索導線・メニュー整理・タブ移動ショートカット ──────────────

    // 現行導線（ツールメニュー配下）は
    // 下の ShellXaml_ToolMenu_ContainsCrossSearchMenuItem の positive 確認が引き続き保証する。

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
        // デバイス移行パックは新 Workspace ではなく Shell 補助機能としてツールメニューへ配置する。
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
        // 閉じるボタンが中央寄りに見えないよう、ヘッダーを Grid 化し
        // 閉じるボタンを Auto 幅の右端カラム（Grid.Column="1"）へ固定している。
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


    // ── SH-19: キーボードショートカット一覧 ─────────────────────

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

    // ── L8: バックアップ復元ガイド ─────────

    [Fact]
    public void ShellXaml_HelpMenu_ContainsBackupRestoreGuideMenuItem()
    {
        var src = ReadShellXaml();
        Assert.Contains("バックアップ復元ガイド(_B)", src);
        Assert.Contains("Shell.BackupRestoreGuideMenuItem", src);
        Assert.Contains("MenuBackupRestoreGuide_Click", src);
    }

    [Fact]
    public void ShellXaml_HelpMenu_BackupRestoreGuideMenuItem_HasAutomationName()
    {
        var src = ReadShellXaml();
        Assert.Contains("AutomationProperties.Name=\"Shell.BackupRestoreGuideMenuItem\"", src);
    }

    [Fact]
    public void BackupRestoreGuideDialog_Title_IsBackupRestoreGuide()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "Dialogs", "BackupRestoreGuideDialog.xaml");
        Assert.True(File.Exists(path), $"BackupRestoreGuideDialog.xaml not found: {path}");
        var src = File.ReadAllText(path);
        Assert.Contains("Title=\"バックアップ復元ガイド\"", src);
    }

    // ── SH-30: Shell コマンドの有効/無効理由ツールチップ統一 ────

    [Fact]
    public void ShellXaml_SaveMenuItems_HaveShowOnDisabled()
    {
        // 無効な MenuItem でもツールチップが表示されるよう、対象コマンドに限定して
        // ToolTipService.ShowOnDisabled="True" を設定していることを確認する。
        var src = ReadShellXaml();
        Assert.Contains("x:Name=\"SaveMenuItem\"", src);
        Assert.Contains("x:Name=\"SaveAsMenuItem\"", src);
        Assert.Contains("x:Name=\"SaveAllMenuItem\"", src);

        var occurrences = System.Text.RegularExpressions.Regex.Matches(
            src, "ToolTipService.ShowOnDisabled=\"True\"").Count;
        // Save/SaveAs/SaveAll（File メニュー）+ タブを閉じる/ピン留め/ピン留め解除（タブコンテキストメニュー）
        Assert.True(occurrences >= 6, $"ToolTipService.ShowOnDisabled が主要項目に不足している（検出数: {occurrences}）");
    }

    [Fact]
    public void ShellXaml_SaveMenuItems_UseClickHandlersNotCommandBinding()
    {
        // SH-30: Command バインドのままだと WPF の CanExecute 再照会で
        // 手動 IsEnabled 制御ができないため、Click ハンドラへ切り替えた。
        // Ctrl+S / Ctrl+Shift+S は Window.CommandBindings 側で引き続き処理する。
        var src = ReadShellXaml();
        Assert.Contains("Click=\"MenuSave_Click\"", src);
        Assert.Contains("Click=\"MenuSaveAll_Click\"", src);
        Assert.Contains("CommandBinding Command=\"ApplicationCommands.Save\" Executed=\"CommandSave_Executed\"", src);
    }

    [Fact]
    public void ShellXaml_TabContextMenu_CloseAndPinItems_HaveTooltipBindings()
    {
        var src = ReadShellXaml();
        Assert.Contains("Binding CloseMenuTooltip", src);
        Assert.Contains("Binding PinMenuTooltip", src);
        Assert.Contains("Binding UnpinMenuTooltip", src);
    }

    [Fact]
    public void ShellXaml_TabContextMenu_PinItem_UsesPinActionVisibleNotShowPinMenuItem()
    {
        // Temp タブでもピン留め項目を表示し、IsEnabled=CanPin で無効理由を出す方式へ変更した。
        // ShowPinMenuItem は既存テスト（NestSuiteDocumentTabTests）が参照するため維持するが、
        // XAML の表示制御はもう使わない。
        var src = ReadShellXaml();
        Assert.Contains("Binding PinActionVisible", src);
        Assert.DoesNotContain("Binding ShowPinMenuItem", src);
    }

    [Fact]
    public void ShellXaml_HelpMenuItems_HaveShortDescriptiveTooltips()
    {
        // 常時有効なヘルプ項目は無効理由ではなく短い説明のみを持つ。
        var src = ReadShellXaml();
        Assert.Contains("ShellCommandTooltipProvider.KeyboardShortcutsTooltip", src);
        Assert.Contains("ShellCommandTooltipProvider.BackupRestoreGuideTooltip", src);
        Assert.Contains("ShellCommandTooltipProvider.FileAssociationTooltip", src);
    }

    [Fact]
    public void NoteNestWorkspaceViewXaml_MarkdownExportMenuItems_HaveTooltipsAndShowOnDisabled()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml");
        var src = File.ReadAllText(path);
        Assert.Contains("Binding MarkdownExportSelectedNoteTooltip", src);
        Assert.Contains("Binding MarkdownExportAllNotesTooltip", src);
        Assert.Contains("ToolTipService.ShowOnDisabled=\"True\"", src);
    }

    // ── L23: 空状態での次操作ガイド ─────────────────────────────

    [Fact]
    public void NoteNestWorkspaceViewXaml_HasAllFourEmptyStateElements()
    {
        var src = ReadNoteNestWorkspaceViewXaml();
        Assert.Contains("NoteNest.EmptyState.Notebooks", src);
        Assert.Contains("NoteNest.EmptyState.Notes", src);
        Assert.Contains("NoteNest.EmptyState.Tasks", src);
        Assert.Contains("NoteNest.EmptyState.Markers", src);
    }

    [Fact]
    public void NoteNestWorkspaceViewXaml_EmptyStateElements_BindToExpectedShowProperties()
    {
        var src = ReadNoteNestWorkspaceViewXaml();
        Assert.Contains("Binding ShowNotebookEmptyState", src);
        Assert.Contains("Binding ShowNoteEmptyState", src);
        Assert.Contains("Binding ShowTaskEmptyState", src);
        Assert.Contains("Binding ShowMarkerEmptyState", src);
    }

    [Fact]
    public void NoteNestWorkspaceViewXaml_EmptyStateElements_DoNotBlockHitTesting()
    {
        // 空状態案内は一覧のスクロール・選択・右クリックを妨げないよう IsHitTestVisible=False とする。
        var src = ReadNoteNestWorkspaceViewXaml();
        var occurrences = System.Text.RegularExpressions.Regex.Matches(src, "IsHitTestVisible=\"False\"").Count;
        Assert.True(occurrences >= 4, $"IsHitTestVisible=\"False\" が期待より少ない（{occurrences}件）");
    }

    [Fact]
    public void NoteNestWorkspaceViewXaml_StillHasAddNotebookAndAddNoteButtons()
    {
        // 空状態案内の追加後も、既存の追加操作（ボタン）が失われていないことを確認する。
        var src = ReadNoteNestWorkspaceViewXaml();
        Assert.Contains("NoteNest.AddNotebookButton", src);
        Assert.Contains("NoteNest.AddNoteButton", src);
        Assert.Contains("Click=\"AddNotebook_Click\"", src);
        Assert.Contains("Click=\"AddNote_Click\"", src);
    }

    [Fact]
    public void NoteNestWorkspaceViewXaml_StillHasNotebookTreeAndTaskAndMarkerLists()
    {
        // 空状態案内の追加後も、既存の一覧（TreeView・タスク・マーカー）が削除されていないことを確認する。
        var src = ReadNoteNestWorkspaceViewXaml();
        Assert.Contains("NoteNest.NotebookTree", src);
        Assert.Contains("Binding TaskGroups", src);
        Assert.Contains("Binding FilteredMarkers", src);
    }

    // ── SH-37: Shell操作の現在地サマリー表示 ────────────────────

    [Fact]
    public void ShellXaml_HelpMenu_HasStateSummaryMenuItem()
    {
        var src = ReadShellXaml();
        Assert.Contains("Shell.StateSummaryMenuItem", src);
        Assert.Contains("現在の状態", src);
        Assert.Contains("Click=\"MenuShowStateSummary_Click\"", src);
        Assert.Contains("ShellCommandTooltipProvider.StateSummaryTooltip", src);
    }

    [Fact]
    public void StateSummaryDialogXaml_HasAllFiveItemsAndCloseButtonOnly()
    {
        var xaml = ReadStateSummaryDialogXaml();
        Assert.Contains("Shell.StateSummary.OpenTabs", xaml);
        Assert.Contains("Shell.StateSummary.UnsavedTabs", xaml);
        Assert.Contains("Shell.StateSummary.PendingRestore", xaml);
        Assert.Contains("Shell.StateSummary.DraftRecovery", xaml);
        Assert.Contains("Shell.StateSummary.TempNestSlots", xaml);
        Assert.Contains("Shell.StateSummary.CloseButton", xaml);
        Assert.Contains("Shell.StateSummaryDialog", xaml);

        // 操作ボタンは閉じるのみ（Buttonは1個だけ）。
        var buttonCount = System.Text.RegularExpressions.Regex.Matches(xaml, "<Button\\b").Count;
        Assert.Equal(1, buttonCount);
    }

    [Fact]
    public void StateSummaryDialogXaml_DoesNotUseEditableTextBox()
    {
        var xaml = ReadStateSummaryDialogXaml();
        Assert.DoesNotContain("<TextBox", xaml);
    }

    [Fact]
    public void StateSummaryDialogXaml_UsesThemeDynamicResources()
    {
        var xaml = ReadStateSummaryDialogXaml();
        Assert.Contains("DynamicResource PrimaryTextBrush", xaml);
        Assert.Contains("DynamicResource SecondaryTextBrush", xaml);
    }

    [Fact]
    public void StateSummaryDialogXaml_CloseButtonIsCancelForEscAndAltF4()
    {
        var xaml = ReadStateSummaryDialogXaml();
        Assert.Contains("IsCancel=\"True\"", xaml);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string ReadStateSummaryDialogXaml()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "Dialogs", "ShellStateSummaryDialog.xaml");
        Assert.True(File.Exists(path), $"ShellStateSummaryDialog.xaml not found: {path}");
        return File.ReadAllText(path);
    }

    private string ReadNoteNestWorkspaceViewXaml()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml");
        Assert.True(File.Exists(path), $"NoteNestWorkspaceView.xaml not found: {path}");
        return File.ReadAllText(path);
    }

    // ── L4: NoteNest 本文エディタのワードラップ切替メニュー ───────

    [Fact]
    public void ShellXaml_ContainsNoteNestWordWrapMenuItem_Checkable()
    {
        var src = ReadShellXaml();
        var start = src.IndexOf("x:Name=\"NoteNestWordWrapMenuItem\"", StringComparison.Ordinal);
        Assert.True(start >= 0, "NoteNestWordWrapMenuItem が見つからない");
        var end = src.IndexOf("/>", start, StringComparison.Ordinal);
        Assert.True(end >= 0);
        var element = src.Substring(start, end - start);

        Assert.Contains("IsCheckable=\"True\"", element);
        Assert.Contains("Click=\"MenuNoteNestWordWrap_Click\"", element);
    }

    [Fact]
    public void ShellXaml_NoteNestWordWrapMenuItem_IsUnderViewMenu_NotDuplicated()
    {
        var src = ReadShellXaml();
        var occurrences = System.Text.RegularExpressions.Regex.Matches(src, "Click=\"MenuNoteNestWordWrap_Click\"").Count;
        Assert.Equal(1, occurrences);
    }

    private string ReadShellXaml()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.xaml");
        Assert.True(File.Exists(path), $"NestSuiteShellWindow.xaml not found: {path}");
        return File.ReadAllText(path);
    }
}
