using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

public class ApplicationVersionTests
{
    [Fact]
    public void ApplicationVersion_UsesAssemblyInformationalVersion()
    {
        Assert.Equal("2.7.14", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void WindowTitle_UsesApplicationVersion()
    {
        var viewModel = new MainViewModel();

        Assert.EndsWith(" - ver2.7.14", viewModel.WindowTitle);
    }

    [Fact]
    public void ApplicationAndSchemaVersionsAreManagedBySeparateSources()
    {
        Assert.Equal("2.7.14", MainViewModel.ApplicationVersion);
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }
}
