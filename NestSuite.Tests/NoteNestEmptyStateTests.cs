using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// L23 (v2.18.1): NoteNest のノートブック・ノート・タスク・マーカー各一覧が空の場合の
/// 次操作ガイド表示条件（優先順位による重複抑制を含む）を確認する。
/// 新規 MainViewModel はサンプルプロジェクト（ノートブック・ノート・タスク・マーカーいずれも
/// 1件以上あり）を読み込むため、空状態を作るテストは明示的にクリアしてから検証する。
/// </summary>
public class NoteNestEmptyStateTests
{
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
}
