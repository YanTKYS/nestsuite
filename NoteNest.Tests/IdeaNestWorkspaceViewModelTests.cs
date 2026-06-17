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
}
