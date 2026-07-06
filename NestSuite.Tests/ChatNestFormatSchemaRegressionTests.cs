using System.Text.Json;
using NestSuite.ChatNest;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// ChatNest (.chatnest) 保存形式・スキーマの非変更を自動テストで固定する。
/// </summary>
public class ChatNestFormatSchemaRegressionTests : IDisposable
{
    private readonly string _tempDir;

    public ChatNestFormatSchemaRegressionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ChatNestFormatSchemaRegressionTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── バージョン定数 ────────────────────────────────────────────────────

    [Fact]
    public void ChatNest_FileVersionConstant_Is_0_4_1()
    {
        Assert.Equal("0.4.1", ChatNestFileService.FileVersionString);
    }

    [Fact]
    public void ChatNest_FileExtensionConstant_Is_chatnest()
    {
        Assert.Equal(".chatnest", ChatNestFileService.FileExtension);
    }

    // ── JSON 構造 ─────────────────────────────────────────────────────────

    [Fact]
    public void ChatNest_SerializedJson_ContainsVersionField()
    {
        var path = Path.Combine(_tempDir, "test.chatnest");
        ChatNestFileService.Save(path, []);
        var json = File.ReadAllText(path);
        Assert.Contains("\"version\"", json);
        Assert.Contains("0.4.1", json);
    }

    [Fact]
    public void ChatNest_SerializedJson_ContainsMessagesField()
    {
        var path = Path.Combine(_tempDir, "test.chatnest");
        ChatNestFileService.Save(path, []);
        var json = File.ReadAllText(path);
        Assert.Contains("\"messages\"", json);
    }

    [Fact]
    public void ChatNest_SavedJson_IsValidJson()
    {
        var path = Path.Combine(_tempDir, "test.chatnest");
        ChatNestFileService.Save(path, []);
        var json = File.ReadAllText(path);
        Assert.True(IsValidJson(json), "保存された .chatnest ファイルは有効な JSON である必要がある");
    }

    [Fact]
    public void ChatNest_SaveLoad_PreservesMessages()
    {
        var path = Path.Combine(_tempDir, "msgs.chatnest");
        var messages = new[]
        {
            new Message { Speaker = Speaker.自分, Text = "テストメッセージ" },
            new Message { Speaker = Speaker.反論, Text = "反論テスト" },
        };
        ChatNestFileService.Save(path, messages);
        var loaded = ChatNestFileService.Load(path);
        Assert.Equal(2, loaded.Count);
        Assert.Equal("テストメッセージ", loaded[0].Text);
        Assert.Equal("反論テスト",       loaded[1].Text);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private static bool IsValidJson(string json)
    {
        try { JsonDocument.Parse(json); return true; }
        catch { return false; }
    }
}
