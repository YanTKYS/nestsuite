using System.IO;
using NestSuite.ChatNest;
using NestSuite.Models;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

public class ChatNestExportFormatterTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    // ── CH-14: BuildPlainTextConversation ────────────────────────────────

    [Fact]
    public void BuildPlainTextConversation_EmptyConversation_ReturnsEmpty()
    {
        var result = ChatNestExportFormatter.BuildPlainTextConversation(System.Array.Empty<Message>());
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void BuildPlainTextConversation_SingleMessage_Format()
    {
        var messages = new[] { new Message { Speaker = Speaker.自分, Text = "テスト" } };
        var result = ChatNestExportFormatter.BuildPlainTextConversation(messages);
        Assert.Equal("自分: テスト", result);
    }

    [Fact]
    public void BuildPlainTextConversation_MultipleMessages_SeparatedByBlankLine()
    {
        var messages = new[]
        {
            new Message { Speaker = Speaker.自分,  Text = "まず要件を整理します。" },
            new Message { Speaker = Speaker.反論,  Text = "保存形式は変更しない方針でよいです。" },
        };
        var result = ChatNestExportFormatter.BuildPlainTextConversation(messages);
        Assert.Contains("自分: まず要件を整理します。", result);
        Assert.Contains("反論: 保存形式は変更しない方針でよいです。", result);
        var idx1 = result.IndexOf("自分:", System.StringComparison.Ordinal);
        var idx2 = result.IndexOf("反論:", System.StringComparison.Ordinal);
        Assert.True(idx1 < idx2);
        Assert.Contains("\n\n", result);
    }

    [Fact]
    public void BuildPlainTextConversation_MessagesInOrder()
    {
        var messages = new[]
        {
            new Message { Speaker = Speaker.自分,  Text = "A" },
            new Message { Speaker = Speaker.補足,  Text = "B" },
            new Message { Speaker = Speaker.結論,  Text = "C" },
        };
        var result = ChatNestExportFormatter.BuildPlainTextConversation(messages);
        var idxA = result.IndexOf("自分: A",  System.StringComparison.Ordinal);
        var idxB = result.IndexOf("補足: B",  System.StringComparison.Ordinal);
        var idxC = result.IndexOf("結論: C",  System.StringComparison.Ordinal);
        Assert.True(idxA < idxB && idxB < idxC);
    }

    [Fact]
    public void BuildPlainTextConversation_EmptyTextMessage_DoesNotCrash()
    {
        var messages = new[]
        {
            new Message { Speaker = Speaker.自分,  Text = "" },
            new Message { Speaker = Speaker.補足,  Text = "補足テキスト" },
        };
        var result = ChatNestExportFormatter.BuildPlainTextConversation(messages);
        Assert.Contains("自分: ", result);
        Assert.Contains("補足: 補足テキスト", result);
    }

    [Fact]
    public void BuildPlainTextConversationWithTimestamp_SingleMessage_ContainsTimestamp()
    {
        var msg = new Message { Speaker = Speaker.自分, Text = "タイムスタンプ付き" };
        var result = ChatNestExportFormatter.BuildPlainTextConversationWithTimestamp(new[] { msg });
        Assert.Contains("[", result);
        Assert.Contains("] 自分: タイムスタンプ付き", result);
    }

    // ── CH-9: BuildMarkdownConversation ──────────────────────────────────

    [Fact]
    public void BuildMarkdownConversation_EmptyConversation_ReturnsEmpty()
    {
        var result = ChatNestExportFormatter.BuildMarkdownConversation(System.Array.Empty<Message>());
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void BuildMarkdownConversation_StartsWithH1()
    {
        var messages = new[] { new Message { Speaker = Speaker.自分, Text = "テスト" } };
        var result = ChatNestExportFormatter.BuildMarkdownConversation(messages);
        Assert.StartsWith("# ChatNest 会話", result);
    }

    [Fact]
    public void BuildMarkdownConversation_ContainsFormattedSpeaker()
    {
        var messages = new[] { new Message { Speaker = Speaker.自分, Text = "テスト" } };
        var result = ChatNestExportFormatter.BuildMarkdownConversation(messages);
        Assert.Contains("**自分**", result);
    }

    [Fact]
    public void BuildMarkdownConversation_SingleMessage_Format()
    {
        var messages = new[] { new Message { Speaker = Speaker.自分, Text = "テスト" } };
        var result = ChatNestExportFormatter.BuildMarkdownConversation(messages);
        Assert.Contains("**自分**: テスト", result);
    }

    [Fact]
    public void BuildMarkdownConversation_MultipleMessages_SeparatedByBlankLine()
    {
        var messages = new[]
        {
            new Message { Speaker = Speaker.自分, Text = "まず要件を整理します。" },
            new Message { Speaker = Speaker.反論, Text = "保存形式は変更しない方針でよいです。" },
        };
        var result = ChatNestExportFormatter.BuildMarkdownConversation(messages);
        Assert.Contains("**自分**: まず要件を整理します。", result);
        Assert.Contains("**反論**: 保存形式は変更しない方針でよいです。", result);
        Assert.Contains("\n\n", result);
    }

    [Fact]
    public void BuildMarkdownConversation_MessagesInOrder()
    {
        var messages = new[]
        {
            new Message { Speaker = Speaker.自分, Text = "A" },
            new Message { Speaker = Speaker.補足, Text = "B" },
            new Message { Speaker = Speaker.結論, Text = "C" },
        };
        var result = ChatNestExportFormatter.BuildMarkdownConversation(messages);
        var idxA = result.IndexOf("**自分**: A",  System.StringComparison.Ordinal);
        var idxB = result.IndexOf("**補足**: B",  System.StringComparison.Ordinal);
        var idxC = result.IndexOf("**結論**: C",  System.StringComparison.Ordinal);
        Assert.True(idxA < idxB && idxB < idxC);
    }

    [Fact]
    public void BuildMarkdownConversation_EmptyTextMessage_DoesNotCrash()
    {
        var messages = new[]
        {
            new Message { Speaker = Speaker.自分, Text = "" },
            new Message { Speaker = Speaker.補足, Text = "補足テキスト" },
        };
        var result = ChatNestExportFormatter.BuildMarkdownConversation(messages);
        Assert.Contains("**自分**: ", result);
        Assert.Contains("**補足**: 補足テキスト", result);
    }


    [Fact]
    public void BuildMarkdownGrouped_CombinesOnlyConsecutiveSameSpeakerMessages()
    {
        var messages = new[]
        {
            new Message { Speaker = Speaker.自分, Text = "A" },
            new Message { Speaker = Speaker.自分, Text = "B" },
            new Message { Speaker = Speaker.補足, Text = "C" },
            new Message { Speaker = Speaker.自分, Text = "D" },
        };

        var result = ChatNestExportFormatter.BuildMarkdownGrouped(messages);

        Assert.Equal("# ChatNest Export\n\n## 自分\nA\nB\n\n## 補足\nC\n\n## 自分\nD", result);
    }

    [Fact]
    public void BuildNestSuiteGrouped_PreservesBlankLineBetweenHeadingAndGroupedText()
    {
        var messages = new[]
        {
            new Message { Speaker = Speaker.自分, Text = "A" },
            new Message { Speaker = Speaker.自分, Text = "B" },
        };

        var result = ChatNestExportFormatter.BuildNestSuiteGrouped(messages);

        Assert.Contains("\n\n## 自分\n\nA\nB", result);
    }

    // ── backlog / release-notes ───────────────────────────────────────────

    // CH-14: 会話全体の整形コピー（プレーンテキスト）(TD-33: 完了済み項目は release-notes.md で管理)
    [Fact]
    public void Backlog_CH14_IsMarkedComplete()
    {
        Assert.Contains("CH-14", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    // CH-9: 会話エクスポート（Markdown）(TD-33: 完了済み項目は release-notes.md で管理)
    [Fact]
    public void Backlog_CH9_IsMarkedComplete()
    {
        Assert.Contains("CH-9", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2106()
    {
        var path = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(path));
        Assert.Contains("v2.10.6", File.ReadAllText(path));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2107()
    {
        var path = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(path));
        Assert.Contains("v2.10.7", File.ReadAllText(path));
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string ReadBacklog() => TestPaths.ReadBacklog();
}
