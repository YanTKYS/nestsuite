using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.7.17 TD-15: NestSuiteShellWindow.Tabs.cs の責務分割を固定する構造テスト。
/// </summary>
public class TabPartialSplitTests
{
    [Theory]
    [InlineData("NestSuite/NestSuite/NestSuiteShellWindow.TabSelection.cs")]
    [InlineData("NestSuite/NestSuite/NestSuiteShellWindow.TabLifecycle.cs")]
    [InlineData("NestSuite/NestSuite/NestSuiteShellWindow.TabClose.cs")]
    [InlineData("NestSuite/NestSuite/NestSuiteShellWindow.TabContextMenu.cs")]
    public void TabResponsibilityPartialFiles_Exist(string relativePath)
    {
        Assert.True(File.Exists(GetRepositoryPath(relativePath)), relativePath);
    }

    [Fact]
    public void TabsOverviewFile_IsKeptSmallAfterSplit()
    {
        var path = GetRepositoryPath("NestSuite/NestSuite/NestSuiteShellWindow.Tabs.cs");
        var lineCount = File.ReadAllLines(path).Length;

        Assert.InRange(lineCount, 1, 80);
    }

    private static string GetRepositoryPath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "NestSuite.sln")))
            directory = directory.Parent;

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
