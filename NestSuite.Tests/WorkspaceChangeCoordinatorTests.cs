using NoteNest.Models;
using NoteNest.Services;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

public class WorkspaceChangeCoordinatorTests
{
    private readonly NoteWorkspaceViewModel _notes = new();
    private readonly TaskBoardViewModel _tasks = new();
    private readonly MarkerPanelViewModel _markers = new(new MarkerExtractorService());
    private readonly EditorStateViewModel _editor = new();

    [Fact]
    public void SelectionChangeIsNotClassifiedAsDataChange()
    {
        var coordinator = CreateCoordinator();
        var changes = new List<WorkspaceChangeEventArgs>();
        coordinator.Changed += (_, change) => changes.Add(change);
        var note = _notes.AddNote(_notes.AddNotebook("NB"), "Note")!;
        changes.Clear();

        _editor.SelectNote(note);

        Assert.NotEmpty(changes);
        Assert.DoesNotContain(changes, change => change.IsDataChanged);
    }

    [Fact]
    public void DocumentLoadAndSelectionAreNotClassifiedAsDataChanges()
    {
        var coordinator = CreateCoordinator();
        var changes = new List<WorkspaceChangeEventArgs>();
        coordinator.Changed += (_, change) => changes.Add(change);
        var project = new Project
        {
            Notebooks = new List<Notebook> { new() { Notes = new List<Note> { new() { Title = "Loaded" } } } },
            Settings = new AppSettings { FontFamily = "Meiryo UI", FontSize = 18 },
        };

        var lastNote = new ProjectDocumentService().Load(project, _notes, _tasks, _editor);
        _editor.SelectNote(lastNote!);

        Assert.DoesNotContain(changes, change => change.IsDataChanged);
    }

    [Fact]
    public void EditorContentRoutesToNoteAndRefreshesMarkers()
    {
        var coordinator = CreateCoordinator();
        var changes = new List<WorkspaceChangeEventArgs>();
        coordinator.Changed += (_, change) => changes.Add(change);
        var note = _notes.AddNote(_notes.AddNotebook("NB"), "Note")!;
        _editor.SelectNote(note);
        changes.Clear();

        _editor.Content = "[TODO] changed";

        Assert.Equal("[TODO] changed", note.Content);
        Assert.Equal(1, _markers.MarkerCount);
        Assert.Contains(changes, change => change.IsDataChanged);
    }

    [Fact]
    public void EditorRelatedNoteRoutesToEditingTask()
    {
        var coordinator = CreateCoordinator();
        var changes = new List<WorkspaceChangeEventArgs>();
        coordinator.Changed += (_, change) => changes.Add(change);
        var note = _notes.AddNote(_notes.AddNotebook("NB"), "Note")!;
        var task = _tasks.AddTask("today", "Task")!;
        _editor.SelectTask(task, null);
        changes.Clear();

        _editor.EditingTaskRelatedNote = note;

        Assert.Equal(note.Id, task.LinkedNoteId);
        Assert.Contains(changes, change => change.IsDataChanged);
    }

    [Fact]
    public void PersistentEditorSettingsAreDataChangesButViewSettingsAreNot()
    {
        var coordinator = CreateCoordinator();
        var changes = new List<WorkspaceChangeEventArgs>();
        coordinator.Changed += (_, change) => changes.Add(change);

        _editor.ShowLineNumbers = true;
        Assert.DoesNotContain(changes, change => change.IsDataChanged);
        changes.Clear();

        _editor.FontSize = 18;
        Assert.Contains(changes, change => change.IsDataChanged);
    }

    private WorkspaceChangeCoordinator CreateCoordinator() => new(_notes, _tasks, _markers, _editor);
}
