using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

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

    [Fact]
    public void Build_WritesLoadedFontFamily_NotLiveDisplayOverride()
    {
        // v2.14.16 BUG: Build() は editor.FontFamily（NestSuite UI 設定駆動の表示専用値、
        // Shell によって上書きされ得る）ではなく editor.SavedFontFamily（読込時点の値）を
        // Settings.FontFamily へ書き戻すことを固定する。UI 設定の変更が Workspace ファイル
        // 本体の差分にならないようにするための回帰確認。
        var source = new Project
        {
            ProjectId = "project-id",
            ProjectName = "Project",
            Settings = new AppSettings { FontFamily = "Meiryo UI", FontSize = 18 },
        };
        var notes = new NoteWorkspaceViewModel();
        var tasks = new TaskBoardViewModel();
        var editor = new EditorStateViewModel();
        var service = new ProjectDocumentService();

        service.Load(source, notes, tasks, editor);
        Assert.Equal("Meiryo UI", editor.FontFamily);

        // Shell の NoteNestEditorFontFamily（UI 設定）駆動の上書きを模擬する。
        editor.FontFamily = "Consolas";

        var built = service.Build(source.ProjectId, source.ProjectName, notes, tasks, editor);

        Assert.Equal("Meiryo UI", built.Settings.FontFamily);
    }
}
