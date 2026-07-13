using System.Windows.Data;
using System.Windows.Media;
using NestSuite.Converters;
using Xunit;

namespace NestSuite.Tests;

// TD-77 (v2.17.9): 呼び出しごとの Brush 生成を止め、凍結済み共有 Brush を返すことの回帰。
public class WorkspaceKindToAccentBrushConverterTests
{
    private readonly WorkspaceKindToAccentBrushConverter _converter = new();

    [Theory]
    [InlineData(NestSuiteWorkspaceKind.NoteNest, 0x4A, 0x90, 0xD9)]
    [InlineData(NestSuiteWorkspaceKind.IdeaNest, 0xE8, 0xA0, 0x20)]
    [InlineData(NestSuiteWorkspaceKind.ChatNest, 0x4C, 0xAF, 0x50)]
    [InlineData(NestSuiteWorkspaceKind.Temp, 0xA0, 0xA0, 0xA8)]
    public void Convert_ReturnsFrozenSharedBrush(NestSuiteWorkspaceKind kind, byte red, byte green, byte blue)
    {
        var first = Assert.IsAssignableFrom<SolidColorBrush>(_converter.Convert(kind, typeof(Brush), null!, null!));
        var second = _converter.Convert(kind, typeof(Brush), null!, null!);

        Assert.Same(first, second);
        Assert.True(first.IsFrozen);
        Assert.Equal(Color.FromRgb(red, green, blue), first.Color);
    }

    [Fact]
    public void Convert_NonKindValue_ReturnsTransparent()
    {
        Assert.Same(Brushes.Transparent, _converter.Convert("not a kind", typeof(Brush), null!, null!));
    }

    [Fact]
    public void ConvertBack_ReturnsBindingDoNothing()
    {
        Assert.Same(Binding.DoNothing, _converter.ConvertBack(null!, typeof(NestSuiteWorkspaceKind), null!, null!));
    }
}
