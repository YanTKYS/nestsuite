using System.Windows.Input;
using NestSuite.ChatNest;
using NestSuite.IdeaNest.Services;
using NestSuite.Services;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.ViewModels;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // v2.11.1: 名前を付けて保存ダイアログの既定ファイル名。
    // v2.14.1 FM-1: 新規保存の標準拡張子は .nestsuite（NestSuiteWorkspaceEnvelope.FileExtension）。
    private const string DefaultIdeaNestFileName = "ideas" + NestSuiteWorkspaceEnvelope.FileExtension;
    private const string DefaultChatNestFileName = "chat"  + NestSuiteWorkspaceEnvelope.FileExtension;

    /// <summary>
    /// v2.13.6 TD-45: IdeaNest / ChatNest 保存の共通実体。
    /// パス正規化 → Workspace 固有シリアライズ + MarkSaved（serializeAndMark）→ 保存後状態更新（updateTabPath）。
    /// serializeAndMark が例外を投げた場合、MarkSaved と状態更新は実行されず未保存状態が維持される。
    /// isModifiedAfterSave の差異（ChatNest の InputText 残留）は updateTabPath 側
    /// （FileSaveStateSync.cs の UpdateXxxTabPath）に集約されており、ここでは扱わない。
    /// NoteNest は vm.SaveToPath という別インターフェース（エラー処理も VM 側）のため対象外。
    /// </summary>
    private bool TrySaveWorkspaceToPath(
        NestSuiteWorkspaceSession session,
        string path,
        Action<string> serializeAndMark,
        Action<NestSuiteWorkspaceSession, string, bool> updateTabPath,
        string errorOperation,
        string errorWorkspaceKind,
        string errorLabel,
        bool showNotification)
    {
        path = NormalizeFilePath(path);
        try
        {
            serializeAndMark(path);
            updateTabPath(session, path, showNotification);
            return true;
        }
        catch (Exception ex)
        {
            LogAndShowSaveError(errorOperation, errorWorkspaceKind, errorLabel, ex, path);
            return false;
        }
    }

    /// <summary>v1.9.7: 指定 Session の IdeaNest を指定パスへ保存する。失敗時はエラーダイアログを表示し false を返す。</summary>
    private bool TrySaveIdeaNestToPath(NestSuiteWorkspaceSession session, string path) =>
        TrySaveIdeaNestToPath(session, path, showNotification: true);

    private bool TrySaveIdeaNestToPath(NestSuiteWorkspaceSession session, string path, bool showNotification)
    {
        var vm = (IdeaNestWorkspaceViewModel)session.WorkspaceViewModel;
        return TrySaveWorkspaceToPath(
            session, path,
            p => { IdeaNestFileService.Save(p, vm.BuildWorkspaceForSave()); vm.MarkSaved(); },
            UpdateIdeaNestTabPath,
            "IdeaNestSave", "IdeaNest", "IdeaNest ファイルの保存に失敗しました。",
            showNotification);
    }

    /// <summary>v1.9.7: 選択中 IdeaNest タブの Session で上書き保存。パスがなければ名前を付けて保存へ委譲する。</summary>
    private void SaveIdeaNestFile()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.IdeaNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        if (_selectedTab.FilePath != null)
            TrySaveIdeaNestToPath(session, _selectedTab.FilePath);
        else
            SaveIdeaNestFileAs();
    }

    /// <summary>v1.9.2: 指定 Session の ChatNest を指定パスへ保存する。失敗時はエラーダイアログを表示し false を返す。</summary>
    private bool TrySaveChatNestToPath(NestSuiteWorkspaceSession session, string path) =>
        TrySaveChatNestToPath(session, path, showNotification: true);

    private bool TrySaveChatNestToPath(NestSuiteWorkspaceSession session, string path, bool showNotification)
    {
        var vm = (ChatNestWorkspaceViewModel)session.WorkspaceViewModel;
        return TrySaveWorkspaceToPath(
            session, path,
            p => { ChatNestFileService.Save(p, vm.MessageModels); vm.MarkSaved(); },
            UpdateChatNestTabPath,
            "ChatNestSave", "ChatNest", "ChatNest ファイルの保存に失敗しました。",
            showNotification);
    }

    /// <summary>v1.9.2: 選択中 ChatNest タブの Session で上書き保存。パスがなければ名前を付けて保存へ委譲する。</summary>
    private void SaveChatNestFile()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.ChatNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        if (_selectedTab.FilePath != null)
            TrySaveChatNestToPath(session, _selectedTab.FilePath);
        else
            SaveChatNestFileAs();
    }

    private void CommandSave_Executed(object sender, ExecutedRoutedEventArgs e) => SaveActiveTab();

    /// <summary>
    /// v2.9.0 SH-21: 別ウィンドウ内の Ctrl+S から呼ばれる。タブ ID を直接受け取り保存する。
    /// v2.9.2 SH-21: selectSavePath を受け取り、SaveAs ダイアログを呼び出し側の Window に出せるようにした。
    ///               null の場合は Shell の _dialogs を使う。重複チェックも tabId を基準に行う。
    /// </summary>
    internal void SaveNoteNestForTabId(string tabId, Func<string, string?>? selectSavePath = null)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab == null || tab.WorkspaceKind != NestSuiteWorkspaceKind.NoteNest) return;
        if (!_sessionManager.TryGet(tabId, out var session) || session == null) return;
        var vm = (MainViewModel)session.WorkspaceViewModel;
        if (tab.FilePath != null)
        {
            var path = NormalizeFilePath(tab.FilePath);
            if (vm.SaveToPath(path))
                UpdateNoteNestTabPath(session, path);
        }
        else
        {
            var selector = selectSavePath ?? _dialogs.SelectProjectSavePath;
            var rawPath = selector(vm.ProjectName);
            if (rawPath == null) return;
            var normalizedPath = NormalizeFilePath(rawPath);
            // v2.9.2: 重複チェックは保存対象の tabId を除外基準にする（_selectedTab でなく）
            if (CheckAndActivateDuplicateTabForSave(NestSuiteWorkspaceKind.NoteNest, normalizedPath, tabId)) return;
            if (vm.SaveToPath(normalizedPath))
                UpdateNoteNestTabPath(session, normalizedPath);
        }
    }

    /// <summary>
    /// v2.9.3 SH-21: 別ウィンドウ内の Ctrl+S から呼ばれる（IdeaNest）。タブ ID を直接受け取り保存する。
    /// selectSavePath を受け取り、SaveAs ダイアログを呼び出し側の Window に出せるようにした。
    /// null の場合は Shell の _dialogs を使う。
    /// </summary>
    internal void SaveIdeaNestForTabId(string tabId, Func<string, string?>? selectSavePath = null)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab == null || tab.WorkspaceKind != NestSuiteWorkspaceKind.IdeaNest) return;
        if (!_sessionManager.TryGet(tabId, out var session) || session == null) return;
        var targetPath = ResolveSaveTargetPath(tab, NestSuiteWorkspaceKind.IdeaNest,
            selectSavePath ?? _dialogs.SelectIdeaNestSavePath, DefaultIdeaNestFileName);
        if (targetPath == null) return;
        TrySaveIdeaNestToPath(session, targetPath);
    }

    /// <summary>
    /// v2.9.4 SH-21: 別ウィンドウ内の Ctrl+S から呼ばれる（ChatNest）。タブ ID を直接受け取り保存する。
    /// selectSavePath を受け取り、SaveAs ダイアログを呼び出し側の Window に出せるようにした。
    /// null の場合は Shell の _dialogs を使う。
    /// </summary>
    internal void SaveChatNestForTabId(string tabId, Func<string, string?>? selectSavePath = null)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab == null || tab.WorkspaceKind != NestSuiteWorkspaceKind.ChatNest) return;
        if (!_sessionManager.TryGet(tabId, out var session) || session == null) return;
        var targetPath = ResolveSaveTargetPath(tab, NestSuiteWorkspaceKind.ChatNest,
            selectSavePath ?? _dialogs.SelectChatNestSavePath, DefaultChatNestFileName);
        if (targetPath == null) return;
        TrySaveChatNestToPath(session, targetPath);
    }

    /// <summary>
    /// v2.9.7: タブ閉鎖・アプリ終了時の NoteNest 保存ヘルパー。
    /// 保存パスがある場合は上書き保存、ない場合は名前を付けて保存ダイアログを表示する。
    /// 保存成功時のみ true を返す。キャンセル・保存失敗時は false を返す。
    /// dirty state は保存成功時のみ解除する（<see cref="UpdateNoteNestTabPath"/> 経由）。
    /// </summary>
    private bool TrySaveNoteNestForClose(NestSuiteWorkspaceSession session)
    {
        var vm = (MainViewModel)session.WorkspaceViewModel;
        if (session.FilePath != null)
        {
            var path = NormalizeFilePath(session.FilePath);
            if (!vm.SaveToPath(path)) return false;
            UpdateNoteNestTabPath(session, path);
            return true;
        }
        else
        {
            var rawPath = _dialogs.SelectProjectSavePath(vm.ProjectName);
            if (rawPath == null) return false;
            var normalizedPath = NormalizeFilePath(rawPath);
            if (CheckAndActivateDuplicateTabForSave(NestSuiteWorkspaceKind.NoteNest, normalizedPath, session.TabId)) return false;
            if (!vm.SaveToPath(normalizedPath)) return false;
            UpdateNoteNestTabPath(session, normalizedPath);
            return true;
        }
    }

    private void SaveActiveTab()
    {
        switch (_selectedTab?.WorkspaceKind)
        {
            case NestSuiteWorkspaceKind.NoteNest: SaveNoteNestFile(); break;
            case NestSuiteWorkspaceKind.ChatNest: SaveChatNestFile(); break;
            case NestSuiteWorkspaceKind.IdeaNest: SaveIdeaNestFile(); break;
        }
    }

    /// <summary>v1.9.5: 選択中 NoteNest タブを上書き保存。パスがなければ名前を付けて保存へ委譲する。</summary>
    private void SaveNoteNestFile()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.NoteNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        var vm = (MainViewModel)session.WorkspaceViewModel;
        if (_selectedTab.FilePath != null)
        {
            var path = NormalizeFilePath(_selectedTab.FilePath);
            if (vm.SaveToPath(path))
                UpdateNoteNestTabPath(session, path);
        }
        else
        {
            SaveNoteNestFileAs();
        }
    }
}
