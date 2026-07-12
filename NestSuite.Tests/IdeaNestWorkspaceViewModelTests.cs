using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.Services;
using NestSuite.IdeaNest.ViewModels;
using Xunit;

namespace NestSuite.Tests;

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
    public void ArchiveFilterMode_FiltersActiveIncludeAndArchivedOnly()
    {
        var cards = new[]
        {
            new IdeaCardViewModel(new Idea { Title = "通常", IsArchived = false, Tags = ["A"] }),
            new IdeaCardViewModel(new Idea { Title = "保管", IsArchived = true, Tags = ["A"] }),
        };
        var filter = new FilterViewModel(() => { }, () => { });

        Assert.Equal(["通常"], filter.Apply(cards).Select(c => c.Title));

        filter.ArchiveFilterMode = ArchiveFilterMode.IncludeArchived;
        Assert.Equal(["通常", "保管"], filter.Apply(cards).Select(c => c.Title));

        filter.ArchiveFilterMode = ArchiveFilterMode.ArchivedOnly;
        Assert.Equal(["保管"], filter.Apply(cards).Select(c => c.Title));
    }

    [Fact]
    public void ArchiveFilterMode_ArchivedOnly_ComposesWithTagAndSearchFilters()
    {
        var cards = new[]
        {
            new IdeaCardViewModel(new Idea { Title = "通常 alpha", IsArchived = false, Tags = ["A"] }),
            new IdeaCardViewModel(new Idea { Title = "保管 alpha", IsArchived = true, Tags = ["A"] }),
            new IdeaCardViewModel(new Idea { Title = "保管 beta", IsArchived = true, Tags = ["B"] }),
        };
        var filter = new FilterViewModel(() => { }, () => { })
        {
            ArchiveFilterMode = ArchiveFilterMode.ArchivedOnly,
            SelectedTag = "A",
            SearchText = "alpha",
        };

        var visible = filter.Apply(cards).ToList();

        Assert.Single(visible);
        Assert.Equal("保管 alpha", visible[0].Title);
    }

    [Fact]
    public void ArchiveFilterMode_ArchivedOnly_EmptyStateUsesArchiveMessage()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace
        {
            Ideas = [new Idea { Title = "通常", IsArchived = false }],
        });

        vm.ArchiveFilterMode = ArchiveFilterMode.ArchivedOnly;

        Assert.True(vm.ShowEmptyState);
        Assert.Equal("アーカイブ済みカードはありません。", vm.EmptyStateMessage);
    }


    [Fact]
    public void ArchiveFilterMode_ArchivedOnly_WithActiveFilterEmptyStatePrioritizesFilterMessage()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace
        {
            Ideas = [new Idea { Title = "保管", IsArchived = true, Tags = ["A"] }],
        });

        vm.ArchiveFilterMode = ArchiveFilterMode.ArchivedOnly;
        vm.SearchText = "一致しない検索語";

        Assert.True(vm.ShowEmptyState);
        Assert.True(vm.HasActiveFilter);
        Assert.Equal("検索語やタグを変更してください。", vm.EmptyStateMessage);
    }

    // ── SH-28: アーカイブ切替の一時フィードバック ────────────────────────

    [Fact]
    public void ToggleArchiveCommand_Execute_ShowsArchivedFeedback()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace
        {
            Ideas = [new Idea { Id = "card1", Title = "通常", IsArchived = false }],
        });
        var card = vm.AllCards.Single();

        vm.ToggleArchiveCommand.Execute(card);

        Assert.True(card.IsArchived);
        Assert.Equal("アーカイブしました", vm.StatusMessage);
    }

    [Fact]
    public void ToggleArchiveCommand_Execute_Twice_ShowsUnarchivedFeedback()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace
        {
            Ideas = [new Idea { Id = "card1", Title = "通常", IsArchived = false }],
        });
        var card = vm.AllCards.Single();

        vm.ToggleArchiveCommand.Execute(card);
        vm.ToggleArchiveCommand.Execute(card);

        Assert.False(card.IsArchived);
        Assert.Equal("アーカイブを解除しました", vm.StatusMessage);
    }


    [Fact]
    public void BuildWorkspaceForSave_IncludesCardsAndDoesNotMarkSaved()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace
        {
            WorkspaceName = "DraftSnapshot",
            Ideas = [new Idea { Id = "card-1", Title = "Title", Body = "Body" }],
            Settings = new WorkspaceSettings { CardSize = "large" },
        });
        vm.MarkDirty();
        var hadChanges = vm.HasChanges;

        var snapshot = vm.BuildWorkspaceForSave();

        Assert.Equal("card-1", snapshot.Ideas.Single().Id);
        Assert.Equal("large", snapshot.Settings.CardSize);
        Assert.Equal(hadChanges, vm.HasChanges);
        Assert.True(vm.HasChanges);
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

// ── ID-14: 新規カード初期値・既存カード保持確認 ──────────────────────────────────────────

public class IdeaNewCardInitialValueTests
{
    [Fact]
    public void NewIdea_Title_IsEmpty()
    {
        var idea = new Idea();
        Assert.Equal(string.Empty, idea.Title);
    }

    [Fact]
    public void NewIdea_Body_IsEmpty()
    {
        var idea = new Idea();
        Assert.Equal(string.Empty, idea.Body);
    }

    [Fact]
    public void NewIdea_Tags_IsEmpty()
    {
        var idea = new Idea();
        Assert.Empty(idea.Tags);
    }

    [Fact]
    public void EditIdeaViewModel_NewCard_TitleIsEmpty()
    {
        var vm = new EditIdeaViewModel(new Idea(), isExistingCard: false);
        Assert.Equal(string.Empty, vm.Title);
    }

    [Fact]
    public void EditIdeaViewModel_NewCard_BodyIsEmpty()
    {
        var vm = new EditIdeaViewModel(new Idea(), isExistingCard: false);
        Assert.Equal(string.Empty, vm.Body);
    }

    [Fact]
    public void EditIdeaViewModel_NewCard_TagsTextIsEmpty()
    {
        var vm = new EditIdeaViewModel(new Idea(), isExistingCard: false);
        Assert.Equal(string.Empty, vm.TagsText);
    }

    [Fact]
    public void EditIdeaViewModel_ExistingCard_PreservesTitle_Body_Tags()
    {
        var idea = new Idea { Title = "既存タイトル", Body = "既存本文", Tags = ["タグA", "タグB"] };
        var vm = new EditIdeaViewModel(idea, isExistingCard: true);
        Assert.Equal("既存タイトル", vm.Title);
        Assert.Equal("既存本文", vm.Body);
        Assert.Contains("タグA", vm.TagsText);
        Assert.Contains("タグB", vm.TagsText);
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
