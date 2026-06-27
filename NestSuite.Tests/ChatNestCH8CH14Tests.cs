using System.IO;
using System.Linq;
using NestSuite.ChatNest;
using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.10.6 CH-8 / CH-14: ChatNest タイムスタンプ表示切替・会話整形コピーの単体テスト + 回帰テスト。
/// </summary>
public class ChatNestCH8CH14Tests
{
    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    // ── バージョン ────────────────────────────────────────────────────────

    [Fact]
    public void ApplicationVersion_Is_2_10_6()
    {
        Assert.Equal("2.10.10", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void NoteNestSchemaVersion_Remains_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }

    // ── CH-8: ShowTimestamps ──────────────────────────────────────────────

    [Fact]
    public void ShowTimestamps_DefaultIsTrue()
    {
        var vm = new ChatNestWorkspaceViewModel();
        Assert.True(vm.ShowTimestamps);
    }

    [Fact]
    public void ShowTimestamps_CanBeSetToFalse()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.ShowTimestamps = false;
        Assert.False(vm.ShowTimestamps);
    }

    [Fact]
    public void ShowTimestamps_CanBeToggledBackToTrue()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.ShowTimestamps = false;
        vm.ShowTimestamps = true;
        Assert.True(vm.ShowTimestamps);
    }

    [Fact]
    public void ShowTimestamps_ToggleDoesNotChangeMessageModel()
    {
        var vm = new ChatNestWorkspaceViewModel();
        var messages = new[]
        {
            new Message { Speaker = Speaker.自分, Text = "テスト発言" },
        };
        vm.LoadMessages(messages);
        vm.ShowTimestamps = false;
        var models = vm.MessageModels.ToList();
        Assert.Single(models);
        Assert.Equal(Speaker.自分, models[0].Speaker);
        Assert.Equal("テスト発言", models[0].Text);
    }

    [Fact]
    public void ShowTimestamps_ChatNestSaveModelUnchanged()
    {
        var vm = new ChatNestWorkspaceViewModel();
        var msg = new Message { Speaker = Speaker.反論, Text = "保存形式変更なし" };
        vm.LoadMessages(new[] { msg });

        vm.ShowTimestamps = false;

        var saved = vm.MessageModels.First();
        Assert.Equal(msg.Id, saved.Id);
        Assert.Equal(msg.Speaker, saved.Speaker);
        Assert.Equal(msg.Text, saved.Text);
        Assert.Equal(msg.CreatedAt, saved.CreatedAt);
    }

    // ── CH-14: ChatNestExportFormatter ───────────────────────────────────

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

    // ── backlog / release-notes ───────────────────────────────────────────

    [Fact]
    public void Backlog_CH8_IsMarkedComplete()
    {
        Assert.Contains("~~CH-8~~", ReadBacklog());
    }

    [Fact]
    public void Backlog_CH14_IsMarkedComplete()
    {
        Assert.Contains("~~CH-14~~", ReadBacklog());
    }

    [Fact]
    public void ReleaseNotes_Contains_V2106()
    {
        var path = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(path));
        Assert.Contains("v2.10.6", File.ReadAllText(path));
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string ReadBacklog()
    {
        var path = Path.Combine(RepoRoot, "docs", "backlog.md");
        Assert.True(File.Exists(path), $"backlog.md not found: {path}");
        return File.ReadAllText(path);
    }
}
