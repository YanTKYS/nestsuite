using NoteNest.Services;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

public class EditorChangeCoordinatorTests
{
    [Fact]
    public void EditorContentRoutesToSelectedNote()
    {
        var notes = new NoteWorkspaceViewModel();
        var tasks = new TaskBoardViewModel();
        var editor = new EditorStateViewModel();
        var coordinator = new EditorChangeCoordinator(notes, tasks, editor);
        var note = notes.AddNote(notes.AddNotebook("NB"), "Note")!;
        editor.SelectNote(note);

        editor.Content = "edited";

        Assert.Equal("edited", note.Content);
    }

    [Fact]
    public void SelectionPublishesViewChangeWithoutDataChange()
    {
        var notes = new NoteWorkspaceViewModel();
        var tasks = new TaskBoardViewModel();
        var editor = new EditorStateViewModel();
        var coordinator = new EditorChangeCoordinator(notes, tasks, editor);
        var changes = new List<WorkspaceChangeEventArgs>();
        coordinator.Changed += (_, change) => changes.Add(change);
        var note = notes.AddNote(notes.AddNotebook("NB"), "Note")!;
        changes.Clear();

        editor.SelectNote(note);

        Assert.NotEmpty(changes);
        Assert.DoesNotContain(changes, change => change.IsDataChanged);
    }
}
