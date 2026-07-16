namespace NestSuite.TempNest;

/// <summary>
/// SH-40 (AT-1 フェーズ1): 「続きから」表示条件・recent files上位抽出のWPF非依存ロジック。
/// docs/planning/at1-continue-start-panel-design-review.md の確定方針に基づく最小実装。
/// Shell（NestSuiteShellWindow）はここから判定結果だけを受け取り、実際のUI・タブ操作・
/// ファイルI/Oは一切行わない（表示条件や上位抽出のためにファイルへアクセスしない）。
/// </summary>
public static class ContinueFromPanelPolicy
{
    public const int MaxRecentItems = 3;

    /// <summary>
    /// 起動時に「続きから」候補を評価すべきかを判定する。既存のTempNestアクティブ化分岐
    /// （通常Workspaceのsession復元が1件も成功せず、かつ起動引数によるファイル指定もない）
    /// と同一条件。新しい「初回起動」判定は追加しない。
    /// </summary>
    public static bool ShouldEvaluateAtStartup(bool sessionRestoreSucceeded, string? launchFilePath) =>
        !sessionRestoreSucceeded && string.IsNullOrEmpty(launchFilePath);

    /// <summary>
    /// recent filesのMRU順一覧から上位<see cref="MaxRecentItems"/>件を返す。
    /// 独自の並べ替え・スコアリング・重複排除・存在確認は行わない（既存順序をそのまま使う）。
    /// </summary>
    public static IReadOnlyList<string> SelectTopRecentItems(IReadOnlyList<string> recentFiles) =>
        recentFiles.Count <= MaxRecentItems ? recentFiles : recentFiles.Take(MaxRecentItems).ToList();
}
