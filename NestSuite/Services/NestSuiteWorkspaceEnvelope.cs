using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NestSuite.Services;

/// <summary>
/// v2.14.1 FM-1: `.nestsuite` wrapper 形式の読み書き。
/// `.nestsuite` は 1 つの Workspace（NoteNest / IdeaNest / ChatNest）を表すラッパーで、
/// 複数 Workspace を格納する統合コンテナではない（1タブ1ファイルを維持）。
/// wrapper 自体の formatVersion と payload 側の schema version は分離して管理する。
/// 方針は docs/development/workspace-file-extension-unification.md 参照。
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
    /// ファイルから workspaceKind を判定する。wrapper として読めない・存在しない場合は null。
    /// 例外を外へ投げない（呼び元は null を「判定不能」として扱う）。
    /// </summary>
    public static string? TryDetectKindFromFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            return Read(File.ReadAllText(path)).WorkspaceKind;
        }
        catch
        {
            return null;
        }
    }
}
