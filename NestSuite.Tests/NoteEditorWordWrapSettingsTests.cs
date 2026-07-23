using System.Windows;
using System.Windows.Controls;
using NestSuite.NoteNest.Editor;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.19.3 L4: bool（折り返し設定）→ WPF TextBox 設定値への変換を確認する。
/// WPF コントロールは生成しない純粋関数のみのテスト。
/// </summary>
public class NoteEditorWordWrapSettingsTests
{
    [Fact]
    public void ToTextWrapping_True_ReturnsWrap()
    {
        Assert.Equal(TextWrapping.Wrap, NoteEditorWordWrapSettings.ToTextWrapping(true));
    }

    [Fact]
    public void ToTextWrapping_False_ReturnsNoWrap()
    {
        Assert.Equal(TextWrapping.NoWrap, NoteEditorWordWrapSettings.ToTextWrapping(false));
    }

    [Fact]
    public void ToHorizontalScrollBarVisibility_True_ReturnsDisabled()
    {
        Assert.Equal(ScrollBarVisibility.Disabled, NoteEditorWordWrapSettings.ToHorizontalScrollBarVisibility(true));
    }

    [Fact]
    public void ToHorizontalScrollBarVisibility_False_ReturnsAuto()
    {
        Assert.Equal(ScrollBarVisibility.Auto, NoteEditorWordWrapSettings.ToHorizontalScrollBarVisibility(false));
    }
}
