namespace NestSuite;

/// <summary>
/// 1 タブのWorkspace実体を保持するセッションモデル。
///
/// <para><see cref="NestSuiteDocumentTab"/> は表示情報（DisplayName / IsModified など）を持つ不変 record。
/// <see cref="NestSuiteWorkspaceSession"/> は Workspace の実体（ViewModel・ファイルパス・未保存状態）を持つ。
/// 両者は <see cref="TabId"/> で対応付けられる。</para>
/// </summary>
public sealed class NestSuiteWorkspaceSession
{
    /// <summary>
    /// 対応する <see cref="NestSuiteDocumentTab.Id"/> と一致するタブ識別子。
    /// <see cref="NestSuiteWorkspaceSessionManager"/> のキーとして使用する。
    /// </summary>
    public string TabId { get; }

    /// <summary>このセッションが属する Workspace の種類。</summary>
    public NestSuiteWorkspaceKind WorkspaceKind { get; }

    /// <summary>
    /// Workspace の ViewModel インスタンス。Workspace 種別ごとに型が異なるため
    /// <see cref="object"/> で保持し、利用側でキャストする。
    /// </summary>
    public object WorkspaceViewModel { get; }

    /// <summary>
    /// 現在開いているファイルパス。無題セッション（未保存）は <c>null</c>。
    /// タブ表示情報（<see cref="NestSuiteDocumentTab.FilePath"/>）と同期する。
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// 未保存変更があるかどうか。
    /// タブ表示情報（<see cref="NestSuiteDocumentTab.IsModified"/>）と同期する。
    /// </summary>
    public bool IsModified { get; set; }

    public NestSuiteWorkspaceSession(
        string tabId,
        NestSuiteWorkspaceKind workspaceKind,
        object workspaceViewModel,
        string? filePath = null,
        bool isModified = false)
    {
        TabId = tabId;
        WorkspaceKind = workspaceKind;
        WorkspaceViewModel = workspaceViewModel;
        FilePath = filePath;
        IsModified = isModified;
    }
}
