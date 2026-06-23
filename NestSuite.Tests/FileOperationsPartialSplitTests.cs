using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.7.18 TD-16: NestSuiteShellWindow.FileOperations.cs の責務分割を固定する構造テスト。
/// </summary>
public class FileOperationsPartialSplitTests
{
    [Theory]
    [InlineData("NestSuite/NestSuite/NestSuiteShellWindow.FileOpen.cs")]
    [InlineData("NestSuite/NestSuite/NestSuiteShellWindow.FileSave.cs")]
    [InlineData("NestSuite/NestSuite/NestSuiteShellWindow.FileSaveAs.cs")]
    [InlineData("NestSuite/NestSuite/NestSuiteShellWindow.FileSaveStateSync.cs")]
    [InlineData("NestSuite/NestSuite/NestSuiteShellWindow.FileCommands.cs")]
    public void FileOperationResponsibilityPartialFiles_Exist(string relativePath)
    {
        Assert.True(File.Exists(GetRepositoryPath(relativePath)), relativePath);
    }

    [Fact]
    public void FileOperationsOverviewFile_IsKeptSmallAfterSplit()
    {
        var path = GetRepositoryPath("NestSuite/NestSuite/NestSuiteShellWindow.FileOperations.cs");
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
