using System.Text.Json;
using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.13 TD-63: NoteNestFormatSchemaRegressionTests から、保存・読込・自動保存・.bak
/// バックアップ・最近使ったファイル・破損 JSON・SaveToPath 通知抑制契約（v2.14.12 SH-33）に
/// 関する回帰テストを分割した。「保存/読込を経ても状態が正しく保たれるか」を中心に扱う。
/// </summary>
public class NoteNestFormatRoundTripTests : IDisposable
{
    private readonly string _tempDir;

    public NoteNestFormatRoundTripTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NoteNestFormatRoundTripTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
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
        // v2.14.16 BUG: FontFamily は NestSuite の UI 設定（NoteNestEditorFontFamily）駆動の
        // 表示専用値になったため、ここで直接変更しても Workspace ファイルの
        // settings.fontFamily には反映されない（SavedFontFamily ＝ 読込時点の値のみを書き戻す）。
        context.Editor.FontFamily = "Meiryo UI";
        context.Editor.FontSize = 18;
        var task = context.Tasks.AddTask("today", "確認タスク")!;
        task.IsCompleted = true;
        task.Comment = "保存するコメント";
        context.Tasks.SetRelatedNote(task, note);
        context.Session.IsModified = true;
        var path = Path.Combine(_tempDir, "regression.notenest");

        context.Lifecycle.Save(path);

        // v2.14.16 BUG: 保存直後の payload に UI 設定駆動の FontFamily（"Meiryo UI"）が
        // 書き込まれていないことを確認する（サンプルプロジェクトの既定 "Yu Gothic UI" のまま）。
        using (var savedJson = JsonDocument.Parse(File.ReadAllText(path)))
            Assert.Equal("Yu Gothic UI", savedJson.RootElement.GetProperty("settings").GetProperty("fontFamily").GetString());

        context.Lifecycle.Open(path);

        var reloadedNote = Assert.Single(context.Notes.AllNotes.Where(item => item.Title == "リンク先"));
        var reloadedTask = Assert.Single(context.Tasks.TaskGroups.SelectMany(group => group.Tasks).Where(item => item.Title == "確認タスク"));
        Assert.Equal("[TODO] 本文と [[リンク先]]", reloadedNote.Content);
        Assert.True(reloadedTask.IsCompleted);
        Assert.Equal("保存するコメント", reloadedTask.Comment);
        Assert.Equal(reloadedNote.Id, reloadedTask.LinkedNoteId);
        Assert.Equal(reloadedNote.Id, context.Editor.SelectedNote?.Id);
        // v2.14.16 BUG: FontFamily は Workspace 保存対象から分離したため、
        // 保存前に直接変更した "Meiryo UI" ではなく、ファイルの settings.fontFamily
        // （読込時点の既定 "Yu Gothic UI"）が復元される。
        Assert.Equal("Yu Gothic UI", context.Editor.FontFamily);
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

    // v2.14.14 バグ修正: 既存ファイルを開いた直後、未保存表示に異常な分数（実機で観測された
    // 「未保存（1065313408分）」相当）が出ないことを回帰確認する。
    [Fact]
    public void Open_ExistingFile_DoesNotShowImplausibleUnsavedMinutes()
    {
        var (lc, session, _, _, _, _) = CreateV146Context();
        lc.CreateNew();
        var path = Path.Combine(_tempDir, "existing.notenest");
        lc.Save(path);

        lc.Open(path);

        Assert.False(session.IsModified);
        Assert.Equal("● 未保存", session.UnsavedIndicatorText);
        Assert.NotNull(session.LastSavedAt);
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
        Assert.Equal(Project.CurrentSchemaVersion, saved.Version);
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

    // ── v2.16.6 TD-64: 自動保存経路（createBackup: false）は .bak を更新しない ──

    [Fact]
    public void Save_CreateBackupFalse_DoesNotCreateBak()
    {
        var path = Path.Combine(_tempDir, "autosave-nobak.notenest");
        var svc = new ProjectFileService();
        svc.Save(path, new Project { ProjectName = "First" });

        svc.Save(path, new Project { ProjectName = "Second" }, createBackup: false);

        Assert.False(File.Exists(path + ".bak"));
    }

    [Fact]
    public void Save_CreateBackupFalse_DoesNotOverwriteExistingBak()
    {
        var path = Path.Combine(_tempDir, "autosave-preserve-bak.notenest");
        var svc = new ProjectFileService();
        svc.Save(path, new Project { ProjectName = "First" });
        svc.Save(path, new Project { ProjectName = "Second" }); // creates .bak containing "First"
        var bakPath = path + ".bak";
        Assert.True(File.Exists(bakPath));
        var bakContentBefore = File.ReadAllText(bakPath);

        svc.Save(path, new Project { ProjectName = "Third" }, createBackup: false);

        Assert.Equal(bakContentBefore, File.ReadAllText(bakPath));
        Assert.Contains("First", bakContentBefore);
    }

    [Fact]
    public void Save_CreateBackupFalse_StillUpdatesPrimaryFile()
    {
        var path = Path.Combine(_tempDir, "autosave-updates-primary.notenest");
        var svc = new ProjectFileService();
        svc.Save(path, new Project { ProjectName = "First" });

        svc.Save(path, new Project { ProjectName = "Second" }, createBackup: false);

        var loaded = svc.Load(path);
        Assert.Equal("Second", loaded.ProjectName);
    }

    [Fact]
    public void Save_CreateBackupFalse_NoTmpFileRemains()
    {
        var path = Path.Combine(_tempDir, "autosave-notmp.notenest");
        var svc = new ProjectFileService();
        svc.Save(path, new Project { ProjectName = "First" });

        svc.Save(path, new Project { ProjectName = "Second" }, createBackup: false);

        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Save_DefaultOverload_StillCreatesBak()
    {
        // createBackup を省略する既存呼び出し（手動保存 / Save All）は従来どおり .bak を作成する。
        var path = Path.Combine(_tempDir, "manual-still-bak.notenest");
        var svc = new ProjectFileService();
        svc.Save(path, new Project { ProjectName = "First" });

        svc.Save(path, new Project { ProjectName = "Second" });

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

    // ── v2.14.12 SH-33: SaveToPath(path, notifyOnError) の通知抑制契約 ──────────
    //
    // Shell 側 TrySaveIdeaNestToPath/TrySaveChatNestToPath の notifyOnError 配線は
    // TrySaveWorkspaceToPath という同じヘルパーを経由しており、NoteNest 側の
    // MainViewModel.SaveToPath(path, notifyOnError) と全く同じ if/else 分岐パターンを使う。
    // NestSuiteShellWindow は WPF Window で直接テストできない（Shell 上の private
    // ヘルパーは WPF ウィンドウに依存するため直接テストしない、という既存方針）ため、
    // ここでの NoteNest 側の検証が両方の notifyOnError 配線の正しさの代表確認となる。

    [Fact]
    public void SaveToPath_NotifyOnErrorFalse_FailureDoesNotInvokeShowErrorDialog()
    {
        var main = new MainViewModel();
        var dialogShown = false;
        main.ShowErrorDialog += (_, _) => dialogShown = true;

        // AtomicFileWriter は保存先ディレクトリを自動作成するため、既存の「ファイル」を
        // 親ディレクトリとして使うことで Directory.CreateDirectory を確実に失敗させる
        // （ChatNestFileServiceTests.Save_ThrowsWhenParentPathIsAFile と同じ手法）。
        var blockingFile = Path.Combine(_tempDir, "block-notify-false");
        File.WriteAllText(blockingFile, "x");
        try
        {
            var badPath = Path.Combine(blockingFile, "sub", "x.notenest");

            var result = main.SaveToPath(badPath, notifyOnError: false);

            Assert.False(result);
            Assert.False(dialogShown);
        }
        finally
        {
            File.Delete(blockingFile);
        }
    }

    [Fact]
    public void SaveToPath_NotifyOnErrorTrue_FailureInvokesShowErrorDialog_DefaultBehaviorPreserved()
    {
        var main = new MainViewModel();
        var dialogShown = false;
        main.ShowErrorDialog += (_, _) => dialogShown = true;

        var blockingFile = Path.Combine(_tempDir, "block-notify-true");
        File.WriteAllText(blockingFile, "x");
        try
        {
            var badPath = Path.Combine(blockingFile, "sub", "x.notenest");

            // 既定（1 引数版）が従来どおりダイアログ通知することの回帰確認。
            var result = main.SaveToPath(badPath);

            Assert.False(result);
            Assert.True(dialogShown);
        }
        finally
        {
            File.Delete(blockingFile);
        }
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
}
