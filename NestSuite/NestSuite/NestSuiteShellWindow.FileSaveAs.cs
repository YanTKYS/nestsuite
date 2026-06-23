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
    /// <summary>v1.9.7: 選択中 IdeaNest タブの Session で名前を付けて保存。ダイアログでパスを選択し保存する。</summary>
    private void SaveIdeaNestFileAs()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.IdeaNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        var defaultName = _selectedTab.FilePath != null
            ? Path.GetFileName(_selectedTab.FilePath)
            : "ideas.ideanest";
        var rawPath = _dialogs.SelectIdeaNestSavePath(defaultName);
        if (rawPath == null) return;
        // v1.9.7 fix: 選択中タブ以外の IdeaNest タブが同じパスを開いていないか確認する
        // 2 つの独立 ViewModel が同じファイルを指す状態を防ぐ
        var normalizedPath = NormalizeFilePath(rawPath);
        if (CheckAndActivateDuplicateTabForSave(NestSuiteWorkspaceKind.IdeaNest, normalizedPath)) return;
        TrySaveIdeaNestToPath(session, normalizedPath);
    }

    /// <summary>
    /// v1.9.2: 選択中 ChatNest タブの Session で名前を付けて保存。ダイアログでパスを選択し保存する。
    /// v1.9.8: 別タブで同じパスが開かれている場合はエラーを表示して既存タブをアクティブ化する。
    /// </summary>
    private void SaveChatNestFileAs()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.ChatNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        var defaultName = _selectedTab.FilePath != null
            ? Path.GetFileName(_selectedTab.FilePath)
            : "chat.chatnest";
        var rawPath = _dialogs.SelectChatNestSavePath(defaultName);
        if (rawPath == null) return;
        var normalizedPath = NormalizeFilePath(rawPath);
        if (CheckAndActivateDuplicateTabForSave(NestSuiteWorkspaceKind.ChatNest, normalizedPath)) return;
        TrySaveChatNestToPath(session, normalizedPath);
    }

    private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
    {
        switch (_selectedTab?.WorkspaceKind)
        {
            case NestSuiteWorkspaceKind.NoteNest:
                SaveNoteNestFileAs();
                break;
            case NestSuiteWorkspaceKind.ChatNest:
                SaveChatNestFileAs();
                break;
            case NestSuiteWorkspaceKind.IdeaNest:
                SaveIdeaNestFileAs();
                break;
        }
    }

    /// <summary>
    /// v1.9.5: 選択中 NoteNest タブを名前を付けて保存する。
    /// v1.9.8 fix: 別タブで同じパスが開かれている場合はエラーを表示して既存タブをアクティブ化する。
    /// </summary>
    private void SaveNoteNestFileAs()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.NoteNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        var vm = (MainViewModel)session.WorkspaceViewModel;
        var rawPath = _dialogs.SelectProjectSavePath(vm.ProjectName);
        if (rawPath == null) return;
        var normalizedPath = NormalizeFilePath(rawPath);
        if (CheckAndActivateDuplicateTabForSave(NestSuiteWorkspaceKind.NoteNest, normalizedPath)) return;
        if (vm.SaveToPath(normalizedPath))
            UpdateNoteNestTabPath(session, normalizedPath);
    }
}
