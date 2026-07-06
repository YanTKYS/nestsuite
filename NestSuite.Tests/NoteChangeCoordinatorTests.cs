using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

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

    // v2.13.2 L19 回帰: ノートブックのリネーム・ノート移動は SelectedNote 自体を変えないため、
    // EditorChangeCoordinator ではなく NoteChangeCoordinator 側で CurrentNotebookName を通知する必要がある。
    [Fact]
    public void RenamingNotebookPublishesCurrentNotebookName()
    {
        var notes = new NoteWorkspaceViewModel();
        var markers = new MarkerPanelViewModel(new MarkerExtractorService());
        var coordinator = new NoteChangeCoordinator(notes, markers);
        var notebook = notes.AddNotebook("旧ノートブック名");
        WorkspaceChangeEventArgs? published = null;
        coordinator.Changed += (_, change) => published = change;

        notes.RenameNotebook(notebook, "新ノートブック名");

        Assert.NotNull(published);
        Assert.Contains(nameof(MainViewModel.CurrentNotebookName), published.PropertyNames);
    }

    [Fact]
    public void MovingNoteToAnotherNotebookPublishesCurrentNotebookName()
    {
        var notes = new NoteWorkspaceViewModel();
        var markers = new MarkerPanelViewModel(new MarkerExtractorService());
        var coordinator = new NoteChangeCoordinator(notes, markers);
        var sourceNotebook = notes.AddNotebook("移動元");
        var targetNotebook = notes.AddNotebook("移動先");
        var note = notes.AddNote(sourceNotebook, "Note")!;
        WorkspaceChangeEventArgs? published = null;
        coordinator.Changed += (_, change) => published = change;

        var moved = notes.MoveNoteToNotebook(note, targetNotebook);

        Assert.True(moved);
        Assert.NotNull(published);
        Assert.Contains(nameof(MainViewModel.CurrentNotebookName), published.PropertyNames);
    }
}
