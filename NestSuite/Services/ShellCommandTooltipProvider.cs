namespace NestSuite.Services;

/// <summary>
/// v2.16.10 SH-30: NestSuite Shell の主要コマンド（保存・名前を付けて保存・すべて保存・
/// タブを閉じる・ピン留め・ピン留め解除・NoteNest Markdown エクスポート）について、
/// 有効時の短い説明と、無効時の「なぜ押せないか」を一貫した文言で返す UI 非依存の判断ロジック。
///
/// 既存の IsEnabled / CanExecute 条件そのものは変えず、同じ条件に対して同じ文言を返すことだけを
/// 担う（呼び出し側は既存の判定条件をそのまま渡す）。内部実装名・例外名・ViewModel 名は含めない。
/// </summary>
public static class ShellCommandTooltipProvider
{
    /// <summary>上書き保存。保存対象タブが無ければ理由、あっても未保存の変更が無ければ理由を返す。</summary>
    public static string SaveTooltip(bool hasSavableTab, bool isModified) =>
        !hasSavableTab ? "保存できるタブがありません"
        : !isModified  ? "未保存の変更がありません"
        : "現在のタブを保存します";

    /// <summary>名前を付けて保存。保存対象タブが無ければ理由を返す（未保存の変更が無くても別名保存は可能）。</summary>
    public static string SaveAsTooltip(bool hasSavableTab) =>
        !hasSavableTab ? "保存できるタブがありません"
        : "現在のタブを別名で保存します";

    /// <summary>すべて保存。未保存タブが 1 件も無ければ理由を返す。</summary>
    public static string SaveAllTooltip(bool hasUnsavedTabs) =>
        !hasUnsavedTabs ? "未保存のタブがありません"
        : "未保存のタブをすべて保存します";

    /// <summary>タブを閉じる。閉じられるタブ（選択中かつ CanClose）が無ければ理由を返す。</summary>
    public static string TabCloseTooltip(bool hasClosableTab) =>
        !hasClosableTab ? "閉じられるタブがありません"
        : "現在のタブを閉じます";

    /// <summary>
    /// ピン留め。Temp タブはピン留め対象外のため専用の理由を返す。
    /// それ以外でピン留めできない場合（未選択・対象外タブ）は「通常タブを選択してください」を返す。
    /// </summary>
    public static string PinTooltip(bool canPin, bool isTempTab) =>
        isTempTab ? "Temp タブはピン留めできません"
        : !canPin ? "通常タブを選択してください"
        : "このタブをピン留めします";

    /// <summary>ピン留め解除。ピン留めされた通常タブでなければ理由を返す。</summary>
    public static string UnpinTooltip(bool canUnpin) =>
        !canUnpin ? "ピン留めされたタブを選択してください"
        : "このタブのピン留めを解除します";

    /// <summary>NoteNest: 選択ノートを対象にした Markdown エクスポート（コピー／保存）。</summary>
    public static string MarkdownExportSelectedNoteTooltip(bool hasSelectedNote) =>
        !hasSelectedNote ? "ノートを選択してください"
        : "選択したノートを Markdown として出力します";

    /// <summary>NoteNest: 全ノートを対象にした Markdown エクスポート。</summary>
    public static string MarkdownExportAllNotesTooltip(bool hasAnyNotes) =>
        !hasAnyNotes ? "エクスポートできるノートがありません"
        : "NoteNest を Markdown として出力します";

    /// <summary>
    /// 特定 Workspace 種別でのみ意味を持つ操作向けの、種別不一致時の無効理由。
    /// 例: NoteNest 固有操作を NoteNest 以外のタブが選択された状態で評価すると
    /// 「NoteNest のタブを選択してください」を返す。
    /// </summary>
    public static string RequireWorkspaceKindTooltip(
        NestSuiteWorkspaceKind requiredKind, bool isMatchingKind, string enabledText) =>
        !isMatchingKind ? $"{WorkspaceKindDisplayName(requiredKind)} のタブを選択してください" : enabledText;

    private static string WorkspaceKindDisplayName(NestSuiteWorkspaceKind kind) => kind switch
    {
        NestSuiteWorkspaceKind.NoteNest => "NoteNest",
        NestSuiteWorkspaceKind.IdeaNest => "IdeaNest",
        NestSuiteWorkspaceKind.ChatNest => "ChatNest",
        NestSuiteWorkspaceKind.Temp     => "TempNest",
        _                                => "対象",
    };

    // ── 常時有効なヘルプ・設定項目。無効理由は持たない短い説明のみ。 ──────────
    public const string KeyboardShortcutsTooltip  = "キーボードショートカット一覧を表示します";
    public const string BackupRestoreGuideTooltip = "バックアップからの復元手順を表示します";
    public const string FileAssociationTooltip    = "ファイルの関連付けを設定します";
}
