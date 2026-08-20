using NestSuite.Services;

namespace NestSuite;

/// <summary>
/// LK-4 (v2.21.0): Workspace 間手動転送の共通ヘルパー。
/// 設計正本: docs/planning/workspace-manual-transfer-helper-design.md（TD-92 / v2.20.1）。
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
    /// 転送内容。共通化するのはこの 2 フィールドのみ（TD-92 §7）。
    /// Workspace 固有情報（タグ・色・発言者・スロット番号等）は含めない。
    /// </summary>
    internal sealed record WorkspaceTransferContent
    {
        public string? Title { get; init; }
        public required string Body { get; init; }
    }

    /// <summary>
    /// 転送先の識別（TD-92 §8: 案 B、WorkspaceKind + タブ Id）。
    /// </summary>
    internal readonly record struct WorkspaceTransferTarget(
        NestSuiteWorkspaceKind Kind,
        string TabId,
        string DisplayName);

    /// <summary>
    /// 転送結果。5 値で確定（TD-92 §10）。これ以上増やさない。
    /// </summary>
    internal enum WorkspaceTransferResult
    {
        Success,
        NoTarget,
        InvalidContent,
        TargetRejected,
        Failed,
    }

    /// <summary>
    /// 指定 WorkspaceKind の、現在開いているタブを転送先候補として列挙する。
    /// 未保存（無題）タブ・別ウィンドウ表示中のタブも候補に含める。閉じているファイル・
    /// 最近使ったファイル・session の pending entry は参照しない（開いているタブのみ）。
    /// タブストリップと同じ並び順（<c>_tabs</c> の順序）を維持する。
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
    /// 1 回呼ぶだけの薄い delegate（TD-92 §9: production interface は作らない）。
    /// </summary>
    private WorkspaceTransferResult TransferToWorkspaceTab<TViewModel>(
        WorkspaceTransferTarget target,
        WorkspaceTransferContent content,
        Func<TViewModel, WorkspaceTransferContent, bool> accept)
        where TViewModel : class
    {
        // LK-3 (v2.22.0): Title と Body のどちらか一方でもあれば有効とする（TD-92 §7 の議論を LK-3 で確定）。
        // LK-4（ChatNest）は常に Title=null で呼ぶため、この判定は Body のみの空判定と同値であり回帰しない。
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
