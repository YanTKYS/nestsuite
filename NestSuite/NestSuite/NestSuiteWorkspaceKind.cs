namespace NestSuite;

/// <summary>
/// NestSuite 上で扱う Workspace の種別。
///
/// <para><b>ToolId との違い</b><br/>
/// <see cref="NestSuiteToolRegistry"/> の ToolId は「ツール機能の定義」を表す。
/// <c>NestSuiteWorkspaceKind</c> は「開いているタブが属する Workspace の種類」を表す。
/// ツールは複数タブを生み出せる（例：NoteNest タブを 2 つ同時に開く）が、
/// 各タブは必ず 1 つの WorkspaceKind に属する。</para>
///
/// <para><b>現在の役割</b><br/>
/// ファイル単位タブ設計（v1.7.2 で導入）の中核モデル。<c>NestSuiteShellWindow</c> は
/// 選択中タブの <c>WorkspaceKind</c> に応じて Workspace 表示を切り替える。</para>
/// </summary>
public enum NestSuiteWorkspaceKind
{
    /// <summary>
    /// NoteNest Workspace。拡張子 <c>.notenest</c> ファイルに対応する。
    /// 統合済み：選択時に <c>NoteNestWorkspaceView</c> を表示する。
    /// </summary>
    NoteNest,

    /// <summary>
    /// ChatNest Workspace。拡張子 <c>.chatnest</c> ファイルに対応する。
    /// 統合済み：選択時に <c>ChatNestWorkspaceView</c> を表示する。
    /// </summary>
    ChatNest,

    /// <summary>
    /// IdeaNest Workspace。拡張子 <c>.ideanest</c> ファイルに対応する。
    /// 統合済み：選択時に <c>IdeaNestWorkspaceView</c> を表示する。
    /// </summary>
    IdeaNest,

    /// <summary>
    /// TempNest Workspace。NestSuite Shell 固定の一時メモ領域。
    /// ファイル型 Workspace ではなく、内部 JSON で管理される。
    /// </summary>
    Temp,
}
