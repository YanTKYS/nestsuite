using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NestSuite.Services;

/// <summary>
/// v2.14.1 FM-1: `.nestsuite` wrapper 形式の読み書き。
/// `.nestsuite` は 1 つの Workspace（NoteNest / IdeaNest / ChatNest）を表すラッパーで、
/// 複数 Workspace を格納する統合コンテナではない（1タブ1ファイルを維持）。
/// wrapper 自体の formatVersion と payload 側の schema version は分離して管理する。
/// 方針は docs/archive/migrations/workspace-file-extension-unification.md 参照。
/// </summary>
public static class NestSuiteWorkspaceEnvelope
{
    public const string FileExtension = ".nestsuite";
    public const string FormatName = "NestSuiteWorkspace";
    public const string CurrentFormatVersion = "1.0";

    public const string KindNoteNest = "NoteNest";
    public const string KindIdeaNest = "IdeaNest";
    public const string KindChatNest = "ChatNest";

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>読み取った wrapper の内容。PayloadJson は既存 FileService のデシリアライザへそのまま渡せる。</summary>
    public sealed record EnvelopeContent(
        string FormatVersion,
        string WorkspaceKind,
        string PayloadSchemaVersion,
        string PayloadJson);

    public static bool IsEnvelopePath(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        string.Equals(Path.GetExtension(path), FileExtension, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 既存 Workspace の保存 JSON（payloadJson）を wrapper で包んだ JSON を返す。
    /// </summary>
    public static string Wrap(string workspaceKind, string payloadSchemaVersion, string payloadJson)
    {
        var envelope = new JsonObject
        {
            ["format"] = FormatName,
            ["formatVersion"] = CurrentFormatVersion,
            ["workspaceKind"] = workspaceKind,
            ["payloadSchemaVersion"] = payloadSchemaVersion,
            ["payload"] = JsonNode.Parse(payloadJson),
        };
        return envelope.ToJsonString(WriteOptions);
    }

    /// <summary>
    /// wrapper JSON を読み取る。必須項目（format / workspaceKind / payload）が欠けている場合は
    /// 分かりやすい <see cref="InvalidDataException"/> で失敗する。
    /// 未知の追加プロパティは無視する（将来の項目追加で読み込みが壊れないようにする）。
    /// formatVersion / payloadSchemaVersion の欠落は許容する（前方互換のため）。
    /// </summary>
    public static EnvelopeContent Read(string envelopeJson)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(envelopeJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("この .nestsuite ファイルの JSON を読み取れません。", ex);
        }

        if (root is not JsonObject obj)
            throw new InvalidDataException("この .nestsuite ファイルの形式が無効です。");

        var format = GetStringOrNull(obj, "format");
        if (!string.Equals(format, FormatName, StringComparison.Ordinal))
            throw new InvalidDataException("この .nestsuite ファイルは NestSuite Workspace 形式ではありません。");

        var workspaceKind = GetStringOrNull(obj, "workspaceKind");
        if (string.IsNullOrWhiteSpace(workspaceKind))
            throw new InvalidDataException("この .nestsuite ファイルの Workspace 種別を判定できません。");

        if (obj["payload"] is not JsonObject payload)
            throw new InvalidDataException("この .nestsuite ファイルに Workspace データ（payload）がありません。");

        return new EnvelopeContent(
            GetStringOrNull(obj, "formatVersion") ?? "",
            workspaceKind,
            GetStringOrNull(obj, "payloadSchemaVersion") ?? "",
            payload.ToJsonString(WriteOptions));
    }

    private static string? GetStringOrNull(JsonObject obj, string propertyName)
    {
        if (!obj.TryGetPropertyValue(propertyName, out var node) || node is null) return null;
        if (node is JsonValue value && value.TryGetValue<string>(out var s)) return s;
        return null;
    }

    /// <summary>
    /// wrapper の workspaceKind が期待する Workspace と一致することを確認する。
    /// 不一致は分かりやすい <see cref="InvalidDataException"/> で失敗する。
    /// </summary>
    public static void EnsureKind(EnvelopeContent envelope, string expectedKind)
    {
        if (!string.Equals(envelope.WorkspaceKind, expectedKind, StringComparison.Ordinal))
            throw new InvalidDataException(
                $"この .nestsuite は {expectedKind} ではなく {envelope.WorkspaceKind} の Workspace です。");
    }

    /// <summary>
    /// v2.14.7 SH-31: ファイルからの workspaceKind 判定結果。失敗時は理由を保持する。
    /// PayloadSchemaVersion は判定成功時のみ有効（呼び元での too-new 事前検出に使う）。
    /// </summary>
    public sealed record KindDetectionResult(
        string? WorkspaceKind,
        string PayloadSchemaVersion,
        WorkspaceKindDetectionFailure Failure);

    /// <summary>
    /// v2.16.34 TD-59b-1: ファイル読込 + wrapper 解析の結果。失敗時は Envelope=null + 理由。
    /// <see cref="ReadFromFile"/> の戻り値。
    /// </summary>
    public sealed record EnvelopeReadResult(
        EnvelopeContent? Envelope,
        WorkspaceKindDetectionFailure Failure);

    /// <summary>
    /// v2.16.34 TD-59b-1 (nestsuite-double-read-design-review.md §8.2, §17):
    /// ファイルを 1 回だけ読んで wrapper を解析する。例外を外へ投げない。
    /// <paramref name="fileExists"/> / <paramref name="readAllText"/> はテスト用の読取り delegate
    /// （省略時は実際の <see cref="File.Exists(string)"/> / <see cref="File.ReadAllText(string)"/>）。
    /// <see cref="ShellFileOpenPlanner.Plan"/> の fileExists/prepareOpen 注入と同じ流儀に揃える
    /// （DI 基盤・InternalsVisibleTo は導入しない）。
    /// failure 分類は従来の <see cref="DetectKindFromFile"/> と同一。
    /// </summary>
    public static EnvelopeReadResult ReadFromFile(
        string path,
        Func<string, bool>? fileExists = null,
        Func<string, string>? readAllText = null)
    {
        fileExists ??= File.Exists;
        readAllText ??= File.ReadAllText;
        try
        {
            if (!fileExists(path))
                return new EnvelopeReadResult(null, WorkspaceKindDetectionFailure.FileNotFound);
            var envelope = Read(readAllText(path));
            return new EnvelopeReadResult(envelope, WorkspaceKindDetectionFailure.None);
        }
        catch (UnauthorizedAccessException)
        {
            return new EnvelopeReadResult(null, WorkspaceKindDetectionFailure.AccessDenied);
        }
        catch (System.Security.SecurityException)
        {
            return new EnvelopeReadResult(null, WorkspaceKindDetectionFailure.AccessDenied);
        }
        catch (InvalidDataException)
        {
            // Read() の失敗（JSON 破損・wrapper 形式不一致・必須項目欠落）はすべて「形式を確認できない」扱い
            return new EnvelopeReadResult(null, WorkspaceKindDetectionFailure.InvalidFormat);
        }
        catch (IOException)
        {
            return new EnvelopeReadResult(null, WorkspaceKindDetectionFailure.IoError);
        }
        catch
        {
            return new EnvelopeReadResult(null, WorkspaceKindDetectionFailure.Unknown);
        }
    }

    /// <summary>
    /// v2.14.7 SH-31: ファイルから workspaceKind を判定し、失敗時は理由つきで返す。
    /// 例外を外へ投げない（呼び元は Failure で文言を出し分ける）。
    /// v2.16.34 TD-59b-1: 実装を <see cref="ReadFromFile"/> の上へ委譲した
    /// （読込・failure 分類ロジック自体は移動しただけで、挙動・戻り値の形は不変）。
    /// </summary>
    public static KindDetectionResult DetectKindFromFile(string path)
    {
        var result = ReadFromFile(path);
        if (result.Failure != WorkspaceKindDetectionFailure.None)
            return new KindDetectionResult(null, "", result.Failure);

        var envelope = result.Envelope!;
        return new KindDetectionResult(
            envelope.WorkspaceKind, envelope.PayloadSchemaVersion, WorkspaceKindDetectionFailure.None);
    }

    /// <summary>
    /// ファイルから workspaceKind を判定する。wrapper として読めない・存在しない場合は null。
    /// 例外を外へ投げない（呼び元は null を「判定不能」として扱う）。
    /// 失敗理由が必要な場合は <see cref="DetectKindFromFile"/> を使う。
    /// </summary>
    public static string? TryDetectKindFromFile(string path) => DetectKindFromFile(path).WorkspaceKind;
}
