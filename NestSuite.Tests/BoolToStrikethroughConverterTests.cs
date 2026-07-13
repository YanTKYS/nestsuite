using System.Windows;
using System.Windows.Data;
using NestSuite.Converters;
using Xunit;

namespace NestSuite.Tests;

// TD-77 (v2.17.9): one-way 表示専用 Converter の ConvertBack 例外解消の回帰。
public class BoolToStrikethroughConverterTests
{
    private readonly BoolToStrikethroughConverter _converter = new();

    [Fact]
    public void Convert_True_ReturnsStrikethroughDecorations()
    {
        Assert.Same(TextDecorations.Strikethrough, _converter.Convert(true, typeof(TextDecorationCollection), null!, null!));
    }

    [Fact]
    public void Convert_False_ReturnsNull()
    {
        Assert.Null(_converter.Convert(false, typeof(TextDecorationCollection), null!, null!));
    }

    [Fact]
    public void ConvertBack_ReturnsBindingDoNothing()
    {
        Assert.Same(Binding.DoNothing, _converter.ConvertBack(null!, typeof(bool), null!, null!));
    }
}
