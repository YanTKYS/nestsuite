namespace NoteNest.NestSuite;

/// <summary>
/// <see cref="NestSuiteDocumentTab"/> を生成するファクトリ（設計レベルの骨格）。
///
/// <para><b>v1.7.2 の位置づけ</b><br/>
/// 骨格のみを提供する。実際のファイル読込・ViewModel 生成・ライフサイクル管理は v1.7.3 以降で行う。
/// このクラスはタブモデルの「どのファイル拡張子がどの WorkspaceKind に対応するか」を
/// 唯一の情報源として管理する。</para>
///
/// <para><b>拡張子とタブの関係</b><br/>
/// <list type="bullet">
///   <item><term>.notenest</term><description>NoteNest タブ（既存保存形式 v1.4.1 を維持）</description></item>
///   <item><term>.chatnest</term><description>ChatNest タブ（v1.7.2 では保存／読込は未実装）</description></item>
///   <item><term>.ideanest</term><description>IdeaNest タブ（v1.7.2 では未統合・将来予定）</description></item>
/// </list>
/// </para>
/// </summary>
public static class NestSuiteTabFactory
{
    /// <summary>拡張子（小文字）から WorkspaceKind へのマッピング。唯一の情報源。</summary>
    private static readonly Dictionary<string, NestSuiteWorkspaceKind> ExtensionMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { ".notenest", NestSuiteWorkspaceKind.NoteNest },
            { ".chatnest", NestSuiteWorkspaceKind.ChatNest },
            { ".ideanest", NestSuiteWorkspaceKind.IdeaNest },
        };

    /// <summary>各 WorkspaceKind に対応するファイル拡張子。</summary>
    public static string GetExtension(NestSuiteWorkspaceKind kind) => kind switch
    {
        NestSuiteWorkspaceKind.NoteNest => ".notenest",
        NestSuiteWorkspaceKind.ChatNest => ".chatnest",
        NestSuiteWorkspaceKind.IdeaNest => ".ideanest",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    /// <summary>
    /// 無題の新規タブを生成する。
    /// <c>FilePath = null</c>、<c>IsUntitled = true</c>、<c>IsModified = false</c>。
    /// </summary>
    public static NestSuiteDocumentTab CreateUntitled(NestSuiteWorkspaceKind kind)
    {
        var ext = GetExtension(kind);
        return new NestSuiteDocumentTab
        {
            Id          = Guid.NewGuid().ToString("N"),
            WorkspaceKind = kind,
            DisplayName = $"無題{ext}",
            FilePath    = null,
            IsModified  = false,
        };
    }

    /// <summary>
    /// ファイルパスからタブを生成する（骨格）。
    /// 拡張子から <see cref="NestSuiteWorkspaceKind"/> を決定する。
    /// <b>v1.7.2 では実ファイルの読込・ViewModel 生成は行わない。</b>
    /// </summary>
    /// <exception cref="ArgumentException">対応していない拡張子の場合。</exception>
    public static NestSuiteDocumentTab FromFilePath(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        if (!ExtensionMap.TryGetValue(ext, out var kind))
            throw new ArgumentException($"対応していないファイル形式です: {ext}", nameof(filePath));

        return new NestSuiteDocumentTab
        {
            Id          = Guid.NewGuid().ToString("N"),
            WorkspaceKind = kind,
            DisplayName = Path.GetFileName(filePath),
            FilePath    = filePath,
            IsModified  = false,
        };
    }

    /// <summary>
    /// ファイルパスの拡張子から <see cref="NestSuiteWorkspaceKind"/> を解決できるかどうかを確認する。
    /// </summary>
    public static bool TryGetKind(string filePath, out NestSuiteWorkspaceKind kind)
    {
        var ext = Path.GetExtension(filePath);
        return ExtensionMap.TryGetValue(ext, out kind);
    }
}
