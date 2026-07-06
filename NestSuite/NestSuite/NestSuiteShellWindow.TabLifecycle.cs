using System.ComponentModel;
using System.Windows.Controls;
using NestSuite.ChatNest;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.Services;
using NestSuite.TempNest;
using NestSuite.ViewModels;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // タブ生成・ViewModel 置換・PropertyChanged 購読を扱う partial。
    // v2.15.1 SH: 各 Nest の新規作成はファイル > 新規作成・タブバー ＋ ボタン（NewWorkspaceSession）に
    // 一本化した。かつてのツールメニュー由来の「既存タブがあれば切替、なければ新規作成」という
    // タブランチャー（EnsureTabForToolId）はツールメニューの Nest 項目撤去に伴い削除した。

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
            updatedTab = NestSuiteTabFactory.FromFilePath(path) with { Id = tab.Id, IsModified = vm.IsModified, IsDetached = tab.IsDetached };
        else
            updatedTab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest) with { Id = tab.Id, IsModified = vm.IsModified, IsDetached = tab.IsDetached };
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
        {
            SyncNoteNestTabForViewModel(vm);
            // v2.13.3 SH-30: タブ同期後にステータスバー（ファイル名・未保存表示）を再計算する。
            // 従来は Window.DataContext への直接 Binding が自動更新していたが、
            // アクティブタブ基準の表示に変更したため明示的な再計算が必要になった。
            if (IsActiveVm(vm)) RefreshWorkspaceStatus();
        }

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

        // L22: NoteNest 本文エディタのフォント種類。EditorFontSize と対称の伝播・永続化経路。
        // _suppressFontSizePropagation は「ファイル読込中の自前設定適用」を示す既存フラグを
        // フォント種類にも共用する（ファイル自身の AppSettings.FontFamily 読込時の再伝播を防ぐ）。
        if (e.PropertyName == nameof(MainViewModel.EditorFontFamily) &&
            sender is MainViewModel familyVm &&
            !_suppressFontSizePropagation &&
            familyVm.EditorFontFamily != _workspaceEditorFontFamily)
            PropagateWorkspaceEditorFontFamily(familyVm.EditorFontFamily, familyVm);

        if (e.PropertyName is nameof(MainViewModel.MarkerCount) or nameof(MainViewModel.TotalIncompleteTaskCountText) &&
            sender is MainViewModel statusVm && IsActiveVm(statusVm))
            RefreshWorkspaceStatus();
    }

    /// <summary>
    /// L22/v2.14.18 SH: NoteNest / IdeaNest / ChatNest / TempNest 共通のフォント種類設定を、
    /// 変更元以外の全セッションへ伝播し ui-settings.json（WorkspaceEditorFontFamily）へ永続化する。
    /// Workspace ファイル本体（.notenest/.nestsuite/.ideanest/.chatnest/tempnest.json）には
    /// 一切保存しない（各 ViewModel の ContentFontFamily/EditorFontFamily は保存モデルに含まれない）。
    /// <paramref name="exclude"/> は変更元 Workspace の ViewModel（既にその場で新しい値を持っている）を
    /// 二重適用しないためのもの。表示 > 本文フォント メニューのように変更元 Workspace が存在しない
    /// 呼び出しでは <c>null</c> を渡し、開いている全セッションへ適用する。
    /// </summary>
    private void PropagateWorkspaceEditorFontFamily(string family, object? exclude)
    {
        _workspaceEditorFontFamily = family;
        foreach (var s in _sessionManager.Sessions)
        {
            if (exclude != null && ReferenceEquals(s.WorkspaceViewModel, exclude)) continue;
            switch (s.WorkspaceViewModel)
            {
                case MainViewModel otherNoteVm: otherNoteVm.EditorFontFamily = family; break;
                case IdeaNestWorkspaceViewModel otherIdeaVm: otherIdeaVm.ContentFontFamily = family; break;
                case ChatNestWorkspaceViewModel otherChatVm: otherChatVm.ContentFontFamily = family; break;
                case TempNestWorkspaceViewModel otherTempVm: otherTempVm.ContentFontFamily = family; break;
            }
        }
        var uiSvc = new UiSettingsService();
        var ui = uiSvc.Load();
        ui.WorkspaceEditorFontFamily = family;
        uiSvc.Save(ui);
        UpdateWorkspaceFontMenuChecks();
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

        // L22: ChatNest メッセージ本文・入力欄のフォント種類。Workspace 共通設定への伝播・永続化。
        if (e.PropertyName == nameof(ChatNestWorkspaceViewModel.ContentFontFamily) &&
            sender is ChatNestWorkspaceViewModel familyVm &&
            familyVm.ContentFontFamily != _workspaceEditorFontFamily)
            PropagateWorkspaceEditorFontFamily(familyVm.ContentFontFamily, familyVm);
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

        // L22: IdeaNest カード本文・カード編集欄のフォント種類。Workspace 共通設定への伝播・永続化。
        if (e.PropertyName == nameof(IdeaNestWorkspaceViewModel.ContentFontFamily) &&
            sender is IdeaNestWorkspaceViewModel familyVm &&
            familyVm.ContentFontFamily != _workspaceEditorFontFamily)
            PropagateWorkspaceEditorFontFamily(familyVm.ContentFontFamily, familyVm);
    }

    /// <summary>
    /// L22: TempNest 各スロットのタイトル欄・本文欄のフォント種類。Workspace 共通設定への伝播・永続化。
    /// TempNest は保存対象の変化を検知する必要がないため、フォント種類変更以外は扱わない。
    /// </summary>
    private void OnTempNestPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TempNestWorkspaceViewModel.ContentFontFamily) &&
            sender is TempNestWorkspaceViewModel familyVm &&
            familyVm.ContentFontFamily != _workspaceEditorFontFamily)
            PropagateWorkspaceEditorFontFamily(familyVm.ContentFontFamily, familyVm);
    }
}
