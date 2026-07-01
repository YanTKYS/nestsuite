using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

public class MainViewModelFacadeTests
{
    [Fact]
    public void XamlCompatibilityPropertiesForwardToResponsibilityOwners()
    {
        var main = new MainViewModel();

        Assert.Same(main.Notes.Notebooks, main.Notebooks);
        Assert.Same(main.Tasks.TaskGroups, main.TaskGroups);
        Assert.Same(main.Session.RecentFiles, main.RecentFiles);
        Assert.Equal(main.Editor.Content, main.EditorContent);
        Assert.Equal(main.Editor.EditorTitle, main.EditorTitle);
        Assert.Equal(main.MarkerPanel.FilteredMarkers.ToArray(), main.FilteredMarkers.ToArray());
    }

    [Fact]
    public void ExistingCompatibilityPropertiesForwardToResponsibilityOwners()
    {
        var main = new MainViewModel();
        var note = main.Notes.AddNote(main.Notes.AddNotebook("NB"), "Note")!;
        main.SelectNote(note);

        Assert.Same(main.MarkerPanel.Markers, main.Markers);
        Assert.Equal(main.MarkerPanel.MarkerCount, main.MarkerCount);
        Assert.Equal(main.Notes.AllNotes.ToArray(), main.AllNotes.ToArray());
        Assert.Equal(main.Editor.SelectedNote?.Title, main.CurrentNoteTitle);
        Assert.Equal(main.Session.LastSavedAt, main.LastSavedAt);
        Assert.Equal("NB", main.CurrentNotebookName);
    }

    [Fact]
    public void SessionNotificationsOnlyRelayPropertiesExposedByFacade()
    {
        var main = new MainViewModel();
        var changed = new List<string?>();
        main.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        main.Session.Start("new-project-id", "New name", "project.notenest", DateTime.Now);

        Assert.Contains(nameof(MainViewModel.ProjectName), changed);
        Assert.Contains(nameof(MainViewModel.ProjectDisplayName), changed);
        Assert.DoesNotContain(nameof(ProjectSessionViewModel.ProjectId), changed);
        Assert.Contains(nameof(MainViewModel.LastSavedAt), changed);
    }

    [Fact]
    public void ClearingEditorRefreshesMarkersFromRemainingNoteWorkspaceNotes()
    {
        var main = new MainViewModel();
        var note = main.Notes.AddNote(main.Notes.AddNotebook("NB"), "Note")!;
        note.Content = "[TODO] marker";
        main.SelectNote(note);
        Assert.Contains(main.MarkerPanel.Markers, marker => marker.SourceNote == note);

        main.DeleteNote(note);

        Assert.DoesNotContain(main.MarkerPanel.Markers, marker => marker.SourceNote == note);
        var extractor = new MarkerExtractorService();
        Assert.Equal(
            main.Notes.AllNotes.Sum(remaining => extractor.Extract(remaining.Content, remaining.Title).Count),
            main.MarkerPanel.MarkerCount);
    }

    // v2.13.2 L19 回帰: ノートブックのリネームは SelectedNote 自体を変えないため、
    // MainViewModel まで CurrentNotebookName の PropertyChanged が届くことを確認する。
    [Fact]
    public void RenamingSelectedNotesNotebookUpdatesCurrentNotebookName()
    {
        var main = new MainViewModel();
        var notebook = main.Notes.AddNotebook("旧名");
        var note = main.Notes.AddNote(notebook, "Note")!;
        main.SelectNote(note);
        var changed = new List<string?>();
        main.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        main.Notes.RenameNotebook(notebook, "新名");

        Assert.Contains(nameof(MainViewModel.CurrentNotebookName), changed);
        Assert.Equal("新名", main.CurrentNotebookName);
    }

    [Fact]
    public void NoteChangesOnlyPublishActiveFacadePropertyNames()
    {
        var notes = new NoteWorkspaceViewModel();
        var markers = new MarkerPanelViewModel(new MarkerExtractorService());
        var coordinator = new NoteChangeCoordinator(notes, markers);
        WorkspaceChangeEventArgs? change = null;
        coordinator.Changed += (_, args) => change = args;

        notes.AddNotebook("NB");

        Assert.NotNull(change);
        Assert.Contains(nameof(MainViewModel.RelatedNoteChoices), change!.PropertyNames);
        Assert.Contains(nameof(MainViewModel.CurrentNoteTitle), change.PropertyNames);
    }
}
