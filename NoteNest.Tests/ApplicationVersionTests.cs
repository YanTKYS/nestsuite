using NoteNest.Models;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

public class ApplicationVersionTests
{
    [Fact]
    public void ApplicationVersion_UsesAssemblyInformationalVersion()
    {
        Assert.Equal("1.3.2", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void WindowTitle_UsesApplicationVersion()
    {
        var viewModel = new MainViewModel();

        Assert.EndsWith(" - ver1.3.2", viewModel.WindowTitle);
        Assert.DoesNotContain($" - ver{Project.CurrentSchemaVersion}", viewModel.WindowTitle);
    }

    [Fact]
    public void ApplicationVersion_IsIndependentFromProjectSchemaVersion()
    {
        Assert.Equal("1.3.1", Project.CurrentSchemaVersion);
        Assert.NotEqual(Project.CurrentSchemaVersion, MainViewModel.ApplicationVersion);
    }
}
