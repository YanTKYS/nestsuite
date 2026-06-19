using System.IO;

namespace NoteNest.NestSuite;

/// <summary>
/// <see cref="NestSuiteDocumentTab"/> を生成するファクトリ（設計レベルの骨格）。
///
/// <para><b>v1.7.2 の位置づけ</b><br/>
/// 骨格のみを提供する。実際のファイル読込・ViewModel 生成・ライフサイクル管理は v1.7.3 以降で行う。
/// このクラスはタブモデルの「どの WorkspaceKind がどのファイル拡張子に対応するか」を
/// <see cref="ExtensionByKind"/> の 1 箇所で管理する。<see cref="KindByExtension"/> は逆引き用として
/// <see cref="ExtensionByKind"/> から導出し、二重管理を防ぐ。</para>
///
/// <para><b>拡張子とタブの関係</b><br/>
/// <list type="bullet">
///   <item><term>.notenest</term><description>NoteNest タブ（既存保存形式 v1.4.1 を維持）</description></item>
///   <item><term>.chatnest</term><description>ChatNest タブ</description></item>
///   <item><term>.ideanest</term><description>IdeaNest タブ</description></item>
/// </list>
/// </para>
/// </summary>
public static class NestSuiteTabFactory
{
    /// <summary>
    /// WorkspaceKind → 拡張子のマッピング。唯一の情報源。
    /// 新しい WorkspaceKind を追加するときはここだけを変更する。
    /// </summary>
    private static readonly IReadOnlyDictionary<NestSuiteWorkspaceKind, string> ExtensionByKind =
        new Dictionary<NestSuiteWorkspaceKind, string>
        {
            [NestSuiteWorkspaceKind.NoteNest] = ".notenest",
            [NestSuiteWorkspaceKind.ChatNest] = ".chatnest",
            [NestSuiteWorkspaceKind.IdeaNest] = ".ideanest",
        };

    /// <summary>
    /// 拡張子 → WorkspaceKind の逆引き辞書（大文字小文字を区別しない）。
    /// <see cref="ExtensionByKind"/> から導出するため、定義は 1 箇所に集約される。
    /// </summary>
    private static readonly IReadOnlyDictionary<string, NestSuiteWorkspaceKind> KindByExtension =
        ExtensionByKind.ToDictionary(p => p.Value, p => p.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>指定した WorkspaceKind に対応するファイル拡張子を返す。</summary>
    /// <exception cref="ArgumentOutOfRangeException">未知の WorkspaceKind の場合。</exception>
    public static string GetExtension(NestSuiteWorkspaceKind kind) =>
        ExtensionByKind.TryGetValue(kind, out var ext)
            ? ext
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, null);

    /// <summary>
    /// 無題の新規タブを生成する。
    /// <c>FilePath = null</c>、<c>IsUntitled = true</c>、<c>IsModified = false</c>。
    /// </summary>
    public static NestSuiteDocumentTab CreateUntitled(NestSuiteWorkspaceKind kind)
    {
        var ext = GetExtension(kind);
        return new NestSuiteDocumentTab
        {
            Id            = Guid.NewGuid().ToString("N"),
            WorkspaceKind = kind,
            DisplayName   = $"無題{ext}",
            FilePath      = null,
            IsModified    = false,
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
        if (!KindByExtension.TryGetValue(ext, out var kind))
            throw new ArgumentException($"対応していないファイル形式です: {ext}", nameof(filePath));

        return new NestSuiteDocumentTab
        {
            Id            = Guid.NewGuid().ToString("N"),
            WorkspaceKind = kind,
            DisplayName   = Path.GetFileName(filePath),
            FilePath      = filePath,
            IsModified    = false,
        };
    }

    /// <summary>
    /// ファイルパスの拡張子から <see cref="NestSuiteWorkspaceKind"/> を解決できるかどうかを確認する。
    /// </summary>
    public static bool TryGetKind(string filePath, out NestSuiteWorkspaceKind kind)
    {
        var ext = Path.GetExtension(filePath);
        return KindByExtension.TryGetValue(ext, out kind);
    }
}
