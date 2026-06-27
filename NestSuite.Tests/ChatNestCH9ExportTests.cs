using System.IO;
using System.Linq;
using NestSuite.ChatNest;
using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.10.7 CH-9: ChatNest 会話エクスポート（テキスト / Markdown）の単体テスト + 回帰テスト。
/// </summary>
public class ChatNestCH9ExportTests
{
    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    // ── バージョン ────────────────────────────────────────────────────────

    [Fact]
    public void ApplicationVersion_Is_2_10_7()
    {
        Assert.Equal("2.10.8", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void NoteNestSchemaVersion_Remains_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
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

    // ── CH-9: ExportConversationCommand ──────────────────────────────────

    [Fact]
    public void ExportConversationCommand_CanExecuteIsFalseWhenEmpty()
    {
        var vm = new ChatNestWorkspaceViewModel();
        Assert.False(vm.ExportConversationCommand.CanExecute(null));
    }

    [Fact]
    public void ExportConversationCommand_CanExecuteIsTrueWhenHasMessages()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages(new[] { new Message { Speaker = Speaker.自分, Text = "テスト" } });
        Assert.True(vm.ExportConversationCommand.CanExecute(null));
    }

    // ── backlog / release-notes ───────────────────────────────────────────

    [Fact]
    public void Backlog_CH9_IsMarkedComplete()
    {
        Assert.Contains("~~CH-9~~", ReadBacklog());
    }

    [Fact]
    public void ReleaseNotes_Contains_V2107()
    {
        var path = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(path));
        Assert.Contains("v2.10.7", File.ReadAllText(path));
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string ReadBacklog()
    {
        var path = Path.Combine(RepoRoot, "docs", "backlog.md");
        Assert.True(File.Exists(path), $"backlog.md not found: {path}");
        return File.ReadAllText(path);
    }
}
