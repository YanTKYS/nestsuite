using System.Windows;
using NestSuite.Converters;
using Xunit;

namespace NestSuite.Tests;

// v2.13.5 M16 フォローアップ: 右ペインのタスク行を、既存タスクの有無に応じて
// Auto（ヒント文の高さのみ）/ 2*（従来どおり）に切り替えるコンバーターの回帰。
public class HasNoTasksToRowHeightConverterTests
{
    private readonly HasNoTasksToRowHeightConverter _converter = new();

    [Fact]
    public void Convert_True_ReturnsAutoGridLength()
    {
        var result = (GridLength)_converter.Convert(true, typeof(GridLength), null!, null!);

        Assert.True(result.IsAuto);
    }

    [Fact]
    public void Convert_False_ReturnsTwoStarGridLength()
    {
        var result = (GridLength)_converter.Convert(false, typeof(GridLength), null!, null!);

        Assert.True(result.IsStar);
        Assert.Equal(2, result.Value);
    }
}
