using System.Linq;
using NestSuite.ChatNest;
using NestSuite.Models;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.13 TD-63: ChatNestWorkspaceViewModelTests から、タイムスタンプ表示切替（CH-8）と
/// ExportConversationCommand（CH-9）に関するテストを分割した。
/// </summary>
public class ChatNestTimestampAndExportCommandTests
{
    // ── CH-8: ShowTimestamps ──────────────────────────────────────────────

    // CH-15: 既定値を false に変更
    [Fact]
    public void ShowTimestamps_DefaultIsFalse()
    {
        var vm = new ChatNestWorkspaceViewModel();
        Assert.False(vm.ShowTimestamps);
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
}
