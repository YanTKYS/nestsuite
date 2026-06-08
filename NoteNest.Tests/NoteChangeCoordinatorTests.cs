using NoteNest.Services;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

public class NoteChangeCoordinatorTests
{
    [Fact]
    public void NoteDataChangeRefreshesMarkersAndPublishesSemanticProperties()
    {
        var notes = new NoteWorkspaceViewModel();
        var markers = new MarkerPanelViewModel(new MarkerExtractorService());
        var coordinator = new NoteChangeCoordinator(notes, markers);
        WorkspaceChangeEventArgs? published = null;
        coordinator.Changed += (_, change) => published = change;
        var note = notes.AddNote(notes.AddNotebook("NB"), "Note")!;

        note.Content = "[TODO] changed";

        Assert.Equal(1, markers.MarkerCount);
        Assert.NotNull(published);
        Assert.True(published.IsDataChanged);
        Assert.Contains(nameof(MainViewModel.RelatedNoteChoices), published.PropertyNames);
    }
}
