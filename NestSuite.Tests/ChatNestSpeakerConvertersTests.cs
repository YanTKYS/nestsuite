using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using NestSuite.ChatNest;
using Xunit;

namespace NestSuite.Tests;

public class ChatNestSpeakerConvertersTests
{
    [Theory]
    [InlineData(Speaker.自分, 232, 240, 254)]
    [InlineData(Speaker.反論, 252, 228, 236)]
    [InlineData(Speaker.補足, 232, 245, 233)]
    [InlineData(Speaker.結論, 255, 248, 225)]
    public void BackgroundConverter_ReturnsFrozenSharedBrush(Speaker speaker, byte red, byte green, byte blue)
    {
        var converter = new SpeakerBackgroundConverter();

        var first = Assert.IsAssignableFrom<SolidColorBrush>(converter.Convert(speaker, typeof(Brush), null!, CultureInfo.InvariantCulture));
        var second = converter.Convert(speaker, typeof(Brush), null!, CultureInfo.InvariantCulture);

        Assert.Same(first, second);
        Assert.True(first.IsFrozen);
        Assert.Equal(Color.FromRgb(red, green, blue), first.Color);
    }

    [Theory]
    [InlineData(Speaker.自分, 25, 118, 210)]
    [InlineData(Speaker.反論, 198, 40, 40)]
    [InlineData(Speaker.補足, 46, 125, 50)]
    [InlineData(Speaker.結論, 230, 119, 0)]
    public void AccentConverter_ReturnsFrozenSharedBrush(Speaker speaker, byte red, byte green, byte blue)
    {
        var converter = new SpeakerAccentConverter();

        var first = Assert.IsAssignableFrom<SolidColorBrush>(converter.Convert(speaker, typeof(Brush), null!, CultureInfo.InvariantCulture));
        var second = converter.Convert(speaker, typeof(Brush), null!, CultureInfo.InvariantCulture);

        Assert.Same(first, second);
        Assert.True(first.IsFrozen);
        Assert.Equal(Color.FromRgb(red, green, blue), first.Color);
    }

    [Fact]
    public void AlignmentConverter_RightAlignsOnlyOwnSpeaker()
    {
        var converter = new SpeakerAlignmentConverter();

        Assert.Equal(HorizontalAlignment.Right, converter.Convert(Speaker.自分, typeof(HorizontalAlignment), null!, CultureInfo.InvariantCulture));
        Assert.Equal(HorizontalAlignment.Left, converter.Convert(Speaker.反論, typeof(HorizontalAlignment), null!, CultureInfo.InvariantCulture));
        Assert.Equal(HorizontalAlignment.Left, converter.Convert(null!, typeof(HorizontalAlignment), null!, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(typeof(SpeakerBackgroundConverter))]
    [InlineData(typeof(SpeakerAccentConverter))]
    [InlineData(typeof(SpeakerAlignmentConverter))]
    public void ConvertBack_ReturnsBindingDoNothing(Type converterType)
    {
        var converter = Assert.IsAssignableFrom<IValueConverter>(Activator.CreateInstance(converterType));

        Assert.Same(Binding.DoNothing, converter.ConvertBack(null!, typeof(object), null!, CultureInfo.InvariantCulture));
    }
}
