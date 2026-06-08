using System.Text.Json;
using NoteNest.Models;
using NoteNest.Services;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

/// <summary>v1.3.x の責務分離後も、主要な利用フローが一体として動作することを確認します。</summary>
public sealed class V140RegressionTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"notenest-v140-{Guid.NewGuid()}");

    [Fact]
    public void SaveAndReloadPreservesNotesTasksLinksSettingsSelectionAndSchema()
    {
        var context = CreateContext();
        context.Lifecycle.CreateNew();
        var notebook = context.Notes.AddNotebook("回帰確認");
        var note = context.Notes.AddNote(notebook, "リンク先")!;
        context.Editor.SelectNote(note);
        context.Editor.Content = "[TODO] 本文と [[リンク先]]";
        context.Editor.FontFamily = "Meiryo UI";
        context.Editor.FontSize = 18;
        var task = context.Tasks.AddTask("today", "確認タスク")!;
        task.IsCompleted = true;
        task.Comment = "保存するコメント";
        context.Tasks.SetRelatedNote(task, note);
        context.Session.IsModified = true;
        var path = Path.Combine(_directory, "regression.notenest");

        context.Lifecycle.Save(path);
        context.Lifecycle.Open(path);

        var reloadedNote = Assert.Single(context.Notes.AllNotes.Where(item => item.Title == "リンク先"));
        var reloadedTask = Assert.Single(context.Tasks.TaskGroups.SelectMany(group => group.Tasks).Where(item => item.Title == "確認タスク"));
        Assert.Equal("[TODO] 本文と [[リンク先]]", reloadedNote.Content);
        Assert.True(reloadedTask.IsCompleted);
        Assert.Equal("保存するコメント", reloadedTask.Comment);
        Assert.Equal(reloadedNote.Id, reloadedTask.LinkedNoteId);
        Assert.Equal(reloadedNote.Id, context.Editor.SelectedNote?.Id);
        Assert.Equal("Meiryo UI", context.Editor.FontFamily);
        Assert.Equal(18, context.Editor.FontSize);
        Assert.Contains(context.Markers.Markers, marker => marker.Type == "TODO" && marker.SourceNote?.Id == reloadedNote.Id);
        Assert.False(context.Session.IsModified);

        using var json = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(Project.CurrentSchemaVersion, json.RootElement.GetProperty("version").GetString());
    }

    [Fact]
    public void OverwriteSaveCreatesBackupAndClearsUnsavedState()
    {
        var context = CreateContext();
        context.Lifecycle.CreateNew();
        var path = Path.Combine(_directory, "backup.notenest");
        context.Lifecycle.Save(path);
        context.Session.ProjectName = "上書き後";
        context.Session.IsModified = true;

        context.Lifecycle.Save(path);

        Assert.True(File.Exists(path + ".bak"));
        Assert.False(context.Session.IsModified);
        Assert.Equal(path, context.Session.CurrentFilePath);
    }

    [Fact]
    public void SelectionAndViewSettingsDoNotMarkModifiedButEditsAndPersistentSettingsDo()
    {
        var main = new MainViewModel();
        var note = main.Notes.AddNote(main.Notes.AddNotebook("NB"), "Note")!;
        var task = main.Tasks.AddTask("today", "Task")!;
        main.IsModified = false;

        main.SelectNote(note);
        main.SelectTask(task);
        main.ShowLineNumbers = !main.ShowLineNumbers;
        main.MarkerSortOrderIndex = 2;

        Assert.False(main.IsModified);

        main.Editor.Content = "task comment";
        Assert.True(main.IsModified);
        Assert.Equal("task comment", task.Comment);

        main.IsModified = false;
        main.Editor.FontSize = 19;
        Assert.True(main.IsModified);
    }

    [Fact]
    public void DeletingRelatedNoteThroughFacadeClearsTaskLink()
    {
        var main = new MainViewModel();
        var note = main.Notes.AddNote(main.Notes.AddNotebook("NB"), "Note")!;
        var task = main.Tasks.AddTask("today", "Task")!;
        main.SetTaskRelatedNote(task, note);
        main.IsModified = false;

        main.DeleteNote(note);

        Assert.Null(task.LinkedNoteId);
        Assert.True(main.IsModified);
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
        var coordinator = new WorkspaceChangeCoordinator(notes, tasks, markers, editor);
        var lifecycle = new ProjectLifecycleService(
            session, notes, tasks, markers, editor,
            recentFiles: new RecentFilesService(Path.Combine(_directory, "recent.json")));
        return new Context(session, notes, tasks, markers, editor, coordinator, lifecycle);
    }

    private sealed record Context(
        ProjectSessionViewModel Session,
        NoteWorkspaceViewModel Notes,
        TaskBoardViewModel Tasks,
        MarkerPanelViewModel Markers,
        EditorStateViewModel Editor,
        WorkspaceChangeCoordinator Coordinator,
        ProjectLifecycleService Lifecycle);
}
