using NestSuite.TempNest;

namespace NestSuite.Services;

/// <summary>
/// SH-37: 「現在の状態」サマリーのうち、Shell の内部タブ・セッション管理に依存しない
/// 集計ロジックだけを切り出したもの。単体テストのための分離であり、将来のホーム画面や
/// ダッシュボードを見越した汎用集計基盤ではない。
/// </summary>
public static class ShellStateSummaryCalculator
{
    /// <summary>
    /// TN-3 の昇格対象判定と同じ基準（本文が空白以外）で、TempNest の入力済みスロット数を数える。
    /// タイトルのみ入力されたスロットは数えない。
    /// </summary>
    public static int CountNonEmptyTempNestSlots(IEnumerable<TempNestSlotViewModel> slots) =>
        slots.Count(slot => !string.IsNullOrWhiteSpace(slot.Body));

    /// <summary>
    /// <see cref="DraftStore.ListDraftFiles"/> が返す下書きファイルパスのうち、
    /// <paramref name="openTabIds"/>（現在開いているタブの ID）に対応するものを除いた件数を返す。
    /// 現在開いているタブの自動保存下書きは「復元候補」ではなく通常の下書きバックアップのため除外する。
    /// </summary>
    public static int CountDraftRecoveryCandidates(IEnumerable<string> draftPaths, IEnumerable<string> openTabIds)
    {
        var openSet = new HashSet<string>(openTabIds, StringComparer.Ordinal);
        return draftPaths.Count(path => DraftStore.TryGetTabId(path, out var tabId) && !openSet.Contains(tabId));
    }
}
