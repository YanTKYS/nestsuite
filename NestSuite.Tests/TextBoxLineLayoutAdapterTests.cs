using NestSuite.NoteNest.Editor;
using Xunit;

namespace NestSuite.Tests;

public class TextBoxLineLayoutAdapterTests
{
    // ── LogicalLineStartChar ──────────────────────────────────────────────

    [Fact]
    public void LogicalLineStartChar_Line0_Returns0()
    {
        Assert.Equal(0, TextBoxLineLayoutAdapter.LogicalLineStartChar("hello\nworld", 0));
    }

    [Fact]
    public void LogicalLineStartChar_Line1_ReturnsAfterFirstNewline()
    {
        Assert.Equal(6, TextBoxLineLayoutAdapter.LogicalLineStartChar("hello\nworld", 1));
    }

    [Fact]
    public void LogicalLineStartChar_Line2_ReturnsAfterSecondNewline()
    {
        Assert.Equal(12, TextBoxLineLayoutAdapter.LogicalLineStartChar("hello\nworld\nfoo", 2));
    }

    [Fact]
    public void LogicalLineStartChar_BeyondLastLine_ReturnsMinusOne()
    {
        Assert.Equal(-1, TextBoxLineLayoutAdapter.LogicalLineStartChar("hello", 1));
    }

    [Fact]
    public void LogicalLineStartChar_EmptyText_Line0_Returns0()
    {
        Assert.Equal(0, TextBoxLineLayoutAdapter.LogicalLineStartChar("", 0));
    }

    [Fact]
    public void LogicalLineStartChar_EmptyText_Line1_ReturnsMinusOne()
    {
        Assert.Equal(-1, TextBoxLineLayoutAdapter.LogicalLineStartChar("", 1));
    }

    [Fact]
    public void LogicalLineStartChar_TrailingNewline_LastEmptyLine()
    {
        // "abc\n" → line 0 starts at 0, line 1 starts at 4 (empty)
        Assert.Equal(0, TextBoxLineLayoutAdapter.LogicalLineStartChar("abc\n", 0));
        Assert.Equal(4, TextBoxLineLayoutAdapter.LogicalLineStartChar("abc\n", 1));
    }

    [Fact]
    public void LogicalLineStartChar_SingleNewline_Line1_Returns1()
    {
        Assert.Equal(1, TextBoxLineLayoutAdapter.LogicalLineStartChar("\n", 1));
    }

    [Fact]
    public void LogicalLineStartChar_MultipleLines_AllCorrect()
    {
        var text = "a\nbb\nccc\ndddd";
        Assert.Equal(0,  TextBoxLineLayoutAdapter.LogicalLineStartChar(text, 0));
        Assert.Equal(2,  TextBoxLineLayoutAdapter.LogicalLineStartChar(text, 1));
        Assert.Equal(5,  TextBoxLineLayoutAdapter.LogicalLineStartChar(text, 2));
        Assert.Equal(9,  TextBoxLineLayoutAdapter.LogicalLineStartChar(text, 3));
        Assert.Equal(-1, TextBoxLineLayoutAdapter.LogicalLineStartChar(text, 4));
    }

    [Fact]
    public void AreLineTopsStrictlyIncreasing_AllowsEmptySequence()
    {
        Assert.True(TextBoxLineLayoutAdapter.AreLineTopsStrictlyIncreasing(Array.Empty<double>()));
    }

    [Fact]
    public void AreLineTopsStrictlyIncreasing_AcceptsVisibleLinePositionsFromMixedJapaneseNote()
    {
        var tops = new[] { 12.0, 29.5, 47.0, 64.5, 82.0, 99.5, 117.0, 134.5, 152.0, 169.5, 187.0, 204.5, 222.0 };

        Assert.True(TextBoxLineLayoutAdapter.AreLineTopsStrictlyIncreasing(tops));
    }

    [Fact]
    public void AreLineTopsStrictlyIncreasing_RejectsAdjacentDuplicateTops()
    {
        var tops = new[] { 187.0, 204.5, 204.5, 222.0 };

        Assert.False(TextBoxLineLayoutAdapter.AreLineTopsStrictlyIncreasing(tops));
    }

    [Fact]
    public void AreLineTopsStrictlyIncreasing_RejectsReversedAdjacentTops()
    {
        var tops = new[] { 187.0, 222.0, 204.5 };

        Assert.False(TextBoxLineLayoutAdapter.AreLineTopsStrictlyIncreasing(tops));
    }
}
