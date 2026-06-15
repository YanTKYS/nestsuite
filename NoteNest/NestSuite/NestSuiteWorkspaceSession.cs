namespace NoteNest.NestSuite;

/// <summary>
/// v1.9.1: 1 タブのWorkspace実体を保持するセッションモデル。
///
/// <para><b>v1.9.0 設計（案B）の実装</b><br/>
/// <see cref="NestSuiteDocumentTab"/> は表示情報（DisplayName / IsModified など）を持つ不変 record。
/// <see cref="NestSuiteWorkspaceSession"/> は Workspace の実体（ViewModel・ファイルパス・未保存状態）を持つ。
/// 両者は <see cref="TabId"/> で対応付けられる。</para>
///
/// <para><b>v1.9.1 の位置づけ（最小骨格）</b><br/>
/// 同一ツール複数ファイル対応の前段階として「セッション管理」の構造を確立する。
/// v1.9.1 では各ツールの ViewModel はまだ 1 インスタンス（既存シェルフィールド）を共有している。
/// <see cref="WorkspaceViewModel"/> の型は将来型安全化する余地を残すため <see cref="object"/> とする。
/// タブごとの ViewModel インスタンス独立化は v1.9.2〜v1.9.4 で行う。</para>
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
    /// Workspace の ViewModel インスタンス。
    /// v1.9.1 では種別ごとに 1 つの既存インスタンスを参照する。
    /// v1.9.2 以降でタブごとの独立インスタンスへ移行する。
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
