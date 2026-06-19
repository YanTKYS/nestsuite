using NoteNest.Models;
using NoteNest.Services;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

public class WorkspaceViewModelTests
{
    [Fact]
    public void NoteWorkspace_OwnsNoteCollectionAndPreventsDuplicateNames()
    {
        var workspace = new NoteWorkspaceViewModel();
        var notebook = workspace.AddNotebook("NB");

        var first = workspace.AddNote(notebook, "Note");
        var duplicate = workspace.AddNote(notebook, "note");

        Assert.NotNull(first);
        Assert.Null(duplicate);
        Assert.Same(first, workspace.FindNoteByTitle("NOTE"));
        Assert.Single(workspace.AllNotes);
        Assert.Equal("Note", Assert.Single(workspace.BuildModels()).Notes.Single().Title);
    }

    [Fact]
    public void NoteWorkspace_RaisesChangedForCollectionTitleAndContentChanges()
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
    public void NoteWorkspace_LoadDoesNotRaiseChanged()
    {
        var workspace = new NoteWorkspaceViewModel();
        var changed = false;
        workspace.Changed += (_, _) => changed = true;

        workspace.Load(new[] { new Notebook { Title = "Loaded" } });

        Assert.False(changed);
        Assert.Equal("Loaded", Assert.Single(workspace.Notebooks).Title);
    }

    [Fact]
    public void MainViewModel_DirectNoteWorkspaceChangeMarksProjectModifiedAndRefreshesMarkers()
    {
        var main = new MainViewModel();
        var notebook = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(notebook, "Note")!;
        var markerCountBeforeChange = main.MarkerCount;
        main.IsModified = false;

        note.Content = "[TODO] direct change";

        Assert.True(main.IsModified);
        Assert.Equal(markerCountBeforeChange + 1, main.MarkerCount);
    }

    [Fact]
    public void TaskBoard_BuildModelPreservesGroupOwnership()
    {
        var board = new TaskBoardViewModel();
        var changeCount = 0;
        board.Changed += (_, _) => changeCount++;
        var task = board.AddTask("today", "Task");

        Assert.NotNull(task);
        Assert.NotNull(board.MoveTask(task!, "backlog"));
        var model = board.BuildModel();
        Assert.Empty(model.Today);
        Assert.Equal("Task", Assert.Single(model.Backlog).Title);
        Assert.Equal(2, changeCount);
    }

    [Fact]
    public void MarkerPanel_OwnsFilteringAndSummary()
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

    [Fact]
    public void MainViewModel_ExposesIndependentWorkspaceOwners()
    {
        var main = new MainViewModel();

        Assert.Same(main.Notes.Notebooks, main.Notebooks);
        Assert.Same(main.Tasks.TaskGroups, main.TaskGroups);
        Assert.Same(main.MarkerPanel.Markers, main.Markers);
    }
}
