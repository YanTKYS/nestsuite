using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NestSuite.Services;

namespace NestSuite.ChatNest;

/// <summary>
/// .chatnest ファイルの保存・読込を担当するサービス。
/// ChatNest v0.4.1 の保存形式（version: "0.4.1", messages 配列）と互換性を持つ。
/// tmp+replace パターンにより保存中断でもファイルが壊れない。
/// </summary>
public static class ChatNestFileService
{
    public const string FileExtension = ".chatnest";
    public const string FileVersionString = "0.4.1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string SerializePayload(IEnumerable<Message> messages)
    {
        var data = new ChatSessionData
        {
            Messages = messages.Select(m => new MessageData
            {
                Id = m.Id,
                Speaker = m.Speaker.ToString(),
                Text = m.Text,
                CreatedAt = m.CreatedAt
            }).ToList()
        };
        return JsonSerializer.Serialize(data, JsonOptions);
    }

    public static string SerializeWrapped(IEnumerable<Message> messages) => NestSuiteWorkspaceEnvelope.Wrap(
        NestSuiteWorkspaceEnvelope.KindChatNest, FileVersionString, SerializePayload(messages));

    /// <summary>.chatnest ファイルにメッセージを保存する（tmp+replace パターン）。</summary>
    /// <exception cref="IOException">ファイル書き込みに失敗した場合。</exception>
    public static void Save(string path, IEnumerable<Message> messages) =>
        Save(path, messages, createBackup: true);

    /// <summary>
    /// v2.16.6 TD-64: createBackup=false の場合、正本は更新するが .bak は更新しない（自動保存向け）。
    /// </summary>
    /// <exception cref="IOException">ファイル書き込みに失敗した場合。</exception>
    public static void Save(string path, IEnumerable<Message> messages, bool createBackup)
    {
        var json = NestSuiteWorkspaceEnvelope.IsEnvelopePath(path)
            ? SerializeWrapped(messages)
            : SerializePayload(messages);
        // v2.14.5 FM-5: 既存ファイルがある場合は .bak を残す（NoteNest / IdeaNest と同方針に統一）
        // v2.16.6 TD-64: createBackup=false（自動保存）では .bak を更新せず atomic write のみ行う
        if (createBackup)
            AtomicFileWriter.WriteAllTextWithBackup(path, json, System.Text.Encoding.UTF8);
        else
            AtomicFileWriter.WriteAllText(path, json, System.Text.Encoding.UTF8);
    }

    /// <summary>.chatnest ファイルを読み込み、Message リストを返す。</summary>
    /// <exception cref="IOException">ファイル読み込みに失敗した場合。</exception>
    /// <exception cref="InvalidDataException">JSON 形式が無効な場合。</exception>
    public static List<Message> Load(string path)
    {
        var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
        // v2.14.1 FM-1: .nestsuite の場合は wrapper を剥がして既存のデシリアライズ経路へ渡す
        NestSuiteWorkspaceEnvelope.EnvelopeContent? envelope = null;
        if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(path))
        {
            envelope = NestSuiteWorkspaceEnvelope.Read(json);
            NestSuiteWorkspaceEnvelope.EnsureKind(envelope, NestSuiteWorkspaceEnvelope.KindChatNest);
            // v2.14.4 FM-4: payload を読む前に、wrapper が宣言する payload schema が現行より新しくないか確認する
            SchemaVersionGuard.EnsureNotNewer(envelope.PayloadSchemaVersion, FileVersionString, "ChatNest");
            json = envelope.PayloadJson;
        }
        return DeserializeAndValidate(json, envelope);
    }

    /// <summary>
    /// v2.16.35 TD-59b-2 (nestsuite-double-read-design-review.md §8.6, §10):
    /// probe（<see cref="NestSuiteTabFactory.TryPrepareOpen"/>）が既に読んだ wrapper を追加読込なしで
    /// デシリアライズする。<paramref name="context"/> の path と解析済み内容は分離できない
    /// （path のみを別引数で受ける overload は追加しない）。
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> が null の場合。</exception>
    /// <exception cref="ArgumentException">
    /// FilePath が空・Temp・path/拡張子/kind の組み合わせが呼び出し契約に反する場合。
    /// </exception>
    /// <exception cref="InvalidDataException">wrapper の workspaceKind が ChatNest ではない場合。</exception>
    public static List<Message> LoadPrepared(WorkspaceFileOpenContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(context.FilePath))
            throw new ArgumentException("FilePath が空です。", nameof(context));
        if (context.WorkspaceKind == NestSuiteWorkspaceKind.Temp)
            throw new ArgumentException("TempNest はファイル型 Workspace ではありません。", nameof(context));

        if (context.Preloaded is { } preloaded)
        {
            // (a) preloaded + レガシー拡張子パス = TryPrepareOpen を経ていない組み合わせ
            if (!NestSuiteWorkspaceEnvelope.IsEnvelopePath(context.FilePath))
                throw new ArgumentException("解析済み envelope はレガシー拡張子には使えません。", nameof(context));
            // (b) path 不一致 = 別ファイルの解析結果を組み替えた誤配線（同種ファイル間も検出）
            if (!NestSuiteOpenFilePolicy.IsSameFile(context.FilePath, preloaded.SourcePath))
                throw new ArgumentException(
                    "解析済み Workspace データの読込元パスが、指定されたファイルパスと一致しません。", nameof(context));
            // (c) wrapper 内容と読込先の不一致（利用者起因の種別違いでも起きるため、既存文言を維持）
            NestSuiteWorkspaceEnvelope.EnsureKind(preloaded.Envelope, NestSuiteWorkspaceEnvelope.KindChatNest);
            // (d) (c) を通過したのに enum が異なる = context の改変等の契約違反
            if (context.WorkspaceKind != NestSuiteWorkspaceKind.ChatNest)
                throw new ArgumentException("WorkspaceKind が読込先と一致しません。", nameof(context));
            // (e) FM-4: wrapper 宣言 schema の too-new 事前確認（現行と同一の SchemaVersionGuard 例外）
            SchemaVersionGuard.EnsureNotNewer(preloaded.Envelope.PayloadSchemaVersion, FileVersionString, "ChatNest");
            // (f) 追加のファイル読込は行わない（0 回）
            return DeserializeAndValidate(preloaded.Envelope.PayloadJson, preloaded.Envelope);
        }

        // (g) .nestsuite なのに preloaded がない = TryPrepareOpen を経ていない契約違反
        if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(context.FilePath))
            throw new ArgumentException(".nestsuite の prepared 読込には解析済み envelope が必要です。", nameof(context));
        // (h) レガシー誤配線（他 Workspace の拡張子を含む）
        if (context.WorkspaceKind != NestSuiteWorkspaceKind.ChatNest)
            throw new ArgumentException("WorkspaceKind が読込先と一致しません。", nameof(context));
        // (h2) v2.16.36 TD-59b-2-2: WorkspaceKind は一致していても、FilePath のレガシー拡張子が
        // 別 Workspace のもの（例: .ideanest）である不正 context をファイル I/O 前に拒否する。
        // 通常の TryPrepareOpen からは生成されないが、FileService 境界で防御する。
        if (!string.Equals(Path.GetExtension(context.FilePath), FileExtension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"prepared 読込の拡張子は {FileExtension} である必要があります。", nameof(context));
        // (i) レガシー拡張子は従来経路（読込 1 回・挙動不変）
        return Load(context.FilePath);
    }

    /// <summary>
    /// payload JSON のデシリアライズ + schema 検証。<see cref="Load"/> / <see cref="LoadPrepared"/> で共有する。
    /// </summary>
    private static List<Message> DeserializeAndValidate(
        string payloadJson, NestSuiteWorkspaceEnvelope.EnvelopeContent? envelope)
    {
        var data = JsonSerializer.Deserialize<ChatSessionData>(payloadJson, JsonOptions)
            ?? throw new InvalidDataException(".chatnest ファイルの形式が無効です。");
        // v2.14.4 FM-4: 現行より新しい version の読み込みを止め、保存で未知データを失う経路を防ぐ
        // （未知 speaker の読込時スキップ仕様自体は従来どおり。新 version 検出時のみ失敗させる）
        SchemaVersionGuard.EnsureNotNewer(data.Version, FileVersionString, "ChatNest");
        if (envelope != null)
            SchemaVersionGuard.EnsureEnvelopeConsistent(
                envelope.PayloadSchemaVersion, data.Version, "ChatNest");

        var result = new List<Message>();
        foreach (var md in data.Messages)
        {
            // v0.4.1 互換: "要約" → "結論" マッピング、未知の発言者はスキップ
            var speakerName = md.Speaker == "要約" ? "結論" : md.Speaker;
            if (!Enum.TryParse<Speaker>(speakerName, out var speaker))
                continue;

            result.Add(new Message
            {
                Id        = md.Id,
                Speaker   = speaker,
                Text      = md.Text,
                CreatedAt = md.CreatedAt
            });
        }
        return result;
    }

    // ── JSON シリアライズ用内部型 ─────────────────────────────────────────

    private sealed class ChatSessionData
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = FileVersionString;

        [JsonPropertyName("messages")]
        public List<MessageData> Messages { get; set; } = new();
    }

    private sealed class MessageData
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("speaker")]
        public string Speaker { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
