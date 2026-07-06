using System.IO;
using NestSuite.Services;

namespace NestSuite;

/// <summary>
/// <see cref="NestSuiteDocumentTab"/> を生成するファクトリ。
///
/// <para>ファイル拡張子（<c>.notenest</c> / <c>.chatnest</c> / <c>.ideanest</c>）および
/// <c>.nestsuite</c> envelope の内容から <see cref="NestSuiteWorkspaceKind"/> を判定し、
/// 判定失敗時は理由（<see cref="WorkspaceKindDetectionFailure"/>）付きで通知する。
/// このクラスはタブモデルの「どの WorkspaceKind がどのファイル拡張子に対応するか」を
/// <see cref="ExtensionByKind"/> の 1 箇所で管理する。<see cref="KindByExtension"/> は逆引き用として
/// <see cref="ExtensionByKind"/> から導出し、二重管理を防ぐ。</para>
///
/// <para><b>拡張子とタブの関係</b><br/>
/// <list type="bullet">
///   <item><term>.notenest</term><description>NoteNest タブ</description></item>
///   <item><term>.chatnest</term><description>ChatNest タブ</description></item>
///   <item><term>.ideanest</term><description>IdeaNest タブ</description></item>
///   <item><term>.nestsuite</term><description>envelope 経由で内容から WorkspaceKind を判定するタブ</description></item>
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
            // v2.14.8: 各 FileService の FileExtension 定数を単一情報源として参照する
            [NestSuiteWorkspaceKind.NoteNest] = ProjectFileService.FileExtension,
            [NestSuiteWorkspaceKind.ChatNest] = ChatNest.ChatNestFileService.FileExtension,
            [NestSuiteWorkspaceKind.IdeaNest] = IdeaNest.Services.IdeaNestFileService.FileExtension,
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
    /// ファイルパスからタブを生成する。
    /// 拡張子（.nestsuite の場合はファイル内容の workspaceKind）から
    /// <see cref="NestSuiteWorkspaceKind"/> を決定する。
    /// レガシー拡張子についてはファイル内容の読込は行わず、ViewModel の生成も行わない
    /// （いずれも呼び出し側の責務）。
    /// </summary>
    /// <exception cref="ArgumentException">対応していない拡張子・種別判定不能の場合。</exception>
    public static NestSuiteDocumentTab FromFilePath(string filePath)
    {
        if (!TryGetKind(filePath, out var kind))
            throw new ArgumentException(
                $"対応していないファイル形式です: {Path.GetExtension(filePath)}", nameof(filePath));

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
    /// ファイルパスから <see cref="NestSuiteWorkspaceKind"/> を解決できるかどうかを確認する。
    /// v2.14.1 FM-1: `.nestsuite` の場合は wrapper の workspaceKind を内容から判定する
    /// （ファイル未存在・wrapper 不正時は false）。全経路の種別判定はこのメソッドに集約されている。
    /// </summary>
    public static bool TryGetKind(string filePath, out NestSuiteWorkspaceKind kind) =>
        TryGetKind(filePath, out kind, out _);

    /// <summary>
    /// v2.14.7 SH-31: 判定失敗理由つきの種別判定。呼び元（セッション復元・pipe・起動引数・最近ファイル）が
    /// 失敗を無言でスキップせず、理由に応じた文言（<see cref="Services.FileErrorMessages.ForKindDetectionFailure"/>）で
    /// 通知できるようにする。
    /// `.nestsuite` は wrapper の payloadSchemaVersion が現行より新しい場合、本読込（FM-4 ガード）まで
    /// 進む前にここで <see cref="WorkspaceKindDetectionFailure.SchemaVersionTooNew"/> として検出する。
    /// </summary>
    public static bool TryGetKind(
        string filePath, out NestSuiteWorkspaceKind kind, out WorkspaceKindDetectionFailure failure)
    {
        failure = WorkspaceKindDetectionFailure.None;
        var ext = Path.GetExtension(filePath);
        if (KindByExtension.TryGetValue(ext, out kind)) return true;

        if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(filePath))
        {
            var result = NestSuiteWorkspaceEnvelope.DetectKindFromFile(filePath);
            if (result.Failure != WorkspaceKindDetectionFailure.None)
            {
                failure = result.Failure;
                kind = default;
                return false;
            }

            var mapped = MapEnvelopeKind(result.WorkspaceKind);
            if (mapped == null)
            {
                failure = WorkspaceKindDetectionFailure.UnknownWorkspaceKind;
                kind = default;
                return false;
            }

            if (IsPayloadSchemaTooNew(mapped.Value, result.PayloadSchemaVersion))
            {
                failure = WorkspaceKindDetectionFailure.SchemaVersionTooNew;
                kind = default;
                return false;
            }

            kind = mapped.Value;
            return true;
        }

        failure = WorkspaceKindDetectionFailure.UnsupportedExtension;
        kind = default;
        return false;
    }

    /// <summary>
    /// v2.14.7 SH-31: wrapper の payloadSchemaVersion が該当 Workspace の現行 schema より新しいかを判定する。
    /// 解釈できない version はここでは失敗にせず false を返す（本読込側の FM-4 ガード・検証に委ねる）。
    /// </summary>
    private static bool IsPayloadSchemaTooNew(NestSuiteWorkspaceKind kind, string payloadSchemaVersion)
    {
        if (string.IsNullOrWhiteSpace(payloadSchemaVersion)) return false;
        var current = kind switch
        {
            NestSuiteWorkspaceKind.NoteNest => Models.Project.CurrentSchemaVersion,
            NestSuiteWorkspaceKind.IdeaNest => IdeaNest.Services.IdeaNestFileService.SchemaVersion,
            NestSuiteWorkspaceKind.ChatNest => ChatNest.ChatNestFileService.FileVersionString,
            _ => null,
        };
        if (current == null) return false;
        if (!SchemaVersionGuard.TryParse(payloadSchemaVersion, out var file) ||
            !SchemaVersionGuard.TryParse(current, out var currentParsed)) return false;
        return file > currentParsed;
    }

    /// <summary>wrapper の workspaceKind 文字列を enum へ対応付ける。未知の種別は null。</summary>
    private static NestSuiteWorkspaceKind? MapEnvelopeKind(string? kindName) => kindName switch
    {
        NestSuiteWorkspaceEnvelope.KindNoteNest => NestSuiteWorkspaceKind.NoteNest,
        NestSuiteWorkspaceEnvelope.KindIdeaNest => NestSuiteWorkspaceKind.IdeaNest,
        NestSuiteWorkspaceEnvelope.KindChatNest => NestSuiteWorkspaceKind.ChatNest,
        _ => null,
    };

    /// <summary>
    /// v2.6.0: TempNest 固定タブを生成する。CanClose=false の固定タブ。
    /// </summary>
    public static NestSuiteDocumentTab CreateTempTab()
        => new NestSuiteDocumentTab
        {
            Id            = "tempnest-fixed",
            WorkspaceKind = NestSuiteWorkspaceKind.Temp,
            DisplayName   = "Temp",
            FilePath      = null,
            IsModified    = false,
            CanClose      = false,
        };
}
