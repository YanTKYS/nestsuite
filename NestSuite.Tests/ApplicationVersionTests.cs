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
        Assert.Equal("2.10.17", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void WindowTitle_UsesApplicationVersion()
    {
        var viewModel = new MainViewModel();

        Assert.EndsWith(" - ver2.10.17", viewModel.WindowTitle);
    }

    [Fact]
    public void ApplicationAndSchemaVersionsAreManagedBySeparateSources()
    {
        Assert.Equal("2.10.17", MainViewModel.ApplicationVersion);
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }

    [Fact]
    public void NoteNestSchemaVersion_Remains_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
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
    public void NoteNestSchemaVersion_IsNotTested_InOtherTestClasses()
    {
        var thisFile = "ApplicationVersionTests.cs";
        var testDir  = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "NestSuite.Tests"));

        var offenders = Directory
            .GetFiles(testDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) != thisFile)
            .Where(f => File.ReadAllText(f).Contains("NoteNestSchemaVersion_Remains_1_4_1"))
            .Select(f => Path.GetRelativePath(testDir, f))
            .ToList();

        Assert.Empty(offenders);
    }
}
