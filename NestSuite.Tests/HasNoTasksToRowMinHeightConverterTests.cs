using NestSuite.Converters;
using Xunit;

namespace NestSuite.Tests;

// v2.13.5 M16 フォローアップ: 右ペインのタスク行の最小高さを、既存タスクの有無に応じて
// 0（縮小可）/ 100（従来どおり）に切り替えるコンバーターの回帰。
public class HasNoTasksToRowMinHeightConverterTests
{
    private readonly HasNoTasksToRowMinHeightConverter _converter = new();

    [Fact]
    public void Convert_True_ReturnsZero()
    {
        var result = (double)_converter.Convert(true, typeof(double), null!, null!);

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void Convert_False_ReturnsHundred()
    {
        var result = (double)_converter.Convert(false, typeof(double), null!, null!);

        Assert.Equal(100.0, result);
    }
}
