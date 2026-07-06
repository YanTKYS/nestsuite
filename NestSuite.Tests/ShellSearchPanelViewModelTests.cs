using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.15.0 SH: Shell 横断検索パネルの ViewModel テスト。
/// パネル状態（検索語・結果・メッセージ）はセッション内のみで保持し、永続化しないことを前提に検証する。
/// </summary>
public class ShellSearchPanelViewModelTests
{
    private static ShellSearchTabEntry CreateIdeaTab(string tabId, string tabTitle, string cardTitle)
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.AllCards.Add(new IdeaCardViewModel(new Idea { Title = cardTitle, Body = "" }));
        return new ShellSearchTabEntry(tabId, tabTitle, NestSuiteWorkspaceKind.IdeaNest, vm);
    }

    [Fact]
    public void SearchText_Empty_ShowsPromptMessage_AndNoResults()
    {
        var panel = new ShellSearchPanelViewModel(() => new[] { CreateIdeaTab("t1", "A", "検索対象") });

        panel.SearchText = "";

        Assert.Empty(panel.Results);
        Assert.True(panel.HasStatusMessage);
        Assert.Contains("検索語を入力してください", panel.StatusMessage);
    }

    [Fact]
    public void SearchText_Match_PopulatesResults_AndClearsMessage()
    {
        var panel = new ShellSearchPanelViewModel(() => new[] { CreateIdeaTab("t1", "A", "検索対象カード") });

        panel.SearchText = "検索対象";

        Assert.Single(panel.Results);
        Assert.False(panel.HasStatusMessage);
    }

    [Fact]
    public void SearchText_TooManyResults_ShowsTruncationMessage()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        for (int i = 0; i < ShellSearchService.MaxResults + 5; i++)
            vm.AllCards.Add(new IdeaCardViewModel(new Idea { Title = $"検索対象{i}", Body = "" }));
        var panel = new ShellSearchPanelViewModel(() =>
            new[] { new ShellSearchTabEntry("t1", "A", NestSuiteWorkspaceKind.IdeaNest, vm) });

        panel.SearchText = "検索対象";

        Assert.Equal(ShellSearchService.MaxResults, panel.Results.Count);
        Assert.True(panel.HasStatusMessage);
        Assert.Contains("先頭100件", panel.StatusMessage);
    }

    [Fact]
    public void SearchText_ExactlyMaxResults_DoesNotShowTruncationMessage()
    {
        // レビュー指摘: 一致がちょうど MaxResults 件だっただけの場合、実際には切り詰められて
        // いないので「結果が多すぎる」メッセージを出してはいけない。
        var vm = new IdeaNestWorkspaceViewModel();
        for (int i = 0; i < ShellSearchService.MaxResults; i++)
            vm.AllCards.Add(new IdeaCardViewModel(new Idea { Title = $"検索対象{i}", Body = "" }));
        var panel = new ShellSearchPanelViewModel(() =>
            new[] { new ShellSearchTabEntry("t1", "A", NestSuiteWorkspaceKind.IdeaNest, vm) });

        panel.SearchText = "検索対象";

        Assert.Equal(ShellSearchService.MaxResults, panel.Results.Count);
        Assert.False(panel.HasStatusMessage);
    }

    [Fact]
    public void Reset_ClearsSearchTextResultsAndMessage()
    {
        var panel = new ShellSearchPanelViewModel(() => new[] { CreateIdeaTab("t1", "A", "検索対象カード") });
        panel.SearchText = "検索対象";
        Assert.Single(panel.Results);

        panel.Reset();

        Assert.Equal("", panel.SearchText);
        Assert.Empty(panel.Results);
        Assert.False(panel.HasStatusMessage);
    }
}
