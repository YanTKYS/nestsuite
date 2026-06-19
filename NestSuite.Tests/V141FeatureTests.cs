using System.Text.Json;
using NoteNest.Models;
using NoteNest.Services;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

public sealed class V141FeatureTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"notenest-v141-{Guid.NewGuid()}");

    [Fact]
    public void NoteTimestampsUpdateAndRoundTrip()
    {
        Directory.CreateDirectory(_directory);
        var created = new DateTime(2026, 1, 2, 3, 4, 5);
        var model = new Note { Title = "Note", CreatedAt = created, UpdatedAt = created };
        var note = new NoteViewModel(model);

        note.Content = "updated";

        Assert.Equal(created, note.CreatedAt);
        Assert.True(note.UpdatedAt > created);
        Assert.Contains("作成:", note.TimestampSummary);
        Assert.Contains("更新:", note.TimestampSummary);
        var path = Path.Combine(_directory, "timestamps.notenest");
        new ProjectFileService().Save(path, new Project { Notebooks = [new Notebook { Notes = [model] }] });
        var loaded = new ProjectFileService().Load(path).Notebooks[0].Notes[0];
        Assert.Equal(model.CreatedAt, loaded.CreatedAt);
        Assert.Equal(model.UpdatedAt, loaded.UpdatedAt);
    }

    [Fact]
    public void LegacyNoteWithoutTimestampsLoadsWithDefaults()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "legacy.notenest");
        File.WriteAllText(path, """{"projectName":"Legacy","notebooks":[{"title":"NB","notes":[{"title":"N","content":"C"}]}]}""");

        var note = Assert.Single(Assert.Single(new ProjectFileService().Load(path).Notebooks).Notes);

        Assert.NotEqual(default(DateTime), note.CreatedAt);
        Assert.NotEqual(default(DateTime), note.UpdatedAt);
    }

    [Fact]
    public void UnifiedExportSupportsTargetsFormatsTasksAndMarkers()
    {
        Directory.CreateDirectory(_directory);
        var project = new Project
        {
            ProjectName = "P",
            Notebooks =
            [
                new Notebook { Id = "nb", Title = "NB", Notes = [new Note { Id = "note", Title = "N", Content = "[TODO] marker" }] },
                new Notebook { Id = "other-nb", Title = "Other", Notes = [new Note { Id = "other-note", Title = "OtherNote", Content = "" }] },
            ],
            Tasks = new TaskCollection
            {
                Today =
                [
                    new NoteTask { Title = "Linked Task", LinkedNoteId = "note" },
                    new NoteTask { Title = "Other Task", LinkedNoteId = "other-note" },
                    new NoteTask { Title = "Unlinked Task" },
                ],
            },
        };
        var service = new ExportService();
        var markdown = Path.Combine(_directory, "export.md");
        var html = Path.Combine(_directory, "export.html");

        service.Export(project, new ExportOptions(ExportTarget.CurrentNote, ExportFormat.Markdown, true, true), markdown, "nb", "note");
        service.Export(project, new ExportOptions(ExportTarget.Project, ExportFormat.Html, true, true), html);

        var markdownText = File.ReadAllText(markdown);
        Assert.Contains("## Tasks", markdownText);
        Assert.Contains("Linked Task", markdownText);
        Assert.DoesNotContain("Other Task", markdownText);
        Assert.DoesNotContain("Unlinked Task", markdownText);
        Assert.Contains("## Markers", markdownText);
        var htmlText = File.ReadAllText(html);
        Assert.Contains("<html>", htmlText);
        Assert.Contains("Other Task", htmlText);
        Assert.Contains("Unlinked Task", htmlText);
        Assert.Equal(".md", ExportService.GetExtension(ExportFormat.Markdown));
    }

    [Fact]
    public void AutoSaveOnlySavesModifiedExistingProject()
    {
        Directory.CreateDirectory(_directory);
        var session = new ProjectSessionViewModel();
        var notes = new NoteWorkspaceViewModel();
        var tasks = new TaskBoardViewModel();
        var markers = new MarkerPanelViewModel(new MarkerExtractorService());
        var editor = new EditorStateViewModel();
        var lifecycle = new ProjectLifecycleService(session, notes, tasks, markers, editor,
            recentFiles: new RecentFilesService(Path.Combine(_directory, "recent.json")));
        lifecycle.CreateNew();
        Assert.False(lifecycle.TryAutoSave());
        var path = Path.Combine(_directory, "auto.notenest");
        lifecycle.Save(path);
        notes.AddNotebook("AutoSaved");
        session.IsModified = true;

        Assert.True(lifecycle.TryAutoSave());
        Assert.False(session.IsModified);
        Assert.Contains("AutoSaved", File.ReadAllText(path));
    }

    [Fact]
    public void ProjectInfoContainsCurrentCountsAndSaveState()
    {
        var main = new MainViewModel();

        Assert.Contains("プロジェクト名:", main.ProjectInfo);
        Assert.Contains("ノートブック:", main.ProjectInfo);
        Assert.Contains("タスク:", main.ProjectInfo);
        Assert.Contains("最終保存:", main.ProjectInfo);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }
}
