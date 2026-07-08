namespace NestSuite.Services;

/// <summary>
/// v2.16.9 SH-29: 終了時の未保存確認 1 件分を表す軽量な情報。件数サマリの表示にのみ使う
/// （保存・破棄の判断自体は既存の個別確認フローが担う）。
/// </summary>
public sealed record UnsavedCloseTarget(NestSuiteWorkspaceKind WorkspaceKind, string DisplayName);

/// <summary>
/// v2.16.9 SH-29: NestSuiteShellWindow.OnClosing の個別未保存確認に入る前に表示する
/// 件数サマリの、UI 非依存の判断・文言組み立てロジック。
/// 対象が 2 件未満のときはサマリを出さず、既存の個別確認フローへそのまま進む。
/// </summary>
public static class UnsavedCloseSummaryBuilder
{
    /// <summary>サマリに列挙する最大件数。超えた分は「ほか N 件」にまとめる。</summary>
    public const int MaxDisplayedItems = 5;

    /// <summary>対象が 2 件以上のときだけ件数サマリを表示する。</summary>
    public static bool ShouldShowSummary(int unsavedCount) => unsavedCount >= 2;

    /// <summary>
    /// 件数サマリの本文を組み立てる。呼び出し前に <see cref="ShouldShowSummary"/> で
    /// 2 件以上であることを確認しておくこと（0/1 件でも呼べるが通常は使わない）。
    /// </summary>
    public static string BuildMessage(IReadOnlyList<UnsavedCloseTarget> targets)
    {
        var shown = targets.Take(MaxDisplayedItems).Select(t => $"- {t.WorkspaceKind}: {t.DisplayName}");
        var remaining = targets.Count - Math.Min(targets.Count, MaxDisplayedItems);

        var itemLines = remaining > 0
            ? string.Join("\n", shown) + $"\n- ほか {remaining} 件"
            : string.Join("\n", shown);

        return $"未保存のタブが {targets.Count} 件あります。\n" +
               "これから順番に保存するか確認します。\n\n" +
               $"{itemLines}\n\n" +
               "確認を続けますか？";
    }

    /// <summary>
    /// 件数サマリ表示要否の判断と、表示した場合の「続ける / やめる」結果をまとめた分岐ロジック。
    /// 2 件未満なら showSummary を呼ばずに true（続行）を返す。
    /// showSummary はダイアログ表示を担う呼び出し側の delegate（本文を渡し、続行可否の bool を受け取る）。
    /// この関数自体はダイアログを持たず、対象タブの状態も変更しない。
    /// </summary>
    public static bool ConfirmContinue(
        IReadOnlyList<UnsavedCloseTarget> targets,
        Func<string, bool> showSummary)
    {
        if (!ShouldShowSummary(targets.Count)) return true;
        return showSummary(BuildMessage(targets));
    }
}
