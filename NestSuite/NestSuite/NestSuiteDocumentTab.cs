using System.IO;

namespace NestSuite;

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
/// <para><b>現在の役割</b><br/>
/// TabControl のタブ 1 枚・複数 Workspace のライフサイクル管理・ファイル保存の共通化を支える
/// 不変モデルとして機能する。</para>
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
    ///   <item><term>IdeaNest</term><description>IdeaNestWorkspaceView を表示</description></item>
    /// </list>
    /// </summary>
    public required NestSuiteWorkspaceKind WorkspaceKind { get; init; }

    /// <summary>
    /// タブの内部ラベル（例：A.notenest、会議メモ.chatnest、無題.chatnest）。
    /// 保存済みの場合はファイル名、未保存の場合は「無題.拡張子」を設定する想定。
    /// タブ見出しには拡張子を省いた <see cref="TabHeaderText"/> を使用する。
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
    /// タブをユーザー操作で閉じられるかどうか。
    /// false の場合は閉じるボタンを非表示にし、中クリック・一括閉じる操作の対象外とする。
    /// TempNest タブは false。通常タブは true（デフォルト）。
    /// </summary>
    public bool CanClose { get; init; } = true;

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
        NestSuiteWorkspaceKind.Temp => "Temp",
        _ => throw new ArgumentOutOfRangeException(nameof(WorkspaceKind), WorkspaceKind, null)
    };

    /// <summary>
    /// v2.6.4: タブ見出し用の拡張子なしファイル名。
    /// TempNest は DisplayName（"Temp"）をそのまま返す。
    /// </summary>
    public string ShortDisplayName =>
        WorkspaceKind == NestSuiteWorkspaceKind.Temp
            ? DisplayName
            : Path.GetFileNameWithoutExtension(DisplayName);

    /// <summary>v2.9.0 SH-21: NoteNest タブかどうか。別ウィンドウ表示メニューの表示制御に使う。</summary>
    public bool IsNoteNest => WorkspaceKind == NestSuiteWorkspaceKind.NoteNest;

    /// <summary>v2.9.3 SH-21: IdeaNest タブかどうか。別ウィンドウ表示メニューの表示制御に使う。</summary>
    public bool IsIdeaNest => WorkspaceKind == NestSuiteWorkspaceKind.IdeaNest;

    /// <summary>v2.9.4 SH-21: ChatNest タブかどうか。別ウィンドウ表示メニューの表示制御に使う。</summary>
    public bool IsChatNest => WorkspaceKind == NestSuiteWorkspaceKind.ChatNest;

    /// <summary>v2.9.1 SH-21: 現在別ウィンドウで表示中かどうか。DetachNoteNestTab/ReAttachNoteNestTab で更新する。</summary>
    public bool IsDetached { get; init; }

    /// <summary>v2.9.4 SH-21: 別ウィンドウに分離できるかどうか（NoteNest / IdeaNest / ChatNest かつ未分離）。</summary>
    public bool IsDetachable => (IsNoteNest || IsIdeaNest || IsChatNest) && !IsDetached;

    /// <summary>
    /// v2.8.9: UI Automation 用 AutomationId。TempNest 固定タブのみ設定し、他は空文字。
    /// </summary>
    public string TabAutomationId => WorkspaceKind == NestSuiteWorkspaceKind.Temp
        ? AutomationIds.Shell.TempTab
        : "";

    /// <summary>
    /// v2.6.4: タブ見出し先頭の Workspace 種別プレフィックス。
    /// </summary>
    public string KindPrefix => WorkspaceKind switch
    {
        NestSuiteWorkspaceKind.NoteNest => "📝 ",
        NestSuiteWorkspaceKind.ChatNest => "💬 ",
        NestSuiteWorkspaceKind.IdeaNest => "💡 ",
        NestSuiteWorkspaceKind.Temp     => "",
        _ => ""
    };

    /// <summary>
    /// v2.6.4: タブ見出しに表示するテキスト。種別プレフィックス＋拡張子なしファイル名。
    /// 例: "📝 業務改善" / "💬 開発メモ" / "💡 ツール改修" / "Temp"
    /// </summary>
    public string TabHeaderText => $"{KindPrefix}{ShortDisplayName}";

    /// <summary>
    /// v1.9.9 / v2.6.4: タブのツールチップ表示用テキスト。
    /// 種類・完全ファイル名・フルパス・保存状態を含む。
    /// </summary>
    public string TooltipText
    {
        get
        {
            var kindLabel = WorkspaceKind switch
            {
                NestSuiteWorkspaceKind.NoteNest => "NoteNest",
                NestSuiteWorkspaceKind.ChatNest => "ChatNest",
                NestSuiteWorkspaceKind.IdeaNest => "IdeaNest",
                NestSuiteWorkspaceKind.Temp     => "TempNest",
                _ => "不明"
            };

            if (WorkspaceKind == NestSuiteWorkspaceKind.Temp)
                return $"種類: {kindLabel}\n説明: 一時メモ\n保存: 自動保存";

            if (FilePath is null)
                return $"種類: {kindLabel}\nファイル名: 未保存（無題）\n場所: —\n状態: 未保存";

            var fileName  = Path.GetFileName(FilePath);
            var stateText = IsModified ? "未保存の変更あり" : "保存済み";
            return $"種類: {kindLabel}\nファイル名: {fileName}\n場所: {FilePath}\n状態: {stateText}";
        }
    }
}
