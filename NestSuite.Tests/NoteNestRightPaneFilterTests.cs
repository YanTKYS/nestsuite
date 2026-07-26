using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// L10: NoteNest右ペイン（マーカー一覧・互換タスク一覧）共通絞り込みの統合テスト。
/// 絞り込み文字列は <see cref="MainViewModel.RightPaneFilterText"/> が保持し、
/// <see cref="MarkerPanelViewModel.FilterText"/> / <see cref="TaskBoardViewModel.FilterText"/> へ
/// Trimして配布される。既存の種別フィルタとのAND条件・M15一括コピー連携・タブ単位の独立・
/// dirtyへの非影響を確認する。
/// </summary>
public class NoteNestRightPaneFilterTests
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

    // ── マーカーの絞り込み対象 ──────────────────────────────────────────────

    [Fact]
    public void MarkerFilter_NoFilterText_ShowsAllMarkers()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "会議メモ")!;
        note.Content = "[TODO] 予算担当へ確認";

        Assert.Single(main.FilteredMarkers);
    }

    [Fact]
    public void MarkerFilter_BodyMatch_ShowsMarker()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "会議メモ")!;
        note.Content = "[TODO] 予算担当へ確認";

        main.RightPaneFilterText = "予算";

        Assert.Single(main.FilteredMarkers);
    }

    [Fact]
    public void MarkerFilter_NoteTitleMatch_ShowsMarker()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "会議メモ")!;
        note.Content = "[TODO] 予算担当へ確認";

        main.RightPaneFilterText = "会議";

        Assert.Single(main.FilteredMarkers);
    }

    [Fact]
    public void MarkerFilter_TypeMatch_ShowsMarker()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "会議メモ")!;
        note.Content = "[TODO] 予算担当へ確認";

        main.RightPaneFilterText = "TODO";

        Assert.Single(main.FilteredMarkers);
    }

    [Fact]
    public void MarkerFilter_NoMatch_HidesMarker()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "会議メモ")!;
        note.Content = "[TODO] 予算担当へ確認";

        main.RightPaneFilterText = "契約";

        Assert.Empty(main.FilteredMarkers);
    }

    // ── 既存の種別フィルタとのAND条件 ───────────────────────────────────────

    [Fact]
    public void MarkerFilter_CombinesWithTypeFilter_AsAndCondition()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "会議メモ")!;
        note.Content = "[TODO] 予算担当へ確認\n[NOTE] 予算の補足";

        main.FilterTodo = true;
        main.FilterNote = false;
        main.RightPaneFilterText = "予算";

        var matched = main.FilteredMarkers.ToList();
        Assert.Single(matched);
        Assert.Equal("TODO", matched[0].Type);
    }

    [Fact]
    public void MarkerFilter_TypeFilterExcludesAll_EvenIfTextMatches()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "会議メモ")!;
        note.Content = "[TODO] 予算担当へ確認";

        main.FilterTodo = false;
        main.RightPaneFilterText = "予算";

        Assert.Empty(main.FilteredMarkers);
    }

    // ── 大文字・小文字 / Trim ──────────────────────────────────────────────

    [Theory]
    [InlineData("test")]
    [InlineData("TEST")]
    [InlineData("Test")]
    public void MarkerFilter_CaseInsensitive(string query)
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "N")!;
        note.Content = "[TODO] test marker";

        main.RightPaneFilterText = query;

        Assert.Single(main.FilteredMarkers);
    }

    [Fact]
    public void MarkerFilter_SurroundingWhitespace_IsTrimmed()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "N")!;
        note.Content = "[TODO] 予算担当へ確認";

        main.RightPaneFilterText = "  予算  ";

        Assert.Single(main.FilteredMarkers);
    }

    [Fact]
    public void MarkerFilter_WhitespaceOnly_TreatedAsNoFilter()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "N")!;
        note.Content = "[TODO] 予算担当へ確認";

        main.RightPaneFilterText = "   ";

        Assert.False(main.HasRightPaneFilterText);
        Assert.Single(main.FilteredMarkers);
    }

    // ── 検索解除 ────────────────────────────────────────────────────────────

    [Fact]
    public void MarkerFilter_ClearedAfterFiltering_ShowsAllAgain()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "N")!;
        note.Content = "[TODO] 予算\n[NOTE] 契約";

        main.RightPaneFilterText = "予算";
        Assert.Single(main.FilteredMarkers);

        main.RightPaneFilterText = string.Empty;
        Assert.Equal(2, main.FilteredMarkers.Count());
    }

    // ── マーカー空状態 ──────────────────────────────────────────────────────

    [Fact]
    public void MarkerFilter_ResultingInZero_ShowsFilterEmptyStateText()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "N")!;
        note.Content = "[TODO] 予算";

        main.RightPaneFilterText = "契約";

        Assert.True(main.ShowMarkerEmptyState);
        Assert.Equal("一致するマーカーはありません", main.MarkerEmptyStateText);
        Assert.False(main.HasFilteredMarkers);
    }

    [Fact]
    public void MarkerFilter_NoMarkersAtAll_ShowsOriginalGuidanceText()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        main.Notes.AddNote(nb, "マーカーなしノート");

        Assert.True(main.ShowMarkerEmptyState);
        Assert.Contains("本文の [TODO] [FIXME] [NOTE]", main.MarkerEmptyStateText);
    }

    // ── タスクの絞り込み対象（タスクタイトル一致採用） ─────────────────────

    [Fact]
    public void TaskFilter_TitleMatch_ShowsTask()
    {
        var main = new MainViewModel();
        ClearAllTasks(main);
        var task = main.Tasks.AddTask("today", "予算資料の作成")!;
        main.Tasks.AddTask("today", "会議室の予約");

        main.RightPaneFilterText = "予算";

        var group = main.TaskGroups.First(g => g.Key == "today");
        Assert.Contains(task, group.IncompleteTasks);
        Assert.Single(group.IncompleteTasks);
    }

    [Fact]
    public void TaskFilter_NoMatch_HidesTask()
    {
        var main = new MainViewModel();
        ClearAllTasks(main);
        main.Tasks.AddTask("today", "予算資料の作成");

        main.RightPaneFilterText = "契約";

        var group = main.TaskGroups.First(g => g.Key == "today");
        Assert.Empty(group.IncompleteTasks);
        Assert.False(group.HasVisibleTasks);
    }

    [Fact]
    public void TaskFilter_MatchesBothIncompleteAndCompleted()
    {
        var main = new MainViewModel();
        ClearAllTasks(main);
        var t1 = main.Tasks.AddTask("today", "予算資料の作成")!;
        var t2 = main.Tasks.AddTask("today", "予算の確認")!;
        t2.IsCompleted = true;

        main.RightPaneFilterText = "予算";

        var group = main.TaskGroups.First(g => g.Key == "today");
        Assert.Single(group.IncompleteTasks);
        Assert.Single(group.CompletedTasks);
    }

    [Fact]
    public void TaskFilter_GroupNameMatch_ShowsAllTasksInGroup()
    {
        var main = new MainViewModel();
        ClearAllTasks(main);
        main.Tasks.AddTask("today", "資料作成");
        main.Tasks.AddTask("today", "会議準備");

        // "今日のタスク" グループ名自体への一致で、グループ内の全タスクを表示する契約。
        main.RightPaneFilterText = "今日";

        var group = main.TaskGroups.First(g => g.Key == "today");
        Assert.Equal(2, group.IncompleteTasks.Count());
    }

    [Fact]
    public void TaskFilter_CaseInsensitive()
    {
        var main = new MainViewModel();
        ClearAllTasks(main);
        main.Tasks.AddTask("today", "TEST task");

        main.RightPaneFilterText = "test";

        var group = main.TaskGroups.First(g => g.Key == "today");
        Assert.Single(group.IncompleteTasks);
    }

    // ── タスク空状態 ────────────────────────────────────────────────────────

    [Fact]
    public void TaskFilter_ResultingInZero_ShowsTaskFilterEmptyState()
    {
        var main = new MainViewModel();
        ClearAllTasks(main);
        main.Tasks.AddTask("today", "予算資料の作成");

        main.RightPaneFilterText = "契約";

        Assert.True(main.ShowTaskFilterEmptyState);
        Assert.False(main.HasAnyVisibleTasks);
    }

    [Fact]
    public void TaskFilter_NoTasksAtAll_DoesNotShowTaskFilterEmptyState()
    {
        var main = new MainViewModel();
        ClearAllTasks(main);

        main.RightPaneFilterText = "契約";

        // タスクが元々0件の場合は ShowTaskEmptyState 側の既存挙動を維持し、
        // ShowTaskFilterEmptyState は表示しない（HasAnyTasks が false のため）。
        Assert.False(main.ShowTaskFilterEmptyState);
    }

    [Fact]
    public void TaskFilter_NoFilterText_DoesNotShowTaskFilterEmptyState()
    {
        var main = new MainViewModel();
        ClearAllTasks(main);
        main.Tasks.AddTask("today", "予算資料の作成");

        Assert.False(main.ShowTaskFilterEmptyState);
    }

    // ── タブ単位（MainViewModelインスタンスごとの独立） ─────────────────────

    [Fact]
    public void RightPaneFilterText_IsIndependentPerMainViewModelInstance()
    {
        var mainA = new MainViewModel();
        var mainB = new MainViewModel();

        mainA.RightPaneFilterText = "予算";

        Assert.Equal("予算", mainA.RightPaneFilterText);
        Assert.Equal(string.Empty, mainB.RightPaneFilterText);
    }

    // ── dirty ───────────────────────────────────────────────────────────────

    [Fact]
    public void RightPaneFilterText_DoesNotMarkModified()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "N")!;
        note.Content = "[TODO] 予算";
        main.IsModified = false;

        main.RightPaneFilterText = "予算";
        Assert.False(main.IsModified);

        main.RightPaneFilterText = string.Empty;
        Assert.False(main.IsModified);
    }

    [Fact]
    public void RightPaneFilterText_DoesNotChangeNoteOrMarkerContent()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "N")!;
        note.Content = "[TODO] 予算担当へ確認";
        var contentBefore = note.Content;
        var markerCountBefore = main.MarkerCount;

        main.RightPaneFilterText = "予算";

        Assert.Equal(contentBefore, note.Content);
        Assert.Equal(markerCountBefore, main.MarkerCount);
    }

    [Fact]
    public void RightPaneFilterText_DoesNotChangeTaskData()
    {
        var main = new MainViewModel();
        ClearAllTasks(main);
        var task = main.Tasks.AddTask("today", "予算資料の作成")!;

        main.RightPaneFilterText = "契約";

        Assert.Equal("予算資料の作成", task.Title);
        Assert.False(task.IsCompleted);
    }

    // ── M15一括コピー連携 ───────────────────────────────────────────────────

    [Fact]
    public void FormatMarkers_WithFilterActive_OnlyIncludesVisibleMarkers()
    {
        var main = new MainViewModel();
        ClearAllNotebooks(main);
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "N")!;
        note.Content = "[TODO] 予算\n[NOTE] 契約";

        main.RightPaneFilterText = "予算";

        var markdown = NoteNestRightPaneMarkdownFormatter.FormatMarkers(main.FilteredMarkers);
        Assert.Contains("予算", markdown);
        Assert.DoesNotContain("契約", markdown);
        Assert.Single(main.FilteredMarkers);
    }

    [Fact]
    public void FormatTasks_WithFilterActive_OnlyIncludesVisibleTasks()
    {
        var main = new MainViewModel();
        ClearAllTasks(main);
        main.Tasks.AddTask("today", "予算資料の作成");
        main.Tasks.AddTask("today", "契約書の確認");

        main.RightPaneFilterText = "予算";

        var markdown = NoteNestRightPaneMarkdownFormatter.FormatTasks(main.TaskGroups);
        Assert.Contains("予算資料の作成", markdown);
        Assert.DoesNotContain("契約書の確認", markdown);

        var visibleCount = main.TaskGroups.SelectMany(g => g.IncompleteTasks.Concat(g.CompletedTasks)).Count();
        Assert.Equal(1, visibleCount);
    }
}
