using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NoteNest.NestSuite.IdeaNest.Models;
using NoteNest.NestSuite.IdeaNest.Services;
using NoteNest.NestSuite.IdeaNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

public class IdeaNestWorkspaceViewModelTests
{
    [Fact]
    public void LoadAndBuildSaveWorkspace_RestoresCardsOrderTagsDatesAndClearsDirty()
    {
        var created = new DateTime(2026, 1, 2, 3, 4, 5);
        var updated = created.AddHours(1);
        var vm = new IdeaNestWorkspaceViewModel();
        vm.MarkDirty();

        vm.LoadFromWorkspace(new Workspace
        {
            WorkspaceName = "回帰確認",
            Ideas =
            [
                new Idea { Id = "first", Body = "本文", Tags = ["タグ"], CreatedAt = created, UpdatedAt = updated },
                new Idea { Id = "second", Title = "2番目" },
            ],
            Settings = new WorkspaceSettings { CardSize = "large", SearchText = "一時検索" },
        });

        Assert.False(vm.HasChanges);
        Assert.Equal(new[] { "first", "second" }, vm.AllCards.Select(card => card.Id));

        var saved = vm.BuildWorkspaceForSave();
        Assert.Equal(IdeaNestSchema.CurrentVersion, saved.Version);
        Assert.Equal(new[] { "first", "second" }, saved.Ideas.Select(idea => idea.Id));
        Assert.Equal("本文", saved.Ideas[0].Body);
        Assert.Equal("タグ", saved.Ideas[0].Tags.Single());
        Assert.Equal(created, saved.Ideas[0].CreatedAt);
        Assert.Equal(updated, saved.Ideas[0].UpdatedAt);
        Assert.Equal("large", saved.Settings.CardSize);
        Assert.Empty(saved.Settings.SearchText);
    }

    [Fact]
    public void MarkDirtyAndMarkSaved_UpdateHasChanges()
    {
        var vm = new IdeaNestWorkspaceViewModel();

        vm.MarkDirty();
        Assert.True(vm.HasChanges);

        vm.MarkSaved();
        Assert.False(vm.HasChanges);
    }
}

// ── v1.16.6: CardOperationsService — テキスト貼り付け・ファイル取り込みのカード作成確認 ───

public class CardOperationsServicePasteTests
{
    private static CardOperationsService MakeService(
        List<Idea> ideas,
        ObservableCollection<IdeaCardViewModel> cards,
        Func<DateTime>? now = null)
        => new(ideas, cards, () => { }, () => { }, () => { }, now);

    [Fact]
    public void CommitAddFromText_SetsPasteTitleFormat()
    {
        var fixedNow = new DateTime(2026, 6, 18, 14, 30, 0);
        var ideas = new List<Idea>();
        var cards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeService(ideas, cards, () => fixedNow);

        var result = svc.CommitAddFromText("テスト本文");

        Assert.True(result);
        Assert.Equal("Paste_202606181430", ideas[0].Title);
        Assert.Equal("テスト本文", ideas[0].Body);
    }

    [Fact]
    public void CommitAddFromText_EmptyBody_ReturnsFalse()
    {
        var ideas = new List<Idea>();
        var cards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeService(ideas, cards);

        Assert.False(svc.CommitAddFromText(string.Empty));
        Assert.False(svc.CommitAddFromText("   "));
        Assert.Empty(ideas);
    }

    [Fact]
    public void CommitAddFromText_WhitespaceOnlyBody_ReturnsFalse()
    {
        var ideas = new List<Idea>();
        var cards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeService(ideas, cards);

        Assert.False(svc.CommitAddFromText("\n\n  \t  "));
        Assert.Empty(ideas);
    }

    [Fact]
    public void CommitAddFromFileContent_UsesFileNameAsTitle()
    {
        var ideas = new List<Idea>();
        var cards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeService(ideas, cards);

        var result = svc.CommitAddFromFileContent("memo", "本文内容");

        Assert.True(result);
        Assert.Equal("memo", ideas[0].Title);
        Assert.Equal("本文内容", ideas[0].Body);
    }

    [Fact]
    public void CommitAddFromFileContent_EmptyBody_ReturnsFalse()
    {
        var ideas = new List<Idea>();
        var cards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeService(ideas, cards);

        Assert.False(svc.CommitAddFromFileContent("memo", string.Empty));
        Assert.False(svc.CommitAddFromFileContent("memo", "   "));
        Assert.Empty(ideas);
    }

    [Fact]
    public void CommitAddFromText_SetsTimestamps()
    {
        var fixedNow = new DateTime(2026, 6, 18, 9, 0, 0);
        var ideas = new List<Idea>();
        var cards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeService(ideas, cards, () => fixedNow);

        svc.CommitAddFromText("内容");

        Assert.Equal(fixedNow, ideas[0].CreatedAt);
        Assert.Equal(fixedNow, ideas[0].UpdatedAt);
    }

    // ── v1.16.8: ChatNest 転記形式の貼り付け ───────────────────────────────

    [Fact]
    public void CommitAddFromText_ChatNestHeader_ExtractsTitleFromHeader()
    {
        var ideas = new List<Idea>();
        var cards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeService(ideas, cards);
        var paste = "[NOTE] ChatNestからの転記: 2026-06-18 14:30\n\n## 自分\n\n本文内容";

        var result = svc.CommitAddFromText(paste);

        Assert.True(result);
        Assert.Equal("ChatNestからの転記: 2026-06-18 14:30", ideas[0].Title);
    }

    [Fact]
    public void CommitAddFromText_ChatNestHeader_StripsHeaderLineFromBody()
    {
        var ideas = new List<Idea>();
        var cards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeService(ideas, cards);
        var paste = "[NOTE] ChatNestからの転記: 2026-06-18 14:30\n\n## 自分\n\n本文内容";

        svc.CommitAddFromText(paste);

        Assert.DoesNotContain("[NOTE]", ideas[0].Body);
        Assert.Contains("## 自分", ideas[0].Body);
        Assert.Contains("本文内容", ideas[0].Body);
    }

    [Fact]
    public void CommitAddFromText_ChatNestHeader_StripsLeadingBlankLineFromBody()
    {
        var ideas = new List<Idea>();
        var cards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeService(ideas, cards);
        var paste = "[NOTE] ChatNestからの転記: 2026-06-18 09:15\n\n## 結論\n\nまとめ";

        svc.CommitAddFromText(paste);

        Assert.StartsWith("## 結論", ideas[0].Body);
    }

    [Fact]
    public void CommitAddFromText_ChatNestHeader_MultipleSpeakers_KeepsAllContent()
    {
        var ideas = new List<Idea>();
        var cards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeService(ideas, cards);
        var paste = "[NOTE] ChatNestからの転記: 2026-06-18 14:30\n\n## 自分\n\n一言目\n二言目\n\n## 反論\n\n反論内容";

        svc.CommitAddFromText(paste);

        Assert.Contains("## 自分", ideas[0].Body);
        Assert.Contains("一言目", ideas[0].Body);
        Assert.Contains("二言目", ideas[0].Body);
        Assert.Contains("## 反論", ideas[0].Body);
        Assert.Contains("反論内容", ideas[0].Body);
    }

    [Fact]
    public void CommitAddFromText_SimilarButNotMatchingHeader_UsesPasteTitle()
    {
        var fixedNow = new DateTime(2026, 6, 18, 14, 30, 0);
        var ideas = new List<Idea>();
        var cards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeService(ideas, cards, () => fixedNow);

        // 角括弧なし → 通常貼り付けとして扱う
        svc.CommitAddFromText("NOTE ChatNestからの転記: 2026-06-18 14:30\n本文");

        Assert.Equal("Paste_202606181430", ideas[0].Title);
    }

    [Fact]
    public void CommitAddFromText_ChatNestHeaderCrLf_ExtractsTitleCorrectly()
    {
        var ideas = new List<Idea>();
        var cards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeService(ideas, cards);
        // Windows 改行（CRLF）
        var paste = "[NOTE] ChatNestからの転記: 2026-06-18 14:30\r\n\r\n## 自分\r\n\r\n本文内容";

        svc.CommitAddFromText(paste);

        Assert.Equal("ChatNestからの転記: 2026-06-18 14:30", ideas[0].Title);
        Assert.DoesNotContain("[NOTE]", ideas[0].Body);
        Assert.Contains("## 自分", ideas[0].Body);
    }
}

public class CardDisplayViewModelTests
{
    private static CardDisplayViewModel Create() =>
        new CardDisplayViewModel(() => { }, () => { });

    // v1.20.0: BodyPreviewMaxLines がカードサイズごとに正しい値を返す
    [Theory]
    [InlineData("small",  3)]
    [InlineData("medium", 5)]
    [InlineData("large",  10)]
    public void BodyPreviewMaxLines_ReturnsExpectedLineCountPerSize(string size, int expected)
    {
        var vm = Create();
        vm.CardSize = size;
        Assert.Equal(expected, vm.BodyPreviewMaxLines);
    }

    [Fact]
    public void BodyPreviewMaxLines_DefaultIsMedium()
    {
        var vm = Create();
        Assert.Equal(5, vm.BodyPreviewMaxLines);
    }
}
