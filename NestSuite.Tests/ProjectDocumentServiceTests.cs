using NoteNest.Models;
using NoteNest.Services;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

public class ProjectDocumentServiceTests
{
    [Fact]
    public void LoadAndBuildRoundTripResponsibilityOwners()
    {
        var source = new Project
        {
            ProjectId = "project-id",
            ProjectName = "Project",
            Notebooks = new List<Notebook> { new() { Title = "NB", Notes = new List<Note> { new() { Id = "last-note", Title = "Note" } } } },
            Tasks = new TaskCollection { Today = new List<NoteTask> { new() { Title = "Task" } } },
            Settings = new AppSettings { LastOpenedNoteId = "last-note", FontFamily = "Meiryo UI", FontSize = 18 },
        };
        var notes = new NoteWorkspaceViewModel();
        var tasks = new TaskBoardViewModel();
        var editor = new EditorStateViewModel();
        var service = new ProjectDocumentService();

        var lastNote = Assert.IsType<NoteViewModel>(service.Load(source, notes, tasks, editor));
        Assert.Equal("last-note", lastNote.Id);
        editor.SelectNote(lastNote);
        var built = service.Build(source.ProjectId, source.ProjectName, notes, tasks, editor);

        Assert.Equal(Project.CurrentSchemaVersion, built.Version);
        Assert.Equal("project-id", built.ProjectId);
        Assert.Equal("Note", Assert.Single(Assert.Single(built.Notebooks).Notes).Title);
        Assert.Equal("Task", Assert.Single(built.Tasks.Today).Title);
        Assert.Equal(editor.SelectedNote!.Id, built.Settings.LastOpenedNoteId);
        Assert.Equal(18, built.Settings.FontSize);
    }
}
