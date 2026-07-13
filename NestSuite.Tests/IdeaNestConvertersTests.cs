using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using NestSuite.IdeaNest.Converters;
using Xunit;

namespace NestSuite.Tests;

// TD-77 (v2.17.9): IdeaNest Converter 群の ConvertBack 例外解消・共有 Brush 化の回帰。
public class IdeaNestConvertersTests
{
    [Fact]
    public void BoolToVisibility_Convert_TrueAndFalse()
    {
        var converter = new IdeaBoolToVisibilityConverter();

        Assert.Equal(Visibility.Visible, converter.Convert(true, typeof(Visibility), null!, null!));
        Assert.Equal(Visibility.Collapsed, converter.Convert(false, typeof(Visibility), null!, null!));
    }

    [Fact]
    public void BoolToVisibility_Invert_FlipsResult()
    {
        var converter = new IdeaBoolToVisibilityConverter { Invert = true };

        Assert.Equal(Visibility.Collapsed, converter.Convert(true, typeof(Visibility), null!, null!));
        Assert.Equal(Visibility.Visible, converter.Convert(false, typeof(Visibility), null!, null!));
    }

    [Fact]
    public void BoolToVisibility_ConvertBack_ReturnsBindingDoNothing()
    {
        var converter = new IdeaBoolToVisibilityConverter();

        Assert.Same(Binding.DoNothing, converter.ConvertBack(null!, typeof(bool), null!, null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StringIsEmptyToVisibility_Convert_EmptyOrWhitespace_ReturnsCollapsed(string? value)
    {
        var converter = new IdeaStringIsEmptyToVisibilityConverter();

        Assert.Equal(Visibility.Collapsed, converter.Convert(value!, typeof(Visibility), null!, null!));
    }

    [Fact]
    public void StringIsEmptyToVisibility_Convert_NonEmpty_ReturnsVisible()
    {
        var converter = new IdeaStringIsEmptyToVisibilityConverter();

        Assert.Equal(Visibility.Visible, converter.Convert("text", typeof(Visibility), null!, null!));
    }

    [Fact]
    public void StringIsEmptyToVisibility_ConvertBack_ReturnsBindingDoNothing()
    {
        var converter = new IdeaStringIsEmptyToVisibilityConverter();

        Assert.Same(Binding.DoNothing, converter.ConvertBack(null!, typeof(string), null!, null!));
    }

    [Theory]
    [InlineData("yellow", "#FFF7CC")]
    [InlineData("pink", "#FCE7F3")]
    [InlineData("blue", "#DBEAFE")]
    [InlineData("green", "#DCFCE7")]
    [InlineData("purple", "#EDE9FE")]
    [InlineData("orange", "#FFEDD5")]
    [InlineData("gray", "#F1F3F5")]
    [InlineData("white", "#FFFFFF")]
    [InlineData("unknown-name", "#FFFFFF")]
    public void ColorNameToBrush_Convert_ReturnsFrozenSharedBrush(string name, string expectedHex)
    {
        var converter = new IdeaColorNameToBrushConverter();
        var expectedColor = (Color)ColorConverter.ConvertFromString(expectedHex);

        var first = Assert.IsAssignableFrom<SolidColorBrush>(converter.Convert(name, typeof(Brush), null!, null!));
        var second = converter.Convert(name, typeof(Brush), null!, null!);

        Assert.Same(first, second);
        Assert.True(first.IsFrozen);
        Assert.Equal(expectedColor, first.Color);
    }

    [Fact]
    public void ColorNameToBrush_ConvertBack_ReturnsBindingDoNothing()
    {
        var converter = new IdeaColorNameToBrushConverter();

        Assert.Same(Binding.DoNothing, converter.ConvertBack(null!, typeof(string), null!, null!));
    }

    [Fact]
    public void HexStringToBrush_Convert_ValidHex_ReturnsFrozenBrushWithColor()
    {
        var converter = new IdeaHexStringToBrushConverter();

        var result = Assert.IsAssignableFrom<SolidColorBrush>(converter.Convert("#112233", typeof(Brush), null!, null!));

        Assert.True(result.IsFrozen);
        Assert.Equal(Color.FromRgb(0x11, 0x22, 0x33), result.Color);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-color")]
    public void HexStringToBrush_Convert_InvalidOrMissing_ReturnsLightYellowFallback(string? value)
    {
        var converter = new IdeaHexStringToBrushConverter();

        Assert.Same(Brushes.LightYellow, converter.Convert(value!, typeof(Brush), null!, null!));
    }

    [Fact]
    public void HexStringToBrush_ConvertBack_ReturnsBindingDoNothing()
    {
        var converter = new IdeaHexStringToBrushConverter();

        Assert.Same(Binding.DoNothing, converter.ConvertBack(null!, typeof(string), null!, null!));
    }

    [Fact]
    public void BodyTrim_Convert_ShortBody_ReturnsBodyUnchanged()
    {
        var converter = new IdeaBodyTrimConverter();

        var result = converter.Convert(new object[] { "line1\nline2", 3 }, typeof(string), null!, null!);

        Assert.Equal("line1\nline2", result);
    }

    [Fact]
    public void BodyTrim_Convert_LongBody_TrimsWithEllipsis()
    {
        var converter = new IdeaBodyTrimConverter();

        var result = converter.Convert(new object[] { "line1\nline2\nline3\nline4", 2 }, typeof(string), null!, null!);

        Assert.Equal("line1\nline2…", result);
    }

    [Fact]
    public void BodyTrim_Convert_InvalidMaxLines_ReturnsBodyAsIs()
    {
        var converter = new IdeaBodyTrimConverter();

        var result = converter.Convert(new object[] { "line1\nline2", 0 }, typeof(string), null!, null!);

        Assert.Equal("line1\nline2", result);
    }

    [Fact]
    public void BodyTrim_ConvertBack_ReturnsBindingDoNothingForEachSource()
    {
        var converter = new IdeaBodyTrimConverter();

        var result = converter.ConvertBack(null!, new[] { typeof(string), typeof(int) }, null!, null!);

        Assert.Equal(2, result.Length);
        Assert.All(result, v => Assert.Same(Binding.DoNothing, v));
    }
}
