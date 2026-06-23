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
}
