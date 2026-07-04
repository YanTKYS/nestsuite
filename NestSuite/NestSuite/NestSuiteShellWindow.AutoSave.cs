using System.Windows.Threading;
using NestSuite.IdeaNest.ViewModels;
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

    private void StartAutoSaveTimer()
    {
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
            if (!AutoSaveCandidatePolicy.IsCandidate(tab.WorkspaceKind, tab.FilePath, tab.IsModified)) continue;
            if (!_sessionManager.TryGet(tab.Id, out var session) || session == null) continue;

            if (AutoSaveTab(tab, session))
                _autoSaveNotifiedFailureTabIds.Remove(tab.Id);
        }
    }

    private bool AutoSaveTab(NestSuiteDocumentTab tab, NestSuiteWorkspaceSession session)
    {
        var path = NormalizeFilePath(tab.FilePath!);
        var succeeded = tab.WorkspaceKind switch
        {
            NestSuiteWorkspaceKind.NoteNest => AutoSaveNoteNestTab(session, path),
            NestSuiteWorkspaceKind.IdeaNest =>
                TrySaveIdeaNestToPath(session, path, showNotification: false, notifyOnError: false),
            NestSuiteWorkspaceKind.ChatNest =>
                TrySaveChatNestToPath(session, path, showNotification: false, notifyOnError: false),
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
        if (!vm.SaveToPath(path, notifyOnError: false)) return false;
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
