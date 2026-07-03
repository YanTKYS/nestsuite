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

    /// <summary>.chatnest ファイルにメッセージを保存する（tmp+replace パターン）。</summary>
    /// <exception cref="IOException">ファイル書き込みに失敗した場合。</exception>
    public static void Save(string path, IEnumerable<Message> messages)
    {
        var data = new ChatSessionData
        {
            Messages = messages.Select(m => new MessageData
            {
                Id        = m.Id,
                Speaker   = m.Speaker.ToString(),
                Text      = m.Text,
                CreatedAt = m.CreatedAt
            }).ToList()
        };

        var json = JsonSerializer.Serialize(data, JsonOptions);
        // v2.14.1 FM-1: .nestsuite の場合は wrapper で包む。payload（既存 ChatNest JSON）の中身は変更しない
        if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(path))
            json = NestSuiteWorkspaceEnvelope.Wrap(
                NestSuiteWorkspaceEnvelope.KindChatNest, FileVersionString, json);
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
        var data = JsonSerializer.Deserialize<ChatSessionData>(json, JsonOptions)
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
