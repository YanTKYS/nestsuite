using NoteNest.Models;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

public class NoteWorkspaceViewModelTests
{
    [Fact]
    public void AddNote_PreventsDuplicateNamesAndBuildsModels()
    {
        var workspace = new NoteWorkspaceViewModel();
        var notebook = workspace.AddNotebook("NB");
        var first = workspace.AddNote(notebook, "Note");

        Assert.Null(workspace.AddNote(notebook, "note"));
        Assert.Same(first, workspace.FindNoteByTitle("NOTE"));
        Assert.Equal("Note", Assert.Single(workspace.BuildModels()).Notes.Single().Title);
    }

    [Fact]
    public void DirectCollectionTitleAndContentChangesRaiseChanged()
    {
        var workspace = new NoteWorkspaceViewModel();
        var changeCount = 0;
        workspace.Changed += (_, _) => changeCount++;
        var notebook = new NotebookViewModel(new Notebook { Title = "NB" });
        workspace.Notebooks.Add(notebook);
        notebook.Title = "Renamed NB";
        var note = new NoteViewModel(new Note { Title = "Note" });
        notebook.Notes.Add(note);
        note.Title = "Renamed Note";
        note.Content = "[TODO] changed";

        Assert.Equal(5, changeCount);
    }

    [Fact]
    public void LoadDoesNotRaiseChanged()
    {
        var workspace = new NoteWorkspaceViewModel();
        var changed = false;
        workspace.Changed += (_, _) => changed = true;

        workspace.Load(new[] { new Notebook { Title = "Loaded" } });

        Assert.False(changed);
        Assert.Equal("Loaded", Assert.Single(workspace.Notebooks).Title);
    }
}
