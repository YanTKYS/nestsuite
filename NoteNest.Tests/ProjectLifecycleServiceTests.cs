using NoteNest.Services;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

public class ProjectLifecycleServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"notenest-lifecycle-{Guid.NewGuid()}");

    [Fact]
    public void CreateNewLoadsWorkspaceAndSessionWithoutUnsavedChange()
    {
        var context = CreateContext();

        context.Lifecycle.CreateNew();

        Assert.NotEmpty(context.Notes.Notebooks);
        Assert.NotNull(context.Editor.SelectedNote);
        Assert.True(context.Session.IsSampleProject);
        Assert.False(context.Session.IsModified);
        Assert.NotEmpty(context.Markers.Markers);
    }

    [Fact]
    public void SaveAndOpenRoundTripOwnsSessionAndRecentFiles()
    {
        var context = CreateContext();
        context.Lifecycle.CreateNew();
        context.Notes.AddNotebook("Added");
        context.Session.IsModified = true;
        var path = Path.Combine(_directory, "roundtrip.notenest");

        context.Lifecycle.Save(path);
        context.Notes.AddNotebook("Temporary");
        context.Lifecycle.Open(path);

        Assert.Equal(path, context.Session.CurrentFilePath);
        Assert.Equal("roundtrip.notenest", context.Session.ProjectDisplayName);
        Assert.False(context.Session.IsModified);
        Assert.False(context.Session.IsSampleProject);
        Assert.Contains(context.Session.RecentFiles, file => file.FullPath == path);
        Assert.Contains(context.Notes.Notebooks, notebook => notebook.Title == "Added");
        Assert.DoesNotContain(context.Notes.Notebooks, notebook => notebook.Title == "Temporary");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }

    private Context CreateContext()
    {
        Directory.CreateDirectory(_directory);
        var session = new ProjectSessionViewModel();
        var notes = new NoteWorkspaceViewModel();
        var tasks = new TaskBoardViewModel();
        var markers = new MarkerPanelViewModel(new MarkerExtractorService());
        var editor = new EditorStateViewModel();
        var lifecycle = new ProjectLifecycleService(
            session, notes, tasks, markers, editor,
            recentFiles: new RecentFilesService(Path.Combine(_directory, "recent.json")));
        return new Context(session, notes, markers, editor, lifecycle);
    }

    private sealed record Context(
        ProjectSessionViewModel Session,
        NoteWorkspaceViewModel Notes,
        MarkerPanelViewModel Markers,
        EditorStateViewModel Editor,
        ProjectLifecycleService Lifecycle);
}
