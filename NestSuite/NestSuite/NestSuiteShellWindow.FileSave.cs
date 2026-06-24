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
    /// <summary>v1.9.7: 指定 Session の IdeaNest を指定パスへ保存する。失敗時はエラーダイアログを表示し false を返す。</summary>
    private bool TrySaveIdeaNestToPath(NestSuiteWorkspaceSession session, string path)
    {
        path = NormalizeFilePath(path);
        var vm = (IdeaNestWorkspaceViewModel)session.WorkspaceViewModel;
        try
        {
            IdeaNestFileService.Save(path, vm.BuildWorkspaceForSave());
            vm.MarkSaved();
            UpdateIdeaNestTabPath(session, path);
            return true;
        }
        catch (Exception ex)
        {
            LogAndShowSaveError("IdeaNestSave", "IdeaNest", "IdeaNest ファイルの保存に失敗しました。", ex, path);
            return false;
        }
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
    private bool TrySaveChatNestToPath(NestSuiteWorkspaceSession session, string path)
    {
        // v1.9.2 fix: 保存先パスも正規化し、タブ・Session に保存されるパスを常にフルパスに統一する
        path = NormalizeFilePath(path);
        var vm = (ChatNestWorkspaceViewModel)session.WorkspaceViewModel;
        try
        {
            ChatNestFileService.Save(path, vm.MessageModels);
            vm.MarkSaved();
            UpdateChatNestTabPath(session, path);
            return true;
        }
        catch (Exception ex)
        {
            LogAndShowSaveError("ChatNestSave", "ChatNest", "ChatNest ファイルの保存に失敗しました。", ex, path);
            return false;
        }
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
