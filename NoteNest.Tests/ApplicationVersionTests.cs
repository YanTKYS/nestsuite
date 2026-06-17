using NoteNest.Models;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

public class ApplicationVersionTests
{
    [Fact]
    public void ApplicationVersion_UsesAssemblyInformationalVersion()
    {
        Assert.Equal("1.16.5", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void WindowTitle_UsesApplicationVersion()
    {
        var viewModel = new MainViewModel();

        Assert.EndsWith(" - ver1.16.5", viewModel.WindowTitle);
    }

    [Fact]
    public void ApplicationAndSchemaVersionsAreManagedBySeparateSources()
    {
        Assert.Equal("1.16.5", MainViewModel.ApplicationVersion);
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }
}
