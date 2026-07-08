using NestSuite.ChatNest;
using NestSuite.Models;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.13 TD-63: ChatNestWorkspaceViewModelTests から、NestSuite/Markdown テキスト組み立てと
/// コピーコマンドの CanExecute（v1.16.5: コピー機能）に関するテストを分割した。
/// </summary>
public class ChatNestExportAndCopyTests
{
    [Fact]
    public void BuildNestSuiteText_WithMessages_FormatsCorrectly()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages([
            new Message { Speaker = Speaker.自分,  Text = "考えA" },
            new Message { Speaker = Speaker.反論, Text = "考えB" },
        ]);

        var text = vm.BuildNestSuiteText();

        Assert.Contains("[NOTE] ChatNestからの転記:", text);
        Assert.Contains("## 自分", text);
        Assert.Contains("考えA", text);
        Assert.Contains("## 反論", text);
        Assert.Contains("考えB", text);
    }

    [Fact]
    public void BuildNestSuiteText_ConsecutiveSameSpeaker_GroupsIntoOneBlock()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages([
            new Message { Speaker = Speaker.自分, Text = "一言目" },
            new Message { Speaker = Speaker.自分, Text = "二言目" },
            new Message { Speaker = Speaker.反論, Text = "反論" },
        ]);

        var text = vm.BuildNestSuiteText();

        // ## 自分 は集約されて 1 回のみ現れる
        Assert.Equal(1, text.Split("## 自分").Length - 1);
        // 両メッセージがブロック内に存在する
        Assert.Contains("一言目", text);
        Assert.Contains("二言目", text);
        // 別発言者は独立したブロックになる
        Assert.Contains("## 反論", text);
    }

    [Fact]
    public void BuildNestSuiteText_EmptyMessages_ReturnsEmptyString()
    {
        var vm = new ChatNestWorkspaceViewModel();

        Assert.Equal(string.Empty, vm.BuildNestSuiteText());
    }

    [Fact]
    public void BuildMarkdownText_ConsecutiveSameSpeaker_GroupsIntoOneBlock()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages([
            new Message { Speaker = Speaker.自分, Text = "A" },
            new Message { Speaker = Speaker.自分, Text = "B" },
            new Message { Speaker = Speaker.結論, Text = "まとめ" },
        ]);

        var text = vm.BuildMarkdownText();

        // ## 自分 は集約されて 1 回のみ現れる
        Assert.Equal(1, text.Split("## 自分").Length - 1);
        Assert.Contains("A", text);
        Assert.Contains("B", text);
        Assert.Contains("## 結論", text);
    }

    [Fact]
    public void BuildMarkdownText_WithMessages_StartsWithH1AndContainsSpeakerH2()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages([
            new Message { Speaker = Speaker.自分,  Text = "主張" },
            new Message { Speaker = Speaker.結論, Text = "まとめ" },
        ]);

        var text = vm.BuildMarkdownText();

        Assert.StartsWith("# ChatNest Export", text);
        Assert.Contains("## 自分", text);
        Assert.Contains("主張", text);
        Assert.Contains("## 結論", text);
        Assert.Contains("まとめ", text);
    }

    [Fact]
    public void BuildMarkdownText_EmptyMessages_ReturnsEmptyString()
    {
        var vm = new ChatNestWorkspaceViewModel();

        Assert.Equal(string.Empty, vm.BuildMarkdownText());
    }

    [Fact]
    public void CopyNestSuiteCommand_CanExecute_FalseWhenEmpty()
    {
        var vm = new ChatNestWorkspaceViewModel();

        Assert.False(vm.CopyNestSuiteCommand.CanExecute(null));
    }

    [Fact]
    public void CopyMarkdownCommand_CanExecute_FalseWhenEmpty()
    {
        var vm = new ChatNestWorkspaceViewModel();

        Assert.False(vm.CopyMarkdownCommand.CanExecute(null));
    }

    [Fact]
    public void CopyNestSuiteCommand_CanExecute_TrueAfterMessageLoaded()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages([new Message { Speaker = Speaker.自分, Text = "test" }]);

        Assert.True(vm.CopyNestSuiteCommand.CanExecute(null));
    }

    [Fact]
    public void CopyMarkdownCommand_CanExecute_TrueAfterMessageLoaded()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages([new Message { Speaker = Speaker.自分, Text = "test" }]);

        Assert.True(vm.CopyMarkdownCommand.CanExecute(null));
    }
}
