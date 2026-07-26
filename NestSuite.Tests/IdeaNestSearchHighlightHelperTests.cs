using System.Collections.Generic;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.Services;
using NestSuite.IdeaNest.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// ID-7: IdeaNestカード一覧のタイトル・本文プレビューハイライト用セグメント分割
/// （<see cref="IdeaNestSearchHighlightHelper"/>）のテスト。
/// M17の3-Run分割helper（<see cref="NestSuite.Services.SearchMatchSegments"/>）を
/// 繰り返し呼び出すだけの薄いラッパーであることを、複数一致・大文字小文字・日本語・
/// 特殊文字の各ケースで確認する。
/// </summary>
public class IdeaNestSearchHighlightHelperTests
{
    private static void AssertSegment(IdeaNestHighlightSegment segment, string text, bool isMatch)
    {
        Assert.Equal(text, segment.Text);
        Assert.Equal(isMatch, segment.IsMatch);
    }

    // ── 基本セグメント生成 ──────────────────────────────────────────────────

    [Fact]
    public void Split_NoQuery_ReturnsSingleNonMatchSegment()
    {
        var result = IdeaNestSearchHighlightHelper.Split("令和8年度予算の確認", "");

        Assert.Single(result);
        AssertSegment(result[0], "令和8年度予算の確認", false);
    }

    [Fact]
    public void Split_NullQuery_ReturnsSingleNonMatchSegment()
    {
        var result = IdeaNestSearchHighlightHelper.Split("本文", null);

        Assert.Single(result);
        AssertSegment(result[0], "本文", false);
    }

    [Fact]
    public void Split_NoMatch_ReturnsWholeTextAsNonMatch()
    {
        var result = IdeaNestSearchHighlightHelper.Split("令和8年度予算の確認", "契約");

        Assert.Single(result);
        AssertSegment(result[0], "令和8年度予算の確認", false);
    }

    [Fact]
    public void Split_MatchAtStart_ReturnsMatchThenRemainder()
    {
        var result = IdeaNestSearchHighlightHelper.Split("予算の確認", "予算");

        Assert.Equal(2, result.Count);
        AssertSegment(result[0], "予算", true);
        AssertSegment(result[1], "の確認", false);
    }

    [Fact]
    public void Split_MatchInMiddle_ReturnsBeforeMatchAfter()
    {
        var result = IdeaNestSearchHighlightHelper.Split("令和8年度予算の確認", "予算");

        Assert.Equal(3, result.Count);
        AssertSegment(result[0], "令和8年度", false);
        AssertSegment(result[1], "予算", true);
        AssertSegment(result[2], "の確認", false);
    }

    [Fact]
    public void Split_MatchAtEnd_ReturnsBeforeThenMatch()
    {
        var result = IdeaNestSearchHighlightHelper.Split("令和8年度予算", "予算");

        Assert.Equal(2, result.Count);
        AssertSegment(result[0], "令和8年度", false);
        AssertSegment(result[1], "予算", true);
    }

    [Fact]
    public void Split_FullTextMatch_ReturnsSingleMatchSegment()
    {
        var result = IdeaNestSearchHighlightHelper.Split("予算", "予算");

        Assert.Single(result);
        AssertSegment(result[0], "予算", true);
    }

    // ── 複数一致 ────────────────────────────────────────────────────────────

    [Fact]
    public void Split_MultipleMatches_HighlightsAllOccurrences()
    {
        var result = IdeaNestSearchHighlightHelper.Split("内容を確認し、日程も確認する", "確認");

        Assert.Equal(5, result.Count);
        AssertSegment(result[0], "内容を", false);
        AssertSegment(result[1], "確認", true);
        AssertSegment(result[2], "し、日程も", false);
        AssertSegment(result[3], "確認", true);
        AssertSegment(result[4], "する", false);
    }

    [Fact]
    public void Split_MultipleMatches_ContiguousQuery_CountsTwoMatches()
    {
        var result = IdeaNestSearchHighlightHelper.Split("確認して再確認する", "確認");

        var matchCount = 0;
        foreach (var segment in result)
            if (segment.IsMatch) matchCount++;

        Assert.Equal(2, matchCount);
    }

    [Fact]
    public void Split_OverlappingPattern_UsesNonOverlappingForwardIndexOf()
    {
        // "aaa" with query "aa": forward IndexOf finds match at 0 ("aa"), then advances past it,
        // leaving "a" with no further match. Matches the existing SearchMatchSegments.Split contract.
        var result = IdeaNestSearchHighlightHelper.Split("aaa", "aa");

        Assert.Equal(2, result.Count);
        AssertSegment(result[0], "aa", true);
        AssertSegment(result[1], "a", false);
    }

    // ── 大文字・小文字 ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("test")]
    [InlineData("TEST")]
    [InlineData("Test")]
    public void Split_CaseInsensitive_MatchesRegardlessOfCase(string body)
    {
        var result = IdeaNestSearchHighlightHelper.Split(body, "test");

        Assert.Single(result);
        AssertSegment(result[0], body, true);
    }

    // ── 日本語 ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("契約内容を確認して担当課へ連絡する", "契約")]
    [InlineData("カタカナのテストです", "テスト")]
    [InlineData("ひらがなのけんさくです", "けんさく")]
    [InlineData("全角１２３テスト", "テスト")]
    public void Split_Japanese_MatchesAsPlainSubstring(string body, string query)
    {
        var result = IdeaNestSearchHighlightHelper.Split(body, query);

        var hasMatch = false;
        foreach (var segment in result)
            if (segment.IsMatch) hasMatch = true;

        Assert.True(hasMatch);
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
    public void Split_SpecialCharacters_TreatedAsLiteralText(string body, string query)
    {
        var result = IdeaNestSearchHighlightHelper.Split(body, query);

        var matchSegments = new List<IdeaNestHighlightSegment>();
        foreach (var segment in result)
            if (segment.IsMatch) matchSegments.Add(segment);

        Assert.NotEmpty(matchSegments);
        Assert.All(matchSegments, s => Assert.Equal(query, s.Text));
    }

    // ── 空白のTrim（既存検索仕様と一致） ────────────────────────────────────

    [Fact]
    public void Split_QueryWithSurroundingWhitespace_IsTrimmedLikeExistingSearch()
    {
        var trimmed = IdeaNestSearchHighlightHelper.Split("予算の確認", "予算");
        var withWhitespace = IdeaNestSearchHighlightHelper.Split("予算の確認", "  予算  ");

        Assert.Equal(trimmed.Count, withWhitespace.Count);
        for (var i = 0; i < trimmed.Count; i++)
        {
            AssertSegment(withWhitespace[i], trimmed[i].Text, trimmed[i].IsMatch);
        }
    }

    [Fact]
    public void Split_WhitespaceOnlyQuery_TreatedAsNoQuery()
    {
        var result = IdeaNestSearchHighlightHelper.Split("予算の確認", "   ");

        Assert.Single(result);
        AssertSegment(result[0], "予算の確認", false);
    }

    // ── タイトル/本文/タグの一致組み合わせ ─────────────────────────────────

    [Fact]
    public void Split_TitleOnlyMatch_HighlightsTitleOnly()
    {
        var card = new IdeaCardViewModel(new Idea { Title = "令和8年度予算の確認", Body = "本文には一致語なし", Tags = new List<string>() });
        var filter = new FilterViewModel(() => { }, () => { });
        filter.SearchText = "予算";

        var matched = filter.Apply(new[] { card });

        Assert.Contains(card, matched);
        Assert.Contains(IdeaNestSearchHighlightHelper.Split(card.Title, "予算"), s => s.IsMatch);
        Assert.DoesNotContain(IdeaNestSearchHighlightHelper.Split(card.Body, "予算"), s => s.IsMatch);
    }

    [Fact]
    public void Split_BodyOnlyMatch_HighlightsBodyOnly()
    {
        var card = new IdeaCardViewModel(new Idea { Title = "無題", Body = "契約内容を確認して担当課へ連絡する", Tags = new List<string>() });
        var filter = new FilterViewModel(() => { }, () => { });
        filter.SearchText = "契約";

        var matched = filter.Apply(new[] { card });

        Assert.Contains(card, matched);
        Assert.DoesNotContain(IdeaNestSearchHighlightHelper.Split(card.Title, "契約"), s => s.IsMatch);
        Assert.Contains(IdeaNestSearchHighlightHelper.Split(card.Body, "契約"), s => s.IsMatch);
    }

    [Fact]
    public void Split_TitleAndBodyBothMatch_HighlightsBoth()
    {
        var card = new IdeaCardViewModel(new Idea { Title = "予算の相談", Body = "来月の予算について話す", Tags = new List<string>() });
        var filter = new FilterViewModel(() => { }, () => { });
        filter.SearchText = "予算";

        var matched = filter.Apply(new[] { card });

        Assert.Contains(card, matched);
        Assert.Contains(IdeaNestSearchHighlightHelper.Split(card.Title, "予算"), s => s.IsMatch);
        Assert.Contains(IdeaNestSearchHighlightHelper.Split(card.Body, "予算"), s => s.IsMatch);
    }

    // ── 検索解除 ────────────────────────────────────────────────────────────

    [Fact]
    public void Split_QueryClearedAfterSearch_ReturnsToNoHighlight()
    {
        var withQuery = IdeaNestSearchHighlightHelper.Split("予算の確認", "予算");
        Assert.Contains(withQuery, s => s.IsMatch);

        var cleared = IdeaNestSearchHighlightHelper.Split("予算の確認", string.Empty);
        Assert.Single(cleared);
        AssertSegment(cleared[0], "予算の確認", false);
    }

    // ── dirty・保存データへの非影響 ─────────────────────────────────────────

    [Fact]
    public void Split_DoesNotMutateCardModel()
    {
        var idea = new Idea { Title = "予算の確認", Body = "予算の話", Tags = new List<string>() };
        var beforeUpdatedAt = idea.UpdatedAt;
        var beforeTitle = idea.Title;
        var beforeBody = idea.Body;

        IdeaNestSearchHighlightHelper.Split(idea.Title, "予算");
        IdeaNestSearchHighlightHelper.Split(idea.Body, "予算");

        Assert.Equal(beforeUpdatedAt, idea.UpdatedAt);
        Assert.Equal(beforeTitle, idea.Title);
        Assert.Equal(beforeBody, idea.Body);
    }

    // ── FilterViewModelの検索仕様との整合 ──────────────────────────────────

    [Theory]
    [InlineData("令和8年度予算の確認", "", "契約", "確認")]
    public void Split_ComparisonMatchesFilterViewModelSearch(
        string title, string tagOnlyBody, string tagOnlyQuery, string titleQuery)
    {
        // FilterViewModel はタイトル・本文・タグを OrdinalIgnoreCase の Contains で判定する。
        // タグだけが一致した場合、タイトル・本文にはハイライトが出ないことを確認する。
        var filter = new FilterViewModel(() => { }, () => { });
        var card = new IdeaCardViewModel(new Idea
        {
            Title = title,
            Body = tagOnlyBody,
            Tags = new List<string> { tagOnlyQuery },
        });

        filter.SearchText = tagOnlyQuery;
        var matched = filter.Apply(new[] { card });

        Assert.Contains(card, matched);

        var titleSegments = IdeaNestSearchHighlightHelper.Split(card.Title, tagOnlyQuery);
        var bodySegments = IdeaNestSearchHighlightHelper.Split(card.Body, tagOnlyQuery);
        Assert.DoesNotContain(titleSegments, s => s.IsMatch);
        Assert.DoesNotContain(bodySegments, s => s.IsMatch);

        // タイトルに一致する検索語を使えば、通常どおりハイライトされる。
        var titleMatchSegments = IdeaNestSearchHighlightHelper.Split(card.Title, titleQuery);
        Assert.Contains(titleMatchSegments, s => s.IsMatch);
    }
}
