using System.IO;
using NestSuite.Services;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    /// <summary>
    /// ファイル読込成功後にタブ・セッション登録・アクティブ化・最近ファイル更新を一括処理する。
    /// ChatNest など PropertyChanged 購読が必要な場合は afterRegister に登録処理を渡す。
    /// afterRegister は _sessionManager.Add の直後・ActivateTab の直前に呼ばれる。
    /// </summary>
    private void RegisterLoadedTab(
        NestSuiteDocumentTab tab,
        NestSuiteWorkspaceSession session,
        string path,
        Action? afterRegister = null)
    {
        _tabs.Add(tab);
        _sessionManager.Add(session);
        afterRegister?.Invoke();
        ActivateTab(tab);
        _recentFiles.Add(path);
        UpdateRecentFilesMenu();
    }

    /// <summary>
    /// ファイル読込例外のエラーログ記録とユーザーダイアログ表示を一括処理する。
    /// </summary>
    private void LogAndShowLoadError(
        string operation,
        string workspaceKind,
        string displayLabel,
        Exception ex,
        string path)
    {
        bool logged = ErrorLogService.Log(operation, ex, workspaceKind, path);
        var logHint = logged ? "\n\n詳細はエラーログに記録されました。" : "";
        _dialogs.ShowError(
            $"{displayLabel}\n{FileErrorMessages.ForLoad(ex)}{logHint}",
            "読込エラー");
    }

    /// <summary>
    /// ファイル保存例外のエラーログ記録とユーザーダイアログ表示を一括処理する。
    /// </summary>
    private void LogAndShowSaveError(
        string operation,
        string workspaceKind,
        string displayLabel,
        Exception ex,
        string path)
    {
        bool logged = ErrorLogService.Log(operation, ex, workspaceKind, path);
        var logHint = logged ? "\n\n詳細はエラーログに記録されました。" : "";
        _dialogs.ShowError(
            $"{displayLabel}\n{FileErrorMessages.ForSave(ex)}{logHint}",
            "保存エラー");
    }

    /// <summary>
    /// 名前を付けて保存の際に、別タブで同じパスが開かれている場合はエラーを表示して
    /// 既存タブをアクティブ化し true を返す。重複なければ false を返す。
    /// </summary>
    private bool CheckAndActivateDuplicateTabForSave(NestSuiteWorkspaceKind kind, string path)
    {
        if (_selectedTab == null) return false;
        var duplicate = _tabs.FirstOrDefault(t =>
            t.Id != _selectedTab.Id &&
            t.WorkspaceKind == kind &&
            NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, path));
        if (duplicate == null) return false;
        _dialogs.ShowError(
            $"「{Path.GetFileName(path)}」は既に別のタブで開かれています。\n既存のタブを表示します。",
            "保存できません");
        ActivateTab(duplicate);
        return true;
    }

    /// <summary>
    /// 指定 kind と path に一致する既存タブを検索し、見つかればアクティブ化・最近ファイル更新して true を返す。
    /// 見つからなければ false を返す。
    /// セッション復元・最近ファイルクリック・パイプ経由オープン時の重複タブ検出に使用する。
    /// </summary>
    private bool TryActivateExistingTab(NestSuiteWorkspaceKind kind, string path)
    {
        var existingTab = _tabs.FirstOrDefault(t =>
            t.WorkspaceKind == kind &&
            NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, path));
        if (existingTab == null) return false;
        ActivateTab(existingTab);
        _recentFiles.Add(path);
        UpdateRecentFilesMenu();
        return true;
    }

    /// <summary>
    /// WorkspaceKind に応じた Load*FileAt メソッドへ委譲する。
    /// セッション復元・最近ファイルクリック・パイプ経由オープン時に使用する。
    /// </summary>
    private void LoadWorkspaceFileAt(NestSuiteWorkspaceKind kind, string path)
    {
        switch (kind)
        {
            case NestSuiteWorkspaceKind.NoteNest: LoadNoteNestFileAt(path); break;
            case NestSuiteWorkspaceKind.ChatNest: LoadChatNestFileAt(path); break;
            case NestSuiteWorkspaceKind.IdeaNest: LoadIdeaNestFileAt(path); break;
        }
    }
}

