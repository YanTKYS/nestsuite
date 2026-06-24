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
        if (GetTabFromContextMenuItem(sender) is { } tab &&
            tab.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest &&
            !_detachedWindows.ContainsKey(tab.Id))
            DetachNoteNestTab(tab);
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
        detachedWindow.SaveAction = () => SaveNoteNestForTabId(capturedTabId);
        detachedWindow.OnDetachedClosed = ReAttachNoteNestTab;

        _detachedWindows[tab.Id] = detachedWindow;

        ActivateTab(tab);

        detachedWindow.Show();
    }

    internal void ReAttachNoteNestTab(string tabId)
    {
        if (!_detachedWindows.ContainsKey(tabId)) return;
        _detachedWindows.Remove(tabId);

        if (!_sessionManager.TryGet(tabId, out var session) || session == null) return;
        var vm = (MainViewModel)session.WorkspaceViewModel;

        // Re-wire view callbacks back to shell's WorkspaceView
        WireNoteNestViewCallbacks(vm, WorkspaceView);
        WorkspaceView.DataContext = vm;

        // If this was the selected tab, re-show workspace in shell
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
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
