using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// L23 (v2.18.1): NoteNest のノートブック・ノート・タスク・マーカー各一覧が空の場合の
/// 次操作ガイド表示条件（優先順位による重複抑制を含む）を確認する。
/// 新規 MainViewModel はサンプルプロジェクト（ノートブック・ノート・タスク・マーカーいずれも
/// 1件以上あり）を読み込むため、空状態を作るテストは明示的にクリアしてから検証する。
/// </summary>
public class NoteNestEmptyStateTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "NoteNestEmptyStateTests_" + Guid.NewGuid().ToString("N"));

    public NoteNestEmptyStateTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private static void ClearAllNotebooks(MainViewModel main)
    {
        foreach (var notebook in main.Notebooks.ToList())
            main.DeleteNotebook(notebook);
    }

    private static void ClearAllTasks(MainViewModel main)
    {
        foreach (var task in main.TaskGroups.SelectMany(g => g.Tasks).ToList())
            main.Tasks.DeleteTask(task);
    }

    [Fact]
    public void SampleProject_HasNoEmptyStatesActive()
    {
        var main = new MainViewModel();

        Assert.False(main.ShowNotebookEmptyState);
        Assert.False(main.ShowNoteEmptyState);
        Assert.False(main.ShowTaskEmptyState);
        Assert.False(main.ShowMarkerEmptyState);
    }

    // ── ノートブック空状態 ───────────────────────────────────────────────

    [Fact]
    public void NoNotebooks_ShowsNotebookEmptyState_Only()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);

        Assert.False(main.HasNotebooks);
        Assert.True(main.ShowNotebookEmptyState);
        Assert.False(main.ShowNoteEmptyState);
        Assert.False(main.ShowTaskEmptyState);
        Assert.False(main.ShowMarkerEmptyState);
    }

    [Fact]
    public void CreatingNotebook_HidesNotebookEmptyState()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        Assert.True(main.ShowNotebookEmptyState);

        main.AddNotebookWithTitle("最初のノートブック");

        Assert.False(main.ShowNotebookEmptyState);
    }

    // ── ノート空状態（ノートブック空状態との重複抑制） ────────────────────

    [Fact]
    public void NotebookExistsButNoNotes_ShowsNoteEmptyState_NotNotebookEmptyState()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        main.Notes.AddNotebook("空のノートブック");

        Assert.False(main.ShowNotebookEmptyState);
        Assert.True(main.ShowNoteEmptyState);
    }

    [Fact]
    public void AddingNoteToEmptyNotebook_HidesNoteEmptyState()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        Assert.True(main.ShowNoteEmptyState);

        main.Notes.AddNote(nb, "最初のノート");

        Assert.False(main.ShowNoteEmptyState);
    }

    [Fact]
    public void DeletingLastNote_ReshowsNoteEmptyState()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "ノート")!;
        Assert.False(main.ShowNoteEmptyState);

        main.DeleteNote(note);

        Assert.True(main.ShowNoteEmptyState);
    }

    [Fact]
    public void DeletingAllNotebooks_HidesNoteEmptyState_ShowsNotebookEmptyStateInstead()
    {
        // ノートブック自体が無くなった場合は、ノート側の重複案内を出さない。
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        Assert.True(main.ShowNoteEmptyState);

        main.DeleteNotebook(nb);

        Assert.True(main.ShowNotebookEmptyState);
        Assert.False(main.ShowNoteEmptyState);
    }

    // ── タスク空状態（ノートが無い間は表示しない） ────────────────────────

    [Fact]
    public void NotesExistButNoTasks_ShowsTaskEmptyState()
    {
        var main = new MainViewModel();
        ClearAllTasks(main);

        Assert.True(main.HasAnyNotes);
        Assert.True(main.ShowTaskEmptyState);
    }

    [Fact]
    public void AddingTask_HidesTaskEmptyState()
    {
        var main = new MainViewModel();
        ClearAllTasks(main);
        Assert.True(main.ShowTaskEmptyState);

        main.Tasks.AddTask(TaskGroupKeys.Today, "タスク");

        Assert.False(main.ShowTaskEmptyState);
    }

    [Fact]
    public void DeletingLastTask_ReshowsTaskEmptyState()
    {
        var main = new MainViewModel();
        ClearAllTasks(main);
        main.Tasks.AddTask(TaskGroupKeys.Today, "タスク");
        var task = main.TaskGroups.SelectMany(g => g.Tasks).Single();
        Assert.False(main.ShowTaskEmptyState);

        main.Tasks.DeleteTask(task);

        Assert.True(main.ShowTaskEmptyState);
    }

    [Fact]
    public void NoNotebooksOrNotes_DoesNotShowTaskEmptyState()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        ClearAllTasks(main);

        Assert.True(main.ShowNotebookEmptyState);
        Assert.False(main.HasAnyNotes);
        Assert.False(main.ShowTaskEmptyState);
    }

    // ── マーカー空状態（ノートが無い間は表示しない） ──────────────────────

    [Fact]
    public void NotesWithoutMarkers_ShowsMarkerEmptyState()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        main.Notes.AddNote(nb, "マーカーなしノート");

        Assert.True(main.ShowMarkerEmptyState);
    }

    [Fact]
    public void AddingMarkerToNoteContent_HidesMarkerEmptyState()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "ノート")!;
        Assert.True(main.ShowMarkerEmptyState);

        note.Content = "[TODO] やること";

        Assert.False(main.ShowMarkerEmptyState);
    }

    [Fact]
    public void RemovingMarkerFromNoteContent_ReshowsMarkerEmptyState()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "ノート")!;
        note.Content = "[TODO] やること";
        Assert.False(main.ShowMarkerEmptyState);

        note.Content = "普通の本文";

        Assert.True(main.ShowMarkerEmptyState);
    }

    [Fact]
    public void NoNotebooksOrNotes_DoesNotShowMarkerEmptyState()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);

        Assert.True(main.ShowNotebookEmptyState);
        Assert.False(main.ShowMarkerEmptyState);
    }

    // ── PropertyChanged 通知 ───────────────────────────────────────────────

    [Fact]
    public void CreatingNotebook_PublishesEmptyStatePropertyChanges()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var changed = new List<string?>();
        main.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        main.Notes.AddNotebook("NB");

        Assert.Contains(nameof(MainViewModel.HasNotebooks), changed);
        Assert.Contains(nameof(MainViewModel.ShowNotebookEmptyState), changed);
        Assert.Contains(nameof(MainViewModel.ShowNoteEmptyState), changed);
    }

    [Fact]
    public void AddingTask_PublishesShowTaskEmptyStateChange()
    {
        var main = new MainViewModel();
        ClearAllTasks(main);
        var changed = new List<string?>();
        main.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        main.Tasks.AddTask(TaskGroupKeys.Today, "タスク");

        Assert.Contains(nameof(MainViewModel.ShowTaskEmptyState), changed);
    }

    [Fact]
    public void EditingNoteContentMarkers_PublishesShowMarkerEmptyStateChange()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "ノート")!;
        var changed = new List<string?>();
        main.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        note.Content = "[TODO] やること";

        Assert.Contains(nameof(MainViewModel.ShowMarkerEmptyState), changed);
    }

    // ── L25 (review7-fable5 REV7-2): ファイル読込直後の空状態表示更新 ──────────

    [Fact]
    public void OpeningFileWithNotebooksAndNotes_ClearsEmptyStates_WithoutMarkingModified()
    {
        var path = Path.Combine(_tempDir, "with-content.notenest");
        new MainViewModel().SaveToPath(path); // サンプルプロジェクトのまま保存(ノートブック・ノートあり)

        var main = new MainViewModel();
        ClearAllNotebooks(main);
        Assert.True(main.ShowNotebookEmptyState);

        var result = main.OpenFileAtStartup(path);

        Assert.True(result);
        Assert.False(main.ShowNotebookEmptyState);
        Assert.False(main.ShowNoteEmptyState);
        Assert.True(main.HasNotebooks);
        Assert.True(main.HasAnyNotes);
        Assert.False(main.IsNoteListEmpty);
        Assert.False(main.IsModified);
    }

    [Fact]
    public void OpeningEmptyFile_ShowsNotebookEmptyState_WithoutMarkingModified()
    {
        var path = Path.Combine(_tempDir, "empty.notenest");
        var writer = new MainViewModel();
        writer.NewProjectCommand.Execute(null);
        writer.SaveToPath(path);

        var main = new MainViewModel(); // サンプルプロジェクトのまま(空状態は非表示)
        Assert.False(main.ShowNotebookEmptyState);

        var result = main.OpenFileAtStartup(path);

        Assert.True(result);
        Assert.True(main.ShowNotebookEmptyState);
        Assert.False(main.ShowNoteEmptyState);
        Assert.False(main.HasNotebooks);
        Assert.False(main.HasAnyNotes);
        Assert.False(main.IsModified);
    }

    [Fact]
    public void OpeningFileWithNotebookButNoNotes_ShowsNoteEmptyState_WithoutMarkingModified()
    {
        var path = Path.Combine(_tempDir, "empty-notebook.notenest");
        var writer = new MainViewModel();
        writer.NewProjectCommand.Execute(null);
        writer.Notes.AddNotebook("空のノートブック");
        writer.SaveToPath(path);

        var main = new MainViewModel();

        var result = main.OpenFileAtStartup(path);

        Assert.True(result);
        Assert.False(main.ShowNotebookEmptyState);
        Assert.True(main.ShowNoteEmptyState);
        Assert.False(main.IsModified);
    }

    [Fact]
    public void OpeningFile_PublishesEmptyStatePropertyChanges_ExactlyOnceAfterLoad()
    {
        var path = Path.Combine(_tempDir, "notify-once.notenest");
        var writer = new MainViewModel();
        writer.NewProjectCommand.Execute(null);
        writer.SaveToPath(path);

        var main = new MainViewModel();
        var changed = new List<string?>();
        main.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        main.OpenFileAtStartup(path);

        Assert.Equal(1, changed.Count(name => name == nameof(MainViewModel.ShowNotebookEmptyState)));
        Assert.Equal(1, changed.Count(name => name == nameof(MainViewModel.HasNotebooks)));
    }
}
