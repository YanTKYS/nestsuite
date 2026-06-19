using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NoteNest.NestSuite.ChatNest;

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
        var tmp  = path + ".tmp";
        File.WriteAllText(tmp, json, System.Text.Encoding.UTF8);

        if (File.Exists(path))
            File.Replace(tmp, path, null);
        else
            File.Move(tmp, path);
    }

    /// <summary>.chatnest ファイルを読み込み、Message リストを返す。</summary>
    /// <exception cref="IOException">ファイル読み込みに失敗した場合。</exception>
    /// <exception cref="InvalidDataException">JSON 形式が無効な場合。</exception>
    public static List<Message> Load(string path)
    {
        var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
        var data = JsonSerializer.Deserialize<ChatSessionData>(json, JsonOptions)
            ?? throw new InvalidDataException(".chatnest ファイルの形式が無効です。");

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
