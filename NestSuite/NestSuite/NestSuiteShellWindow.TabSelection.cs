using System.Windows;
using System.Windows.Input;
using NestSuite.ChatNest;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.TempNest;
using NestSuite.ViewModels;

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

                // v2.13.3 SH-30: TempNest にはファイル名表示がないため「/」を前置しない
                NestSuiteModeSuffix.Text = "TempNest";
                RefreshWorkspaceStatus();
                return;
            }

            var toolId = tab.ToolId;
            var tool = NestSuiteToolRegistry.ToolDefinitions.First(t => t.Id == toolId);

            bool isNoteNest = toolId == NestSuiteToolRegistry.NoteNestToolId;
            bool isChatNest = toolId == NestSuiteToolRegistry.ChatNestToolId;
            bool isIdeaNest = toolId == NestSuiteToolRegistry.IdeaNestToolId;

            // v2.9.0 SH-21: 別ウィンドウ表示中のタブにはプレースホルダーを出す
            bool isDetachedNoteNest = isNoteNest && _detachedWindows.ContainsKey(tab.Id);
            // v2.9.3 SH-21: IdeaNest も別ウィンドウ表示に対応
            bool isDetachedIdeaNest = isIdeaNest && _detachedWindows.ContainsKey(tab.Id);
            // v2.9.4 SH-21: ChatNest も別ウィンドウ表示に対応
            bool isDetachedChatNest = isChatNest && _detachedWindows.ContainsKey(tab.Id);

            // Workspace 表示切替（選択タブに対応する Workspace のみ表示）
            WorkspaceView.Visibility               = (isNoteNest && !isDetachedNoteNest) ? Visibility.Visible : Visibility.Collapsed;
            ChatWorkspaceView.Visibility           = (isChatNest && !isDetachedChatNest) ? Visibility.Visible : Visibility.Collapsed;
            IdeaNestWorkspaceView.Visibility       = (isIdeaNest && !isDetachedIdeaNest) ? Visibility.Visible : Visibility.Collapsed;
            TempNestWorkspaceView.Visibility       = Visibility.Collapsed;
            UnintegratedPlaceholder.Visibility     = tool.IsIntegrated ? Visibility.Collapsed : Visibility.Visible;
            DetachedNoteNestPlaceholder.Visibility = (isDetachedNoteNest || isDetachedIdeaNest || isDetachedChatNest) ? Visibility.Visible : Visibility.Collapsed;

            // v1.9.5: NoteNest タブ切替時（非別ウィンドウ）に選択タブの MainViewModel に DataContext を差し替える
            if (isNoteNest && !isDetachedNoteNest &&
                _sessionManager.TryGet(tab.Id, out var noteNestSession) && noteNestSession != null)
                DataContext = noteNestSession.WorkspaceViewModel;

            // v1.9.2: ChatNest タブ切替時（非別ウィンドウ）に選択タブの ViewModel に DataContext を差し替える
            if (isChatNest && !isDetachedChatNest &&
                _sessionManager.TryGet(tab.Id, out var chatSession) && chatSession != null)
                ChatWorkspaceView.DataContext = chatSession.WorkspaceViewModel;

            // v1.9.7: IdeaNest タブ切替時（非別ウィンドウ）に選択タブの ViewModel に DataContext を差し替える
            if (isIdeaNest && !isDetachedIdeaNest &&
                _sessionManager.TryGet(tab.Id, out var ideaNestSession) && ideaNestSession != null)
                IdeaNestWorkspaceView.DataContext = ideaNestSession.WorkspaceViewModel;
            if (!tool.IsIntegrated)
            {
                PlaceholderTitle.Text = tool.DisplayName;
                PlaceholderMessage.Text =
                    $"{tool.DisplayName} はまだ統合されていません。\n将来のバージョンで統合予定です。";
            }

            // ステータスバー更新
            NestSuiteModeSuffix.Text = $"  /  {tool.DisplayName}";
            RefreshWorkspaceStatus();
        }
        finally
        {
            _isActivatingTab = false;
        }
    }

    // ── v2.4.0 SH-4: タブ切替キーボードショートカット ───────────────────

    /// <summary>
    /// v2.4.0 SH-4: Ctrl+Tab / Ctrl+Shift+Tab / Ctrl+1〜9 でタブを切り替える。
    /// NoteNest の Ctrl+Enter / Escape など既存ショートカットは e.Handled = false のままにして
    /// WPF の通常ルーティングへ流す。
    /// v2.15.1 SH: Shift+←→ によるタブ切替は廃止した。テキスト入力中の範囲選択操作
    /// （TextBox 等の Shift+←→ による文字選択）と競合するため、Shell 側では横取りしない。
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

    private bool IsActiveVm(object vm)
    {
        if (!TryGetActiveSession(out var session) || session == null) return false;
        return ReferenceEquals(session.WorkspaceViewModel, vm);
    }

    private void RefreshWorkspaceStatus()
    {
        RefreshShellStatusBar();

        if (_transientStatus.IsActive) return;
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

    /// <summary>
    /// v2.13.3 SH-30: ステータスバー左側（ファイル名・未保存表示）を、
    /// 現在アクティブな <see cref="_selectedTab"/> を基準に再計算する。
    /// Window.DataContext（NoteNest 固有の MainViewModel）には依存しない。
    /// 閉じたタブや非アクティブな Workspace の状態が残らないよう、タブ切替・タブクローズ・
    /// 未保存状態変化のたびに <see cref="RefreshWorkspaceStatus"/> 経由で呼び出す。
    /// </summary>
    private void RefreshShellStatusBar()
    {
        var tab = _selectedTab;
        if (tab == null)
        {
            ShellStatusFileText.Text = "";
            ShellStatusUnsavedText.Text = "";
            ShellStatusUnsavedText.Visibility = Visibility.Collapsed;
            return;
        }

        // TempNest はファイルに紐づかないため、ファイル名欄は空のままにする（L20/SH-29 とは別軸）
        ShellStatusFileText.Text = tab.WorkspaceKind == NestSuiteWorkspaceKind.Temp ? "" : tab.DisplayName;

        // NoteNest はアクティブ Session の MainViewModel から経過時間つき表示を引き継ぐ。
        // それ以外は tab.IsModified に基づく既定表示にする。
        var unsavedText = "● 未保存";
        if (tab.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest &&
            TryGetActiveSession(out var noteSession) &&
            noteSession?.WorkspaceViewModel is MainViewModel noteVm)
            unsavedText = noteVm.UnsavedIndicatorText;

        ShellStatusUnsavedText.Text = unsavedText;
        ShellStatusUnsavedText.Visibility = tab.IsModified ? Visibility.Visible : Visibility.Collapsed;
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
}
