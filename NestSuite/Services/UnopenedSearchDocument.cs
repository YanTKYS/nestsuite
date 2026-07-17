namespace NestSuite.Services;

/// <summary>
/// SH-41 (AT-2 フェーズ1): 「最近のファイルも検索」ON時に読み込んだ、未オープンrecent file 1件分の
/// 検索用スナップショット。<see cref="SavedModel"/> はWorkspace ViewModelではなく保存モデル
/// （NoteNest=<c>Project</c>・IdeaNest=<c>Workspace</c>・ChatNest=<c>List&lt;Message&gt;</c>）そのもの。
/// パネル表示中かつ「最近のファイルも検索」ON中だけメモリ上に保持し、永続化しない。
/// </summary>
public sealed record UnopenedSearchDocument(
    NestSuiteWorkspaceKind WorkspaceKind,
    string FilePath,
    string FileName,
    object SavedModel);

/// <summary>
/// SH-41: 1ファイル分の読込結果。<see cref="Document"/> が null の場合は読込失敗
/// （不存在・破損・未対応形式・schema too-new 等、理由を問わずスキップ対象）。
/// </summary>
public sealed record UnopenedFileLoadResult(string FilePath, UnopenedSearchDocument? Document)
{
    public bool Succeeded => Document != null;
}
