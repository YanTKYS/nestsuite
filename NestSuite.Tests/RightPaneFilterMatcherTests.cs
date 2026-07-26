using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// L10: NoteNest右ペイン（マーカー・互換タスク）共通絞り込みの一致判定
/// （<see cref="RightPaneFilterMatcher"/>）のテスト。通常の大文字小文字を区別しない
/// 部分一致のみを行い、正規表現は使わないことを固定する。
/// </summary>
public class RightPaneFilterMatcherTests
{
    // ── 基本一致 ────────────────────────────────────────────────────────────

    [Fact]
    public void Matches_EmptyFilter_AlwaysTrue()
    {
        Assert.True(RightPaneFilterMatcher.Matches("何でもいい", ""));
        Assert.True(RightPaneFilterMatcher.Matches(null, ""));
        Assert.True(RightPaneFilterMatcher.Matches("何でもいい", null));
    }

    [Fact]
    public void Matches_NullOrEmptyCandidate_WithNonEmptyFilter_IsFalse()
    {
        Assert.False(RightPaneFilterMatcher.Matches(null, "予算"));
        Assert.False(RightPaneFilterMatcher.Matches("", "予算"));
    }

    [Fact]
    public void Matches_PartialMatch_IsTrue()
    {
        Assert.True(RightPaneFilterMatcher.Matches("令和8年度予算の確認", "予算"));
    }

    [Fact]
    public void Matches_NoMatch_IsFalse()
    {
        Assert.False(RightPaneFilterMatcher.Matches("令和8年度予算の確認", "契約"));
    }

    // ── 大文字・小文字 ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("test")]
    [InlineData("TEST")]
    [InlineData("Test")]
    public void Matches_CaseInsensitive(string candidate)
    {
        Assert.True(RightPaneFilterMatcher.Matches(candidate, "test"));
    }

    // ── 日本語 ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("契約内容を確認して担当課へ連絡する", "契約")]
    [InlineData("カタカナのテストです", "テスト")]
    [InlineData("ひらがなのけんさくです", "けんさく")]
    [InlineData("全角１２３テスト", "テスト")]
    public void Matches_Japanese_PlainSubstring(string candidate, string filter)
    {
        Assert.True(RightPaneFilterMatcher.Matches(candidate, filter));
    }

    // ── 特殊文字（正規表現として解釈しない） ───────────────────────────────

    [Theory]
    [InlineData("a.b.c", ".")]
    [InlineData("a*b*c", "*")]
    [InlineData("a[b]c", "[")]
    [InlineData("a[b]c", "]")]
    [InlineData("a(b)c", "(")]
    [InlineData("a(b)c", ")")]
    [InlineData(@"a\b\c", @"\")]
    public void Matches_SpecialCharacters_TreatedAsLiteralText(string candidate, string filter)
    {
        Assert.True(RightPaneFilterMatcher.Matches(candidate, filter));
    }

    // ── MatchesAny ──────────────────────────────────────────────────────────

    [Fact]
    public void MatchesAny_MatchesWhenAnyCandidateMatches()
    {
        Assert.True(RightPaneFilterMatcher.MatchesAny("予算", "無関係", "予算担当", "TODO"));
    }

    [Fact]
    public void MatchesAny_FalseWhenNoCandidateMatches()
    {
        Assert.False(RightPaneFilterMatcher.MatchesAny("予算", "無関係", "契約", "TODO"));
    }

    [Fact]
    public void MatchesAny_EmptyFilter_AlwaysTrue()
    {
        Assert.True(RightPaneFilterMatcher.MatchesAny("", "何でも"));
        Assert.True(RightPaneFilterMatcher.MatchesAny(null, "何でも"));
    }
}
