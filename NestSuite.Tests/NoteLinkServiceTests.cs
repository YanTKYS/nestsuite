using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

public class NoteLinkServiceTests
{
    [Fact]
    public void ExtractLinkAtCursor_ReturnsNull_WhenNoLink()
    {
        Assert.Null(NoteLinkService.ExtractLinkAtCursor("hello world", 5));
    }

    [Fact]
    public void ExtractLinkAtCursor_ReturnsTitle_WhenCaretInsideLink()
    {
        var text = "See [[My Note]] here";
        Assert.Equal("My Note", NoteLinkService.ExtractLinkAtCursor(text, 8));
    }

    [Fact]
    public void ExtractLinkAtCursor_ReturnsTitle_WhenCaretAtOpenBracket()
    {
        var text = "[[Note A]] text";
        Assert.Equal("Note A", NoteLinkService.ExtractLinkAtCursor(text, 0));
    }

    [Fact]
    public void ExtractLinkAtCursor_ReturnsTitle_WhenCaretAtCloseBracket()
    {
        var text = "[[Note A]]";
        Assert.Equal("Note A", NoteLinkService.ExtractLinkAtCursor(text, 9));
    }

    [Fact]
    public void ExtractLinkAtCursor_ReturnsNull_WhenCaretJustAfterLink()
    {
        var text = "[[Note A]] text";
        Assert.Null(NoteLinkService.ExtractLinkAtCursor(text, 10));
    }

    [Fact]
    public void ExtractLinkAtCursor_ReturnsCorrectTitle_WhenMultipleLinks()
    {
        var text = "[[Note A]] and [[Note B]]";
        Assert.Equal("Note A", NoteLinkService.ExtractLinkAtCursor(text, 4));
        Assert.Equal("Note B", NoteLinkService.ExtractLinkAtCursor(text, 19));
    }

    [Fact]
    public void ExtractAllLinks_ReturnsAllTitles()
    {
        var text = "[[Note A]] and [[Note B]] and text";
        var links = NoteLinkService.ExtractAllLinks(text).ToList();
        Assert.Equal(2, links.Count);
        Assert.Contains("Note A", links);
        Assert.Contains("Note B", links);
    }

    [Fact]
    public void ExtractAllLinks_ReturnsEmpty_WhenNoLinks()
    {
        Assert.Empty(NoteLinkService.ExtractAllLinks("no links here"));
    }

    [Fact]
    public void ExtractAllLinks_IgnoresNestedBrackets()
    {
        // [[foo[bar]]] — nested bracket should not match
        var links = NoteLinkService.ExtractAllLinks("[[foo[bar]]]").ToList();
        Assert.Empty(links);
    }

    // ── Edge cases (v2.8.6) ───────────────────────────────────────────────────

    [Fact]
    public void ExtractAllLinks_DuplicateLink_ReturnsBothOccurrences()
    {
        var links = NoteLinkService.ExtractAllLinks("[[A]] and [[A]]").ToList();
        Assert.Equal(2, links.Count);
        Assert.All(links, l => Assert.Equal("A", l));
    }

    [Fact]
    public void ExtractAllLinks_EmptyContent_ReturnsEmpty()
    {
        Assert.Empty(NoteLinkService.ExtractAllLinks(""));
    }

    [Fact]
    public void ExtractLinkAtCursor_EmptyContent_ReturnsNull()
    {
        Assert.Null(NoteLinkService.ExtractLinkAtCursor("", 0));
    }
}
