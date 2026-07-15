using NestSuite.Models;
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

    // L25 (review7-fable5 REV7-2): NoteWorkspaceViewModel.Load 完了後に Reloaded が発火し、
    // NoteChangeCoordinator は空状態等の派生プロパティを isDataChanged=false で 1 回だけ通知する。
    // データ変更として扱われないため MainViewModel.IsModified を変化させない。
    [Fact]
    public void LoadReloadsPublishSemanticPropertiesOnce_WithoutDataChangedFlag()
    {
        var notes = new NoteWorkspaceViewModel();
        var markers = new MarkerPanelViewModel(new MarkerExtractorService());
        var coordinator = new NoteChangeCoordinator(notes, markers);
        var published = new List<WorkspaceChangeEventArgs>();
        coordinator.Changed += (_, change) => published.Add(change);

        notes.Load(new[]
        {
            new Notebook { Title = "NB", Notes = new() { new Note { Title = "N" } } },
        });

        var change = Assert.Single(published);
        Assert.False(change.IsDataChanged);
        Assert.Contains(nameof(MainViewModel.HasNotebooks), change.PropertyNames);
        Assert.Contains(nameof(MainViewModel.ShowNotebookEmptyState), change.PropertyNames);
        Assert.Contains(nameof(MainViewModel.ShowNoteEmptyState), change.PropertyNames);
    }

    [Fact]
    public void LoadRefreshesMarkerCount_BeforePublishingChange()
    {
        var notes = new NoteWorkspaceViewModel();
        var markers = new MarkerPanelViewModel(new MarkerExtractorService());
        var coordinator = new NoteChangeCoordinator(notes, markers);
        var markerCountAtPublish = -1;
        coordinator.Changed += (_, _) => markerCountAtPublish = markers.MarkerCount;

        notes.Load(new[]
        {
            new Notebook { Title = "NB", Notes = new() { new Note { Title = "N", Content = "[TODO] やること" } } },
        });

        Assert.Equal(1, markerCountAtPublish);
    }
}
