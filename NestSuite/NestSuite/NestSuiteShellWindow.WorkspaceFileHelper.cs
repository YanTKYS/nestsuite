using System.IO;
using NestSuite.Services;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // ファイル読込後の共通後処理（RegisterLoadedTab）・保存後の共通更新（ApplySavedWorkspaceState）・
    // エラー表示・重複チェック・WorkspaceKind 別ファイル開くルーティングを扱う partial。

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
    /// 保存成功後のタブ・Session・最近ファイル・通知を共通更新する。
    /// 保存失敗時は呼ばない。
    /// </summary>
    private bool ApplySavedWorkspaceState(
        NestSuiteWorkspaceSession session,
        string path,
        bool isModifiedAfterSave,
        bool showNotification = true)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == session.TabId);
        if (tab == null) return false;
        if (!SavedWorkspaceStateUpdater.TryCreate(tab, path, isModifiedAfterSave, out var state)) return false;

        ReplaceTab(tab, state.UpdatedTab);
        SavedWorkspaceStateUpdater.ApplyToSession(session, state);
        _recentFiles.Add(state.RecentFilePath);
        UpdateRecentFilesMenu();
        if (showNotification) ShowStatusNotification("  |  保存しました");
        return true;
    }

    /// <summary>
    /// 名前を付けて保存の際に、別タブで同じパスが開かれている場合はエラーを表示して
    /// 既存タブをアクティブ化し true を返す。重複なければ false を返す。
    /// v2.9.2 SH-21: excludeTabId を指定することで、別ウィンドウ保存時も正しいタブを除外できる。
    /// </summary>
    private bool CheckAndActivateDuplicateTabForSave(
        NestSuiteWorkspaceKind kind, string path, string? excludeTabId = null)
    {
        var effectiveExcludeId = excludeTabId ?? _selectedTab?.Id;
        if (effectiveExcludeId == null) return false;
        var duplicate = _tabs.FirstOrDefault(t =>
            t.Id != effectiveExcludeId &&
            NestSuiteOpenFilePolicy.IsDuplicateForSave(t.FilePath, t.WorkspaceKind, path, kind));
        if (duplicate == null) return false;
        _dialogs.ShowError(
            $"「{Path.GetFileName(path)}」は既に別のタブで開かれています。\n既存のタブを表示します。",
            "保存できません");
        ActivateTab(duplicate);
        return true;
    }

    /// <summary>
    /// v2.13.6 TD-45: 保存先パスを解決する（SaveForTabId / SaveAll 共通）。
    /// タブに FilePath があれば正規化して返す（重複チェックなし＝上書き保存）。
    /// なければ selectSavePath で選択させ、キャンセル時・重複タブ検出時は null を返す。
    /// 呼び出し側は null を「保存中止（キャンセル扱い）」として処理する。
    /// </summary>
    private string? ResolveSaveTargetPath(
        NestSuiteDocumentTab tab,
        NestSuiteWorkspaceKind kind,
        Func<string, string?> selectSavePath,
        string defaultFileName)
    {
        if (tab.FilePath != null) return NormalizeFilePath(tab.FilePath);
        var rawPath = selectSavePath(defaultFileName);
        if (rawPath == null) return null;
        var normalizedPath = NormalizeFilePath(rawPath);
        if (CheckAndActivateDuplicateTabForSave(kind, normalizedPath, tab.Id)) return null;
        return normalizedPath;
    }

    /// <summary>
    /// 指定 kind と path に一致する既存タブを検索し、見つかればアクティブ化・最近ファイル更新して true を返す。
    /// 見つからなければ false を返す。
    /// セッション復元・最近ファイルクリック・パイプ経由オープン時の重複タブ検出に使用する。
    /// </summary>
    private bool TryActivateExistingTab(NestSuiteWorkspaceKind kind, string path)
    {
        var existingTab = ShellFileOpenPlanner.FindOpenedFileTab(_tabs, kind, path);
        if (existingTab == null) return false;
        ActivateExistingTabForOpen(existingTab, path);
        return true;
    }

    /// <summary>
    /// 既存タブ再利用時の UI 操作と最近ファイル更新を Shell 側に残す。
    /// </summary>
    private void ActivateExistingTabForOpen(NestSuiteDocumentTab existingTab, string path)
    {
        ActivateTab(existingTab);
        _recentFiles.Add(path);
        UpdateRecentFilesMenu();
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

