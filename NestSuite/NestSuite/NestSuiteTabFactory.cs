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
            // v2.19.0 SH-43: .txt は他の 3 形式と異なり .nestsuite wrapper へは格納しない
            // （IsPayloadSchemaTooNew の対象外・envelope kind マッピングも追加しない）が、
            // 拡張子 → WorkspaceKind の判定・タブ生成は同じ ExtensionByKind 経路を共有する。
            [NestSuiteWorkspaceKind.PlainText] = PlainText.PlainTextFileService.FileExtension,
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
    /// v2.16.34 TD-59b-1: <see cref="TryGetKind(string, out NestSuiteWorkspaceKind)"/> +
    /// <see cref="FromResolvedKind"/> の合成として実装する（挙動は不変）。
    /// </summary>
    /// <exception cref="ArgumentException">対応していない拡張子・種別判定不能の場合。</exception>
    public static NestSuiteDocumentTab FromFilePath(string filePath)
    {
        if (!TryGetKind(filePath, out var kind))
            throw new ArgumentException(
                $"対応していないファイル形式です: {Path.GetExtension(filePath)}", nameof(filePath));

        return FromResolvedKind(filePath, kind);
    }

    /// <summary>
    /// v2.16.34 TD-59b-1 (nestsuite-double-read-design-review.md §8.4):
    /// 判定済み kind からタブを生成する。ファイル I/O を行わない（<see cref="TryGetKind(string, out NestSuiteWorkspaceKind)"/>
    /// を呼ばない）。<see cref="TryPrepareOpen"/> で probe 済みの呼び出し元が、読込#3（再判定）を
    /// 省略するために使う。Temp を含む未知の kind は <see cref="GetExtension"/> と同じ基準で
    /// <see cref="ArgumentOutOfRangeException"/> により明示的に失敗する。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Temp や未知の kind の場合。</exception>
    public static NestSuiteDocumentTab FromResolvedKind(string filePath, NestSuiteWorkspaceKind kind)
    {
        GetExtension(kind); // 未知の kind（Temp 含む）をここで明示的に失敗させる（現行 factory 方針と同じ基準）

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
    /// ファイルパスから <see cref="NestSuiteWorkspaceKind"/> を解決できるか確認する。
    /// `.nestsuite` を含む実際の probe・wrapper 解析・失敗分類は
    /// <see cref="TryPrepareOpen"/> に集約され、この overload は
    /// WorkspaceKind だけを必要とする呼び出し向けに結果を委譲して返す。
    /// </summary>
    public static bool TryGetKind(string filePath, out NestSuiteWorkspaceKind kind) =>
        TryGetKind(filePath, out kind, out _);

    /// <summary>
    /// v2.14.7 SH-31: 判定失敗理由つきの種別判定。呼び元（セッション復元・pipe・起動引数・最近ファイル）が
    /// 失敗を無言でスキップせず、理由に応じた文言（<see cref="Services.FileErrorMessages.ForKindDetectionFailure"/>）で
    /// 通知できるようにする。
    /// `.nestsuite` は wrapper の payloadSchemaVersion が現行より新しい場合、本読込（FM-4 ガード）まで
    /// 進む前にここで <see cref="WorkspaceKindDetectionFailure.SchemaVersionTooNew"/> として検出する。
    /// v2.16.34 TD-59b-1: 実装を <see cref="TryPrepareOpen"/> へ委譲する（context は破棄する）。
    /// 種別判定の集約点・公開挙動は不変。
    /// </summary>
    public static bool TryGetKind(
        string filePath, out NestSuiteWorkspaceKind kind, out WorkspaceKindDetectionFailure failure)
    {
        var success = TryPrepareOpen(filePath, out var context, out failure);
        kind = success ? context.WorkspaceKind : default;
        return success;
    }

    /// <summary>
    /// v2.16.34 TD-59b-1 (nestsuite-double-read-design-review.md §8.3, §16, §17):
    /// 種別判定と wrapper 解析を 1 回の読込で行い、本読込まで使えるコンテキストを返す。
    /// 種別判定の集約点は引き続きこのクラス（<see cref="TryGetKind(string, out NestSuiteWorkspaceKind, out WorkspaceKindDetectionFailure)"/>
    /// はこのメソッドへ委譲する）。
    ///
    /// <para>入口で <paramref name="filePath"/> を <see cref="Path.GetFullPath(string)"/> により
    /// 正規化し、<c>context.FilePath</c> / <c>context.Preloaded.SourcePath</c> には
    /// 同一の正規化済みパスを格納する。Shell 層の <see cref="ShellFileOpenPlanner"/> には
    /// 依存しない。</para>
    ///
    /// <para>レガシー拡張子（.notenest / .ideanest / .chatnest）は読込 delegate を呼ばず
    /// <c>Preloaded = null</c> の context を返す。`.nestsuite` は
    /// <see cref="NestSuiteWorkspaceEnvelope.ReadFromFile"/> を 1 回だけ呼び、失敗時（wrapper 解析
    /// 失敗・未知 kind・schema too-new のいずれも）は再読込しない。未対応拡張子は読込を行わない。</para>
    ///
    /// <para><paramref name="fileExists"/> / <paramref name="readAllText"/> はテスト用の読取り
    /// delegate で、<see cref="NestSuiteWorkspaceEnvelope.ReadFromFile"/> へそのまま転送する
    /// （省略時は実際の I/O）。</para>
    /// </summary>
    public static bool TryPrepareOpen(
        string filePath,
        out WorkspaceFileOpenContext context,
        out WorkspaceKindDetectionFailure failure,
        Func<string, bool>? fileExists = null,
        Func<string, string>? readAllText = null)
    {
        failure = WorkspaceKindDetectionFailure.None;
        context = default!;

        // v2.16.35 TD-59b-2: null・空・空白のみ・Path.GetFullPath が例外になる不正 path は、
        // Try... API として例外を外へ出さず UnsupportedExtension（既存 enum で最も近い値）として扱う。
        if (string.IsNullOrWhiteSpace(filePath))
        {
            failure = WorkspaceKindDetectionFailure.UnsupportedExtension;
            return false;
        }

        string normalizedPath;
        try
        {
            normalizedPath = Path.GetFullPath(filePath);
        }
        catch
        {
            failure = WorkspaceKindDetectionFailure.UnsupportedExtension;
            return false;
        }

        var ext = Path.GetExtension(normalizedPath);

        if (KindByExtension.TryGetValue(ext, out var legacyKind))
        {
            context = new WorkspaceFileOpenContext(normalizedPath, legacyKind, preloaded: null);
            return true;
        }

        if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(normalizedPath))
        {
            var readResult = NestSuiteWorkspaceEnvelope.ReadFromFile(normalizedPath, fileExists, readAllText);
            if (readResult.Failure != WorkspaceKindDetectionFailure.None)
            {
                failure = readResult.Failure;
                return false;
            }

            var envelope = readResult.Envelope!;
            var mapped = MapEnvelopeKind(envelope.WorkspaceKind);
            if (mapped == null)
            {
                failure = WorkspaceKindDetectionFailure.UnknownWorkspaceKind;
                return false;
            }

            if (IsPayloadSchemaTooNew(mapped.Value, envelope.PayloadSchemaVersion))
            {
                failure = WorkspaceKindDetectionFailure.SchemaVersionTooNew;
                return false;
            }

            var preloaded = new PreloadedWorkspaceEnvelope(normalizedPath, envelope);
            context = new WorkspaceFileOpenContext(normalizedPath, mapped.Value, preloaded);
            return true;
        }

        failure = WorkspaceKindDetectionFailure.UnsupportedExtension;
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

    /// <summary>
    /// v2.16.39 TD-59b-5 (nestsuite-double-read-design-review.md §9, §24):
    /// 既に判定済み・信頼できる <paramref name="kind"/> と <paramref name="filePath"/> の拡張子の
    /// 組み合わせが妥当かどうかを、ファイル I/O なしで確認する。<b>WorkspaceKind を判定する API
    /// ではない</b>（それは <see cref="TryPrepareOpen"/> の役割のまま）。
    ///
    /// <para>保存直後の内部状態更新（<c>SavedWorkspaceStateUpdater</c>）や、読込済み ViewModel の
    /// タブ同期（<c>SyncNoteNestTabForViewModel</c>）など、WorkspaceKind が既に信頼できる内部状態
    /// として確定している経路専用。利用者が任意のファイルを開く入口の検証には使わない
    /// （そちらは引き続き <see cref="TryPrepareOpen"/> → <c>LoadPrepared</c> → <c>EnsureKind</c> →
    /// schema 検証を使う）。</para>
    ///
    /// <para><c>.nestsuite</c> は拡張子だけで NoteNest / IdeaNest / ChatNest のいずれとも
    /// 組み合わせ妥当とする（wrapper 内容は読まない）。レガシー拡張子は <paramref name="kind"/> に
    /// 対応する拡張子（大文字小文字を区別しない）と一致する場合のみ true。null・空・空白 path、
    /// <see cref="Path.GetFullPath(string)"/> が例外になる入力、未対応拡張子、Temp、未知の kind は
    /// すべて false。</para>
    /// </summary>
    public static bool IsPathCompatibleWithResolvedKind(string filePath, NestSuiteWorkspaceKind kind)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return false;

        string normalizedPath;
        try
        {
            normalizedPath = Path.GetFullPath(filePath);
        }
        catch
        {
            return false;
        }

        if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(normalizedPath))
        {
            return kind is NestSuiteWorkspaceKind.NoteNest
                or NestSuiteWorkspaceKind.IdeaNest
                or NestSuiteWorkspaceKind.ChatNest;
        }

        return ExtensionByKind.TryGetValue(kind, out var expectedExt) &&
            string.Equals(Path.GetExtension(normalizedPath), expectedExt, StringComparison.OrdinalIgnoreCase);
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
