using System.IO;
using System.Linq;
using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

public class ApplicationVersionTests
{
    [Fact]
    public void ApplicationVersion_UsesAssemblyInformationalVersion()
    {
        Assert.Equal("2.18.11", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void WindowTitle_UsesApplicationVersion()
    {
        var viewModel = new MainViewModel();

        Assert.EndsWith(" - ver2.18.11", viewModel.WindowTitle);
    }

    [Fact]
    public void ApplicationAndSchemaVersionsAreManagedBySeparateSources()
    {
        Assert.Equal("2.18.11", MainViewModel.ApplicationVersion);
        Assert.Equal("1.4.2", Project.CurrentSchemaVersion);
    }

    // 次回 schema bump ではこのリテラルと docs チェックリスト記載箇所のみを更新する
    [Fact]
    public void NoteNestSchemaVersion_IsPinned()
    {
        Assert.Equal("1.4.2", Project.CurrentSchemaVersion);
    }

    [Fact]
    public void ApplicationVersion_IsNotTested_InOtherTestClasses()
    {
        var thisFile = "ApplicationVersionTests.cs";
        var testDir  = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "NestSuite.Tests"));

        var offenders = Directory
            .GetFiles(testDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) != thisFile)
            .Where(f => File.ReadAllText(f).Contains("MainViewModel.ApplicationVersion"))
            .Select(f => Path.GetRelativePath(testDir, f))
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void CurrentSchemaVersionLiteral_IsNotHardcoded_InOtherTestClasses()
    {
        var thisFile = "ApplicationVersionTests.cs";
        var testDir  = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "NestSuite.Tests"));
        var literal = "\"" + Project.CurrentSchemaVersion + "\"";

        var offenders = Directory
            .GetFiles(testDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) != thisFile)
            .Where(f => File.ReadAllText(f).Contains(literal))
            .Select(f => Path.GetRelativePath(testDir, f))
            .ToList();

        Assert.Empty(offenders);
    }
}
