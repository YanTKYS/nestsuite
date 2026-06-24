using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.IdeaNest.Views;
using NestSuite.ViewModels;
using NestSuite.Views;

namespace NestSuite;

/// <summary>v2.9.0 SH-21: NoteNest / IdeaNest タブの別ウィンドウ分離／再統合処理。</summary>
public partial class NestSuiteShellWindow
{
    private readonly Dictionary<string, DetachedWorkspaceWindow> _detachedWindows = new();

    private void TabContextDetach_Click(object sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is not { } tab || !tab.IsDetachable) return;
        if (tab.IsNoteNest)
            DetachNoteNestTab(tab);
        else if (tab.IsIdeaNest)
            DetachIdeaNestTab(tab);
    }

    private void TabContextReturnDetached_Click(object sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } tab && tab.IsDetached)
            ReturnDetachedTab(tab.Id);
    }

    private void ReturnDetachedButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTab?.IsDetached == true)
            ReturnDetachedTab(_selectedTab.Id);
    }

    private void ReturnDetachedTab(string tabId)
    {
        if (!_detachedWindows.TryGetValue(tabId, out var dw)) return;
        dw.OnDetachedClosed = null;
        dw.Close();
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab?.IsNoteNest == true)
            ReAttachNoteNestTab(tabId);
        else if (tab?.IsIdeaNest == true)
            ReAttachIdeaNestTab(tabId);
    }

    private void DetachNoteNestTab(NestSuiteDocumentTab tab)
    {
        if (_detachedWindows.ContainsKey(tab.Id)) return;
        if (!_sessionManager.TryGet(tab.Id, out var session) || session == null) return;

        var vm = (MainViewModel)session.WorkspaceViewModel;
        var view = new NoteNestWorkspaceView { DataContext = vm };
        var detachedWindow = new DetachedWorkspaceWindow(tab.Id, tab.DisplayName, view) { Owner = this };
        view.DialogHost = detachedWindow;
        WireNoteNestViewCallbacks(vm, view);

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

    private void DetachIdeaNestTab(NestSuiteDocumentTab tab)
    {
        if (_detachedWindows.ContainsKey(tab.Id)) return;
        if (!_sessionManager.TryGet(tab.Id, out var session) || session == null) return;

        var vm = (IdeaNestWorkspaceViewModel)session.WorkspaceViewModel;
        var view = new IdeaNestWorkspaceView
        {
            DataContext = vm,
            ShowMenu = false
        };

        var detachedWindow = new DetachedWorkspaceWindow(tab.Id, tab.DisplayName, view) { Owner = this };

        var capturedTabId = tab.Id;
        var capturedWindow = detachedWindow;
        detachedWindow.SaveAction = () => SaveIdeaNestForTabId(capturedTabId,
            defaultName => capturedWindow.SelectIdeaNestSavePath(defaultName));
        detachedWindow.OnDetachedClosed = ReAttachIdeaNestTab;

        _detachedWindows[tab.Id] = detachedWindow;

        ReplaceTab(tab, tab with { IsDetached = true });
        ActivateTab(_tabs.First(t => t.Id == tab.Id));

        detachedWindow.Show();
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

    internal void ReAttachIdeaNestTab(string tabId)
    {
        if (!_detachedWindows.ContainsKey(tabId)) return;
        _detachedWindows.Remove(tabId);

        // v2.9.3 SH-21: detached window が閉じた後、owner resolver を Shell 側へ明示的に戻す。
        // Shell の IdeaNestWorkspaceView は detach 前から同じ VM を DataContext に持つため、
        // ActivateTab での DataContext 再代入だけでは DataContextChanged が発火せず
        // ConfigureWorkspace() が実行されない。SetOwnerResolver を直接呼ぶことで解決する。
        // 未選択タブの再統合時（ActivateTab を呼ばないケース）にも確実に戻るようここで実施する。
        if (_sessionManager.TryGet(tabId, out var session) &&
            session?.WorkspaceViewModel is IdeaNestWorkspaceViewModel ideaNestVm)
            ideaNestVm.SetOwnerResolver(() => Window.GetWindow(this.IdeaNestWorkspaceView));

        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            ReplaceTab(tab, tab with { IsDetached = false });
            tab = _tabs.First(t => t.Id == tabId);
        }

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
