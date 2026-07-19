using System.IO;
using System.Windows;
using NestSuite.ViewModels;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // 名前を付けて保存ダイアログと保存処理を扱う partial。
    // 保存成功後の同期は FileSaveStateSync.cs の UpdateXxxTabPath に委譲する。

    /// <summary>v1.9.7: 選択中 IdeaNest タブの Session で名前を付けて保存。ダイアログでパスを選択し保存する。</summary>
    private void SaveIdeaNestFileAs()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.IdeaNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        var defaultName = _selectedTab.FilePath != null
            ? Path.GetFileName(_selectedTab.FilePath)
            : DefaultIdeaNestFileName;
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
            : DefaultChatNestFileName;
        var rawPath = _dialogs.SelectChatNestSavePath(defaultName);
        if (rawPath == null) return;
        var normalizedPath = NormalizeFilePath(rawPath);
        if (CheckAndActivateDuplicateTabForSave(NestSuiteWorkspaceKind.ChatNest, normalizedPath)) return;
        TrySaveChatNestToPath(session, normalizedPath);
    }

    /// <summary>
    /// v2.19.0 SH-43: 選択中 PlainText タブの Session で名前を付けて保存。ダイアログでパスを選択し保存する。
    /// </summary>
    private void SaveTextFileAs()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.PlainText) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        var defaultName = _selectedTab.FilePath != null
            ? Path.GetFileName(_selectedTab.FilePath)
            : DefaultTextFileName;
        var rawPath = _dialogs.SelectPlainTextSavePath(defaultName);
        if (rawPath == null) return;
        var normalizedPath = NormalizeFilePath(rawPath);
        if (CheckAndActivateDuplicateTabForSave(NestSuiteWorkspaceKind.PlainText, normalizedPath)) return;
        TrySaveTextToPath(session, normalizedPath);
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
            case NestSuiteWorkspaceKind.PlainText:
                SaveTextFileAs();
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
