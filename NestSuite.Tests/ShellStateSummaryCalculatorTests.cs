using NestSuite.Services;
using NestSuite.TempNest;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// SH-37 (v2.18.3): 「現在の状態」サマリーの集計ロジックのうち、Shell の内部タブ・
/// セッション管理から独立している <see cref="ShellStateSummaryCalculator"/> を確認する。
/// Shell 自体（タブ数・未保存判定・pending restore）は既存 Shell ロジックをそのまま
/// 再利用するだけであり、NestSuiteShellWindow を直接構築する単体テストは行わない
/// （既存コードベースの慣習と同様、実機確認で担保する）。
/// </summary>
public class ShellStateSummaryCalculatorTests
{
    // ── CountNonEmptyTempNestSlots ────────────────────────────────────────

    [Fact]
    public void CountNonEmptyTempNestSlots_CountsSlotWithBody()
    {
        var slots = new[] { new TempNestSlotViewModel { Body = "本文あり" } };
        Assert.Equal(1, ShellStateSummaryCalculator.CountNonEmptyTempNestSlots(slots));
    }

    [Fact]
    public void CountNonEmptyTempNestSlots_DoesNotCountEmptyBody()
    {
        var slots = new[] { new TempNestSlotViewModel { Body = "" } };
        Assert.Equal(0, ShellStateSummaryCalculator.CountNonEmptyTempNestSlots(slots));
    }

    [Fact]
    public void CountNonEmptyTempNestSlots_DoesNotCountWhitespaceOnlyBody()
    {
        var slots = new[] { new TempNestSlotViewModel { Body = "   \r\n\t " } };
        Assert.Equal(0, ShellStateSummaryCalculator.CountNonEmptyTempNestSlots(slots));
    }

    [Fact]
    public void CountNonEmptyTempNestSlots_TitleOnly_DoesNotCount()
    {
        var slots = new[] { new TempNestSlotViewModel { Title = "タイトルだけ", Body = "" } };
        Assert.Equal(0, ShellStateSummaryCalculator.CountNonEmptyTempNestSlots(slots));
    }

    [Fact]
    public void CountNonEmptyTempNestSlots_AllFourSlotsWithBody_CountsFour()
    {
        var slots = new[]
        {
            new TempNestSlotViewModel { Body = "1" },
            new TempNestSlotViewModel { Body = "2" },
            new TempNestSlotViewModel { Body = "3" },
            new TempNestSlotViewModel { Body = "4" },
        };
        Assert.Equal(4, ShellStateSummaryCalculator.CountNonEmptyTempNestSlots(slots));
    }

    [Fact]
    public void CountNonEmptyTempNestSlots_MixedSlots_CountsOnlyNonEmpty()
    {
        var slots = new[]
        {
            new TempNestSlotViewModel { Body = "入力あり" },
            new TempNestSlotViewModel { Body = "" },
            new TempNestSlotViewModel { Title = "タイトルのみ", Body = "" },
            new TempNestSlotViewModel { Body = "   " },
        };
        Assert.Equal(1, ShellStateSummaryCalculator.CountNonEmptyTempNestSlots(slots));
    }

    [Fact]
    public void CountNonEmptyTempNestSlots_NoSlots_ReturnsZero()
    {
        Assert.Equal(0, ShellStateSummaryCalculator.CountNonEmptyTempNestSlots(Array.Empty<TempNestSlotViewModel>()));
    }

    // ── CountDraftRecoveryCandidates ──────────────────────────────────────

    private static string NewDraftPath() => $"draft-{Guid.NewGuid():N}.nestsuite";

    [Fact]
    public void CountDraftRecoveryCandidates_NoDraftFiles_ReturnsZero()
    {
        var count = ShellStateSummaryCalculator.CountDraftRecoveryCandidates(
            Array.Empty<string>(), Array.Empty<string>());
        Assert.Equal(0, count);
    }

    [Fact]
    public void CountDraftRecoveryCandidates_DraftNotMatchingAnyOpenTab_CountsAsOne()
    {
        var draft = NewDraftPath();
        var count = ShellStateSummaryCalculator.CountDraftRecoveryCandidates(
            new[] { draft }, Array.Empty<string>());
        Assert.Equal(1, count);
    }

    [Fact]
    public void CountDraftRecoveryCandidates_DraftMatchingOpenTab_ExcludedFromCount()
    {
        var tabId = Guid.NewGuid().ToString("N");
        var draft = $"draft-{tabId}.nestsuite";

        var count = ShellStateSummaryCalculator.CountDraftRecoveryCandidates(
            new[] { draft }, new[] { tabId });

        Assert.Equal(0, count);
    }

    [Fact]
    public void CountDraftRecoveryCandidates_MultipleDrafts_CountsOnlyOrphaned()
    {
        var openTabId = Guid.NewGuid().ToString("N");
        var ownDraft = $"draft-{openTabId}.nestsuite";
        var orphan1 = NewDraftPath();
        var orphan2 = NewDraftPath();

        var count = ShellStateSummaryCalculator.CountDraftRecoveryCandidates(
            new[] { ownDraft, orphan1, orphan2 }, new[] { openTabId });

        Assert.Equal(2, count);
    }

    [Fact]
    public void CountDraftRecoveryCandidates_MalformedPath_IsIgnoredWithoutThrowing()
    {
        var count = ShellStateSummaryCalculator.CountDraftRecoveryCandidates(
            new[] { "not-a-draft-file.txt" }, Array.Empty<string>());
        Assert.Equal(0, count);
    }
}
