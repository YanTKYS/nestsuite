using System.Windows;
using NestSuite.Dialogs;
using NestSuite.Services;
using NestSuite.TempNest;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // SH-37: Shell操作の現在地サマリー表示。利用者が明示的に開いたときだけ現在状態を収集し、
    // 読み取り専用ダイアログとして表示する。自動更新タイマー・バックグラウンド監視・
    // 履歴保存は行わない。既存の未保存判定・pending restore・下書き検出・TempNest状態を
    // そのまま再利用し、判定条件を新規に複製しない。

    private void MenuShowStateSummary_Click(object sender, RoutedEventArgs e) => ShowStateSummary();

    private void ShowStateSummary()
    {
        var summary = BuildStateSummary();
        var dialog = new ShellStateSummaryDialog(summary) { Owner = this };
        dialog.ShowDialog();
    }

    /// <summary>
    /// 表示時点の現在状態からサマリーを生成する。呼び出しのたびに最新値を集計するだけで、
    /// 値をフィールドへ保持したり session へ保存したりはしない。
    /// </summary>
    private ShellStateSummary BuildStateSummary() => new(
        OpenTabCount: _tabs.Count,
        UnsavedTabCount: GetUnsavedCloseConfirmationTargets().Count,
        PendingRestoreCount: _pendingSessionRestoreEntries.Count,
        DraftRecoveryCandidateCount: TryGetDraftRecoveryCandidateCount(),
        NonEmptyTempNestSlotCount: GetNonEmptyTempNestSlotCount());

    /// <summary>
    /// 既存の <see cref="DraftStore.ListDraftFiles"/> から、現在開いているタブの
    /// 自動保存下書き（正常な下書き）を除いた件数を返す。列挙に失敗した場合は
    /// null（ダイアログ側で「取得できません」表示）を返し、ErrorLog へ記録する。
    /// </summary>
    private int? TryGetDraftRecoveryCandidateCount()
    {
        try
        {
            var draftPaths = DraftStore.ListDraftFiles();
            var openTabIds = _tabs.Select(t => t.Id);
            return ShellStateSummaryCalculator.CountDraftRecoveryCandidates(draftPaths, openTabIds);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("ShellStateSummaryDraftRecovery", ex, filePath: DraftStore.DefaultRootDirectory);
            return null;
        }
    }

    private int GetNonEmptyTempNestSlotCount()
    {
        var tempSession = _sessionManager.Sessions.FirstOrDefault(s => s.WorkspaceKind == NestSuiteWorkspaceKind.Temp);
        if (tempSession?.WorkspaceViewModel is not TempNestWorkspaceViewModel tempVm) return 0;

        var slots = new[] { tempVm.Slot1, tempVm.Slot2, tempVm.Slot3, tempVm.Slot4 };
        return ShellStateSummaryCalculator.CountNonEmptyTempNestSlots(slots);
    }
}
