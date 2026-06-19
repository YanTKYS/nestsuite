using System.IO;
using NoteNest.NestSuite.ChatNest;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.7.4: ChatNestFileService の保存・読込動作を確認するテスト。
/// ファイルシステムへの書き込みを伴うため、TempDir に出力して後始末する。
/// </summary>
public class ChatNestFileServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public ChatNestFileServiceTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string TempPath(string name) => Path.Combine(_tempDir, name);

    // ── 定数 ─────────────────────────────────────────────────────────────

    [Fact]
    public void FileExtension_IsExpected()
    {
        Assert.Equal(".chatnest", ChatNestFileService.FileExtension);
    }

    [Fact]
    public void FileVersionString_IsExpected()
    {
        Assert.Equal("0.4.1", ChatNestFileService.FileVersionString);
    }

    // ── 保存 ─────────────────────────────────────────────────────────────

    [Fact]
    public void Save_CreatesFile()
    {
        var path = TempPath("test.chatnest");
        ChatNestFileService.Save(path, []);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Save_DoesNotLeaveTmpFile()
    {
        var path = TempPath("test.chatnest");
        ChatNestFileService.Save(path, []);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Save_JsonContainsVersionField()
    {
        var path = TempPath("ver.chatnest");
        ChatNestFileService.Save(path, []);
        var json = File.ReadAllText(path);
        Assert.Contains("\"version\"", json);
        Assert.Contains("0.4.1", json);
    }

    [Fact]
    public void Save_JsonContainsMessagesField()
    {
        var path = TempPath("msgs.chatnest");
        ChatNestFileService.Save(path, []);
        var json = File.ReadAllText(path);
        Assert.Contains("\"messages\"", json);
    }

    [Fact]
    public void Save_Overwrites_ExistingFile()
    {
        var path = TempPath("overwrite.chatnest");
        ChatNestFileService.Save(path, [new Message { Speaker = Speaker.自分, Text = "first" }]);
        ChatNestFileService.Save(path, [new Message { Speaker = Speaker.反論, Text = "second" }]);
        var loaded = ChatNestFileService.Load(path);
        Assert.Single(loaded);
        Assert.Equal("second", loaded[0].Text);
    }

    // ── 読込 ─────────────────────────────────────────────────────────────

    [Fact]
    public void Load_EmptyMessages_ReturnsEmptyList()
    {
        var path = TempPath("empty.chatnest");
        ChatNestFileService.Save(path, []);
        var result = ChatNestFileService.Load(path);
        Assert.Empty(result);
    }

    [Fact]
    public void Load_PreservesMessageCount()
    {
        var messages = new[]
        {
            new Message { Speaker = Speaker.自分,  Text = "メッセージ1" },
            new Message { Speaker = Speaker.反論,  Text = "メッセージ2" },
            new Message { Speaker = Speaker.補足,  Text = "メッセージ3" },
            new Message { Speaker = Speaker.結論,  Text = "メッセージ4" },
        };
        var path = TempPath("roundtrip.chatnest");
        ChatNestFileService.Save(path, messages);
        var result = ChatNestFileService.Load(path);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void Load_PreservesId()
    {
        var id = Guid.NewGuid();
        var path = TempPath("id.chatnest");
        ChatNestFileService.Save(path, [new Message { Id = id, Speaker = Speaker.自分, Text = "test" }]);
        var result = ChatNestFileService.Load(path);
        Assert.Equal(id, result[0].Id);
    }

    [Fact]
    public void Load_PreservesSpeaker()
    {
        var path = TempPath("speaker.chatnest");
        ChatNestFileService.Save(path, [
            new Message { Speaker = Speaker.自分,  Text = "a" },
            new Message { Speaker = Speaker.反論,  Text = "b" },
            new Message { Speaker = Speaker.補足,  Text = "c" },
            new Message { Speaker = Speaker.結論,  Text = "d" },
        ]);
        var result = ChatNestFileService.Load(path);
        Assert.Equal(Speaker.自分,  result[0].Speaker);
        Assert.Equal(Speaker.反論,  result[1].Speaker);
        Assert.Equal(Speaker.補足,  result[2].Speaker);
        Assert.Equal(Speaker.結論,  result[3].Speaker);
    }

    [Fact]
    public void Load_PreservesText()
    {
        var path = TempPath("text.chatnest");
        ChatNestFileService.Save(path, [new Message { Speaker = Speaker.自分, Text = "こんにちは世界" }]);
        var result = ChatNestFileService.Load(path);
        Assert.Equal("こんにちは世界", result[0].Text);
    }

    [Fact]
    public void Load_PreservesCreatedAt()
    {
        var at = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.FromHours(9));
        var path = TempPath("createdat.chatnest");
        ChatNestFileService.Save(path, [new Message { Speaker = Speaker.自分, Text = "t", CreatedAt = at }]);
        var result = ChatNestFileService.Load(path);
        Assert.Equal(at, result[0].CreatedAt);
    }

    // ── v0.4.1 互換: "要約" → "結論" マッピング ─────────────────────────

    [Fact]
    public void Load_MapsYoyaku_ToKetsuron()
    {
        // "要約" は旧 ChatNest v0.4.1 以前の発言者。読込時に "結論" へ変換する。
        var path = TempPath("compat.chatnest");
        var json = """
            {
              "version": "0.4.1",
              "messages": [
                { "id": "00000000-0000-0000-0000-000000000001", "speaker": "要約", "text": "まとめ", "createdAt": "2025-01-01T00:00:00+00:00" }
              ]
            }
            """;
        File.WriteAllText(path, json, System.Text.Encoding.UTF8);
        var result = ChatNestFileService.Load(path);
        Assert.Single(result);
        Assert.Equal(Speaker.結論, result[0].Speaker);
    }

    // ── 未知の発言者はスキップ ───────────────────────────────────────────

    [Fact]
    public void Load_SkipsUnknownSpeaker()
    {
        var path = TempPath("unknown.chatnest");
        var json = """
            {
              "version": "0.4.1",
              "messages": [
                { "id": "00000000-0000-0000-0000-000000000001", "speaker": "UNKNOWN_FUTURE", "text": "未来", "createdAt": "2025-01-01T00:00:00+00:00" },
                { "id": "00000000-0000-0000-0000-000000000002", "speaker": "自分",           "text": "既知", "createdAt": "2025-01-01T00:00:00+00:00" }
              ]
            }
            """;
        File.WriteAllText(path, json, System.Text.Encoding.UTF8);
        var result = ChatNestFileService.Load(path);
        Assert.Single(result);
        Assert.Equal("既知", result[0].Text);
    }

    // ── エラー系 ─────────────────────────────────────────────────────────

    [Fact]
    public void Load_ThrowsInvalidDataException_WhenJsonIsEmpty()
    {
        var path = TempPath("invalid.chatnest");
        File.WriteAllText(path, "null");
        Assert.Throws<InvalidDataException>(() => ChatNestFileService.Load(path));
    }

    [Fact]
    public void Load_ThrowsException_WhenFileNotFound()
    {
        var path = TempPath("notexist.chatnest");
        Assert.Throws<FileNotFoundException>(() => ChatNestFileService.Load(path));
    }
}
