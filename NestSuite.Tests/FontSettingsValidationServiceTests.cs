using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

// v2.8.7: TD-17 — FontSettingsDialog validation logic tests.
public class FontSettingsValidationServiceTests
{
    // ── Constants ────────────────────────────────────────────────────────────

    [Fact]
    public void MinFontSize_Is6()
    {
        Assert.Equal(6, FontSettingsValidationService.MinFontSize);
    }

    [Fact]
    public void MaxFontSize_Is72()
    {
        Assert.Equal(72, FontSettingsValidationService.MaxFontSize);
    }

    // ── IsFontSizeInRange ────────────────────────────────────────────────────

    [Theory]
    [InlineData(6,  true)]
    [InlineData(12, true)]
    [InlineData(72, true)]
    [InlineData(5,  false)]
    [InlineData(73, false)]
    [InlineData(0,  false)]
    public void IsFontSizeInRange_BoundaryAndMid(double size, bool expected)
    {
        Assert.Equal(expected, FontSettingsValidationService.IsFontSizeInRange(size));
    }

    // ── ValidateFontSize ─────────────────────────────────────────────────────

    [Fact]
    public void ValidateFontSize_ValidSize_ReturnsTrueWithParsedValue()
    {
        Assert.True(FontSettingsValidationService.ValidateFontSize("14", out var size));
        Assert.Equal(14, size);
    }

    [Fact]
    public void ValidateFontSize_MinSize_ReturnsTrue()
    {
        Assert.True(FontSettingsValidationService.ValidateFontSize("6", out _));
    }

    [Fact]
    public void ValidateFontSize_MaxSize_ReturnsTrue()
    {
        Assert.True(FontSettingsValidationService.ValidateFontSize("72", out _));
    }

    [Fact]
    public void ValidateFontSize_BelowMin_ReturnsFalse()
    {
        Assert.False(FontSettingsValidationService.ValidateFontSize("5", out _));
    }

    [Fact]
    public void ValidateFontSize_AboveMax_ReturnsFalse()
    {
        Assert.False(FontSettingsValidationService.ValidateFontSize("73", out _));
    }

    [Fact]
    public void ValidateFontSize_NonNumericText_ReturnsFalse()
    {
        Assert.False(FontSettingsValidationService.ValidateFontSize("abc", out _));
    }

    [Fact]
    public void ValidateFontSize_EmptyText_ReturnsFalse()
    {
        Assert.False(FontSettingsValidationService.ValidateFontSize("", out _));
    }

    [Fact]
    public void ValidateFontSize_NullText_ReturnsFalse()
    {
        Assert.False(FontSettingsValidationService.ValidateFontSize(null, out _));
    }
}
