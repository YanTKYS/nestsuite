using NestSuite.TempNest;

namespace NestSuite;

/// <summary>
/// SH-40 (AT-1 フェーズ1): 通常Workspaceのsession復元・起動引数によるファイル指定が
/// 一切なかった起動時にだけ、TempNest上部へ「続きから」（recent files上位3件・保持中draft件数）を
/// 表示するための、Shell側の最小限の配線。設計根拠は
/// docs/planning/at1-continue-start-panel-design-review.md。
///
/// <para>責務分離: 表示条件・上位抽出の純粋ロジックは <see cref="ContinueFromPanelPolicy"/>、
/// 表示用データ・排他状態は <see cref="TempNestWorkspaceViewModel"/> が保持する。Shellは
/// 「起動時にどの値を渡すか」「recentリンクを開く実処理」「通常タブ追加時の抑止」だけを担う。
/// AT-5（<see cref="NestSuiteShellWindow.GettingStartedHint"/>）の
/// <c>_tabs.CollectionChanged</c> 追跡を再利用し、個々の経路（新規作成・ファイルを開く・
/// session復元・draft復元・pipe転送等）へ重複してフックしない。</para>
/// </summary>
public partial class NestSuiteShellWindow
{
    /// <summary>
    /// SH-40: 起動時に一度だけ、recent files上位3件と保持中draft件数をTempNestへ渡す。
    /// 追加のファイルI/Oは行わない（recent filesは<see cref="_recentFilesCache"/>、draft件数は
    /// <see cref="RestoreDraftsAtStartup"/>が既に列挙した結果から算出済みの値を受け取るだけ）。
    /// </summary>
    private void ApplyContinueFromCandidatesAtStartup(NestSuiteDocumentTab tempTab, int retainedDraftCount)
    {
        if (!_sessionManager.TryGet(tempTab.Id, out var tempSession) ||
            tempSession?.WorkspaceViewModel is not TempNestWorkspaceViewModel tempVm)
            return;

        var recentTop = ContinueFromPanelPolicy.SelectTopRecentItems(_recentFilesCache);
        tempVm.SetContinueFromCandidates(recentTop, retainedDraftCount);
    }

    /// <summary>SH-40: recentリンククリック時にファイル不存在等で一覧から削除された項目をパネルからも取り除く。</summary>
    private void RemoveContinueFromRecentItemIfPresent(string filePath) =>
        FindTempNestViewModel()?.RemoveRecentContinueItem(filePath);

    /// <summary>SH-40: 通常タブ追加時に「続きから」を再表示しないようにする単発ラッチを立てる。</summary>
    private void MarkContinueFromDismissedIfPresent() =>
        FindTempNestViewModel()?.MarkContinueFromDismissed();

    private TempNestWorkspaceViewModel? FindTempNestViewModel() =>
        _sessionManager.Sessions.FirstOrDefault(s => s.WorkspaceKind == NestSuiteWorkspaceKind.Temp)
            ?.WorkspaceViewModel as TempNestWorkspaceViewModel;
}
