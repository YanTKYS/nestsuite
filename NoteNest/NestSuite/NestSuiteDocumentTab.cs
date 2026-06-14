namespace NoteNest.NestSuite;

/// <summary>
/// NestSuite でのタブの最小モデル。1 つのファイル／作業単位を表す不変レコード。
///
/// <para><b>NestSuite の最終タブはファイル／作業単位</b><br/>
/// NestSuite の最終タブはツール単位（「NoteNest」タブ 1 枚・「ChatNest」タブ 1 枚）ではなく、
/// ファイル／作業単位で持つ設計を目指す。<br/>
/// 目指す形：<c>[NoteNest: A.notenest] [ChatNest: 会議メモ.chatnest] [NoteNest: B.notenest]</c><br/>
/// 避ける形：<c>[NoteNest] [ChatNest] [IdeaNest]</c></para>
///
/// <para><b>NestSuiteTool との違い</b><br/>
/// <see cref="NestSuiteTool"/> はツールの「機能定義」（何ができるか、統合状態）を表す。<br/>
/// <c>NestSuiteDocumentTab</c> は「何が開いているか」（どのファイルが・どのツールで・変更済みか）を表す。<br/>
/// 1 つのツールから複数タブが生まれる（例：NoteNest で A.notenest と B.notenest を同時に開く）。</para>
///
/// <para><b>v1.7.2 の位置づけ</b><br/>
/// 設計レベルのモデル定義として導入する。本格的な TabControl・複数 Workspace のライフサイクル管理・
/// ファイル保存の共通化は v1.7.3 以降で行う。</para>
/// </summary>
public sealed record NestSuiteDocumentTab
{
    /// <summary>タブを一意識別する ID。</summary>
    public required string Id { get; init; }

    /// <summary>
    /// このタブが属する Workspace の種類。
    /// 将来の NestSuiteShellWindow は選択中タブの WorkspaceKind に応じて Workspace 表示を切り替える。
    /// <list type="bullet">
    ///   <item><term>NoteNest</term><description>NoteNestWorkspaceView を表示</description></item>
    ///   <item><term>ChatNest</term><description>ChatNestWorkspaceView を表示</description></item>
    ///   <item><term>IdeaNest</term><description>未統合プレースホルダーを表示（統合後は IdeaNestWorkspaceView）</description></item>
    /// </list>
    /// </summary>
    public required NestSuiteWorkspaceKind WorkspaceKind { get; init; }

    /// <summary>
    /// タブに表示するラベル（例：A.notenest、会議メモ.chatnest、無題.chatnest）。
    /// 保存済みの場合はファイル名、未保存の場合は「無題.拡張子」を設定する想定。
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// 対応するファイルパス。<c>null</c> の場合は未保存の無題タブ。
    /// 保存後にファイルパスが確定したらタブを再生成して DisplayName に反映する。
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// 未保存変更があるかどうか。タブ名への <c>*</c> 表示などに使用する想定。
    /// </summary>
    public bool IsModified { get; init; }

    /// <summary>
    /// ファイルに紐づいていない無題タブかどうか（<c>FilePath is null</c>）。
    /// </summary>
    public bool IsUntitled => FilePath is null;

    /// <summary>
    /// <see cref="NestSuiteToolRegistry"/> の ToolId に対応する文字列。
    /// <see cref="WorkspaceKind"/> から一意に導出されるため、
    /// 設定する必要はなく読み取り専用として提供する。
    /// </summary>
    public string ToolId => WorkspaceKind switch
    {
        NestSuiteWorkspaceKind.NoteNest => NestSuiteToolRegistry.NoteNestToolId,
        NestSuiteWorkspaceKind.ChatNest => NestSuiteToolRegistry.ChatNestToolId,
        NestSuiteWorkspaceKind.IdeaNest => NestSuiteToolRegistry.IdeaNestToolId,
        _ => throw new ArgumentOutOfRangeException(nameof(WorkspaceKind), WorkspaceKind, null)
    };
}
