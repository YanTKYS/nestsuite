using System.Collections.ObjectModel;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.Services;
using NestSuite.IdeaNest.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// ID-14: カラーフィルタチップへのカード枚数表示。
/// 件数の定義（色条件を適用する直前の集合から計算し、選択中の色フィルタ自身は除外する）と、
/// 検索・タグ・アーカイブ条件の反映、カード操作後の更新、フィルター結果との一致を確認する。
/// </summary>
public class IdeaNestColorFilterCountTests
{
    private static IdeaCardViewModel MakeCard(string color, bool archived = false, string[]? tags = null, string title = "", string body = "") =>
        new(new Idea { Title = title, Body = body, Color = color, IsArchived = archived, Tags = tags?.ToList() ?? new() });

    // ── 基本件数 ─────────────────────────────────────────────────────────────

    [Fact]
    public void ComputeColorCounts_CountsCardsPerColor()
    {
        var cards = new[] { MakeCard("red"), MakeCard("red"), MakeCard("blue") };
        var filter = new FilterViewModel(() => { }, () => { });

        var counts = filter.ComputeColorCounts(cards);

        Assert.Equal(2, counts["red"]);
        Assert.Equal(1, counts["blue"]);
    }

    [Fact]
    public void ComputeColorCounts_UnusedColor_IsAbsentFromDictionary_TreatedAsZero()
    {
        var cards = new[] { MakeCard("red") };
        var filter = new FilterViewModel(() => { }, () => { });

        var counts = filter.ComputeColorCounts(cards);

        Assert.False(counts.ContainsKey("green"));
        Assert.Equal(0, counts.TryGetValue("green", out var count) ? count : 0);
    }

    [Fact]
    public void ComputeColorCounts_MultipleCardsSameColor_CountsAll()
    {
        var cards = new[] { MakeCard("blue"), MakeCard("blue"), MakeCard("blue") };
        var filter = new FilterViewModel(() => { }, () => { });

        Assert.Equal(3, filter.ComputeColorCounts(cards)["blue"]);
    }

    [Fact]
    public void ComputeColorCounts_UnsetColor_FallsBackToYellow_MatchingApplyBehavior()
    {
        var cards = new[] { MakeCard(""), MakeCard("   ") };
        var filter = new FilterViewModel(() => { }, () => { });

        Assert.Equal(2, filter.ComputeColorCounts(cards)["yellow"]);
    }

    [Fact]
    public void ComputeColorCounts_DoesNotMutateCards()
    {
        var cards = new[] { MakeCard("red") };
        var filter = new FilterViewModel(() => { }, () => { });

        filter.ComputeColorCounts(cards);

        Assert.Equal("red", cards[0].Color);
    }

    // ── 自己フィルター除外 ───────────────────────────────────────────────────

    [Fact]
    public void ComputeColorCounts_WithColorFilterSelected_StillCountsOtherColors()
    {
        var cards = new[] { MakeCard("red"), MakeCard("red"), MakeCard("blue"), MakeCard("green") };
        var filter = new FilterViewModel(() => { }, () => { }) { SelectedColor = "red" };

        var counts = filter.ComputeColorCounts(cards);

        Assert.Equal(2, counts["red"]);
        Assert.Equal(1, counts["blue"]);
        Assert.Equal(1, counts["green"]);
    }

    [Fact]
    public void ComputeColorCounts_SelectedColorCount_MatchesApplyResultForThatColor()
    {
        var cards = new[] { MakeCard("red"), MakeCard("red"), MakeCard("blue") };
        var filter = new FilterViewModel(() => { }, () => { }) { SelectedColor = "red" };

        var countForRed = filter.ComputeColorCounts(cards)["red"];
        var visibleWhenRedSelected = filter.Apply(cards).Count();

        Assert.Equal(visibleWhenRedSelected, countForRed);
    }

    // ── 検索条件 ─────────────────────────────────────────────────────────────

    [Fact]
    public void ComputeColorCounts_SearchText_OnlyCountsMatchingCards()
    {
        var cards = new[]
        {
            MakeCard("red", title: "Meeting notes"),
            MakeCard("red", title: "Random idea"),
            MakeCard("blue", title: "Meeting agenda"),
        };
        var filter = new FilterViewModel(() => { }, () => { }) { SearchText = "meeting" };

        var counts = filter.ComputeColorCounts(cards);

        Assert.Equal(1, counts["red"]);
        Assert.Equal(1, counts["blue"]);
    }

    [Fact]
    public void ComputeColorCounts_ClearingSearchText_RestoresFullCounts()
    {
        var cards = new[] { MakeCard("red", title: "Meeting"), MakeCard("red", title: "Other") };
        var filter = new FilterViewModel(() => { }, () => { }) { SearchText = "meeting" };
        Assert.Equal(1, filter.ComputeColorCounts(cards)["red"]);

        filter.SearchText = "";

        Assert.Equal(2, filter.ComputeColorCounts(cards)["red"]);
    }

    // ── タグ条件 ─────────────────────────────────────────────────────────────

    [Fact]
    public void ComputeColorCounts_SelectedTag_OnlyCountsMatchingCards()
    {
        var cards = new[]
        {
            MakeCard("red", tags: ["A"]),
            MakeCard("red", tags: ["B"]),
            MakeCard("blue", tags: ["A"]),
        };
        var filter = new FilterViewModel(() => { }, () => { }) { SelectedTag = "A" };

        var counts = filter.ComputeColorCounts(cards);

        Assert.Equal(1, counts["red"]);
        Assert.Equal(1, counts["blue"]);
    }

    [Fact]
    public void ComputeColorCounts_SearchAndTagCombined_IntersectsBoth()
    {
        var cards = new[]
        {
            MakeCard("red", tags: ["A"], title: "Meeting"),
            MakeCard("red", tags: ["A"], title: "Other"),
            MakeCard("red", tags: ["B"], title: "Meeting"),
        };
        var filter = new FilterViewModel(() => { }, () => { }) { SelectedTag = "A", SearchText = "meeting" };

        Assert.Equal(1, filter.ComputeColorCounts(cards)["red"]);
    }

    // ── アーカイブ ────────────────────────────────────────────────────────────

    [Fact]
    public void ComputeColorCounts_ActiveOnly_CountsOnlyNonArchivedCards()
    {
        var cards = new[] { MakeCard("red", archived: false), MakeCard("red", archived: true) };
        var filter = new FilterViewModel(() => { }, () => { });

        Assert.Equal(1, filter.ComputeColorCounts(cards)["red"]);
    }

    [Fact]
    public void ComputeColorCounts_ArchivedOnly_CountsOnlyArchivedCards()
    {
        var cards = new[] { MakeCard("red", archived: false), MakeCard("red", archived: true), MakeCard("red", archived: true) };
        var filter = new FilterViewModel(() => { }, () => { }) { ArchiveFilterMode = ArchiveFilterMode.ArchivedOnly };

        Assert.Equal(2, filter.ComputeColorCounts(cards)["red"]);
    }

    // ── フィルター結果一致 ───────────────────────────────────────────────────

    [Fact]
    public void ComputeColorCounts_AllColors_MatchApplyResultForEachColorSelection()
    {
        var cards = new[]
        {
            MakeCard("red"), MakeCard("red"), MakeCard("blue"), MakeCard("green"), MakeCard("green"), MakeCard("green"),
        };
        var filter = new FilterViewModel(() => { }, () => { });
        var counts = filter.ComputeColorCounts(cards);

        foreach (var color in new[] { "red", "blue", "green" })
        {
            filter.SelectedColor = color;
            Assert.Equal(counts[color], filter.Apply(cards).Count());
        }
    }

    // ── カード操作（CardOperationsService 経由） ────────────────────────────

    private static CardOperationsService MakeService(List<Idea> ideas, ObservableCollection<IdeaCardViewModel> cards) =>
        new(ideas, cards, () => { }, () => { }, () => { });

    [Fact]
    public void CommitAdd_IncreasesColorCount()
    {
        var ideas = new List<Idea>();
        var cards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeService(ideas, cards);
        var filter = new FilterViewModel(() => { }, () => { });

        svc.CommitAdd(new Idea { Title = "New", Color = "purple" });

        Assert.Equal(1, filter.ComputeColorCounts(cards)["purple"]);
    }

    [Fact]
    public void CommitDelete_DecreasesColorCount()
    {
        var ideas = new List<Idea>();
        var cards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeService(ideas, cards);
        var added = svc.CommitAdd(new Idea { Title = "ToDelete", Color = "orange" })!;
        var filter = new FilterViewModel(() => { }, () => { });
        Assert.Equal(1, filter.ComputeColorCounts(cards)["orange"]);

        svc.CommitDelete(added);

        Assert.False(filter.ComputeColorCounts(cards).ContainsKey("orange"));
    }

    [Fact]
    public void ChangingCardColor_ThenCommitEdit_MovesCountFromOldToNewColor()
    {
        var ideas = new List<Idea>();
        var cards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeService(ideas, cards);
        var card = svc.CommitAdd(new Idea { Title = "Recolor", Color = "white" })!;
        var filter = new FilterViewModel(() => { }, () => { });
        Assert.Equal(1, filter.ComputeColorCounts(cards)["white"]);

        card.Color = "gray";
        svc.CommitEdit(card);

        var counts = filter.ComputeColorCounts(cards);
        Assert.False(counts.ContainsKey("white"));
        Assert.Equal(1, counts["gray"]);
    }

    [Fact]
    public void ToggleArchive_MovesCountBetweenActiveAndArchivedViews()
    {
        var ideas = new List<Idea>();
        var cards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeService(ideas, cards);
        var card = svc.CommitAdd(new Idea { Title = "Archivable", Color = "pink" })!;
        var filter = new FilterViewModel(() => { }, () => { });
        Assert.Equal(1, filter.ComputeColorCounts(cards)["pink"]);

        card.IsArchived = true;

        Assert.False(filter.ComputeColorCounts(cards).ContainsKey("pink"));

        filter.ArchiveFilterMode = ArchiveFilterMode.ArchivedOnly;
        Assert.Equal(1, filter.ComputeColorCounts(cards)["pink"]);
    }

    // ── IdeaNestWorkspaceViewModel 統合（ColorItems.Count / Workspace再読込） ──

    [Fact]
    public void LoadFromWorkspace_PopulatesColorItemCounts()
    {
        var vm = new IdeaNestWorkspaceViewModel();

        vm.LoadFromWorkspace(new Workspace
        {
            Ideas =
            [
                new Idea { Title = "1", Color = "red" },
                new Idea { Title = "2", Color = "red" },
                new Idea { Title = "3", Color = "blue" },
            ],
        });

        Assert.Equal(2, vm.ColorItems.Single(i => i.Name == "red").Count);
        Assert.Equal(1, vm.ColorItems.Single(i => i.Name == "blue").Count);
        Assert.Equal(0, vm.ColorItems.Single(i => i.Name == "green").Count);
    }

    [Fact]
    public void ReloadingWorkspace_RecomputesColorItemCounts_ForNewCardSet()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace { Ideas = [new Idea { Title = "1", Color = "red" }] });
        Assert.Equal(1, vm.ColorItems.Single(i => i.Name == "red").Count);

        vm.LoadFromWorkspace(new Workspace { Ideas = [new Idea { Title = "2", Color = "blue" }] });

        Assert.Equal(0, vm.ColorItems.Single(i => i.Name == "red").Count);
        Assert.Equal(1, vm.ColorItems.Single(i => i.Name == "blue").Count);
    }

    [Fact]
    public void ToggleArchiveCommand_UpdatesColorItemCounts()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace { Ideas = [new Idea { Title = "1", Color = "green", IsArchived = false }] });
        var card = vm.AllCards.Single();
        Assert.Equal(1, vm.ColorItems.Single(i => i.Name == "green").Count);

        vm.ToggleArchiveCommand.Execute(card);

        Assert.Equal(0, vm.ColorItems.Single(i => i.Name == "green").Count);
    }

    [Fact]
    public void DeleteIdeaCommand_DecreasesColorItemCount()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace { Ideas = [new Idea { Title = "1", Color = "orange" }] });
        var card = vm.AllCards.Single();
        Assert.Equal(1, vm.ColorItems.Single(i => i.Name == "orange").Count);

        vm.DeleteIdeaCommand.Execute(card);

        Assert.Equal(0, vm.ColorItems.Single(i => i.Name == "orange").Count);
    }

    [Fact]
    public void SearchTextChange_UpdatesColorItemCounts()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace
        {
            Ideas =
            [
                new Idea { Title = "Meeting", Color = "red" },
                new Idea { Title = "Other", Color = "red" },
            ],
        });
        Assert.Equal(2, vm.ColorItems.Single(i => i.Name == "red").Count);

        vm.SearchText = "meeting";

        Assert.Equal(1, vm.ColorItems.Single(i => i.Name == "red").Count);
    }

    // ── 表示形式 ─────────────────────────────────────────────────────────────

    [Fact]
    public void ColorFilterItemViewModel_TooltipText_ContainsDisplayNameAndCount()
    {
        var item = new ColorFilterItemViewModel("red", "赤") { Count = 5 };

        Assert.Equal("赤：5件", item.TooltipText);
    }

    [Fact]
    public void ColorFilterItemViewModel_TooltipText_HandlesZeroCount()
    {
        var item = new ColorFilterItemViewModel("green", "緑") { Count = 0 };

        Assert.Equal("緑：0件", item.TooltipText);
    }

    [Fact]
    public void ColorFilterItemViewModel_TooltipText_HandlesThreeDigitCount_WithoutTruncation()
    {
        var item = new ColorFilterItemViewModel("red", "赤") { Count = 128 };

        Assert.Equal("赤：128件", item.TooltipText);
        Assert.Equal(128, item.Count);
    }

    [Fact]
    public void ColorFilterItemViewModel_AutomationName_ContainsDisplayNameAndCount()
    {
        var item = new ColorFilterItemViewModel("blue", "青") { Count = 3 };

        Assert.Equal("青、3件", item.AutomationName);
    }

    [Fact]
    public void ColorFilterItemViewModel_SettingCount_RaisesPropertyChangedForDerivedText()
    {
        var item = new ColorFilterItemViewModel("red", "赤");
        var changed = new List<string?>();
        item.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        item.Count = 7;

        Assert.Contains(nameof(ColorFilterItemViewModel.Count), changed);
        Assert.Contains(nameof(ColorFilterItemViewModel.TooltipText), changed);
        Assert.Contains(nameof(ColorFilterItemViewModel.AutomationName), changed);
    }

    // ── 保存回帰 ─────────────────────────────────────────────────────────────

    [Fact]
    public void BuildWorkspaceForSave_DoesNotIncludeColorCounts()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace
        {
            Ideas = [new Idea { Title = "1", Color = "red" }, new Idea { Title = "2", Color = "red" }],
        });

        var saved = vm.BuildWorkspaceForSave();

        var json = System.Text.Json.JsonSerializer.Serialize(saved);
        Assert.DoesNotContain("count", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, saved.Ideas.Count);
    }
}
