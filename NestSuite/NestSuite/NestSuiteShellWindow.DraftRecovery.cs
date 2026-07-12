using System.IO;
using System.Text;
using System.Windows;
using NestSuite.ChatNest;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.Services;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    private sealed class DraftRestoreSummary
    {
        public int RestoredCount { get; set; }
        public int PartialTransientCount { get; set; }
        public int BodyQuarantinedCount { get; set; }
        public int QuarantineFailedCount { get; set; }
        public int CollisionWriteFailedCount { get; set; }
        public int DiscardDeleteFailedCount { get; set; }

        public bool HasIssues =>
            PartialTransientCount > 0 ||
            BodyQuarantinedCount > 0 ||
            QuarantineFailedCount > 0 ||
            CollisionWriteFailedCount > 0 ||
            DiscardDeleteFailedCount > 0;
    }

    private sealed record DraftRestoreIssue(
        bool PartialTransient = false,
        bool BodyQuarantined = false,
        bool QuarantineFailed = false,
        bool CollisionWriteFailed = false);

    private void RestoreDraftsAtStartup()
    {
        if (!TryListStartupDraftFiles(
                () => DraftStore.ListDraftFiles(),
                ex => ErrorLogService.Log(
                    "DraftRestoreList",
                    ex,
                    filePath: DraftStore.DefaultRootDirectory),
                out var draftPaths))
        {
            return;
        }

        if (draftPaths.Count == 0)
            return;

        var choice = MessageBox.Show(
            $"前回終了時に保存されていない下書きが {draftPaths.Count} 件見つかりました。\n" +
            "復元しますか？\n\n" +
            "はい:\n無題タブとして復元します。\n\n" +
            "いいえ:\n下書きを破棄します。元に戻せません。\n\n" +
            "キャンセル:\n今回は何もせず保持し、次回起動時にもう一度確認します。",
            "下書きの復元",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);

        if (choice == MessageBoxResult.No)
        {
            var discardSummary = DiscardStartupDrafts(draftPaths);
            ShowDraftRestoreSummary(discardSummary);
            return;
        }

        if (choice != MessageBoxResult.Yes)
            return;

        var summary = new DraftRestoreSummary();
        NestSuiteDocumentTab? lastRestoredTab = null;
        foreach (var draftPath in draftPaths)
        {
            if (TryRestoreDraft(draftPath, out var restoredTab, out var issue))
            {
                summary.RestoredCount++;
                lastRestoredTab = restoredTab;
            }

            if (issue != null)
            {
                if (issue.PartialTransient) summary.PartialTransientCount++;
                if (issue.BodyQuarantined) summary.BodyQuarantinedCount++;
                if (issue.QuarantineFailed) summary.QuarantineFailedCount++;
                if (issue.CollisionWriteFailed) summary.CollisionWriteFailedCount++;
            }
        }

        if (lastRestoredTab != null)
            ActivateTab(lastRestoredTab);

        ShowDraftRestoreSummary(summary);
    }

    private static bool TryListStartupDraftFiles(
        Func<IReadOnlyList<string>> listDraftFiles,
        Action<Exception> logError,
        out IReadOnlyList<string> draftPaths)
    {
        try
        {
            draftPaths = listDraftFiles();
            return true;
        }
        catch (Exception ex)
        {
            logError(ex);
            draftPaths = Array.Empty<string>();
            return false;
        }
    }

    private DraftRestoreSummary DiscardStartupDrafts(IReadOnlyList<string> draftPaths)
    {
        var summary = new DraftRestoreSummary();
        foreach (var draftPath in draftPaths)
        {
            if (!DraftStore.TryGetTabId(draftPath, out var tabId))
                continue;

            try
            {
                DraftStore.Delete(tabId);
            }
            catch (Exception ex)
            {
                summary.DiscardDeleteFailedCount++;
                ErrorLogService.Log("DraftRestoreDiscard", ex, filePath: draftPath);
            }
        }

        return summary;
    }

    private bool TryRestoreDraft(
        string draftPath,
        out NestSuiteDocumentTab? restoredTab,
        out DraftRestoreIssue? issue)
    {
        restoredTab = null;
        issue = null;

        if (!DraftStore.TryGetTabId(draftPath, out var originalTabId))
            return false;

        if (!NestSuiteTabFactory.TryPrepareOpen(draftPath, out var context, out var failure))
        {
            ErrorLogService.Log(
                "DraftRestore",
                new InvalidDataException($"下書き本体を読み取れませんでした: {failure}"),
                filePath: draftPath);
            issue = QuarantineWorkspaceDraftAfterBodyFailure(draftPath);
            return false;
        }

        try
        {
            return context.WorkspaceKind switch
            {
                NestSuiteWorkspaceKind.NoteNest => RestoreNoteNestDraft(draftPath, originalTabId, context, out restoredTab, out issue),
                NestSuiteWorkspaceKind.IdeaNest => RestoreIdeaNestDraft(draftPath, originalTabId, context, out restoredTab, out issue),
                NestSuiteWorkspaceKind.ChatNest => RestoreChatNestDraft(draftPath, originalTabId, context, out restoredTab, out issue),
                _ => FailAndQuarantineWorkspaceDraft(draftPath, new InvalidDataException("下書きの WorkspaceKind がサポート対象外です。"), out issue),
            };
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("DraftRestore", ex, context.WorkspaceKind.ToString(), draftPath);
            issue = QuarantineWorkspaceDraftAfterBodyFailure(draftPath);
            restoredTab = null;
            return false;
        }
    }

    private bool RestoreNoteNestDraft(
        string draftPath,
        string originalTabId,
        WorkspaceFileOpenContext context,
        out NestSuiteDocumentTab? restoredTab,
        out DraftRestoreIssue? issue)
    {
        var project = new ProjectFileService().LoadPrepared(context);
        var serialized = new ProjectFileService().SerializeWrapped(project);
        return RestoreLoadedDraft(
            draftPath,
            originalTabId,
            NestSuiteWorkspaceKind.NoteNest,
            serialized,
            transientStateForCollision: null,
            (session) => ((MainViewModel)session.WorkspaceViewModel).OpenProjectSnapshotAsUntitled(project),
            out restoredTab,
            out issue);
    }

    private bool RestoreIdeaNestDraft(
        string draftPath,
        string originalTabId,
        WorkspaceFileOpenContext context,
        out NestSuiteDocumentTab? restoredTab,
        out DraftRestoreIssue? issue)
    {
        var workspace = IdeaNestFileService.LoadPrepared(context);
        var serialized = IdeaNestFileService.SerializeWrapped(workspace);
        return RestoreLoadedDraft(
            draftPath,
            originalTabId,
            NestSuiteWorkspaceKind.IdeaNest,
            serialized,
            transientStateForCollision: null,
            (session) => ((IdeaNestWorkspaceViewModel)session.WorkspaceViewModel).LoadFromWorkspaceAsDraft(workspace),
            out restoredTab,
            out issue);
    }

    private bool RestoreChatNestDraft(
        string draftPath,
        string originalTabId,
        WorkspaceFileOpenContext context,
        out NestSuiteDocumentTab? restoredTab,
        out DraftRestoreIssue? issue)
    {
        var messages = ChatNestFileService.LoadPrepared(context);
        var transientResult = DraftStore.ReadTransientState(draftPath);
        var transientState = HandleTransientDraftReadResult(draftPath, transientResult, out var partialTransient, out var quarantineFailed);
        var serialized = ChatNestFileService.SerializeWrapped(messages);

        var restored = RestoreLoadedDraft(
            draftPath,
            originalTabId,
            NestSuiteWorkspaceKind.ChatNest,
            serialized,
            transientState,
            (session) => ((ChatNestWorkspaceViewModel)session.WorkspaceViewModel).LoadMessagesAsDraft(messages, transientState),
            out restoredTab,
            out issue);

        if (partialTransient || quarantineFailed)
        {
            issue = issue == null
                ? new DraftRestoreIssue(PartialTransient: partialTransient, QuarantineFailed: quarantineFailed)
                : issue with { PartialTransient = issue.PartialTransient || partialTransient, QuarantineFailed = issue.QuarantineFailed || quarantineFailed };
        }

        return restored;
    }

    private bool RestoreLoadedDraft(
        string draftPath,
        string originalTabId,
        NestSuiteWorkspaceKind workspaceKind,
        string wrappedJsonForCollision,
        ChatNestTransientDraftState? transientStateForCollision,
        Action<NestSuiteWorkspaceSession> loadDraftModel,
        out NestSuiteDocumentTab? restoredTab,
        out DraftRestoreIssue? issue)
    {
        restoredTab = null;
        issue = null;
        var hasCollision = _tabs.Any(t => t.Id == originalTabId);
        var restoredTabId = hasCollision ? Guid.NewGuid().ToString("N") : originalTabId;

        if (!TryWriteCollisionPairBeforeRestore(
                hasCollision,
                restoredTabId,
                wrappedJsonForCollision,
                transientStateForCollision,
                (tabId, wrappedJson, transientState) => DraftStore.WriteWorkspaceDraft(tabId, wrappedJson, transientState),
                ex => ErrorLogService.Log("DraftRestoreCollisionWrite", ex, workspaceKind.ToString(), draftPath),
                out issue))
        {
            return false;
        }

        var tab = NestSuiteTabFactory.CreateUntitled(workspaceKind) with
        {
            Id = restoredTabId,
            IsModified = true,
        };
        NestSuiteWorkspaceSession? session = null;
        try
        {
            session = CreateSessionForTab(tab);
            _tabs.Add(tab);
            _sessionManager.Add(session);
            loadDraftModel(session);
        }
        catch (Exception ex)
        {
            CleanupFailedDraftRestore(tab, session);
            if (hasCollision)
            {
                ErrorLogService.Log("DraftRestore", ex, workspaceKind.ToString(), draftPath);
                RollbackCollisionPair(
                    restoredTabId,
                    tabId => DraftStore.Delete(tabId),
                    rollbackEx => ErrorLogService.Log("DraftRestoreCollisionRollback", rollbackEx, workspaceKind.ToString(), draftPath));
                issue = new DraftRestoreIssue(CollisionWriteFailed: true);
                return false;
            }

            throw;
        }

        restoredTab = _tabs.FirstOrDefault(t => t.Id == restoredTabId) ?? tab;
        ActivateTab(restoredTab);

        if (hasCollision)
        {
            try
            {
                DraftStore.Delete(originalTabId);
            }
            catch (Exception ex)
            {
                ErrorLogService.Log("DraftRestoreCollisionDelete", ex, workspaceKind.ToString(), draftPath);
            }
        }

        return true;
    }

    private static bool TryWriteCollisionPairBeforeRestore(
        bool hasCollision,
        string restoredTabId,
        string wrappedJsonForCollision,
        ChatNestTransientDraftState? transientStateForCollision,
        Action<string, string, ChatNestTransientDraftState?> writeDraft,
        Action<Exception> logError,
        out DraftRestoreIssue? issue)
    {
        issue = null;
        if (!hasCollision)
            return true;

        try
        {
            writeDraft(restoredTabId, wrappedJsonForCollision, transientStateForCollision);
            return true;
        }
        catch (Exception ex)
        {
            logError(ex);
            issue = new DraftRestoreIssue(CollisionWriteFailed: true);
            return false;
        }
    }

    private static void RollbackCollisionPair(
        string restoredTabId,
        Action<string> deleteDraft,
        Action<Exception> logError)
    {
        try
        {
            deleteDraft(restoredTabId);
        }
        catch (Exception ex)
        {
            logError(ex);
        }
    }

    private void CleanupFailedDraftRestore(NestSuiteDocumentTab tab, NestSuiteWorkspaceSession? session)
    {
        if (session?.WorkspaceViewModel is MainViewModel noteVm)
            noteVm.PropertyChanged -= OnNoteNestSessionPropertyChanged;
        if (session?.WorkspaceViewModel is IdeaNestWorkspaceViewModel ideaVm)
            ideaVm.PropertyChanged -= OnIdeaNestPropertyChanged;
        if (session?.WorkspaceViewModel is ChatNestWorkspaceViewModel chatVm)
            chatVm.PropertyChanged -= OnChatNestPropertyChanged;
        if (session?.WorkspaceViewModel is IDisposable disposable)
            disposable.Dispose();

        _sessionManager.Remove(tab.Id);
        var currentTab = _tabs.FirstOrDefault(t => t.Id == tab.Id);
        if (currentTab != null)
            _tabs.Remove(currentTab);
    }

    private ChatNestTransientDraftState? HandleTransientDraftReadResult(
        string draftPath,
        TransientDraftReadResult result,
        out bool partialTransient,
        out bool quarantineFailed)
    {
        partialTransient = false;
        quarantineFailed = false;

        switch (result.Status)
        {
            case TransientDraftReadStatus.NotPresent:
                return null;
            case TransientDraftReadStatus.Loaded:
                return result.State;
            case TransientDraftReadStatus.InvalidFormat:
            case TransientDraftReadStatus.UnsupportedVersion:
            case TransientDraftReadStatus.HashMismatch:
                partialTransient = true;
                ErrorLogService.Log("DraftRestoreTransient", new InvalidDataException(result.Status.ToString()), filePath: draftPath);
                try
                {
                    DraftStore.QuarantineTransientState(draftPath);
                }
                catch (Exception ex)
                {
                    quarantineFailed = true;
                    ErrorLogService.Log("DraftRestoreQuarantine", ex, filePath: draftPath);
                }
                return null;
            case TransientDraftReadStatus.IoError:
                partialTransient = true;
                ErrorLogService.Log("DraftRestoreTransient", new IOException(result.Detail ?? "ChatNest sidecar read failed."), filePath: draftPath);
                return null;
            default:
                partialTransient = true;
                ErrorLogService.Log("DraftRestoreTransient", new InvalidDataException(result.Status.ToString()), filePath: draftPath);
                return null;
        }
    }

    private bool FailAndQuarantineWorkspaceDraft(
        string draftPath,
        Exception ex,
        out DraftRestoreIssue? issue)
    {
        ErrorLogService.Log("DraftRestore", ex, filePath: draftPath);
        issue = QuarantineWorkspaceDraftAfterBodyFailure(draftPath);
        return false;
    }

    private DraftRestoreIssue QuarantineWorkspaceDraftAfterBodyFailure(string draftPath)
    {
        try
        {
            DraftStore.QuarantineWorkspaceDraft(draftPath);
            return new DraftRestoreIssue(BodyQuarantined: true);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("DraftRestoreQuarantine", ex, filePath: draftPath);
            return new DraftRestoreIssue(QuarantineFailed: true);
        }
    }

    private static void ShowDraftRestoreSummary(DraftRestoreSummary summary)
    {
        if (!summary.HasIssues)
            return;

        var message = new StringBuilder();
        if (summary.RestoredCount > 0)
            message.AppendLine($"{summary.RestoredCount}件の下書きを復元しました。").AppendLine();

        if (summary.PartialTransientCount > 0)
            message.AppendLine($"・{summary.PartialTransientCount}件は未送信入力などの一時状態を復元できなかったため、確定済みの内容だけを復元しました。");
        if (summary.BodyQuarantinedCount > 0)
            message.AppendLine($"・{summary.BodyQuarantinedCount}件は下書きを読み取れなかったため、削除せず drafts フォルダー内へ退避しました。");
        if (summary.QuarantineFailedCount > 0)
            message.AppendLine($"・{summary.QuarantineFailedCount}件は読み取れない下書きまたは sidecar の退避にも失敗したため、そのまま保持しました。");
        if (summary.CollisionWriteFailedCount > 0)
            message.AppendLine($"・{summary.CollisionWriteFailedCount}件はタブID衝突を安全に解消できなかったため、復元せず元の下書きを保持しました。");
        if (summary.DiscardDeleteFailedCount > 0)
            message.AppendLine($"・{summary.DiscardDeleteFailedCount}件は下書きの破棄に失敗したため、そのまま保持しました。");

        MessageBox.Show(
            message.ToString().TrimEnd(),
            "下書きの復元",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}
