using System.Collections.Generic;
using NoteNest.NestSuite.ChatNest;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.7.0: ChatNest 統合検証用 Workspace ViewModel の最小動作確認。
/// 投稿・発言者切替・クリア・読込など、UI（MessageBox）に依存しない経路のみを検証する。
/// 発言削除は MessageBox を伴うため本テストの対象外（NestSuite 配下の暫定許容）。
/// </summary>
public class ChatNestWorkspaceViewModelTests
{
    [Fact]
    public void Post_WithText_AddsMessageWithSelectedSpeaker()
    {
        var vm = new ChatNestWorkspaceViewModel
        {
            SelectedSpeaker = Speaker.反論,
            InputText = "それは違うと思う",
        };

        vm.PostCommand.Execute(null);

        var message = Assert.Single(vm.Messages);
        Assert.Equal(Speaker.反論, message.Speaker);
        Assert.Equal("それは違うと思う", message.Text);
        Assert.Equal(string.Empty, vm.InputText);
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void Post_WithWhitespaceOnly_DoesNotAddMessage()
    {
        var vm = new ChatNestWorkspaceViewModel { InputText = "   " };

        Assert.False(vm.PostCommand.CanExecute(null));
        vm.PostCommand.Execute(null);

        Assert.Empty(vm.Messages);
    }

    [Fact]
    public void Post_TrimsSurroundingWhitespace()
    {
        var vm = new ChatNestWorkspaceViewModel { InputText = "  まとめ  " };

        vm.PostCommand.Execute(null);

        Assert.Equal("まとめ", Assert.Single(vm.Messages).Text);
    }

    [Fact]
    public void CycleSpeaker_Forward_AdvancesThroughAllSpeakersAndWraps()
    {
        var vm = new ChatNestWorkspaceViewModel { SelectedSpeaker = Speaker.自分 };

        vm.CycleSpeaker(forward: true);
        Assert.Equal(Speaker.反論, vm.SelectedSpeaker);

        vm.CycleSpeaker(forward: true);
        Assert.Equal(Speaker.補足, vm.SelectedSpeaker);

        vm.CycleSpeaker(forward: true);
        Assert.Equal(Speaker.結論, vm.SelectedSpeaker);

        // 末尾から先頭へ循環する
        vm.CycleSpeaker(forward: true);
        Assert.Equal(Speaker.自分, vm.SelectedSpeaker);
    }

    [Fact]
    public void CycleSpeaker_Backward_WrapsToLast()
    {
        var vm = new ChatNestWorkspaceViewModel { SelectedSpeaker = Speaker.自分 };

        vm.CycleSpeaker(forward: false);

        Assert.Equal(Speaker.結論, vm.SelectedSpeaker);
    }

    [Fact]
    public void WorkspaceModified_RaisedOnPost()
    {
        var vm = new ChatNestWorkspaceViewModel { InputText = "発火確認" };
        bool raised = false;
        vm.WorkspaceModified += (_, _) => raised = true;

        vm.PostCommand.Execute(null);

        Assert.True(raised);
    }

    [Fact]
    public void Clear_RemovesMessagesAndResetsState()
    {
        var vm = new ChatNestWorkspaceViewModel { InputText = "残す？" };
        vm.PostCommand.Execute(null);

        vm.Clear();

        Assert.Empty(vm.Messages);
        Assert.Equal(string.Empty, vm.InputText);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void HasUnsavedChanges_FreshViewModel_IsFalse()
    {
        var vm = new ChatNestWorkspaceViewModel();

        Assert.False(vm.HasUnsavedChanges);
    }

    [Fact]
    public void HasUnsavedChanges_AfterPost_IsTrue()
    {
        var vm = new ChatNestWorkspaceViewModel { InputText = "投稿する" };

        vm.PostCommand.Execute(null);

        // 投稿後は入力欄が空でも IsDirty により未保存扱いとなる
        Assert.Equal(string.Empty, vm.InputText);
        Assert.True(vm.HasUnsavedChanges);
    }

    [Fact]
    public void HasUnsavedChanges_WithUnpostedInputOnly_IsTrue()
    {
        var vm = new ChatNestWorkspaceViewModel { InputText = "下書き中" };

        // 投稿前でも入力欄に内容があれば未保存扱いとする
        Assert.False(vm.IsDirty);
        Assert.True(vm.HasUnsavedChanges);
    }

    [Fact]
    public void HasUnsavedChanges_WithWhitespaceInputOnly_IsFalse()
    {
        var vm = new ChatNestWorkspaceViewModel { InputText = "   " };

        Assert.False(vm.HasUnsavedChanges);
    }

    [Fact]
    public void HasUnsavedChanges_RaisesPropertyChanged_WhenInputTextChanges()
    {
        var vm = new ChatNestWorkspaceViewModel();
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.InputText = "入力";

        Assert.Contains(nameof(ChatNestWorkspaceViewModel.HasUnsavedChanges), changed);
    }

    [Fact]
    public void Clear_ResetsHasUnsavedChanges()
    {
        var vm = new ChatNestWorkspaceViewModel { InputText = "残す？" };
        vm.PostCommand.Execute(null);
        Assert.True(vm.HasUnsavedChanges);

        vm.Clear();

        Assert.False(vm.HasUnsavedChanges);
    }

    [Fact]
    public void LoadMessages_ReplacesContentsAndMarksClean()
    {
        var vm = new ChatNestWorkspaceViewModel();
        var loaded = new[]
        {
            new Message { Speaker = Speaker.自分, Text = "A" },
            new Message { Speaker = Speaker.結論, Text = "B" },
        };

        vm.LoadMessages(loaded);

        Assert.Equal(2, vm.Messages.Count);
        Assert.Equal("A", vm.Messages[0].Text);
        Assert.Equal("B", vm.Messages[1].Text);
        Assert.False(vm.IsDirty);
    }

    // ── v1.7.5: 案A — 入力中テキストの保存後の扱い ─────────────────────

    [Fact]
    public void MarkSaved_WhenInputTextRemains_HasUnsavedChangesIsTrue()
    {
        // 案A: MarkSaved() は IsDirty のみ解消する。InputText が残っていれば HasUnsavedChanges = true のまま。
        // → タブの IsModified・未保存マーカーが保存後も維持される。
        var vm = new ChatNestWorkspaceViewModel { InputText = "投稿済み" };
        vm.PostCommand.Execute(null); // IsDirty = true, InputText = ""
        vm.InputText = "入力中テキスト";

        vm.MarkSaved();

        Assert.False(vm.IsDirty);
        Assert.True(vm.HasUnsavedChanges);
    }

    [Fact]
    public void MarkSaved_WhenInputTextEmpty_HasUnsavedChangesIsFalse()
    {
        // 投稿後に入力欄が空の状態で MarkSaved() を呼んだ場合は未保存状態が完全に解消される。
        var vm = new ChatNestWorkspaceViewModel { InputText = "投稿する" };
        vm.PostCommand.Execute(null); // IsDirty = true, InputText = ""

        vm.MarkSaved();

        Assert.False(vm.IsDirty);
        Assert.False(vm.HasUnsavedChanges);
    }

    [Fact]
    public void LoadMessages_SetsHasUnsavedChangesFalse()
    {
        // LoadMessages() は InputText もクリアするため、読込直後は HasUnsavedChanges = false。
        var vm = new ChatNestWorkspaceViewModel { InputText = "入力中" };
        vm.PostCommand.Execute(null); // IsDirty = true

        vm.LoadMessages([new Message { Speaker = Speaker.自分, Text = "ロード" }]);

        Assert.False(vm.IsDirty);
        Assert.Equal(string.Empty, vm.InputText);
        Assert.False(vm.HasUnsavedChanges);
    }

    [Fact]
    public void LoadMessages_ThenPost_HasUnsavedChangesIsTrue()
    {
        // 読込後に追加投稿した場合は未保存状態に変わる。
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages([new Message { Speaker = Speaker.補足, Text = "既存" }]);
        Assert.False(vm.HasUnsavedChanges); // 読込直後はクリーン

        vm.InputText = "新規追加";
        vm.PostCommand.Execute(null);

        Assert.True(vm.IsDirty);
        Assert.True(vm.HasUnsavedChanges);
    }

    // ── v1.16.5: コピー機能 ───────────────────────────────────────────────

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
