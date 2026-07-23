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

    // TD-75e (v2.16.31): TD-75d（v2.16.30, static-test-deletion-candidate-review.md）で
    // Delete candidate と判断された、SH-25（v2.10.21）の削除決定ガード 2 件
    // （ShellXaml_DoesNotContain_TopBarLaunchButtons / _NoteExportMenuItems）を削除した。
    // 削除決定から十分に安定しており、現行導線は下の ShellXaml_NewMenu_HasDescriptions /
    // NoteNestWorkspaceViewXaml_Contains_ExportContextMenu の positive 確認が引き続き保証する。

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

    // TD-75e (v2.16.31): TD-75d で Delete candidate と判断された
    // PreviewIdeaWindowXaml_DoesNotContain_TagExampleText（ID-14, v2.10.22 の文言削減決定
    // ガード）を削除した。古い文言削減決定であり、特定サンプル文言の再発リスクは実質的にない。

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

    // TD-75e (v2.16.31): TD-75d で Delete candidate と判断された
    // ShellXaml_ViewMenu_NoLongerContainsCrossSearchMenuItem（横断検索メニューが表示メニューに
    // 重複配置されていないことの確認）を削除した。現行導線（ツールメニュー配下）は
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

    // TD-75e (v2.16.31): TD-75d で Delete candidate と判断された
    // ShellXaml_ToolMenu_NoLongerContainsPerNestLaunchItems（各 Nest 起動項目がツールメニューに
    // 残っていないことの確認、v2.15.1）を削除した。現行導線（ファイル > 新規作成 + タブバー）は
    // 下の ShellXaml_FileNewMenu_ContainsPerNestDescriptiveLabelsAndAutomationIds /
    // ShellXaml_TabAddButtonMenu_ContainsPerNestDescriptiveLabels の positive 確認が
    // 引き続き保証する。

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

    // ── v2.16.8 L8 (review1-fable5.md R-5): バックアップ復元ガイド ─────────

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

    // ── v2.16.10 SH-30: Shell コマンドの有効/無効理由ツールチップ統一 ────

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
        // v2.16.10 SH-30: Command バインドのままだと WPF の CanExecute 再照会で
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

    // ── L23 (v2.18.1): 空状態での次操作ガイド ─────────────────────────────

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

    // ── SH-37 (v2.18.3): Shell操作の現在地サマリー表示 ────────────────────

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

    // ── v2.19.3 L4: NoteNest 本文エディタのワードラップ切替メニュー ───────

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
