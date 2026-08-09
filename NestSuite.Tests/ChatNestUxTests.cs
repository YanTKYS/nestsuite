using NestSuite.ChatNest;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.7.11 CH-5・CH-7・CH-10 の ViewModel 動作検証。
/// CH-7（発言者チップ）は純粋 XAML スタイルのため ViewModel テスト対象外。
/// </summary>
public class ChatNestUxTests
{
    // ── CH-10: 発言単体コピー ────────────────────────────────────────────────

    [Fact]
    public void CopyMessageCommand_InvokesCallback_WithMessageViewModel()
    {
        MessageViewModel? captured = null;
        var msg = new Message { Speaker = Speaker.自分, Text = "コピーテスト" };
        var msgVm = new MessageViewModel(
            msg,
            _ => { },
            _ => { },
            _ => { },
            m => captured = m,
            _ => { });

        msgVm.CopyMessageCommand.Execute(null);

        Assert.Same(msgVm, captured);
    }

    [Fact]
    public void CopyMessageCommand_DoesNotIncludeSpeakerOrTimestamp()
    {
        string? copiedText = null;
        var msg = new Message { Speaker = Speaker.補足, Text = "本文だけコピー" };
        var msgVm = new MessageViewModel(
            msg,
            _ => { },
            _ => { },
            _ => { },
            m => copiedText = m.Text,
            _ => { });

        msgVm.CopyMessageCommand.Execute(null);

        Assert.Equal("本文だけコピー", copiedText);
    }

    // ── CH-5: 会話内検索 ─────────────────────────────────────────────────────

    [Fact]
    public void SearchText_WhenSet_FiltersMessages()
    {
        var vm = CreateVmWithMessages("りんごが好き", "バナナが好き", "りんごジュース");

        vm.SearchText = "りんご";

        Assert.Equal("1 / 2件", vm.SearchResultSummary);
    }

    [Fact]
    public void SearchText_NoMatch_Returns0件()
    {
        var vm = CreateVmWithMessages("りんご", "バナナ");

        vm.SearchText = "メロン";

        Assert.Equal("0件", vm.SearchResultSummary);
    }

    [Fact]
    public void SearchText_Empty_ReturnsEmptySummary()
    {
        var vm = CreateVmWithMessages("りんご", "バナナ");
        vm.SearchText = "りんご";

        vm.SearchText = string.Empty;

        Assert.Equal(string.Empty, vm.SearchResultSummary);
    }

    [Fact]
    public void SearchText_IsCaseInsensitive()
    {
        var vm = CreateVmWithMessages("Hello World", "hello world", "HELLO");

        vm.SearchText = "hello";

        Assert.Equal("1 / 3件", vm.SearchResultSummary);
    }

    [Fact]
    public void SearchText_MatchesSpeaker()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.SelectedSpeaker = Speaker.自分;
        vm.InputText = "テスト発言";
        vm.PostCommand.Execute(null);
        vm.SelectedSpeaker = Speaker.反論;
        vm.InputText = "別の発言";
        vm.PostCommand.Execute(null);

        vm.SearchText = "反論";

        Assert.Equal("1 / 1件", vm.SearchResultSummary);
    }

    [Fact]
    public void SearchNextCommand_AdvancesToNextResult()
    {
        var vm = CreateVmWithMessages("りんご1", "バナナ", "りんご2");
        vm.SearchText = "りんご";

        Assert.Equal("1 / 2件", vm.SearchResultSummary);

        vm.SearchNextCommand.Execute(null);

        Assert.Equal("2 / 2件", vm.SearchResultSummary);
    }

    [Fact]
    public void SearchNextCommand_WrapsAroundToFirst()
    {
        var vm = CreateVmWithMessages("りんご1", "りんご2");
        vm.SearchText = "りんご";
        vm.SearchNextCommand.Execute(null); // → 2/2

        vm.SearchNextCommand.Execute(null); // → 1/2 (wrap)

        Assert.Equal("1 / 2件", vm.SearchResultSummary);
    }

    [Fact]
    public void SearchPreviousCommand_WrapsAroundToLast()
    {
        var vm = CreateVmWithMessages("りんご1", "りんご2");
        vm.SearchText = "りんご";

        vm.SearchPreviousCommand.Execute(null); // → 2/2 (wrap)

        Assert.Equal("2 / 2件", vm.SearchResultSummary);
    }

    [Fact]
    public void IsSearchCurrent_SetOnFirstMatch_OnSearch()
    {
        var vm = CreateVmWithMessages("りんご", "バナナ", "りんご2");
        vm.SearchText = "りんご";

        Assert.True(vm.Messages[0].IsSearchCurrent);
        Assert.False(vm.Messages[1].IsSearchCurrent);
        Assert.False(vm.Messages[2].IsSearchCurrent);
    }

    [Fact]
    public void IsSearchCurrent_MovesOnNavigate()
    {
        var vm = CreateVmWithMessages("りんご", "バナナ", "りんご2");
        vm.SearchText = "りんご";

        vm.SearchNextCommand.Execute(null);

        Assert.False(vm.Messages[0].IsSearchCurrent);
        Assert.True(vm.Messages[2].IsSearchCurrent);
    }

    [Fact]
    public void SearchState_ResetOnClear()
    {
        var vm = CreateVmWithMessages("りんご");
        vm.SearchText = "りんご";
        Assert.Equal("1 / 1件", vm.SearchResultSummary);

        vm.Clear();

        Assert.Equal(string.Empty, vm.SearchResultSummary);
        Assert.Equal(string.Empty, vm.SearchText);
    }

    [Fact]
    public void IsSearchBarVisible_DefaultsFalse()
    {
        var vm = new ChatNestWorkspaceViewModel();

        Assert.False(vm.IsSearchBarVisible);
    }

    [Fact]
    public void OpenSearchCommand_SetsIsSearchBarVisibleTrue()
    {
        var vm = new ChatNestWorkspaceViewModel();

        vm.OpenSearchCommand.Execute(null);

        Assert.True(vm.IsSearchBarVisible);
    }

    [Fact]
    public void CloseSearchCommand_ClearsSearchAndHidesBar()
    {
        var vm = CreateVmWithMessages("りんご");
        vm.OpenSearchCommand.Execute(null);
        vm.SearchText = "りんご";

        vm.CloseSearchCommand.Execute(null);

        Assert.False(vm.IsSearchBarVisible);
        Assert.Equal(string.Empty, vm.SearchText);
        Assert.Equal(string.Empty, vm.SearchResultSummary);
    }

    // ── CH-5 回帰: 編集確定後の検索再計算 ───────────────────────────────────

    [Fact]
    public void Search_AfterEdit_MatchingMessageRemoved_SummaryUpdates()
    {
        var vm = CreateVmWithMessages("りんご", "バナナ");
        vm.SearchText = "りんご";
        Assert.Equal("1 / 1件", vm.SearchResultSummary);

        // 「りんご」→「メロン」に編集確定
        var msgVm = vm.Messages[0];
        msgVm.BeginEditCommand.Execute(null);
        msgVm.EditingText = "メロン";
        msgVm.CommitEditCommand.Execute(null);

        Assert.Equal("0件", vm.SearchResultSummary);
        Assert.False(msgVm.IsSearchCurrent);
    }

    [Fact]
    public void Search_AfterEdit_NonMatchingMessageBecomesMatch_SummaryUpdates()
    {
        var vm = CreateVmWithMessages("バナナ", "メロン");
        vm.SearchText = "りんご";
        Assert.Equal("0件", vm.SearchResultSummary);

        // 「バナナ」→「りんご」に編集確定
        var msgVm = vm.Messages[0];
        msgVm.BeginEditCommand.Execute(null);
        msgVm.EditingText = "りんご";
        msgVm.CommitEditCommand.Execute(null);

        Assert.Equal("1 / 1件", vm.SearchResultSummary);
        Assert.True(msgVm.IsSearchCurrent);
    }

    // ── ヘルパー ─────────────────────────────────────────────────────────────

    private static ChatNestWorkspaceViewModel CreateVmWithMessages(params string[] texts)
    {
        var vm = new ChatNestWorkspaceViewModel();
        foreach (var t in texts)
        {
            vm.SelectedSpeaker = Speaker.自分;
            vm.InputText = t;
            vm.PostCommand.Execute(null);
        }
        return vm;
    }
}
