using NestSuite.Services;

namespace NestSuite;

/// <summary>
/// Workspace 間手動転送（TempNest → NoteNest / TempNest → IdeaNest / ChatNest → IdeaNest）の
/// 共通ヘルパー。<b>ここが転送契約の正本</b>で、新しい転送導線を足す場合も共通化するのは
/// 以下の範囲だけに留め、UI 文言・ダイアログ・保存・session 操作は各導線側へ置く。
///
/// <para><b>責務</b><br/>
/// 転送先候補の列挙・TabId + WorkspaceKind による対象解決・session からの ViewModel 解決・
/// 本文の空判定・受入 delegate の呼び出し・結果の返却・予期しない例外の ErrorLog 記録のみを行う。</para>
///
/// <para><b>持たない責務</b><br/>
/// 利用者向け文言・ダイアログ表示・タブ生成／削除／切替・ファイル保存・session 保存・
/// dirty の直接操作・転送元の変更。これらは呼出元（各転送の導線）と転送先の既存処理が担う。</para>
/// </summary>
public partial class NestSuiteShellWindow
{
    /// <summary>
    /// 転送内容。共通化するのはこの 2 フィールドのみ。
    /// Workspace 固有情報（タグ・色・発言者・スロット番号等）は含めない。
    /// <c>Title</c> が null / 空の場合、タイトル生成は転送先の既存処理に委ねる
    /// （ChatNest 発言の転送は常に <c>Title</c> なし）。
    /// </summary>
    internal sealed record WorkspaceTransferContent
    {
        public string? Title { get; init; }
        public required string Body { get; init; }
    }

    /// <summary>
    /// 転送先の識別。WorkspaceKind + タブ Id で識別し、表示名では識別しない
    /// （同名タブが複数あっても誤転送しないため）。
    /// </summary>
    internal readonly record struct WorkspaceTransferTarget(
        NestSuiteWorkspaceKind Kind,
        string TabId,
        string DisplayName);

    /// <summary>
    /// 転送結果。5 値で確定。これ以上増やさない（呼出元は結果値ごとに利用者向け文言を決める）。
    ///
    /// <para>成功時のみ、転送先は既存の追加経路（<c>CommitAdd</c> → <c>MarkDirty</c> /
    /// <c>AddNote</c> → <c>IsModified</c>）を通って dirty になる。共通ヘルパーは
    /// <c>IsModified</c> / <c>HasChanges</c> を直接代入しない。失敗時はいずれの結果値でも
    /// 転送元・転送先とも変更しない。<b>転送は保存ではない</b>（保存は利用者の通常操作）。</para>
    /// </summary>
    internal enum WorkspaceTransferResult
    {
        /// <summary>転送先へ追加され、転送先が dirty になった。</summary>
        Success,
        /// <summary>対象タブが存在しない／解決できない（途中で閉じられた・種別不一致）。</summary>
        NoTarget,
        /// <summary>Title と Body の両方が空（空白のみ）。</summary>
        InvalidContent,
        /// <summary>転送先が追加を受け付けなかった（既存の受入 API が失敗を返した）。</summary>
        TargetRejected,
        /// <summary>予期しない例外。ErrorLog へ記録済み。</summary>
        Failed,
    }

    /// <summary>
    /// 指定 WorkspaceKind の、現在開いているタブを転送先候補として列挙する。
    /// 未保存（無題）タブ・別ウィンドウ表示中（Detached）のタブも候補に含める。閉じているファイル・
    /// 最近使ったファイル・session の pending entry は参照しない（開いているタブのみ）。
    /// タブストリップと同じ並び順（<c>_tabs</c> の順序）を維持する。
    ///
    /// <para>候補が 0 件なら呼出元は転送せず案内のみ（タブを自動生成しない）、1 件なら選択 UI なしで
    /// 直接転送、複数件なら <c>WorkspaceTransferTargetDialog</c> で明示選択させる。
    /// 転送成功後も転送先タブを前面化・切替しない（現在のタブに留まる）。</para>
    /// </summary>
    private IReadOnlyList<WorkspaceTransferTarget> EnumerateTransferTargets(NestSuiteWorkspaceKind kind) =>
        _tabs.Where(t => t.WorkspaceKind == kind)
             .Select(t => new WorkspaceTransferTarget(
                 kind,
                 t.Id,
                 t.IsDetached ? $"{t.ShortDisplayName}（別ウィンドウ）" : t.ShortDisplayName))
             .ToList();

    /// <summary>
    /// 転送先タブへ内容を追加する。<paramref name="accept"/> は転送先 Workspace の既存 public API を
    /// 1 回呼ぶだけの薄い delegate。転送のために production interface・共通基盤は新設しない。
    /// 例外の捕捉は <paramref name="accept"/> の呼び出し 1 か所のみに限る。
    /// </summary>
    private WorkspaceTransferResult TransferToWorkspaceTab<TViewModel>(
        WorkspaceTransferTarget target,
        WorkspaceTransferContent content,
        Func<TViewModel, WorkspaceTransferContent, bool> accept)
        where TViewModel : class
    {
        // Title と Body のどちらか一方でもあれば有効とする（ChatNest 発言の転送は常に Title=null）。
        if (string.IsNullOrWhiteSpace(content.Title) && string.IsNullOrWhiteSpace(content.Body))
            return WorkspaceTransferResult.InvalidContent;

        var tab = _tabs.FirstOrDefault(t => t.Id == target.TabId && t.WorkspaceKind == target.Kind);
        if (tab == null) return WorkspaceTransferResult.NoTarget;

        if (!_sessionManager.TryGet(tab.Id, out var session) || session?.WorkspaceViewModel is not TViewModel vm)
            return WorkspaceTransferResult.NoTarget;

        try
        {
            return accept(vm, content)
                ? WorkspaceTransferResult.Success
                : WorkspaceTransferResult.TargetRejected;
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("WorkspaceTransfer", ex);
            return WorkspaceTransferResult.Failed;
        }
    }
}
