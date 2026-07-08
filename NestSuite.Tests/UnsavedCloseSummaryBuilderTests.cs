using NestSuite;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.9 SH-29: 終了時の未保存タブ件数サマリの、UI 非依存な判断・文言組み立てロジックのテスト。
/// 実 UI（MessageBox / NestSuiteShellWindow）は起動せず、ConfirmContinue に注入する
/// delegate で「続ける / やめる」を模擬する。
/// </summary>
public class UnsavedCloseSummaryBuilderTests
{
    private static UnsavedCloseTarget Note(string name) =>
        new(NestSuiteWorkspaceKind.NoteNest, name);

    private static UnsavedCloseTarget Idea(string name) =>
        new(NestSuiteWorkspaceKind.IdeaNest, name);

    private static UnsavedCloseTarget Chat(string name) =>
        new(NestSuiteWorkspaceKind.ChatNest, name);

    // ── ShouldShowSummary ───────────────────────────────────────────────

    [Fact]
    public void ShouldShowSummary_ZeroTargets_IsFalse()
    {
        Assert.False(UnsavedCloseSummaryBuilder.ShouldShowSummary(0));
    }

    [Fact]
    public void ShouldShowSummary_OneTarget_IsFalse()
    {
        Assert.False(UnsavedCloseSummaryBuilder.ShouldShowSummary(1));
    }

    [Fact]
    public void ShouldShowSummary_TwoTargets_IsTrue()
    {
        Assert.True(UnsavedCloseSummaryBuilder.ShouldShowSummary(2));
    }

    [Fact]
    public void ShouldShowSummary_ManyTargets_IsTrue()
    {
        Assert.True(UnsavedCloseSummaryBuilder.ShouldShowSummary(8));
    }

    // ── ConfirmContinue: 0 / 1 件では summary を出さない ───────────────────

    [Fact]
    public void ConfirmContinue_NoTargets_DoesNotShowSummary_ReturnsTrue()
    {
        var shown = false;

        var result = UnsavedCloseSummaryBuilder.ConfirmContinue([], _ => { shown = true; return true; });

        Assert.True(result);
        Assert.False(shown);
    }

    [Fact]
    public void ConfirmContinue_OneTarget_DoesNotShowSummary_ReturnsTrue()
    {
        var shown = false;

        var result = UnsavedCloseSummaryBuilder.ConfirmContinue(
            [Note("a.notenest")], _ => { shown = true; return true; });

        Assert.True(result);
        Assert.False(shown);
    }

    // ── ConfirmContinue: 2 件以上では summary を出す ────────────────────────

    [Fact]
    public void ConfirmContinue_TwoTargets_ShowsSummary()
    {
        var shown = false;

        UnsavedCloseSummaryBuilder.ConfirmContinue(
            [Note("a.notenest"), Idea("b.ideanest")], _ => { shown = true; return true; });

        Assert.True(shown);
    }

    [Fact]
    public void ConfirmContinue_UserContinues_ReturnsTrue_AndDoesNotThrow()
    {
        var result = UnsavedCloseSummaryBuilder.ConfirmContinue(
            [Note("a.notenest"), Idea("b.ideanest")], _ => true);

        Assert.True(result);
    }

    [Fact]
    public void ConfirmContinue_UserCancels_ReturnsFalse()
    {
        var result = UnsavedCloseSummaryBuilder.ConfirmContinue(
            [Note("a.notenest"), Idea("b.ideanest")], _ => false);

        Assert.False(result);
    }

    [Fact]
    public void ConfirmContinue_PassesBuiltMessage_ToShowSummary()
    {
        string? captured = null;

        UnsavedCloseSummaryBuilder.ConfirmContinue(
            [Note("a.notenest"), Idea("b.ideanest")],
            message => { captured = message; return true; });

        Assert.NotNull(captured);
        Assert.Equal(UnsavedCloseSummaryBuilder.BuildMessage([Note("a.notenest"), Idea("b.ideanest")]), captured);
    }

    // ── BuildMessage: 文言内容 ──────────────────────────────────────────

    [Fact]
    public void BuildMessage_ContainsCount()
    {
        var message = UnsavedCloseSummaryBuilder.BuildMessage(
            [Note("a.notenest"), Idea("b.ideanest"), Chat("c.chatnest")]);

        Assert.Contains("3 件", message);
    }

    [Fact]
    public void BuildMessage_MentionsIndividualConfirmationContinues()
    {
        var message = UnsavedCloseSummaryBuilder.BuildMessage([Note("a.notenest"), Idea("b.ideanest")]);

        Assert.Contains("順番に", message);
    }

    [Fact]
    public void BuildMessage_ListsEachTarget_WithWorkspaceKindAndName()
    {
        var message = UnsavedCloseSummaryBuilder.BuildMessage(
            [Note("事業計画.notenest"), Idea("アイデア整理.ideanest"), Chat("会話メモ.chatnest")]);

        Assert.Contains("NoteNest: 事業計画.notenest", message);
        Assert.Contains("IdeaNest: アイデア整理.ideanest", message);
        Assert.Contains("ChatNest: 会話メモ.chatnest", message);
    }

    [Fact]
    public void BuildMessage_UpToFiveTargets_ListsAllWithoutTruncation()
    {
        var targets = new[]
        {
            Note("A.notenest"), Note("B.notenest"), Idea("C.ideanest"),
            Chat("D.chatnest"), Note("E.notenest"),
        };

        var message = UnsavedCloseSummaryBuilder.BuildMessage(targets);

        Assert.Contains("A.notenest", message);
        Assert.Contains("E.notenest", message);
        Assert.DoesNotContain("ほか", message);
    }

    [Fact]
    public void BuildMessage_MoreThanFiveTargets_TruncatesWithRemainingCount()
    {
        var targets = new[]
        {
            Note("A.notenest"), Note("B.notenest"), Idea("C.ideanest"),
            Chat("D.chatnest"), Note("E.notenest"), Note("F.notenest"),
            Idea("G.ideanest"), Chat("H.chatnest"),
        };

        var message = UnsavedCloseSummaryBuilder.BuildMessage(targets);

        Assert.Contains("8 件", message);
        Assert.Contains("A.notenest", message);
        Assert.Contains("E.notenest", message);
        Assert.DoesNotContain("F.notenest", message);
        Assert.DoesNotContain("G.notenest", message);
        Assert.Contains("ほか 3 件", message);
    }

    [Fact]
    public void BuildMessage_UntitledTab_DoesNotBreakFormatting()
    {
        var message = UnsavedCloseSummaryBuilder.BuildMessage(
            [Note("無題.notenest"), Idea("IdeaNest")]);

        Assert.Contains("NoteNest: 無題.notenest", message);
        Assert.Contains("IdeaNest: IdeaNest", message);
    }
}
