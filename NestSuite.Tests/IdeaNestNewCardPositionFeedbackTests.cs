using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// ID-15 (v2.18.2): IdeaNest 新規カード作成後の位置フィードバック
/// （<see cref="IdeaNestWorkspaceViewModel.ApplyNewCardPositionFeedback"/>）の確認。
/// 作成カードは <see cref="NestSuite.IdeaNest.Views.PreviewIdeaWindow"/> のモーダル表示を経由するため、
/// ダイアログを介さずに検証できるよう <c>ApplyNewCardPositionFeedback</c> を直接呼び出す。
/// </summary>
public class IdeaNestNewCardPositionFeedbackTests
{
    private static IdeaNestWorkspaceViewModel CreateWithCards(params Idea[] ideas)
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace { Ideas = new(ideas) });
        return vm;
    }

    // ── キャンセル・作成失敗（created == null） ───────────────────────────

    [Fact]
    public void NullCreatedCard_DoesNotSelectAnything()
    {
        var vm = CreateWithCards(new Idea { Id = "a", Title = "A" });

        vm.ApplyNewCardPositionFeedback(null);

        Assert.Null(vm.SelectedCard);
    }

    [Fact]
    public void NullCreatedCard_DoesNotShowStatusMessage()
    {
        var vm = CreateWithCards();

        vm.ApplyNewCardPositionFeedback(null);

        Assert.Equal(string.Empty, vm.StatusMessage);
    }

    [Fact]
    public void NullCreatedCard_DoesNotRaiseScrollRequested()
    {
        var vm = CreateWithCards(new Idea { Id = "a", Title = "A" });
        var raised = false;
        vm.ScrollRequested += (_, _) => raised = true;

        vm.ApplyNewCardPositionFeedback(null);

        Assert.False(raised);
    }

    // ── 表示対象（フィルターなし） ─────────────────────────────────────────

    [Fact]
    public void VisibleCard_IsSelected()
    {
        var vm = CreateWithCards(new Idea { Id = "a", Title = "A" });
        var card = vm.AllCards.Single(c => c.Id == "a");

        vm.ApplyNewCardPositionFeedback(card);

        Assert.Same(card, vm.SelectedCard);
        Assert.True(card.IsSelected);
    }

    [Fact]
    public void VisibleCard_RaisesScrollRequestedWithCard()
    {
        var vm = CreateWithCards(new Idea { Id = "a", Title = "A" });
        var card = vm.AllCards.Single(c => c.Id == "a");
        IdeaCardViewModel? received = null;
        vm.ScrollRequested += (_, c) => received = c;

        vm.ApplyNewCardPositionFeedback(card);

        Assert.Same(card, received);
    }

    [Fact]
    public void VisibleCard_DoesNotShowStatusMessage()
    {
        // 選択・スクロールで十分に分かるため、表示対象内では通知を出さない。
        var vm = CreateWithCards(new Idea { Id = "a", Title = "A" });
        var card = vm.AllCards.Single(c => c.Id == "a");

        vm.ApplyNewCardPositionFeedback(card);

        Assert.Equal(string.Empty, vm.StatusMessage);
    }

    [Fact]
    public void SameTitleCards_SelectsTheActualCreatedInstance()
    {
        // タイトル一致からの推測ではなく、渡されたインスタンスそのものを対象にする。
        var vm = CreateWithCards(
            new Idea { Id = "first",  Title = "同じタイトル" },
            new Idea { Id = "second", Title = "同じタイトル" });
        var first  = vm.AllCards.Single(c => c.Id == "first");
        var second = vm.AllCards.Single(c => c.Id == "second");

        vm.ApplyNewCardPositionFeedback(second);

        Assert.Same(second, vm.SelectedCard);
        Assert.False(first.IsSelected);
        Assert.True(second.IsSelected);
    }

    [Theory]
    [InlineData("UpdatedDesc")]
    [InlineData("CreatedDesc")]
    [InlineData("TitleAsc")]
    public void SortModeDoesNotAffectWhichCardIsSelected(string sortMode)
    {
        var vm = CreateWithCards(
            new Idea { Id = "a", Title = "Aタイトル" },
            new Idea { Id = "b", Title = "Bタイトル" });
        vm.SortMode = sortMode;
        var target = vm.AllCards.Single(c => c.Id == "b");

        vm.ApplyNewCardPositionFeedback(target);

        Assert.Same(target, vm.SelectedCard);
    }

    // ── 連続作成 ───────────────────────────────────────────────────────────

    [Fact]
    public void ConsecutiveCreations_OnlyLatestCardIsSelected()
    {
        var vm = CreateWithCards(
            new Idea { Id = "a", Title = "A" },
            new Idea { Id = "b", Title = "B" });
        var first  = vm.AllCards.Single(c => c.Id == "a");
        var second = vm.AllCards.Single(c => c.Id == "b");

        vm.ApplyNewCardPositionFeedback(first);
        vm.ApplyNewCardPositionFeedback(second);

        Assert.Same(second, vm.SelectedCard);
        Assert.False(first.IsSelected);
        Assert.True(second.IsSelected);
    }

    // ── 表示対象外（検索・タグ・色・アーカイブ条件） ────────────────────────

    [Fact]
    public void FilteredOutBySearchText_DoesNotSelectOrScroll()
    {
        var vm = CreateWithCards(new Idea { Id = "a", Title = "対象カード", Body = "本文" });
        var card = vm.AllCards.Single(c => c.Id == "a");
        vm.SearchText = "一致しない検索語";

        vm.ApplyNewCardPositionFeedback(card);

        Assert.Null(vm.SelectedCard);
        Assert.False(card.IsSelected);
    }

    [Fact]
    public void FilteredOutBySearchText_ShowsNotification_AndKeepsSearchText()
    {
        var vm = CreateWithCards(new Idea { Id = "a", Title = "対象カード", Body = "本文" });
        var card = vm.AllCards.Single(c => c.Id == "a");
        vm.SearchText = "一致しない検索語";

        vm.ApplyNewCardPositionFeedback(card);

        Assert.Contains("現在の絞り込み条件では表示されていません", vm.StatusMessage);
        Assert.Equal("一致しない検索語", vm.SearchText);
    }

    [Fact]
    public void FilteredOutByTag_DoesNotSelectOrScroll_AndKeepsTagFilter()
    {
        var vm = CreateWithCards(new Idea { Id = "a", Title = "A", Tags = new() { "タグ1" } });
        var card = vm.AllCards.Single(c => c.Id == "a");
        vm.SelectedTag = "タグ2";

        vm.ApplyNewCardPositionFeedback(card);

        Assert.Null(vm.SelectedCard);
        Assert.Equal("タグ2", vm.SelectedTag);
    }

    [Fact]
    public void FilteredOutByColor_DoesNotSelectOrScroll_AndKeepsColorFilter()
    {
        var vm = CreateWithCards(new Idea { Id = "a", Title = "A", Color = "white" });
        var card = vm.AllCards.Single(c => c.Id == "a");
        vm.SelectedColor = "blue";

        vm.ApplyNewCardPositionFeedback(card);

        Assert.Null(vm.SelectedCard);
        Assert.Equal("blue", vm.SelectedColor);
    }

    [Fact]
    public void FilteredOutByArchiveMode_DoesNotSelectOrScroll_AndKeepsArchiveMode()
    {
        var vm = CreateWithCards(new Idea { Id = "a", Title = "A", IsArchived = true });
        var card = vm.AllCards.Single(c => c.Id == "a");
        // 既定は ActiveOnly のため、アーカイブ済みカードは表示対象外のまま。

        vm.ApplyNewCardPositionFeedback(card);

        Assert.Null(vm.SelectedCard);
        Assert.Equal(ArchiveFilterMode.ActiveOnly, vm.ArchiveFilterMode);
    }

    [Fact]
    public void FilteredOutCard_RaisesScrollRequested_Never()
    {
        var vm = CreateWithCards(new Idea { Id = "a", Title = "A" });
        var card = vm.AllCards.Single(c => c.Id == "a");
        vm.SearchText = "一致しない";
        var raised = false;
        vm.ScrollRequested += (_, _) => raised = true;

        vm.ApplyNewCardPositionFeedback(card);

        Assert.False(raised);
    }

    // ── 一覧再構築だけでは誤発火しない ──────────────────────────────────────

    [Fact]
    public void RefreshVisibleAlone_DoesNotRaiseScrollRequested()
    {
        var vm = CreateWithCards(new Idea { Id = "a", Title = "A" });
        var raised = false;
        vm.ScrollRequested += (_, _) => raised = true;

        // ソート変更は RefreshVisible を経由する既存の一覧更新であり、新規作成ではない。
        vm.SortMode = "TitleAsc";

        Assert.False(raised);
    }

    [Fact]
    public void EditingAnotherCard_DoesNotClearExistingSelection()
    {
        var vm = CreateWithCards(
            new Idea { Id = "a", Title = "A" },
            new Idea { Id = "b", Title = "B" });
        var a = vm.AllCards.Single(c => c.Id == "a");
        var b = vm.AllCards.Single(c => c.Id == "b");
        vm.ApplyNewCardPositionFeedback(a);

        vm.TogglePinCommand.Execute(b);

        Assert.Same(a, vm.SelectedCard);
        Assert.True(a.IsSelected);
    }

    // ── ワークスペース再構築で残留しない ─────────────────────────────────────

    [Fact]
    public void ReloadingWorkspace_ClearsSelectedCard()
    {
        var vm = CreateWithCards(new Idea { Id = "a", Title = "A" });
        var card = vm.AllCards.Single(c => c.Id == "a");
        vm.ApplyNewCardPositionFeedback(card);
        Assert.Same(card, vm.SelectedCard);

        vm.LoadFromWorkspace(new Workspace { Ideas = new() { new Idea { Id = "b", Title = "B" } } });

        Assert.Null(vm.SelectedCard);
    }

    // ── XAML静的確認 ───────────────────────────────────────────────────────
    // カード一覧が選択状態を表現できること、既存のコンテキストメニュー・ボタン・
    // テーマ追従リソースを失っていないことを最小限に確認する。

    [Fact]
    public void WorkspaceViewXaml_CardsItemsControl_HasNameForScrollLookup()
    {
        var xaml = ReadWorkspaceViewXaml();
        Assert.Contains("x:Name=\"CardsItemsControl\"", xaml);
        Assert.Contains("ItemsSource=\"{Binding VisibleCards}\"", xaml);
    }

    [Fact]
    public void WorkspaceViewXaml_CardTemplate_BindsIsSelectedUsingExistingAccentBrush()
    {
        var xaml = ReadWorkspaceViewXaml();
        Assert.Contains("Binding IsSelected", xaml);
        // 既存のキーボードフォーカス強調と同じアクセント色を再利用し、新しい色は追加していない。
        Assert.Contains("IdeaAccentBrush", xaml);
    }

    [Fact]
    public void WorkspaceViewXaml_StillHasExistingCardContextMenuAndButtons()
    {
        var xaml = ReadWorkspaceViewXaml();
        Assert.Contains("Header=\"プレビュー(_V)\"", xaml);
        Assert.Contains("Header=\"削除(_D)\"", xaml);
        Assert.Contains("Command=\"{Binding DataContext.TogglePinCommand", xaml);
        Assert.Contains("Command=\"{Binding DataContext.ToggleArchiveCommand", xaml);
        Assert.Contains("IdeaNest.AddIdeaButton", xaml);
        Assert.Contains("MouseLeftButtonUp=\"OnCardMouseLeftButtonUp\"", xaml);
    }

    private static string ReadWorkspaceViewXaml()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NestSuite.Tests.csproj")))
            dir = dir.Parent;

        Assert.NotNull(dir);
        var path = Path.Combine(dir!.Parent!.FullName, "NestSuite", "NestSuite", "IdeaNest", "Views", "IdeaNestWorkspaceView.xaml");
        Assert.True(File.Exists(path), $"IdeaNestWorkspaceView.xaml not found: {path}");
        return File.ReadAllText(path);
    }
}
