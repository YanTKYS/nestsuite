namespace NoteNest.NestSuite;

/// <summary>
/// NestSuite 上で扱う Workspace の種別。
///
/// <para><b>ToolId との違い</b><br/>
/// <see cref="NestSuiteToolRegistry"/> の ToolId は「ツール機能の定義」を表す。
/// <c>NestSuiteWorkspaceKind</c> は「開いているタブが属する Workspace の種類」を表す。
/// ツールは複数タブを生み出せる（例：NoteNest タブを 2 つ同時に開く）が、
/// 各タブは必ず 1 つの WorkspaceKind に属する。</para>
///
/// <para><b>v1.7.2 の位置づけ</b><br/>
/// ファイル単位タブ設計（v1.7.2）で導入する最小モデルの一部。
/// 将来の <c>NestSuiteShellWindow</c> では、選択中タブの <c>WorkspaceKind</c> に応じて
/// Workspace 表示を切り替える想定。本格的な TabControl 実装は v1.7.3 以降で行う。</para>
/// </summary>
public enum NestSuiteWorkspaceKind
{
    /// <summary>
    /// NoteNest Workspace。拡張子 <c>.notenest</c> ファイルに対応する。
    /// 統合済み：選択時に <c>NoteNestWorkspaceView</c> を表示する。
    /// </summary>
    NoteNest,

    /// <summary>
    /// ChatNest Workspace。将来的に拡張子 <c>.chatnest</c> ファイルに対応する。
    /// 統合検証段階：選択時に <c>ChatNestWorkspaceView</c> を表示する。
    /// v1.7.2 では <c>.chatnest</c> の保存／読込は実装しない。
    /// </summary>
    ChatNest,

    /// <summary>
    /// IdeaNest Workspace。将来的に拡張子 <c>.ideanest</c> ファイルに対応する想定。
    /// v1.7.2 では未統合：選択時に未統合プレースホルダーを表示する。
    /// IdeaNest 統合前に IdeaNestWorkspaceView の切り出しが必要。
    /// </summary>
    IdeaNest,
}
