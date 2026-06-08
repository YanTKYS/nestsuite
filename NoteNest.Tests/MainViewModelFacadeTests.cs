using System.Reflection;
using NoteNest.Services;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

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
    public void RedundantOwnerPropertiesAreNotRepeatedOnFacade()
    {
        var publicProperties = typeof(MainViewModel).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet();

        Assert.DoesNotContain("Markers", publicProperties);
        Assert.DoesNotContain("MarkerCount", publicProperties);
        Assert.DoesNotContain("AllNotes", publicProperties);
        Assert.DoesNotContain("CurrentNoteTitle", publicProperties);
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
        Assert.DoesNotContain(nameof(ProjectSessionViewModel.LastSavedAt), changed);
    }

    [Fact]
    public void ClearingEditorRefreshesMarkersFromNoteWorkspaceOwner()
    {
        var main = new MainViewModel();
        var note = main.Notes.AddNote(main.Notes.AddNotebook("NB"), "Note")!;
        note.Content = "[TODO] marker";
        main.SelectNote(note);

        main.DeleteNote(note);

        Assert.Empty(main.MarkerPanel.Markers);
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
        Assert.DoesNotContain("CurrentNoteTitle", change.PropertyNames);
    }
}
