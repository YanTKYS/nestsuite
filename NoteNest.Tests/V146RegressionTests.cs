using NoteNest.Models;
using NoteNest.Services;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

/// <summary>v1.4.6 回帰確認：v1.4.x 各変更後の主要フローが一体として動作することを確認します。</summary>
public sealed class V146RegressionTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"notenest-v146-{Guid.NewGuid()}");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private (ProjectLifecycleService Lifecycle, ProjectSessionViewModel Session,
             NoteWorkspaceViewModel Notes, TaskBoardViewModel Tasks,
             MarkerPanelViewModel Markers, EditorStateViewModel Editor) CreateContext()
    {
        var session = new ProjectSessionViewModel();
        var notes   = new NoteWorkspaceViewModel();
        var tasks   = new TaskBoardViewModel();
        var markers = new MarkerPanelViewModel(new MarkerExtractorService());
        var editor  = new EditorStateViewModel();
        var lifecycle = new ProjectLifecycleService(
            session, notes, tasks, markers, editor,
            recentFiles: new RecentFilesService(Path.Combine(_dir, "recent.json")));
        Directory.CreateDirectory(_dir);
        return (lifecycle, session, notes, tasks, markers, editor);
    }

    // ── 起動導線 ─────────────────────────────────────────────────────────────

    [Fact]
    public void NewProject_StartsUnmodified()
    {
        var (lc, session, _, _, _, _) = CreateContext();
        lc.CreateNew();
        Assert.False(session.IsModified);
        Assert.Null(session.CurrentFilePath);
    }

    // ── 保存・読込 ───────────────────────────────────────────────────────────

    [Fact]
    public void SaveLoad_RoundTrip_PreservesNotesTasksAndSchema()
    {
        var (lc, session, notes, tasks, _, editor) = CreateContext();
        lc.CreateNew();

        var nb   = notes.AddNotebook("RegressionNB");
        var note = notes.AddNote(nb, "RegressionNote")!;
        editor.SelectNote(note);
        editor.Content = "[TODO] check me";
        var task = tasks.AddTask("today", "RegressionTask")!;
        task.Comment = "commit this";
        session.IsModified = true;

        var path = Path.Combine(_dir, "regression.notenest");
        lc.Save(path);
        Assert.False(session.IsModified);

        var saved = new ProjectFileService().Load(path);
        Assert.Equal("1.4.1", saved.Version);
        var regressionNb = saved.Notebooks.First(nb => nb.Title == "RegressionNB");
        Assert.Equal("[TODO] check me", regressionNb.Notes[0].Content);
        Assert.Contains(saved.Tasks.Today, t => t.Title == "RegressionTask" && t.Comment == "commit this");
    }

    [Fact]
    public void Save_CreatesBakFile()
    {
        var (lc, session, _, _, _, _) = CreateContext();
        lc.CreateNew();
        var path = Path.Combine(_dir, "bak.notenest");
        lc.Save(path);
        session.IsModified = true;
        lc.Save(path);
        Assert.True(File.Exists(path + ".bak"));
    }

    [Fact]
    public void Load_BrokenJson_Throws()
    {
        var path = Path.Combine(_dir, "broken.notenest");
        Directory.CreateDirectory(_dir);
        File.WriteAllText(path, "{ not valid json");
        Assert.ThrowsAny<Exception>(() => new ProjectFileService().Load(path));
    }

    // ── 自動保存 ─────────────────────────────────────────────────────────────

    [Fact]
    public void AutoSave_DoesNotSave_WhenFilePathIsNull()
    {
        var (lc, session, _, _, _, _) = CreateContext();
        lc.CreateNew();
        session.IsModified = true;
        Assert.False(lc.TryAutoSave());
    }

    [Fact]
    public void AutoSave_DoesNotSave_WhenNotModified()
    {
        var (lc, session, _, _, _, _) = CreateContext();
        lc.CreateNew();
        var path = Path.Combine(_dir, "auto.notenest");
        lc.Save(path);
        Assert.False(session.IsModified);
        Assert.False(lc.TryAutoSave());
    }

    [Fact]
    public void AutoSave_Saves_WhenModifiedAndPathSet()
    {
        var (lc, session, notes, _, _, _) = CreateContext();
        lc.CreateNew();
        var path = Path.Combine(_dir, "auto.notenest");
        lc.Save(path);
        notes.AddNotebook("AutoNB");
        session.IsModified = true;

        Assert.True(lc.TryAutoSave());
        Assert.False(session.IsModified);
        Assert.Contains("AutoNB", File.ReadAllText(path));
    }

    // ── 最近使ったファイル ────────────────────────────────────────────────────

    [Fact]
    public void RecentFiles_AddedOnSave()
    {
        var (lc, _, _, _, _, _) = CreateContext();
        lc.CreateNew();
        var path = Path.Combine(_dir, "recent.notenest");
        lc.Save(path);

        var recentSvc = new RecentFilesService(Path.Combine(_dir, "recent.json"));
        Assert.Contains(path, recentSvc.Load());
    }

    [Fact]
    public void RecentFiles_ClearRemovesAll()
    {
        var recentPath = Path.Combine(_dir, "recent.json");
        Directory.CreateDirectory(_dir);
        var svc = new RecentFilesService(recentPath);
        svc.Add("path/a");
        svc.Add("path/b");

        var result = svc.ClearAndGetUpdatedList();

        Assert.Empty(result);
        Assert.Empty(svc.Load());
    }

    [Fact]
    public void RecentFiles_AtomicWrite_NoPermanentTempFile()
    {
        var recentPath = Path.Combine(_dir, "recent.json");
        Directory.CreateDirectory(_dir);
        var svc = new RecentFilesService(recentPath);
        svc.Add("path/x");

        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }

    // ── ノート日時 ───────────────────────────────────────────────────────────

    [Fact]
    public void NoteTimestamps_SetOnCreate()
    {
        var before = DateTime.Now.AddSeconds(-1);
        var note = new NoteViewModel(new Note { Title = "New" });
        Assert.True(note.CreatedAt >= before);
        Assert.Equal(note.CreatedAt, note.UpdatedAt);
    }

    [Fact]
    public void NoteTimestamps_CreatedAt_NotChangedOnEdit()
    {
        var note = new NoteViewModel(new Note { Title = "T" });
        var created = note.CreatedAt;
        note.Content = "changed";
        Assert.Equal(created, note.CreatedAt);
    }

    [Fact]
    public void NoteTimestamps_UpdatedAt_ChangesOnContentEdit()
    {
        var model = new Note { CreatedAt = new DateTime(2025, 1, 1), UpdatedAt = new DateTime(2025, 1, 1) };
        var note = new NoteViewModel(model);
        note.Content = "changed";
        Assert.True(note.UpdatedAt > new DateTime(2025, 1, 1));
    }

    [Fact]
    public void LegacyNote_WithoutTimestamps_LoadsWithDefaults()
    {
        var path = Path.Combine(_dir, "legacy.notenest");
        Directory.CreateDirectory(_dir);
        File.WriteAllText(path, """{"projectName":"L","notebooks":[{"title":"NB","notes":[{"title":"N","content":"C"}]}]}""");

        var note = new ProjectFileService().Load(path).Notebooks[0].Notes[0];
        Assert.NotEqual(default(DateTime), note.CreatedAt);
        Assert.NotEqual(default(DateTime), note.UpdatedAt);
    }

    // ── 保存スキーマバージョン ────────────────────────────────────────────────

    [Fact]
    public void SchemaVersion_Is_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }

    [Fact]
    public void SavedFile_ContainsSchemaVersion_1_4_1()
    {
        var (lc, session, _, _, _, _) = CreateContext();
        lc.CreateNew();
        var path = Path.Combine(_dir, "schema.notenest");
        lc.Save(path);
        Assert.Contains("\"1.4.1\"", File.ReadAllText(path));
    }

    // ── 未保存状態 ───────────────────────────────────────────────────────────

    [Fact]
    public void IsModified_FalseAfterSave()
    {
        var (lc, session, _, _, _, _) = CreateContext();
        lc.CreateNew();
        session.IsModified = true;
        var path = Path.Combine(_dir, "mod.notenest");
        lc.Save(path);
        Assert.False(session.IsModified);
    }

    // ── エクスポート ─────────────────────────────────────────────────────────

    [Fact]
    public void Export_Txt_WritesUtf8()
    {
        Directory.CreateDirectory(_dir);
        var project = new Project
        {
            ProjectName = "テスト",
            Notebooks = [new Notebook { Title = "NB", Notes = [new Note { Title = "日本語ノート", Content = "日本語本文" }] }]
        };
        var svc  = new ExportService();
        var path = Path.Combine(_dir, "export.txt");
        new ExportService().Export(project, new ExportOptions(ExportTarget.Project, ExportFormat.Text, false, false), path);

        var text = File.ReadAllText(path, System.Text.Encoding.UTF8);
        Assert.Contains("日本語ノート", text);
        Assert.Contains("日本語本文", text);
    }

    [Fact]
    public void Export_Markdown_ContainsHeadings()
    {
        Directory.CreateDirectory(_dir);
        var project = new Project
        {
            ProjectName = "MD",
            Notebooks = [new Notebook { Title = "NB", Notes = [new Note { Title = "MDNote", Content = "body" }] }]
        };
        var path = Path.Combine(_dir, "export.md");
        new ExportService().Export(project, new ExportOptions(ExportTarget.Project, ExportFormat.Markdown, false, false), path);

        var text = File.ReadAllText(path);
        Assert.Contains("# ", text);
        Assert.Contains("MDNote", text);
    }

    [Fact]
    public void Export_Html_ContainsHtmlTags()
    {
        Directory.CreateDirectory(_dir);
        var project = new Project
        {
            ProjectName = "HTML",
            Notebooks = [new Notebook { Title = "NB", Notes = [new Note { Title = "HtmlNote", Content = "body" }] }]
        };
        var path = Path.Combine(_dir, "export.html");
        new ExportService().Export(project, new ExportOptions(ExportTarget.Project, ExportFormat.Html, false, false), path);

        var text = File.ReadAllText(path);
        Assert.Contains("<html>", text);
        Assert.Contains("HtmlNote", text);
    }
}
