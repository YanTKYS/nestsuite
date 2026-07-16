using NestSuite.TempNest;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// SH-40 (AT-1 フェーズ1): TempNest上部の「続きから」導線。
/// 設計根拠: docs/planning/at1-continue-start-panel-design-review.md。
/// Shellコンストラクター起因の起動分岐そのもの（Window構築が必要）は既存方針どおり
/// 単体テスト対象外とし、判定ロジック（<see cref="ContinueFromPanelPolicy"/>）と
/// 表示用データ・排他状態（<see cref="TempNestWorkspaceViewModel"/>）だけを確認する。
/// </summary>
public class SH40ContinueFromPanelTests
{
    // ── ContinueFromPanelPolicy（WPF非依存・純粋ロジック） ──────────────────

    [Theory]
    [InlineData(false, null, true)]
    [InlineData(false, "", true)]
    [InlineData(true, null, false)]
    [InlineData(false, "C:\\file.nestsuite", false)]
    [InlineData(true, "C:\\file.nestsuite", false)]
    public void ShouldEvaluateAtStartup_MatchesExistingTempActivationBranch(
        bool sessionRestoreSucceeded, string? launchFilePath, bool expected)
    {
        Assert.Equal(expected, ContinueFromPanelPolicy.ShouldEvaluateAtStartup(sessionRestoreSucceeded, launchFilePath));
    }

    [Fact]
    public void SelectTopRecentItems_ReturnsAllWhenAtOrBelowMax()
    {
        Assert.Empty(ContinueFromPanelPolicy.SelectTopRecentItems([]));
        Assert.Equal(["a"], ContinueFromPanelPolicy.SelectTopRecentItems(["a"]));
        Assert.Equal(["a", "b"], ContinueFromPanelPolicy.SelectTopRecentItems(["a", "b"]));
        Assert.Equal(["a", "b", "c"], ContinueFromPanelPolicy.SelectTopRecentItems(["a", "b", "c"]));
    }

    [Fact]
    public void SelectTopRecentItems_TakesTopThree_PreservingMruOrder_WhenMoreThanThree()
    {
        var result = ContinueFromPanelPolicy.SelectTopRecentItems(["a", "b", "c", "d", "e"]);
        Assert.Equal(["a", "b", "c"], result);
    }

    // ── TempNestWorkspaceViewModel: SetContinueFromCandidates / ShouldShowContinueFrom ──

    private static TempNestWorkspaceViewModel MakeEmptyTempNestViewModel()
    {
        var vm = new TempNestWorkspaceViewModel();
        foreach (var slot in new[] { vm.Slot1, vm.Slot2, vm.Slot3, vm.Slot4 })
            slot.LoadFromSlot(new TempNestSlot());
        return vm;
    }

    [Fact]
    public void ShouldShowContinueFrom_False_WhenNoRecentAndNoDraftCandidates()
    {
        using var vm = MakeEmptyTempNestViewModel();
        vm.SetContinueFromCandidates([], 0);

        Assert.False(vm.ShouldShowContinueFrom);
    }

    [Fact]
    public void ShouldShowContinueFrom_True_WhenOnlyRecentItemsPresent()
    {
        using var vm = MakeEmptyTempNestViewModel();
        vm.SetContinueFromCandidates(["C:\\a.nestsuite"], 0);

        Assert.True(vm.ShouldShowContinueFrom);
        Assert.Single(vm.RecentContinueItems);
        Assert.False(vm.HasRetainedDraftCandidates);
    }

    [Fact]
    public void ShouldShowContinueFrom_True_WhenOnlyDraftCandidatePresent()
    {
        using var vm = MakeEmptyTempNestViewModel();
        vm.SetContinueFromCandidates([], 1);

        Assert.True(vm.ShouldShowContinueFrom);
        Assert.False(vm.HasRecentContinueItems);
        Assert.True(vm.HasRetainedDraftCandidates);
    }

    [Fact]
    public void ShouldShowContinueFrom_True_WhenBothRecentAndDraftPresent()
    {
        using var vm = MakeEmptyTempNestViewModel();
        vm.SetContinueFromCandidates(["C:\\a.nestsuite", "C:\\b.chatnest"], 2);

        Assert.True(vm.ShouldShowContinueFrom);
        Assert.Equal(2, vm.RecentContinueItems.Count);
        Assert.Equal("保存されていない下書きが2件あります。次回起動時に確認できます。", vm.RetainedDraftCountMessage);
    }

    [Fact]
    public void RetainedDraftCountMessage_UsesSingularWording_ForOneDraft()
    {
        using var vm = MakeEmptyTempNestViewModel();
        vm.SetContinueFromCandidates([], 1);

        Assert.Equal("保存されていない下書きが1件あります。次回起動時に確認できます。", vm.RetainedDraftCountMessage);
    }

    [Fact]
    public void RecentContinueItems_PreservesGivenOrder_AndExposesFileNameOnly()
    {
        using var vm = MakeEmptyTempNestViewModel();
        vm.SetContinueFromCandidates(["C:\\dir\\first.nestsuite", "C:\\dir\\second.chatnest"], 0);

        Assert.Equal("first.nestsuite", vm.RecentContinueItems[0].FileName);
        Assert.Equal("second.chatnest", vm.RecentContinueItems[1].FileName);
        Assert.Equal("C:\\dir\\first.nestsuite", vm.RecentContinueItems[0].FilePath);
        Assert.Equal("", vm.RecentContinueItems[0].LeadingSeparator);
        Assert.Equal(" ・ ", vm.RecentContinueItems[1].LeadingSeparator);
    }

    [Fact]
    public void RecentContinueItems_AutomationIdsAreSequential()
    {
        using var vm = MakeEmptyTempNestViewModel();
        vm.SetContinueFromCandidates(["C:\\a.nestsuite", "C:\\b.nestsuite", "C:\\c.nestsuite"], 0);

        Assert.Equal("TempNest.ContinueFrom.Recent1", vm.RecentContinueItems[0].AutomationId);
        Assert.Equal("TempNest.ContinueFrom.Recent2", vm.RecentContinueItems[1].AutomationId);
        Assert.Equal("TempNest.ContinueFrom.Recent3", vm.RecentContinueItems[2].AutomationId);
    }

    [Fact]
    public void OpenCommand_InvokesOpenContinueFromRecentRequested_WithFilePath()
    {
        using var vm = MakeEmptyTempNestViewModel();
        string? opened = null;
        vm.OpenContinueFromRecentRequested = path => opened = path;
        vm.SetContinueFromCandidates(["C:\\dir\\file.nestsuite"], 0);

        vm.RecentContinueItems[0].OpenCommand.Execute(null);

        Assert.Equal("C:\\dir\\file.nestsuite", opened);
    }

    [Fact]
    public void RemoveRecentContinueItem_RemovesMatchingPath_AndKeepsOthers()
    {
        using var vm = MakeEmptyTempNestViewModel();
        vm.SetContinueFromCandidates(["C:\\a.nestsuite", "C:\\b.nestsuite"], 0);

        vm.RemoveRecentContinueItem("C:\\a.nestsuite");

        Assert.Single(vm.RecentContinueItems);
        Assert.Equal("C:\\b.nestsuite", vm.RecentContinueItems[0].FilePath);
    }

    [Fact]
    public void RemoveRecentContinueItem_FallsBackToAT5_WhenLastItemRemoved_AndNoDraftCandidates()
    {
        using var vm = MakeEmptyTempNestViewModel();
        vm.SetContinueFromCandidates(["C:\\a.nestsuite"], 0);
        Assert.True(vm.ShouldShowContinueFrom);
        Assert.False(vm.ShouldShowGettingStartedHint);

        vm.RemoveRecentContinueItem("C:\\a.nestsuite");

        Assert.False(vm.ShouldShowContinueFrom);
        Assert.True(vm.ShouldShowGettingStartedHint);
    }

    [Fact]
    public void MarkContinueFromDismissed_MakesShouldShowFalse_EvenWithCandidates()
    {
        using var vm = MakeEmptyTempNestViewModel();
        vm.SetContinueFromCandidates(["C:\\a.nestsuite"], 1);

        vm.MarkContinueFromDismissed();

        Assert.False(vm.ShouldShowContinueFrom);
    }

    [Fact]
    public void MarkContinueFromDismissed_IsIdempotent()
    {
        using var vm = MakeEmptyTempNestViewModel();
        vm.SetContinueFromCandidates(["C:\\a.nestsuite"], 0);

        vm.MarkContinueFromDismissed();
        vm.MarkContinueFromDismissed();

        Assert.False(vm.ShouldShowContinueFrom);
    }

    [Fact]
    public void MarkContinueFromDismissed_DoesNotResetOnSlotInput_PhaseOnePolicy()
    {
        // フェーズ1方針: TempNestへの入力（OnSlotChangedはAT-5のみをdismissする）では
        // SH-40は抑止されない。MarkContinueFromDismissedを明示的に呼んだ場合だけ抑止される。
        using var vm = MakeEmptyTempNestViewModel();
        vm.SetContinueFromCandidates(["C:\\a.nestsuite"], 0);

        vm.Slot1.Body = "入力してみる";

        Assert.True(vm.ShouldShowContinueFrom);
    }

    // ── AT-5との排他 ──────────────────────────────────────────────────────

    [Fact]
    public void AT5_NotShown_WhenContinueFromHasCandidates_EvenIfSlotsAreEmpty()
    {
        using var vm = MakeEmptyTempNestViewModel();
        Assert.True(vm.ShouldShowGettingStartedHint); // 候補設定前はAT-5が表示される

        vm.SetContinueFromCandidates(["C:\\a.nestsuite"], 0);

        Assert.True(vm.IsCompletelyEmpty);
        Assert.False(vm.ShouldShowGettingStartedHint);
        Assert.True(vm.ShouldShowContinueFrom);
    }

    [Fact]
    public void AT5_StillShown_WhenContinueFromHasNoCandidates()
    {
        using var vm = MakeEmptyTempNestViewModel();
        vm.SetContinueFromCandidates([], 0);

        Assert.True(vm.ShouldShowGettingStartedHint);
        Assert.False(vm.ShouldShowContinueFrom);
    }

    [Fact]
    public void AT5_And_ContinueFrom_AreNeverBothTrue()
    {
        using var vm = MakeEmptyTempNestViewModel();

        foreach (var (recent, draft) in new[] { (0, 0), (1, 0), (0, 1), (2, 3) })
        {
            var recentPaths = new List<string>();
            for (var i = 0; i < recent; i++) recentPaths.Add($"C:\\file{i}.nestsuite");
            vm.SetContinueFromCandidates(recentPaths, draft);

            Assert.False(vm.ShouldShowGettingStartedHint && vm.ShouldShowContinueFrom);
        }
    }

    [Fact]
    public void ExistingAT5AutoDismiss_ViaSlotInput_StillWorks_WhenNoContinueFromCandidates()
    {
        // 既存AT-5テストの回帰確認（SH-40導入後もAT-5自身の判定は変更していないこと）。
        using var vm = MakeEmptyTempNestViewModel();
        vm.Slot2.Title = "テスト";

        Assert.False(vm.ShouldShowGettingStartedHint);
    }

    // ── 静的UI契約 ────────────────────────────────────────────────────────

    private static string ReadTempNestWorkspaceViewXaml()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "NestSuite.sln")))
            dir = dir.Parent;
        var root = dir?.FullName ?? throw new DirectoryNotFoundException("Repo root not found.");
        return File.ReadAllText(Path.Combine(root, "NestSuite", "NestSuite", "TempNest", "TempNestWorkspaceView.xaml"));
    }

    [Fact]
    public void Xaml_ContinueFromPanel_BoundToShouldShowContinueFrom()
    {
        var xaml = ReadTempNestWorkspaceViewXaml();
        Assert.Contains("AutomationId=\"TempNest.ContinueFrom\"", xaml);
        Assert.Contains("Visibility=\"{Binding ShouldShowContinueFrom, Converter={StaticResource BoolToVis}}\"", xaml);
    }

    [Fact]
    public void Xaml_ContinueFromPanel_RecentLinksBoundToOpenCommand_WithToolTipAndAutomationName()
    {
        var xaml = ReadTempNestWorkspaceViewXaml();
        Assert.Contains("Command=\"{Binding OpenCommand}\"", xaml);
        Assert.Contains("ToolTip=\"{Binding FilePath}\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"{Binding AutomationName}\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"{Binding AutomationId}\"", xaml);
    }

    [Fact]
    public void Xaml_ContinueFromPanel_DraftHint_HasAutomationNameAndIsNotFocusable()
    {
        var xaml = ReadTempNestWorkspaceViewXaml();
        Assert.Contains("AutomationId=\"TempNest.ContinueFrom.DraftHint\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"{Binding RetainedDraftCountMessage}\"", xaml);
    }

    [Fact]
    public void Xaml_ContinueFromPanel_HasNoCloseButtonOrDismissLink()
    {
        var xaml = ReadTempNestWorkspaceViewXaml();
        var idx = xaml.IndexOf("TempNest.ContinueFrom\"", StringComparison.Ordinal);
        var end = xaml.IndexOf("<!-- Slot 1", idx, StringComparison.Ordinal);
        var block = xaml.Substring(idx, end - idx);

        Assert.DoesNotContain("閉じる", block);
        Assert.DoesNotContain("今後表示しない", block);
        Assert.DoesNotContain("×", block);
    }

    [Fact]
    public void Xaml_ContinueFromPanel_UsesOnlyExistingDynamicResources_NoNewFixedColors()
    {
        var xaml = ReadTempNestWorkspaceViewXaml();
        var idx = xaml.IndexOf("ContinueFromLinkButtonStyle\" TargetType", StringComparison.Ordinal);
        var end = xaml.IndexOf("</Style>", idx, StringComparison.Ordinal);
        var block = xaml.Substring(idx, end - idx);

        Assert.DoesNotContain("Color=\"#", block);
        Assert.Contains("DynamicResource AccentBrush", block);
        Assert.Contains("DynamicResource HoverBackgroundBrush", block);
    }

    [Fact]
    public void Xaml_TempNest_StillHasAllFourSlots_AfterAddingContinueFromRow()
    {
        var xaml = ReadTempNestWorkspaceViewXaml();
        for (var i = 1; i <= 4; i++)
        {
            Assert.Contains($"TempNest.Slot{i}.TitleBox", xaml);
            Assert.Contains($"TempNest.Slot{i}.BodyBox", xaml);
        }
    }

    [Fact]
    public void Xaml_ContinueFromPanel_RecentListLimitsToItemsControlWithoutHorizontalScroll()
    {
        var xaml = ReadTempNestWorkspaceViewXaml();
        Assert.Contains("AutomationId=\"TempNest.ContinueFrom.RecentList\"", xaml);
        Assert.DoesNotContain("ScrollViewer.HorizontalScrollBarVisibility=\"Visible\"", xaml);
        Assert.DoesNotContain("ScrollViewer.HorizontalScrollBarVisibility=\"Auto\"", xaml);
    }
}
