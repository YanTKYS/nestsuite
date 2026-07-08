using System.Windows;
using System.Windows.Input;
using NestSuite.ViewModels;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // Ctrl+Shift+S による全タブ一括保存（SH-20）を扱う partial。
    // 個別の保存処理は FileSave.cs・FileSaveStateSync.cs に委譲する。

    public static readonly RoutedCommand SaveAllCommand = new RoutedCommand(
        "SaveAll", typeof(NestSuiteShellWindow),
        new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift) });

    private enum SaveAllTabResult { Saved, Cancelled, Failed }

    private void CommandSaveAll_Executed(object sender, ExecutedRoutedEventArgs e) => SaveAllTabs();

    /// <summary>v2.16.10 SH-30: ファイルメニュー「すべて保存」の Click ハンドラ。Ctrl+Shift+S と同じ SaveAllTabs に委譲する。</summary>
    private void MenuSaveAll_Click(object sender, RoutedEventArgs e) => SaveAllTabs();

    /// <summary>
    /// v2.10.4 SH-20: 未保存の全タブ（NoteNest/IdeaNest/ChatNest）を順に保存する。
    /// TempNest は CanClose=false のため対象外。SaveAs キャンセル・保存失敗時は中断する。
    /// </summary>
    private void SaveAllTabs()
    {
        var targets = _tabs.Where(t => t.IsModified && t.CanClose).ToList();
        if (targets.Count == 0)
        {
            ShowStatusNotification("  |  保存が必要なタブはありません");
            return;
        }
        int savedCount = 0;
        foreach (var tab in targets)
        {
            var result = TrySaveTabForSaveAll(tab);
            if (result == SaveAllTabResult.Cancelled || result == SaveAllTabResult.Failed)
            {
                ShowStatusNotification("  |  すべて保存を中断しました");
                return;
            }
            savedCount++;
        }
        ShowStatusNotification($"  |  {savedCount}件保存しました");
    }

    /// <summary>
    /// v2.10.4 SH-20: 指定タブを SaveAll 用に保存する。
    /// 既存パスがある場合は上書き保存、ない場合は SaveAs ダイアログを表示する。
    /// キャンセル・保存失敗時は Cancelled/Failed を返す。
    /// </summary>
    private SaveAllTabResult TrySaveTabForSaveAll(NestSuiteDocumentTab tab)
    {
        if (!_sessionManager.TryGet(tab.Id, out var session) || session == null)
            return SaveAllTabResult.Failed;

        return tab.WorkspaceKind switch
        {
            NestSuiteWorkspaceKind.NoteNest  => TrySaveNoteNestForSaveAll(tab, session),
            NestSuiteWorkspaceKind.IdeaNest  => TrySaveIdeaNestForSaveAll(tab, session),
            NestSuiteWorkspaceKind.ChatNest  => TrySaveChatNestForSaveAll(tab, session),
            _                                => SaveAllTabResult.Failed,
        };
    }

    private SaveAllTabResult TrySaveNoteNestForSaveAll(NestSuiteDocumentTab tab, NestSuiteWorkspaceSession session)
    {
        var vm = (MainViewModel)session.WorkspaceViewModel;
        if (tab.FilePath != null)
        {
            var path = NormalizeFilePath(tab.FilePath);
            if (!vm.SaveToPath(path)) return SaveAllTabResult.Failed;
            ApplySavedWorkspaceState(session, path, isModifiedAfterSave: false, showNotification: false);
            return SaveAllTabResult.Saved;
        }
        else
        {
            var rawPath = _dialogs.SelectProjectSavePath(vm.ProjectName);
            if (rawPath == null) return SaveAllTabResult.Cancelled;
            var normalizedPath = NormalizeFilePath(rawPath);
            if (CheckAndActivateDuplicateTabForSave(NestSuiteWorkspaceKind.NoteNest, normalizedPath, tab.Id))
                return SaveAllTabResult.Cancelled;
            if (!vm.SaveToPath(normalizedPath)) return SaveAllTabResult.Failed;
            ApplySavedWorkspaceState(session, normalizedPath, isModifiedAfterSave: false, showNotification: false);
            return SaveAllTabResult.Saved;
        }
    }

    /// <summary>
    /// v2.13.6 TD-45: 保存実体を TrySaveIdeaNestToPath へ委譲する。
    /// 従来はここに FileService.Save + MarkSaved の複製があり、FileSave.cs 側との
    /// ドリフトリスクがあったため、シリアライズは各 Workspace につき 1 箇所に統一した。
    /// </summary>
    private SaveAllTabResult TrySaveIdeaNestForSaveAll(NestSuiteDocumentTab tab, NestSuiteWorkspaceSession session)
    {
        var targetPath = ResolveSaveTargetPath(tab, NestSuiteWorkspaceKind.IdeaNest,
            _dialogs.SelectIdeaNestSavePath, DefaultIdeaNestFileName);
        if (targetPath == null) return SaveAllTabResult.Cancelled;
        return TrySaveIdeaNestToPath(session, targetPath, showNotification: false)
            ? SaveAllTabResult.Saved
            : SaveAllTabResult.Failed;
    }

    /// <summary>
    /// v2.13.6 TD-45: 保存実体を TrySaveChatNestToPath へ委譲する。
    /// isModifiedAfterSave（InputText 残留時の HasUnsavedChanges 引き継ぎ）は
    /// UpdateChatNestTabPath（FileSaveStateSync.cs）に集約されている。
    /// </summary>
    private SaveAllTabResult TrySaveChatNestForSaveAll(NestSuiteDocumentTab tab, NestSuiteWorkspaceSession session)
    {
        var targetPath = ResolveSaveTargetPath(tab, NestSuiteWorkspaceKind.ChatNest,
            _dialogs.SelectChatNestSavePath, DefaultChatNestFileName);
        if (targetPath == null) return SaveAllTabResult.Cancelled;
        return TrySaveChatNestToPath(session, targetPath, showNotification: false)
            ? SaveAllTabResult.Saved
            : SaveAllTabResult.Failed;
    }
}
