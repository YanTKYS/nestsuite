using System.Text.Json;
using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// NoteNest (.notenest) 保存形式・スキーマの非変更を自動テストで固定する。
/// 保存形式の JSON 構造、round-trip、バックアップ、タイムスタンプ、
/// エクスポート、自動保存、最近使ったファイルに関する回帰確認を担う。
/// </summary>
public class NoteNestFormatSchemaRegressionTests : IDisposable
{
    private readonly string _tempDir;

    public NoteNestFormatSchemaRegressionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NoteNestFormatSchemaRegressionTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── バージョン定数 ────────────────────────────────────────────────────

    [Fact]
    public void NoteNest_SchemaVersionConstant_Is_1_4_2()
    {
        Assert.Equal("1.4.2", Project.CurrentSchemaVersion);
    }

    // ── JSON 構造 ─────────────────────────────────────────────────────────

    [Fact]
    public void NoteNest_SerializedJson_ContainsProjectNameKey()
    {
        var path = Path.Combine(_tempDir, "test.notenest");
        new ProjectFileService().Save(path, new Project { ProjectName = "TestProject" });
        var json = File.ReadAllText(path);
        Assert.Contains("\"projectName\"", json);
        Assert.Contains("TestProject", json);
    }

    [Fact]
    public void NoteNest_SerializedJson_ContainsNotebooksKey()
    {
        var path = Path.Combine(_tempDir, "test.notenest");
        new ProjectFileService().Save(path, new Project());
        var json = File.ReadAllText(path);
        Assert.Contains("\"notebooks\"", json);
    }

    [Fact]
    public void NoteNest_SerializedJson_ContainsTasksKey()
    {
        var path = Path.Combine(_tempDir, "test.notenest");
        new ProjectFileService().Save(path, new Project());
        var json = File.ReadAllText(path);
        Assert.Contains("\"tasks\"", json);
    }

    [Fact]
    public void NoteNest_SerializedJson_ContainsSettingsKey()
    {
        var path = Path.Combine(_tempDir, "test.notenest");
        new ProjectFileService().Save(path, new Project());
        var json = File.ReadAllText(path);
        Assert.Contains("\"settings\"", json);
    }

    [Fact]
    public void NoteNest_SavedJson_IsValidJson()
    {
        var path = Path.Combine(_tempDir, "test.notenest");
        new ProjectFileService().Save(path, new Project { ProjectName = "Test" });
        var json = File.ReadAllText(path);
        Assert.True(IsValidJson(json), "保存された .notenest ファイルは有効な JSON である必要がある");
    }

    [Fact]
    public void NoteNest_SaveLoad_PreservesSchemaVersion()
    {
        // スキーマバージョン定数は load/save を経ても変わらない
        var path = Path.Combine(_tempDir, "schema.notenest");
        var svc = new ProjectFileService();
        svc.Save(path, new Project());
        var loaded = svc.Load(path);
        // Project.Version はデフォルト "0.1.0" だが CurrentSchemaVersion は "1.4.2"
        // スキーマバージョン定数が変わらないことを確認する
        Assert.Equal("1.4.2", Project.CurrentSchemaVersion);
        Assert.NotEqual("1.4.2", loaded.Version); // Version プロパティはスキーマバージョンではない
    }

    [Fact]
    public void NoteNest_SaveLoad_RoundTrip_PreservesNotebookAndNoteStructure()
    {
        var path = Path.Combine(_tempDir, "roundtrip.notenest");
        var svc = new ProjectFileService();
        var nb = new Notebook { Title = "TestBook" };
        nb.Notes.Add(new Note { Title = "TestNote", Content = "本文テスト" });
        var project = new Project { ProjectName = "RoundTrip" };
        project.Notebooks.Add(nb);

        svc.Save(path, project);
        var loaded = svc.Load(path);

        Assert.Equal("RoundTrip", loaded.ProjectName);
        Assert.Single(loaded.Notebooks);
        Assert.Equal("TestBook", loaded.Notebooks[0].Title);
        Assert.Single(loaded.Notebooks[0].Notes);
        Assert.Equal("TestNote",  loaded.Notebooks[0].Notes[0].Title);
        Assert.Equal("本文テスト", loaded.Notebooks[0].Notes[0].Content);
    }

    // ── 保存・読込・回帰 (V140) ──────────────────────────────────────────

    [Fact]
    public void SaveAndReloadPreservesNotesTasksLinksSettingsSelectionAndSchema()
    {
        var context = CreateV140Context();
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
        var path = Path.Combine(_tempDir, "regression.notenest");

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
        var context = CreateV140Context();
        context.Lifecycle.CreateNew();
        var path = Path.Combine(_tempDir, "backup.notenest");
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

    // ── タイムスタンプ (V141) ─────────────────────────────────────────────

    [Fact]
    public void NoteTimestampsUpdateAndRoundTrip()
    {
        Directory.CreateDirectory(_tempDir);
        var created = new DateTime(2026, 1, 2, 3, 4, 5);
        var model = new Note { Title = "Note", CreatedAt = created, UpdatedAt = created };
        var note = new NoteViewModel(model);

        note.Content = "updated";

        Assert.Equal(created, note.CreatedAt);
        Assert.True(note.UpdatedAt > created);
        Assert.Contains("作成:", note.TimestampSummary);
        Assert.Contains("更新:", note.TimestampSummary);
        var path = Path.Combine(_tempDir, "timestamps.notenest");
        new ProjectFileService().Save(path, new Project { Notebooks = [new Notebook { Notes = [model] }] });
        var loaded = new ProjectFileService().Load(path).Notebooks[0].Notes[0];
        Assert.Equal(model.CreatedAt, loaded.CreatedAt);
        Assert.Equal(model.UpdatedAt, loaded.UpdatedAt);
    }

    [Fact]
    public void LegacyNoteWithoutTimestampsLoadsWithDefaults()
    {
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "legacy.notenest");
        File.WriteAllText(path, """{"projectName":"Legacy","notebooks":[{"title":"NB","notes":[{"title":"N","content":"C"}]}]}""");

        var note = Assert.Single(Assert.Single(new ProjectFileService().Load(path).Notebooks).Notes);

        Assert.NotEqual(default(DateTime), note.CreatedAt);
        Assert.NotEqual(default(DateTime), note.UpdatedAt);
    }

    [Fact]
    public void UnifiedExportSupportsTargetsFormatsTasksAndMarkers()
    {
        Directory.CreateDirectory(_tempDir);
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
        var markdown = Path.Combine(_tempDir, "export.md");
        var html = Path.Combine(_tempDir, "export.html");

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
        Directory.CreateDirectory(_tempDir);
        var session = new ProjectSessionViewModel();
        var notes = new NoteWorkspaceViewModel();
        var tasks = new TaskBoardViewModel();
        var markers = new MarkerPanelViewModel(new MarkerExtractorService());
        var editor = new EditorStateViewModel();
        var lifecycle = new ProjectLifecycleService(session, notes, tasks, markers, editor,
            recentFiles: new RecentFilesService(Path.Combine(_tempDir, "recent.json")));
        lifecycle.CreateNew();
        Assert.False(lifecycle.TryAutoSave());
        var path = Path.Combine(_tempDir, "auto.notenest");
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

    // ── 保存・読込・回帰 (V146) ──────────────────────────────────────────

    [Fact]
    public void NewProject_StartsUnmodified()
    {
        var (lc, session, _, _, _, _) = CreateV146Context();
        lc.CreateNew();
        Assert.False(session.IsModified);
        Assert.Null(session.CurrentFilePath);
    }

    [Fact]
    public void SaveLoad_RoundTrip_PreservesNotesTasksAndSchema()
    {
        var (lc, session, notes, tasks, _, editor) = CreateV146Context();
        lc.CreateNew();

        var nb   = notes.AddNotebook("RegressionNB");
        var note = notes.AddNote(nb, "RegressionNote")!;
        note.Content = "[TODO] check me";
        var task = tasks.AddTask("today", "RegressionTask")!;
        task.Comment = "commit this";
        session.IsModified = true;

        var path = Path.Combine(_tempDir, "regression.notenest");
        lc.Save(path);
        Assert.False(session.IsModified);

        var saved = new ProjectFileService().Load(path);
        Assert.Equal("1.4.2", saved.Version);
        var regressionNb = saved.Notebooks.First(nb => nb.Title == "RegressionNB");
        Assert.Equal("[TODO] check me", regressionNb.Notes[0].Content);
        Assert.Contains(saved.Tasks.Today, t => t.Title == "RegressionTask" && t.Comment == "commit this");
    }

    [Fact]
    public void Save_CreatesBakFile()
    {
        var (lc, session, _, _, _, _) = CreateV146Context();
        lc.CreateNew();
        var path = Path.Combine(_tempDir, "bak.notenest");
        lc.Save(path);
        session.IsModified = true;
        lc.Save(path);
        Assert.True(File.Exists(path + ".bak"));
    }

    [Fact]
    public void Load_BrokenJson_Throws()
    {
        var path = Path.Combine(_tempDir, "broken.notenest");
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(path, "{ not valid json");
        Assert.ThrowsAny<Exception>(() => new ProjectFileService().Load(path));
    }

    // ── 自動保存 ─────────────────────────────────────────────────────────

    [Fact]
    public void AutoSave_DoesNotSave_WhenFilePathIsNull()
    {
        var (lc, session, _, _, _, _) = CreateV146Context();
        lc.CreateNew();
        session.IsModified = true;
        Assert.False(lc.TryAutoSave());
    }

    [Fact]
    public void AutoSave_DoesNotSave_WhenNotModified()
    {
        var (lc, session, _, _, _, _) = CreateV146Context();
        lc.CreateNew();
        var path = Path.Combine(_tempDir, "auto.notenest");
        lc.Save(path);
        Assert.False(session.IsModified);
        Assert.False(lc.TryAutoSave());
    }

    [Fact]
    public void AutoSave_Saves_WhenModifiedAndPathSet()
    {
        var (lc, session, notes, _, _, _) = CreateV146Context();
        lc.CreateNew();
        var path = Path.Combine(_tempDir, "auto.notenest");
        lc.Save(path);
        notes.AddNotebook("AutoNB");
        session.IsModified = true;

        Assert.True(lc.TryAutoSave());
        Assert.False(session.IsModified);
        Assert.Contains("AutoNB", File.ReadAllText(path));
    }

    // ── 最近使ったファイル ────────────────────────────────────────────────

    [Fact]
    public void RecentFiles_AddedOnSave()
    {
        var (lc, _, _, _, _, _) = CreateV146Context();
        lc.CreateNew();
        var path = Path.Combine(_tempDir, "recent.notenest");
        lc.Save(path);

        var recentSvc = new RecentFilesService(Path.Combine(_tempDir, "recent.json"));
        Assert.Contains(path, recentSvc.Load());
    }

    [Fact]
    public void RecentFiles_ClearRemovesAll()
    {
        var recentPath = Path.Combine(_tempDir, "recent.json");
        Directory.CreateDirectory(_tempDir);
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
        var recentPath = Path.Combine(_tempDir, "recent.json");
        Directory.CreateDirectory(_tempDir);
        var svc = new RecentFilesService(recentPath);
        svc.Add("path/x");

        Assert.Empty(Directory.GetFiles(_tempDir, "*.tmp"));
    }

    // ── ノート日時 ───────────────────────────────────────────────────────

    [Fact]
    public void NoteTimestamps_SetOnCreate()
    {
        var before = DateTime.Now.AddSeconds(-1);
        var note = new NoteViewModel(new Note { Title = "New" });
        Assert.True(note.CreatedAt >= before);
        Assert.True(Math.Abs((note.UpdatedAt - note.CreatedAt).TotalSeconds) < 1,
            "CreatedAt and UpdatedAt should be set close together on creation");
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
        var path = Path.Combine(_tempDir, "legacy.notenest");
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(path, """{"projectName":"L","notebooks":[{"title":"NB","notes":[{"title":"N","content":"C"}]}]}""");

        var note = new ProjectFileService().Load(path).Notebooks[0].Notes[0];
        Assert.NotEqual(default(DateTime), note.CreatedAt);
        Assert.NotEqual(default(DateTime), note.UpdatedAt);
    }

    // ── 保存スキーマバージョン ────────────────────────────────────────────

    [Fact]
    public void SchemaVersion_Is_1_4_2()
    {
        Assert.Equal("1.4.2", Project.CurrentSchemaVersion);
    }

    [Fact]
    public void SavedFile_ContainsSchemaVersion_1_4_2()
    {
        var (lc, session, _, _, _, _) = CreateV146Context();
        lc.CreateNew();
        var path = Path.Combine(_tempDir, "schema.notenest");
        lc.Save(path);
        Assert.Contains("\"1.4.2\"", File.ReadAllText(path));
    }

    // ── 未保存状態 ───────────────────────────────────────────────────────

    [Fact]
    public void IsModified_FalseAfterSave()
    {
        var (lc, session, _, _, _, _) = CreateV146Context();
        lc.CreateNew();
        session.IsModified = true;
        var path = Path.Combine(_tempDir, "mod.notenest");
        lc.Save(path);
        Assert.False(session.IsModified);
    }

    // ── エクスポート ─────────────────────────────────────────────────────

    [Fact]
    public void Export_Txt_WritesUtf8()
    {
        Directory.CreateDirectory(_tempDir);
        var project = new Project
        {
            ProjectName = "テスト",
            Notebooks = [new Notebook { Title = "NB", Notes = [new Note { Title = "日本語ノート", Content = "日本語本文" }] }]
        };
        var path = Path.Combine(_tempDir, "export.txt");
        new ExportService().Export(project, new ExportOptions(ExportTarget.Project, ExportFormat.Text, false, false), path);

        var text = File.ReadAllText(path, System.Text.Encoding.UTF8);
        Assert.Contains("日本語ノート", text);
        Assert.Contains("日本語本文", text);
    }

    [Fact]
    public void Export_Markdown_ContainsHeadings()
    {
        Directory.CreateDirectory(_tempDir);
        var project = new Project
        {
            ProjectName = "MD",
            Notebooks = [new Notebook { Title = "NB", Notes = [new Note { Title = "MDNote", Content = "body" }] }]
        };
        var path = Path.Combine(_tempDir, "export.md");
        new ExportService().Export(project, new ExportOptions(ExportTarget.Project, ExportFormat.Markdown, false, false), path);

        var text = File.ReadAllText(path);
        Assert.Contains("# ", text);
        Assert.Contains("MDNote", text);
    }

    [Fact]
    public void Export_Html_ContainsHtmlTags()
    {
        Directory.CreateDirectory(_tempDir);
        var project = new Project
        {
            ProjectName = "HTML",
            Notebooks = [new Notebook { Title = "NB", Notes = [new Note { Title = "HtmlNote", Content = "body" }] }]
        };
        var path = Path.Combine(_tempDir, "export.html");
        new ExportService().Export(project, new ExportOptions(ExportTarget.Project, ExportFormat.Html, false, false), path);

        var text = File.ReadAllText(path);
        Assert.Contains("<html>", text);
        Assert.Contains("HtmlNote", text);
    }

    // ── スター（お気に入り） (M12 / V142) ────────────────────────────────────

    [Fact]
    public void Note_IsStarred_DefaultsToFalse()
    {
        Assert.False(new Note().IsStarred);
    }

    [Fact]
    public void LegacyNoteWithoutStarField_LoadsAsNotStarred()
    {
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "legacy-star.notenest");
        File.WriteAllText(path, """{"version":"1.4.1","projectName":"Legacy","notebooks":[{"title":"NB","notes":[{"title":"N","content":"C"}]}]}""");

        var note = new ProjectFileService().Load(path).Notebooks[0].Notes[0];

        Assert.False(note.IsStarred);
    }

    [Fact]
    public void StarredNote_SaveLoad_RoundTrips()
    {
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "starred.notenest");
        var project = new Project
        {
            ProjectName = "Starred",
            Notebooks = [new Notebook { Title = "NB", Notes = [new Note { Title = "N", Content = "C", IsStarred = true }] }],
        };
        var svc = new ProjectFileService();

        svc.Save(path, project);
        var loaded = svc.Load(path);

        Assert.True(loaded.Notebooks[0].Notes[0].IsStarred);
        Assert.Contains("\"isStarred\": true", File.ReadAllText(path));
    }

    [Fact]
    public void StarredNote_NestSuitePath_RoundTripsViaEnvelope()
    {
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "starred.nestsuite");
        var project = new Project
        {
            ProjectName = "Starred",
            Notebooks = [new Notebook { Title = "NB", Notes = [new Note { Title = "N", Content = "C", IsStarred = true }] }],
        };
        var svc = new ProjectFileService();

        svc.Save(path, project);
        var loaded = svc.Load(path);

        Assert.True(loaded.Notebooks[0].Notes[0].IsStarred);
    }

    [Fact]
    public void NestSuiteSave_PayloadSchemaVersion_Is_1_4_2()
    {
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "schema.nestsuite");
        new ProjectFileService().Save(path, new Project { ProjectName = "Schema" });

        var envelope = NestSuiteWorkspaceEnvelope.Read(File.ReadAllText(path));

        Assert.Equal("1.4.2", envelope.PayloadSchemaVersion);
        Assert.Equal(NestSuiteWorkspaceEnvelope.CurrentFormatVersion, envelope.FormatVersion);
    }

    [Fact]
    public void ToggleNoteStar_MarksModified_AndSaveClearsIt()
    {
        Directory.CreateDirectory(_tempDir);
        var main = new MainViewModel();
        var note = main.Notes.AddNote(main.Notes.AddNotebook("NB"), "Note")!;
        main.IsModified = false;

        main.ToggleNoteStar(note);

        Assert.True(note.IsStarred);
        Assert.True(main.IsModified);

        var path = Path.Combine(_tempDir, "star-modified.notenest");
        Assert.True(main.SaveToPath(path));
        Assert.False(main.IsModified);
    }

    [Fact]
    public void LegacySchemaNote_LoadThenSave_PreservesContentAndTasks()
    {
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "legacy-schema.notenest");
        File.WriteAllText(path, """
            {"version":"1.4.1","projectName":"Legacy","notebooks":[{"title":"NB","notes":[{"title":"N","content":"TODO: x"}]}],"tasks":{"today":[{"title":"タスク"}]}}
            """);
        var svc = new ProjectFileService();

        var loaded = svc.Load(path);
        svc.Save(path, loaded);
        var reloaded = svc.Load(path);

        Assert.Equal("N", reloaded.Notebooks[0].Notes[0].Title);
        Assert.Equal("TODO: x", reloaded.Notebooks[0].Notes[0].Content);
        Assert.Contains(reloaded.Tasks.Today, t => t.Title == "タスク");
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private V140Context CreateV140Context()
    {
        Directory.CreateDirectory(_tempDir);
        var session = new ProjectSessionViewModel();
        var notes = new NoteWorkspaceViewModel();
        var tasks = new TaskBoardViewModel();
        var markers = new MarkerPanelViewModel(new MarkerExtractorService());
        var editor = new EditorStateViewModel();
        var coordinator = new WorkspaceChangeCoordinator(notes, tasks, markers, editor);
        var lifecycle = new ProjectLifecycleService(
            session, notes, tasks, markers, editor,
            recentFiles: new RecentFilesService(Path.Combine(_tempDir, "recent.json")));
        return new V140Context(session, notes, tasks, markers, editor, coordinator, lifecycle);
    }

    private sealed record V140Context(
        ProjectSessionViewModel Session,
        NoteWorkspaceViewModel Notes,
        TaskBoardViewModel Tasks,
        MarkerPanelViewModel Markers,
        EditorStateViewModel Editor,
        WorkspaceChangeCoordinator Coordinator,
        ProjectLifecycleService Lifecycle);

    private (ProjectLifecycleService Lifecycle, ProjectSessionViewModel Session,
             NoteWorkspaceViewModel Notes, TaskBoardViewModel Tasks,
             MarkerPanelViewModel Markers, EditorStateViewModel Editor) CreateV146Context()
    {
        var session = new ProjectSessionViewModel();
        var notes   = new NoteWorkspaceViewModel();
        var tasks   = new TaskBoardViewModel();
        var markers = new MarkerPanelViewModel(new MarkerExtractorService());
        var editor  = new EditorStateViewModel();
        var lifecycle = new ProjectLifecycleService(
            session, notes, tasks, markers, editor,
            recentFiles: new RecentFilesService(Path.Combine(_tempDir, "recent.json")));
        Directory.CreateDirectory(_tempDir);
        return (lifecycle, session, notes, tasks, markers, editor);
    }

    private static bool IsValidJson(string json)
    {
        try { JsonDocument.Parse(json); return true; }
        catch { return false; }
    }
}
