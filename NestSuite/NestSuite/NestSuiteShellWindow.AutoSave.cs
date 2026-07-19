using System.Windows.Threading;
using NestSuite.ChatNest;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.IdeaNest.Services;
using NestSuite.Services;
using NestSuite.ViewModels;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // v2.14.12 SH-33: 既存Workspaceの自動保存。
    // 保存先パスを持つ NoteNest / IdeaNest / ChatNest タブのうち未保存のものだけを、
    // 一定間隔で既存の保存経路（Try*ToPath / vm.SaveToPath）を通じて保存する。
    // 新規未保存タブ・TempNest は対象外（AutoSaveCandidatePolicy 参照）。
    // 保存失敗時は dirty を維持し、同一タブへの失敗通知は再成功するまで 1 回のみ表示する。
    // 保存形式・schema・session 形式は一切変更しない（既存の保存経路をそのまま呼ぶだけ）。

    private static readonly TimeSpan AutoSaveInterval = TimeSpan.FromSeconds(30);
    private DispatcherTimer? _autoSaveTimer;
    private readonly HashSet<string> _autoSaveNotifiedFailureTabIds = new();
    private readonly HashSet<string> _draftAutoSaveLoggedFailureTabIds = new();

    private void StartAutoSaveTimer()
    {
        if (_autoSaveTimer != null) return;
        _autoSaveTimer = new DispatcherTimer { Interval = AutoSaveInterval };
        _autoSaveTimer.Tick += (_, _) => RunAutoSaveTick();
        _autoSaveTimer.Start();
    }

    private void StopAutoSaveTimer()
    {
        _autoSaveTimer?.Stop();
        _autoSaveTimer = null;
    }

    /// <summary>
    /// タイマー tick 本体。全タブを走査し、自動保存対象（<see cref="AutoSaveCandidatePolicy"/>）
    /// のみを保存する。対象がなければ何もしない。
    /// </summary>
    private void RunAutoSaveTick()
    {
        foreach (var tab in _tabs.ToList())
        {
            if (!_sessionManager.TryGet(tab.Id, out var session) || session == null) continue;

            if (tab.FilePath != null)
            {
                var isDirtyForAutoSave = ResolveAutoSaveDirtyState(tab, session);
                if (!AutoSaveCandidatePolicy.IsCandidate(tab.WorkspaceKind, tab.FilePath, isDirtyForAutoSave)) continue;

                if (AutoSaveTab(tab, session))
                    _autoSaveNotifiedFailureTabIds.Remove(tab.Id);
                continue;
            }

            if (tab.WorkspaceKind == NestSuiteWorkspaceKind.Temp) continue;
            var isDirtyForDraft = ResolveDraftDirtyState(tab, session);
            if (DraftCandidatePolicy.IsCandidate(tab.WorkspaceKind, tab.FilePath, isDirtyForDraft))
                WriteDraftForTab(tab, session);
            else
                TryDeleteDraftForTab(tab.Id, "DraftDeleteCandidateFalse");
        }
    }

    /// <summary>
    /// v2.14.12 SH-33 レビュー対応: 自動保存の dirty 判定には、原則 <c>tab.IsModified</c> を使う。
    /// ただし ChatNest だけは例外で、<c>ChatNestWorkspaceViewModel.HasUnsavedChanges</c>
    /// （= tab.IsModified の元）が投稿前の入力欄テキスト・編集中差分という
    /// **永続化されない一時状態**を含むため、保存しても解消されず無限に自動保存が
    /// 繰り返されてしまう。ChatNest では実際に永続化される変更のみを表す
    /// <c>IsDirty</c>（<c>MarkSaved()</c> で確実にクリアされる）を判定に使う。
    /// </summary>
    private static bool ResolveAutoSaveDirtyState(NestSuiteDocumentTab tab, NestSuiteWorkspaceSession session) =>
        tab.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest
            ? ((ChatNestWorkspaceViewModel)session.WorkspaceViewModel).IsDirty
            : tab.IsModified;

    private static bool ResolveDraftDirtyState(NestSuiteDocumentTab tab, NestSuiteWorkspaceSession session) =>
        tab.WorkspaceKind switch
        {
            NestSuiteWorkspaceKind.NoteNest => tab.IsModified,
            NestSuiteWorkspaceKind.IdeaNest => ((IdeaNestWorkspaceViewModel)session.WorkspaceViewModel).HasChanges,
            // SH-36 drafts intentionally use HasUnsavedChanges, unlike SH-33 auto-save's IsDirty,
            // because InputText and EditingText are not covered by normal ChatNest save files.
            NestSuiteWorkspaceKind.ChatNest => ((ChatNestWorkspaceViewModel)session.WorkspaceViewModel).HasUnsavedChanges,
            _ => false,
        };

    private void WriteDraftForTab(NestSuiteDocumentTab tab, NestSuiteWorkspaceSession session)
    {
        try
        {
            var wrappedJson = tab.WorkspaceKind switch
            {
                NestSuiteWorkspaceKind.NoteNest => new ProjectFileService().SerializeWrapped(
                    ((MainViewModel)session.WorkspaceViewModel).CreateProjectSnapshotForDraft()),
                NestSuiteWorkspaceKind.IdeaNest => IdeaNestFileService.SerializeWrapped(
                    ((IdeaNestWorkspaceViewModel)session.WorkspaceViewModel).BuildWorkspaceForSave()),
                NestSuiteWorkspaceKind.ChatNest => ChatNestFileService.SerializeWrapped(
                    ((ChatNestWorkspaceViewModel)session.WorkspaceViewModel).MessageModels.ToList()),
                _ => null,
            };
            if (wrappedJson == null) return;
            var transient = session.WorkspaceViewModel is ChatNestWorkspaceViewModel chatVm
                ? chatVm.CreateTransientDraftState()
                : null;
            DraftStore.WriteWorkspaceDraft(tab.Id, wrappedJson, transient);
            _draftAutoSaveLoggedFailureTabIds.Remove(tab.Id);
        }
        catch (Exception ex)
        {
            if (_draftAutoSaveLoggedFailureTabIds.Add(tab.Id))
                ErrorLogService.Log("DraftAutoSave", ex, tab.WorkspaceKind.ToString(), null);
        }
    }

    private static void TryDeleteDraftForTab(string tabId, string operation)
    {
        try { DraftStore.Delete(tabId); }
        catch (Exception ex) { ErrorLogService.Log(operation, ex, filePath: null); }
    }

    private bool AutoSaveTab(NestSuiteDocumentTab tab, NestSuiteWorkspaceSession session)
    {
        var path = NormalizeFilePath(tab.FilePath!);
        // v2.16.6 TD-64: 自動保存は正本を更新するが .bak は更新しない
        // （createBackup: false。atomic write は維持する。docs/planning/review1-fable5.md R-1）
        var succeeded = tab.WorkspaceKind switch
        {
            NestSuiteWorkspaceKind.NoteNest => AutoSaveNoteNestTab(session, path),
            NestSuiteWorkspaceKind.IdeaNest =>
                TrySaveIdeaNestToPath(session, path, showNotification: false, notifyOnError: false, createBackup: false),
            NestSuiteWorkspaceKind.ChatNest =>
                TrySaveChatNestToPath(session, path, showNotification: false, notifyOnError: false, createBackup: false),
            NestSuiteWorkspaceKind.PlainText =>
                TrySaveTextToPath(session, path, showNotification: false, notifyOnError: false, createBackup: false),
            _ => false,
        };

        if (succeeded)
        {
            // v2.14.12 SH-33: 常時表示 UI を増やさないため、通知はアクティブタブの自動保存時のみ短時間表示する。
            // バックグラウンドタブは未保存マーク（●）が消えることで結果が分かる。
            if (tab.Id == _selectedTab?.Id)
                ShowStatusNotification("  |  自動保存しました");
        }
        else
        {
            NotifyAutoSaveFailure(tab);
        }

        return succeeded;
    }

    private bool AutoSaveNoteNestTab(NestSuiteWorkspaceSession session, string path)
    {
        var vm = (MainViewModel)session.WorkspaceViewModel;
        // v2.16.6 TD-64: 自動保存では .bak を更新しない
        if (!vm.SaveToPath(path, notifyOnError: false, createBackup: false)) return false;
        UpdateNoteNestTabPath(session, path, showNotification: false);
        return true;
    }

    /// <summary>
    /// 自動保存失敗を通知する。同一タブでは、直前の通知後に一度成功するまで再通知しない
    /// （<see cref="RunAutoSaveTick"/> が成功時に <see cref="_autoSaveNotifiedFailureTabIds"/> から除去する）。
    /// </summary>
    private void NotifyAutoSaveFailure(NestSuiteDocumentTab tab)
    {
        if (!_autoSaveNotifiedFailureTabIds.Add(tab.Id)) return;
        _dialogs.ShowError(
            $"自動保存に失敗しました。\n手動保存を試してください。\n\n対象: {tab.FilePath}",
            "自動保存エラー");
    }
}
