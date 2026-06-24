using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using NestSuite.ViewModels;
using NestSuite.Views;

namespace NestSuite;

/// <summary>v2.9.0 SH-21: NoteNest タブの別ウィンドウ分離／再統合処理。</summary>
public partial class NestSuiteShellWindow
{
    private readonly Dictionary<string, DetachedWorkspaceWindow> _detachedWindows = new();

    private void TabContextDetach_Click(object sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } tab && tab.IsDetachable)
            DetachNoteNestTab(tab);
    }

    private void TabContextReturnDetached_Click(object sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } tab && tab.IsDetached)
            ReturnNoteNestTab(tab.Id);
    }

    private void ReturnDetachedButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTab?.IsDetached == true)
            ReturnNoteNestTab(_selectedTab.Id);
    }

    private void DetachNoteNestTab(NestSuiteDocumentTab tab)
    {
        if (_detachedWindows.ContainsKey(tab.Id)) return;
        if (!_sessionManager.TryGet(tab.Id, out var session) || session == null) return;

        var vm = (MainViewModel)session.WorkspaceViewModel;
        var detachedWindow = new DetachedWorkspaceWindow(tab.Id, tab.DisplayName) { Owner = this };

        detachedWindow.WorkspaceView.DataContext = vm;
        WireNoteNestViewCallbacks(vm, detachedWindow.WorkspaceView);

        var capturedTabId = tab.Id;
        var capturedWindow = detachedWindow;
        // v2.9.2 SH-21: SaveAs ダイアログが別ウィンドウ上に出るよう SelectProjectSavePath を差し替える
        detachedWindow.SaveAction = () => SaveNoteNestForTabId(capturedTabId,
            defaultName => capturedWindow.SelectProjectSavePath(defaultName));
        detachedWindow.OnDetachedClosed = ReAttachNoteNestTab;

        _detachedWindows[tab.Id] = detachedWindow;

        // v2.9.1 SH-21: タブの IsDetached フラグを立てる（コンテキストメニューとプレースホルダー制御に使う）
        ReplaceTab(tab, tab with { IsDetached = true });
        ActivateTab(_tabs.First(t => t.Id == tab.Id));

        detachedWindow.Show();
    }

    /// <summary>
    /// ユーザー操作（「このタブへ戻す」ボタン・コンテキストメニュー）で呼ぶ再統合エントリポイント。
    /// 別ウィンドウを閉じてから ReAttachNoteNestTab を呼ぶ。
    /// </summary>
    private void ReturnNoteNestTab(string tabId)
    {
        if (!_detachedWindows.TryGetValue(tabId, out var dw)) return;
        dw.OnDetachedClosed = null;
        dw.Close();
        ReAttachNoteNestTab(tabId);
    }

    internal void ReAttachNoteNestTab(string tabId)
    {
        if (!_detachedWindows.ContainsKey(tabId)) return;
        _detachedWindows.Remove(tabId);

        if (!_sessionManager.TryGet(tabId, out var session) || session == null) return;
        var vm = (MainViewModel)session.WorkspaceViewModel;

        // 閉じた別ウィンドウへの参照を外す。選択中タブか否かに関係なく常に実施する。
        WireNoteNestViewCallbacks(vm, WorkspaceView);

        // v2.9.1 SH-21: IsDetached フラグを落とす
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            ReplaceTab(tab, tab with { IsDetached = false });
            tab = _tabs.First(t => t.Id == tabId);
        }

        // DataContext 更新と表示切替は ActivateTab に委ねる。
        // 別タブが選択中の場合は WorkspaceView.DataContext を上書きしない。
        if (tab != null && _selectedTab?.Id == tabId)
            ActivateTab(tab);
    }

    /// <summary>
    /// NoteNest ViewModel の View 依存コールバックを指定 View にバインドする。
    /// 別ウィンドウへの分離時と Shell への再統合時の両方で使用する。
    /// </summary>
    private void WireNoteNestViewCallbacks(MainViewModel vm, NoteNestWorkspaceView view)
    {
        vm.NavigateToLine = view.NavigateToLine;
        vm.SyncTreeSelectionCallback = note => view.SyncTreeSelection(note);
        vm.NavigateToMarker = m =>
        {
            bool shouldSwitch = m.SourceNote != null &&
                                (m.SourceNote != vm.SelectedNote || vm.IsTaskCommentMode);
            if (shouldSwitch)
            {
                vm.SelectNote(m.SourceNote!);
                view.SyncTreeSelection(m.SourceNote!);
            }
            var line = m.LineNumber;
            if (shouldSwitch)
                Dispatcher.BeginInvoke(() => vm.NavigateToLine?.Invoke(line),
                    DispatcherPriority.Loaded);
            else
                vm.NavigateToLine?.Invoke(line);
        };
    }
}
