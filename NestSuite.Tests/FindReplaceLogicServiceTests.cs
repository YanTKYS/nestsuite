using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

// v2.8.7: TD-17 — FindReplaceDialog logic tests.
public class FindReplaceLogicServiceTests
{
    // ── ComputeMatchPositions ────────────────────────────────────────────────

    [Fact]
    public void ComputeMatchPositions_EmptyKeyword_ReturnsEmpty()
    {
        var result = FindReplaceLogicService.ComputeMatchPositions("", "some text", StringComparison.Ordinal);
        Assert.Empty(result);
    }

    [Fact]
    public void ComputeMatchPositions_NoMatch_ReturnsEmpty()
    {
        var result = FindReplaceLogicService.ComputeMatchPositions("xyz", "hello world", StringComparison.Ordinal);
        Assert.Empty(result);
    }

    [Fact]
    public void ComputeMatchPositions_SingleMatch_ReturnsPosition()
    {
        var result = FindReplaceLogicService.ComputeMatchPositions("world", "hello world", StringComparison.Ordinal);
        Assert.Single(result);
        Assert.Equal(6, result[0]);
    }

    [Fact]
    public void ComputeMatchPositions_MultipleMatches_ReturnsAllPositions()
    {
        var result = FindReplaceLogicService.ComputeMatchPositions("ab", "ababab", StringComparison.Ordinal);
        Assert.Equal(new[] { 0, 2, 4 }, result);
    }

    [Fact]
    public void ComputeMatchPositions_CaseSensitive_DoesNotMatchDifferentCase()
    {
        var result = FindReplaceLogicService.ComputeMatchPositions("ABC", "abc ABC", StringComparison.Ordinal);
        Assert.Single(result);
        Assert.Equal(4, result[0]);
    }

    [Fact]
    public void ComputeMatchPositions_CaseInsensitive_MatchesDifferentCase()
    {
        var result = FindReplaceLogicService.ComputeMatchPositions("abc", "ABC abc", StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, result.Count);
        Assert.Equal(0, result[0]);
        Assert.Equal(4, result[1]);
    }

    [Fact]
    public void ComputeMatchPositions_OverlappingPattern_AdvancesOneCharEachTime()
    {
        // "aaa" with keyword "aa": overlapping matches at positions 0 and 1
        var result = FindReplaceLogicService.ComputeMatchPositions("aa", "aaa", StringComparison.Ordinal);
        Assert.Equal(new[] { 0, 1 }, result);
    }

    // ── AdvanceForward ───────────────────────────────────────────────────────

    [Fact]
    public void AdvanceForward_NotAtEnd_AdvancesWithoutWrapping()
    {
        var (next, wrapped) = FindReplaceLogicService.AdvanceForward(1, 5);
        Assert.Equal(2, next);
        Assert.False(wrapped);
    }

    [Fact]
    public void AdvanceForward_AtEnd_WrapsToFirst()
    {
        var (next, wrapped) = FindReplaceLogicService.AdvanceForward(4, 5);
        Assert.Equal(0, next);
        Assert.True(wrapped);
    }

    [Fact]
    public void AdvanceForward_EmptyCount_ReturnsNegativeOne()
    {
        var (next, wrapped) = FindReplaceLogicService.AdvanceForward(0, 0);
        Assert.Equal(-1, next);
        Assert.False(wrapped);
    }

    // ── AdvanceBackward ──────────────────────────────────────────────────────

    [Fact]
    public void AdvanceBackward_NotAtStart_RetreatsWithoutWrapping()
    {
        var (prev, wrapped) = FindReplaceLogicService.AdvanceBackward(3, 5);
        Assert.Equal(2, prev);
        Assert.False(wrapped);
    }

    [Fact]
    public void AdvanceBackward_AtStart_WrapsToLast()
    {
        var (prev, wrapped) = FindReplaceLogicService.AdvanceBackward(0, 5);
        Assert.Equal(4, prev);
        Assert.True(wrapped);
    }

    [Fact]
    public void AdvanceBackward_EmptyCount_ReturnsNegativeOne()
    {
        var (prev, wrapped) = FindReplaceLogicService.AdvanceBackward(0, 0);
        Assert.Equal(-1, prev);
        Assert.False(wrapped);
    }

    // ── ReplaceAll ───────────────────────────────────────────────────────────

    [Fact]
    public void ReplaceAll_EmptyKeyword_ReturnsOriginalText()
    {
        var result = FindReplaceLogicService.ReplaceAll("hello world", "", "X", StringComparison.Ordinal);
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void ReplaceAll_CaseSensitive_OnlyReplacesExactCase()
    {
        var result = FindReplaceLogicService.ReplaceAll("abc ABC abc", "abc", "X", StringComparison.Ordinal);
        Assert.Equal("X ABC X", result);
    }

    [Fact]
    public void ReplaceAll_CaseInsensitive_ReplacesAnyCase()
    {
        var result = FindReplaceLogicService.ReplaceAll("abc ABC abc", "abc", "X", StringComparison.OrdinalIgnoreCase);
        Assert.Equal("X X X", result);
    }

    [Fact]
    public void ReplaceAll_EmptyReplacement_DeletesKeyword()
    {
        var result = FindReplaceLogicService.ReplaceAll("aXbXc", "X", "", StringComparison.Ordinal);
        Assert.Equal("abc", result);
    }

    // ── BuildMatchContext ────────────────────────────────────────────────────

    [Fact]
    public void BuildMatchContext_AtStart_NoLeadingEllipsis()
    {
        var result = FindReplaceLogicService.BuildMatchContext("hello world", 0, "hello");
        Assert.StartsWith("hello", result);
        Assert.DoesNotContain("…", result.TrimEnd('…'));
    }

    [Fact]
    public void BuildMatchContext_AtFarMiddle_HasEllipses()
    {
        var longText = new string('a', 40) + "TARGET" + new string('b', 40);
        var matchStart = 40;
        var result = FindReplaceLogicService.BuildMatchContext(longText, matchStart, "TARGET");
        Assert.StartsWith("…", result);
        Assert.EndsWith("…", result);
        Assert.Contains("TARGET", result);
    }

    [Fact]
    public void BuildMatchContext_NewlinesReplacedWithSpaces()
    {
        var result = FindReplaceLogicService.BuildMatchContext("line1\nTARGET\nline3", 6, "TARGET");
        Assert.DoesNotContain('\n', result);
        Assert.Contains("TARGET", result);
    }
}
