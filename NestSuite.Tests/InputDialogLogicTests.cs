using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

// v2.8.7: TD-17 — InputDialog behavioral contract tests.
// InputDialog accepts any text as-is: no trimming, no validation, empty string allowed.
public class InputDialogLogicTests
{
    [Fact]
    public void ProcessInput_NormalText_IsReturnedAsIs()
    {
        Assert.Equal("Hello World", InputDialogLogic.ProcessInput("Hello World"));
    }

    [Fact]
    public void ProcessInput_LeadingWhitespace_IsNotTrimmed()
    {
        Assert.Equal("  text", InputDialogLogic.ProcessInput("  text"));
    }

    [Fact]
    public void ProcessInput_TrailingWhitespace_IsNotTrimmed()
    {
        Assert.Equal("text  ", InputDialogLogic.ProcessInput("text  "));
    }

    [Fact]
    public void ProcessInput_EmptyString_IsAccepted()
    {
        Assert.Equal("", InputDialogLogic.ProcessInput(""));
    }

    [Fact]
    public void IsAcceptable_NonNullText_ReturnsTrue()
    {
        Assert.True(InputDialogLogic.IsAcceptable(""));
        Assert.True(InputDialogLogic.IsAcceptable("any text"));
    }

    [Fact]
    public void IsAcceptable_NullText_ReturnsFalse()
    {
        Assert.False(InputDialogLogic.IsAcceptable(null));
    }
}
