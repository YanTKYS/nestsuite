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

            // v2.9.0 SH-21: 別ウィンドウ表示中の NoteNest タブにはプレースホルダーを出す
            bool isDetachedNoteNest = isNoteNest && _detachedWindows.ContainsKey(tab.Id);

            // Workspace 表示切替（選択タブに対応する Workspace のみ表示）
            WorkspaceView.Visibility               = (isNoteNest && !isDetachedNoteNest) ? Visibility.Visible : Visibility.Collapsed;
            ChatWorkspaceView.Visibility           = isChatNest ? Visibility.Visible : Visibility.Collapsed;
            IdeaNestWorkspaceView.Visibility       = isIdeaNest ? Visibility.Visible : Visibility.Collapsed;
            TempNestWorkspaceView.Visibility       = Visibility.Collapsed;
            UnintegratedPlaceholder.Visibility     = tool.IsIntegrated ? Visibility.Collapsed : Visibility.Visible;
            DetachedNoteNestPlaceholder.Visibility = isDetachedNoteNest ? Visibility.Visible : Visibility.Collapsed;

            // v1.9.5: NoteNest タブ切替時（非別ウィンドウ）に選択タブの MainViewModel に DataContext を差し替える
            if (isNoteNest && !isDetachedNoteNest &&
                _sessionManager.TryGet(tab.Id, out var noteNestSession) && noteNestSession != null)
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

    // ── v2.4.0 SH-4: タブ切替キーボードショートカット ───────────────────

    /// <summary>
    /// v2.4.0 SH-4: Ctrl+Tab / Ctrl+Shift+Tab / Ctrl+1〜9 / Shift+←→ でタブを切り替える。
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

        if (shift && !ctrl && (e.Key == Key.Left || e.Key == Key.Right))
        {
            NavigateTab(forward: e.Key == Key.Right);
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
}
