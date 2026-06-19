using NoteNest.Models;
using NoteNest.Services;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

public class MarkerPanelViewModelTests
{
    [Fact]
    public void RefreshOwnsFilteringAndSummary()
    {
        var panel = new MarkerPanelViewModel(new MarkerExtractorService());
        var note = new NoteViewModel(new Note { Title = "N", Content = "[TODO] a\n[FIXME] b" });

        panel.Refresh(new[] { note });
        panel.FilterTodo = false;

        Assert.Equal(2, panel.MarkerCount);
        Assert.Contains("TODO: 1", panel.ProjectMarkerSummary);
        Assert.Equal("1/2個", panel.FilteredMarkerCountText);
        Assert.Equal("FIXME", Assert.Single(panel.FilteredMarkers).Type);
    }
}
