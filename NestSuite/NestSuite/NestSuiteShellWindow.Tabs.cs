using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using NestSuite.ChatNest;
using NestSuite.FileAssociation;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.IdeaNest.Services;
using NestSuite.NoteNest.Editor;
using NestSuite.Services;
using NestSuite.TempNest;
using NestSuite.ViewModels;
using NestSuite.Views;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    /// <summary>
    /// 指定タブをアクティブ化し、Workspace 表示・サイドバーハイライト・メニュー・ステータスバーを同期する。
    /// v1.7.3: SelectTool を置き換え、タブモデルを通じてツール切替を一元管理する。
    /// <see cref="_isActivatingTab"/> ガードにより TabStrip の SelectionChanged との再帰を防ぐ。
    /// </summary>
    private void ActivateTab(NestSuiteDocumentTab tab)
    {
        if (_isActivatingTab) return;
        _isActivatingTab = true;
        try
        {
            _selectedTab = tab;
            TabStrip.SelectedItem = tab;

            // v2.6.0: TempNest は ToolDefinitions に登録されていないため先に処理する
            if (tab.WorkspaceKind == NestSuiteWorkspaceKind.Temp)
            {
                WorkspaceView.Visibility           = Visibility.Collapsed;
                ChatWorkspaceView.Visibility       = Visibility.Collapsed;
                IdeaNestWorkspaceView.Visibility   = Visibility.Collapsed;
                TempNestWorkspaceView.Visibility   = Visibility.Visible;
                UnintegratedPlaceholder.Visibility = Visibility.Collapsed;

                if (_sessionManager.TryGet(tab.Id, out var tempSession) && tempSession != null)
                    TempNestWorkspaceView.DataContext = tempSession.WorkspaceViewModel;

                foreach (var kvp in _sidebarBorders)
                    UpdateSidebarHighlight(kvp.Value, kvp.Key, "");
                foreach (var kvp in _toolMenuItems)
                    kvp.Value.IsChecked = false;

                NestSuiteModeSuffix.Text = "  /  TempNest";
                RefreshWorkspaceStatus();
                return;
            }

            var toolId = tab.ToolId;
            var tool = NestSuiteToolRegistry.ToolDefinitions.First(t => t.Id == toolId);

            bool isNoteNest = toolId == NestSuiteToolRegistry.NoteNestToolId;
            bool isChatNest = toolId == NestSuiteToolRegistry.ChatNestToolId;
            bool isIdeaNest = toolId == NestSuiteToolRegistry.IdeaNestToolId;

            // Workspace 表示切替（選択タブに対応する Workspace のみ表示）
            WorkspaceView.Visibility           = isNoteNest ? Visibility.Visible : Visibility.Collapsed;
            ChatWorkspaceView.Visibility       = isChatNest ? Visibility.Visible : Visibility.Collapsed;
            IdeaNestWorkspaceView.Visibility   = isIdeaNest ? Visibility.Visible : Visibility.Collapsed;
            TempNestWorkspaceView.Visibility   = Visibility.Collapsed;
            UnintegratedPlaceholder.Visibility = tool.IsIntegrated ? Visibility.Collapsed : Visibility.Visible;

            // v1.9.5: NoteNest タブ切替時に選択タブの MainViewModel に DataContext を差し替える
            if (isNoteNest && _sessionManager.TryGet(tab.Id, out var noteNestSession) && noteNestSession != null)
                DataContext = noteNestSession.WorkspaceViewModel;

            // v1.9.2: ChatNest タブ切替時に選択タブの ViewModel に DataContext を差し替える
            if (isChatNest && _sessionManager.TryGet(tab.Id, out var chatSession) && chatSession != null)
                ChatWorkspaceView.DataContext = chatSession.WorkspaceViewModel;

            // v1.9.7: IdeaNest タブ切替時に選択タブの ViewModel に DataContext を差し替える
            if (isIdeaNest && _sessionManager.TryGet(tab.Id, out var ideaNestSession) && ideaNestSession != null)
                IdeaNestWorkspaceView.DataContext = ideaNestSession.WorkspaceViewModel;
            if (!tool.IsIntegrated)
            {
                PlaceholderTitle.Text = tool.DisplayName;
                PlaceholderMessage.Text =
                    $"{tool.DisplayName} はまだ統合されていません。\n将来のバージョンで統合予定です。";
            }

            // サイドバー選択ハイライト更新
            foreach (var kvp in _sidebarBorders)
                UpdateSidebarHighlight(kvp.Value, kvp.Key, toolId);

            // ツールメニューのチェック状態更新
            foreach (var kvp in _toolMenuItems)
                kvp.Value.IsChecked = kvp.Key == toolId;

            // ステータスバー更新
            NestSuiteModeSuffix.Text = $"  /  {tool.DisplayName}";
            RefreshWorkspaceStatus();
        }
        finally
        {
            _isActivatingTab = false;
        }
    }

    /// <summary>
    /// 指定ツール ID に対応するタブを開く。既存タブがあればそれをアクティブ化し、
    /// なければ無題タブを新規作成してアクティブ化する。
    /// v1.7.3: サイドバー・ツールメニューのクリックから呼ばれるタブランチャーエントリポイント。
    /// </summary>
    private void EnsureTabForToolId(string toolId)
    {
        var existing = _tabs.FirstOrDefault(t => t.ToolId == toolId);
        if (existing != null)
        {
            ActivateTab(existing);
            return;
        }

        var kind = toolId switch
        {
            NestSuiteToolRegistry.NoteNestToolId => NestSuiteWorkspaceKind.NoteNest,
            NestSuiteToolRegistry.ChatNestToolId => NestSuiteWorkspaceKind.ChatNest,
            NestSuiteToolRegistry.IdeaNestToolId => NestSuiteWorkspaceKind.IdeaNest,
            _ => throw new ArgumentException($"未知のツール ID: {toolId}", nameof(toolId))
        };

        var tab = NestSuiteTabFactory.CreateUntitled(kind);
        _tabs.Add(tab);
        _sessionManager.Add(CreateSessionForTab(tab));
        ActivateTab(tab);
    }

    private static void UpdateSidebarHighlight(Border border, string borderToolId, string selectedToolId)
    {
        if (borderToolId == selectedToolId)
            border.SetResourceReference(Border.BackgroundProperty, "SelectedNoteBg");
        else
            border.ClearValue(Border.BackgroundProperty);
    }

    private void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isActivatingTab) return;
        if (TabStrip.SelectedItem is NestSuiteDocumentTab tab)
            ActivateTab(tab);
    }

    /// <summary>
    /// oldTab をコレクション内で newTab に置き換え、選択中だった場合は _selectedTab と TabStrip 選択状態も更新する。
    /// _isActivatingTab ガードにより TabStrip_SelectionChanged との再帰を防ぐ。
    /// </summary>
    private void ReplaceTab(NestSuiteDocumentTab oldTab, NestSuiteDocumentTab newTab)
    {
        var index = _tabs.IndexOf(oldTab);
        if (index < 0) return;
        _tabs[index] = newTab;
        // v1.9.1: TabId は変わらないため Session は既存のものを更新する（削除・再追加しない）
        if (_sessionManager.TryGet(oldTab.Id, out var session) && session != null)
        {
            session.FilePath = newTab.FilePath;
            session.IsModified = newTab.IsModified;
        }
        if (_selectedTab?.Id == oldTab.Id)
        {
            _selectedTab = newTab;
            _isActivatingTab = true;
            try { TabStrip.SelectedItem = newTab; }
            finally { _isActivatingTab = false; }
        }
    }

    /// <summary>
    /// v1.9.5: 指定した NoteNest MainViewModel に対応するタブの FilePath・IsModified を同期する。
    /// Session Manager から ViewModel に対応する Session を逆引きしてタブを更新する。
    /// ChatNest の <see cref="SyncChatNestTabForViewModel"/> と対称な実装。
    /// </summary>
    private void SyncNoteNestTabForViewModel(MainViewModel vm)
    {
        var session = _sessionManager.Sessions.FirstOrDefault(s => ReferenceEquals(s.WorkspaceViewModel, vm));
        if (session == null) return;
        var tab = _tabs.FirstOrDefault(t => t.Id == session.TabId);
        if (tab == null) return;
        NestSuiteDocumentTab updatedTab;
        if (vm.CurrentFilePath is string path &&
            NestSuiteTabFactory.TryGetKind(path, out var kind) &&
            kind == NestSuiteWorkspaceKind.NoteNest)
            updatedTab = NestSuiteTabFactory.FromFilePath(path) with { Id = tab.Id, IsModified = vm.IsModified };
        else
            updatedTab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest) with { Id = tab.Id, IsModified = vm.IsModified };
        ReplaceTab(tab, updatedTab);
    }

    /// <summary>
    /// v1.9.2: 指定した ChatNest ViewModel に対応するタブの IsModified を同期する。
    /// Session Manager から ViewModel に対応する Session を逆引きしてタブを更新する。
    /// </summary>
    private void SyncChatNestTabForViewModel(ChatNestWorkspaceViewModel vm) =>
        SyncTabModifiedState(vm, vm.HasUnsavedChanges);

    private void OnNoteNestSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsModified) &&
            sender is MainViewModel saveVm && !saveVm.IsModified && saveVm.CurrentFilePath != null)
        {
            var s = _sessionManager.Sessions
                .FirstOrDefault(s2 => ReferenceEquals(s2.WorkspaceViewModel, saveVm));
            if (s?.IsModified == true)
                ShowStatusNotification("  |  保存しました");
        }

        if (e.PropertyName is nameof(MainViewModel.CurrentFilePath) or nameof(MainViewModel.IsModified) &&
            sender is MainViewModel vm)
            SyncNoteNestTabForViewModel(vm);

        // v1.14.0: CurrentFilePath 変化時にセッションがすでに存在する場合は保存先として最近ファイルに追加する
        // （セッション登録前の OpenFileAtStartup による変化は session == null で除外される）
        if (e.PropertyName == nameof(MainViewModel.CurrentFilePath) &&
            sender is MainViewModel noteVm &&
            noteVm.CurrentFilePath is string filePath)
        {
            var session = _sessionManager.Sessions.FirstOrDefault(s => ReferenceEquals(s.WorkspaceViewModel, noteVm));
            if (session != null)
            {
                _recentFiles.Add(filePath);
                UpdateRecentFilesMenu();
            }
        }

        if (e.PropertyName == nameof(MainViewModel.EditorFontSize) &&
            sender is MainViewModel fontVm &&
            !_suppressFontSizePropagation &&
            Math.Abs(fontVm.EditorFontSize - _noteNestEditorFontSize) > 0.01)
        {
            _noteNestEditorFontSize = fontVm.EditorFontSize;
            foreach (var s in _sessionManager.Sessions
                .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest &&
                            !ReferenceEquals(s.WorkspaceViewModel, fontVm)))
            {
                if (s.WorkspaceViewModel is MainViewModel otherVm)
                    otherVm.EditorFontSize = _noteNestEditorFontSize;
            }
            var uiSvc = new UiSettingsService();
            var ui = uiSvc.Load();
            ui.NoteNestEditorFontSize = _noteNestEditorFontSize;
            uiSvc.Save(ui);
        }

        if (e.PropertyName is nameof(MainViewModel.MarkerCount) or nameof(MainViewModel.TotalIncompleteTaskCountText) &&
            sender is MainViewModel statusVm && IsActiveVm(statusVm))
            RefreshWorkspaceStatus();
    }

    private void OnChatNestPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatNestWorkspaceViewModel.HasUnsavedChanges) &&
            sender is ChatNestWorkspaceViewModel vm)
            SyncChatNestTabForViewModel(vm);

        if (e.PropertyName is nameof(ChatNestWorkspaceViewModel.HasUnsavedChanges)
                           or nameof(ChatNestWorkspaceViewModel.SelectedSpeaker) &&
            sender is ChatNestWorkspaceViewModel statusVm && IsActiveVm(statusVm))
            RefreshWorkspaceStatus();
    }

    private void OnIdeaNestPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IdeaNestWorkspaceViewModel.HasChanges) &&
            sender is IdeaNestWorkspaceViewModel vm)
            SyncIdeaNestTabForViewModel(vm);

        if (e.PropertyName is nameof(IdeaNestWorkspaceViewModel.HasChanges)
                           or nameof(IdeaNestWorkspaceViewModel.CountText)
                           or nameof(IdeaNestWorkspaceViewModel.HasActiveFilter) &&
            sender is IdeaNestWorkspaceViewModel statusVm && IsActiveVm(statusVm))
            RefreshWorkspaceStatus();
    }

    private bool IsActiveVm(object vm)
    {
        if (!TryGetActiveSession(out var session) || session == null) return false;
        return ReferenceEquals(session.WorkspaceViewModel, vm);
    }

    private void RefreshWorkspaceStatus()
    {
        if (_isShowingNotification) return;
        if (!TryGetActiveSession(out var session) || session == null)
        {
            WorkspaceStatusText.Text = "";
            return;
        }
        WorkspaceStatusText.Text = session.WorkspaceViewModel switch
        {
            MainViewModel vm                  => BuildNoteNestStatusText(vm),
            ChatNestWorkspaceViewModel vm     => BuildChatNestStatusText(vm),
            IdeaNestWorkspaceViewModel vm     => BuildIdeaNestStatusText(vm),
            TempNestWorkspaceViewModel        => "",
            _                                 => ""
        };
    }

    private static string BuildNoteNestStatusText(MainViewModel vm)
    {
        var noteCount   = vm.AllNotes.Count();
        var taskCount   = vm.TaskGroups.Sum(g => g.Tasks.Count);
        var markerCount = vm.MarkerCount;
        return $"  |  ノート {noteCount}  タスク {taskCount}  マーカー {markerCount}";
    }

    private static string BuildIdeaNestStatusText(IdeaNestWorkspaceViewModel vm)
    {
        var filterText = vm.HasActiveFilter ? "  フィルター中" : "";
        return $"  |  {vm.CountText}{filterText}";
    }

    private static string BuildChatNestStatusText(ChatNestWorkspaceViewModel vm) =>
        $"  |  発言 {vm.Messages.Count}  発言者: {vm.SelectedSpeaker}";

    /// <summary>
    /// v1.9.7: 指定 IdeaNest ViewModel に対応するタブの IsModified を HasChanges に同期する。
    /// Session Manager で ViewModel から逆引きしてタブを特定する。
    /// </summary>
    private void SyncIdeaNestTabForViewModel(IdeaNestWorkspaceViewModel vm) =>
        SyncTabModifiedState(vm, vm.HasChanges);

    /// <summary>
    /// v1.9.7: IdeaNest タブを閉じる前の確認と PropertyChanged 購読解除。
    /// ViewModel はタブごとの独立インスタンスのため LoadFromWorkspace リセットは不要。
    /// </summary>
    private bool ConfirmAndResetIdeaNest(NestSuiteDocumentTab tab) =>
        ConfirmTabClose(tab, () =>
        {
            if (_sessionManager.TryGet(tab.Id, out var session) &&
                session?.WorkspaceViewModel is IdeaNestWorkspaceViewModel vm)
            {
                vm.PropertyChanged -= OnIdeaNestPropertyChanged;
                vm.Dispose();
            }
        });

    // ── v1.7.6: タブを閉じる操作 ──────────────────────────────────────────

    /// <summary>
    /// タブの × ボタンクリックハンドラ。Button.Tag にバインドされたタブモデルを取り出し、
    /// <see cref="CloseTab"/> に委譲する。e.Handled = true で ListBoxItem 選択変更の
    /// 余分な伝播を抑制する。
    /// </summary>
    private void TabClose_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is NestSuiteDocumentTab tab)
            CloseTab(tab);
        e.Handled = true;
    }

    // ── v2.4.0 SH-2: タブコンテキストメニュー ────────────────────────────

    private void TabContextClose_Click(object sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } tab)
            CloseTab(tab);
    }

    private void TabContextCloseOthers_Click(object sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } keepTab)
            CloseOtherTabs(keepTab);
    }

    private void TabContextCloseRight_Click(object sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } pivotTab)
            CloseTabsToRight(pivotTab);
    }

    private static NestSuiteDocumentTab? GetTabFromContextMenuItem(object sender)
    {
        if (sender is MenuItem mi &&
            mi.Parent is ContextMenu cm &&
            cm.PlacementTarget is FrameworkElement el &&
            el.DataContext is NestSuiteDocumentTab tab)
            return tab;
        return null;
    }

    /// <summary>
    /// v2.4.0 SH-2: keepTab 以外のすべてのタブを順に閉じる。未保存確認を各タブで行う。
    /// いずれかのタブでユーザーがキャンセルした場合、そのタブ以降の処理を中断する。
    /// </summary>
    private void CloseOtherTabs(NestSuiteDocumentTab keepTab)
    {
        foreach (var tab in _tabs.Where(t => t.Id != keepTab.Id && t.CanClose).ToList())
        {
            if (!CloseTab(tab)) break;
        }
    }

    /// <summary>
    /// v2.4.0 SH-2: pivotTab より右側（インデックスが大きい）のタブを順に閉じる。未保存確認を各タブで行う。
    /// いずれかのタブでユーザーがキャンセルした場合、そのタブ以降の処理を中断する。
    /// </summary>
    private void CloseTabsToRight(NestSuiteDocumentTab pivotTab)
    {
        var idx = _tabs.IndexOf(pivotTab);
        if (idx < 0) return;
        foreach (var tab in _tabs.Skip(idx + 1).ToList())
        {
            if (!CloseTab(tab)) break;
        }
    }

    // ── v2.4.0 SH-3: 中クリックでタブを閉じる ────────────────────────────

    /// <summary>v2.4.0 SH-3: 中ボタンクリックで対象タブを閉じる。未保存確認を通す。</summary>
    private void TabStrip_PreviewMouseButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle) return;
        var tab = GetTabFromVisualTree(e.OriginalSource as DependencyObject);
        if (tab == null) return;
        if (!tab.CanClose) return;
        CloseTab(tab);
        e.Handled = true;
    }

    // ── v2.4.0 SH-4: タブ切替キーボードショートカット ───────────────────

    /// <summary>
    /// v2.4.0 SH-4: Ctrl+Tab / Ctrl+Shift+Tab / Ctrl+1〜9 でタブを切り替える。
    /// NoteNest の Ctrl+Enter / Escape など既存ショートカットは e.Handled = false のままにして
    /// WPF の通常ルーティングへ流す。
    /// </summary>
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Handled) return;

        var ctrl  = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        var shift = (Keyboard.Modifiers & ModifierKeys.Shift)   == ModifierKeys.Shift;

        if (ctrl && e.Key == Key.Tab)
        {
            NavigateTab(forward: !shift);
            e.Handled = true;
            return;
        }

        if (ctrl && !shift && e.Key >= Key.D1 && e.Key <= Key.D9)
        {
            var targetIndex = e.Key - Key.D1;
            if (targetIndex < _tabs.Count)
                ActivateTab(_tabs[targetIndex]);
            e.Handled = true;
            return;
        }

        if (ctrl && !shift && e.Key == Key.F &&
            _selectedTab?.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest)
        {
            if (TryGetActiveSession(out var session) && session?.WorkspaceViewModel is MainViewModel vm)
            {
                var state = WorkspaceView.GetFindReplaceState("", "", null, null);
                WorkspaceView.OpenFindReplace(state.LastSearchText, state.LastReplaceText,
                    state.Left, state.Top,
                    note =>
                    {
                        if (note != vm.SelectedNote || vm.IsTaskCommentMode)
                        {
                            vm.SelectNote(note);
                            WorkspaceView.SyncTreeSelection(note);
                        }
                    });
            }
            e.Handled = true;
        }
    }

    /// <summary>v2.4.0 SH-4: タブを前後方向に循環移動する。</summary>
    private void NavigateTab(bool forward)
    {
        if (_tabs.Count == 0) return;
        if (_selectedTab == null) { ActivateTab(_tabs[0]); return; }
        var idx = _tabs.IndexOf(_selectedTab);
        if (idx < 0) return;
        var newIdx = forward
            ? (idx + 1) % _tabs.Count
            : (idx - 1 + _tabs.Count) % _tabs.Count;
        ActivateTab(_tabs[newIdx]);
    }

    /// <summary>
    /// 指定タブを閉じる。
    /// 未保存の場合は確認ダイアログを表示し、キャンセル時はタブを残して false を返す。
    /// 閉じた後は右隣または左隣のタブをアクティブ化する。
    /// タブが 0 件になった場合は無題 NoteNest タブを自動作成する。
    ///
    /// <para>NoteNest: <see cref="ConfirmAndResetNoteNest"/> で確認後 PropertyChanged 購読解除・Dispose。</para>
    /// <para>ChatNest: <see cref="ConfirmAndResetChatNest"/> で確認後 PropertyChanged 購読解除。</para>
    /// <para>IdeaNest: <see cref="ConfirmAndResetIdeaNest"/> で確認後 PropertyChanged 購読解除。</para>
    /// <returns>タブを閉じた場合 true、ユーザーがキャンセルした場合 false。</returns>
    /// </summary>
    private bool CloseTab(NestSuiteDocumentTab tab)
    {
        // Id で検索して最新のタブを取得（Button.Tag バインドが古いレコードを持つ場合に備える）
        var idx = -1;
        for (int i = 0; i < _tabs.Count; i++)
        {
            if (_tabs[i].Id == tab.Id)
            {
                idx = i;
                tab = _tabs[i];
                break;
            }
        }
        if (idx < 0) return false;

        // v2.6.0: Temp タブなど CanClose=false のタブは閉じない
        if (!tab.CanClose) return false;

        switch (tab.WorkspaceKind)
        {
            case NestSuiteWorkspaceKind.NoteNest:
                if (!ConfirmAndResetNoteNest(tab)) return false;
                break;

            case NestSuiteWorkspaceKind.ChatNest:
                if (!ConfirmAndResetChatNest(tab)) return false;
                break;

            case NestSuiteWorkspaceKind.IdeaNest:
                if (!ConfirmAndResetIdeaNest(tab)) return false;
                break;
        }

        // v1.9.1: タブ削除と同時に対応 Session を破棄する
        _sessionManager.Remove(tab.Id);
        _tabs.RemoveAt(idx);

        // v2.6.0: Temp タブが常に存在するため _tabs.Count == 0 にはならない
        // 右隣を優先、なければ左隣（最後のタブなら idx-1）
        var nextIdx = Math.Min(idx, _tabs.Count - 1);
        ActivateTab(_tabs[nextIdx]);
        return true;
    }

    /// <summary>
    /// NoteNest タブを閉じる前の確認とリセット。
    /// 未保存の場合は確認ダイアログを表示。確認後は PropertyChanged 購読を解除する。
    /// v1.9.5: ViewModel はタブごとの独立インスタンスのため CreateNewProjectDirect() は不要。
    /// </summary>
    private bool ConfirmAndResetNoteNest(NestSuiteDocumentTab tab) =>
        ConfirmTabClose(tab, () =>
        {
            if (_sessionManager.TryGet(tab.Id, out var session) &&
                session?.WorkspaceViewModel is MainViewModel vm)
            {
                vm.PropertyChanged -= OnNoteNestSessionPropertyChanged;
                vm.Dispose();
            }
        });

    /// <summary>
    /// ChatNest タブを閉じる前の確認とリセット。
    /// 未保存の場合は確認ダイアログを表示。確認後は <see cref="ChatNestWorkspaceViewModel.Clear"/>
    /// でリセットする。
    /// </summary>
    private bool ConfirmAndResetChatNest(NestSuiteDocumentTab tab) =>
        ConfirmTabClose(tab, () =>
        {
            if (_sessionManager.TryGet(tab.Id, out var session) &&
                session?.WorkspaceViewModel is ChatNestWorkspaceViewModel vm)
            {
                vm.PropertyChanged -= OnChatNestPropertyChanged;
                vm.Dispose();
            }
        });
}
