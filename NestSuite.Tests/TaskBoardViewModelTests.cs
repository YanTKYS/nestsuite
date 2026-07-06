using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

public class TaskBoardViewModelTests
{
    [Fact]
    public void MoveTaskAndBuildModelPreserveGroupOwnership()
    {
        var board = new TaskBoardViewModel();
        var changeCount = 0;
        board.Changed += (_, _) => changeCount++;
        var task = board.AddTask("today", "Task")!;

        Assert.NotNull(board.MoveTask(task, "backlog"));
        var model = board.BuildModel();
        Assert.Empty(model.Today);
        Assert.Equal("Task", Assert.Single(model.Backlog).Title);
        Assert.Equal(2, changeCount);
    }

    [Fact]
    public void DirectPersistentTaskChangesRaiseChanged()
    {
        var board = new TaskBoardViewModel();
        var task = board.AddTask("today", "Task")!;
        var changeCount = 0;
        board.Changed += (_, _) => changeCount++;

        task.Title = "Renamed";
        task.Comment = "Comment";
        task.LinkedNoteId = "note-id";

        Assert.Equal(3, changeCount);
    }

    [Fact]
    public void ClearLinksRaisesChangedOnlyWhenPersistentDataChanges()
    {
        var board = new TaskBoardViewModel();
        var task = board.AddTask("today", "Task")!;
        task.LinkedNoteId = "note-id";
        var changeCount = 0;
        board.Changed += (_, _) => changeCount++;

        board.ClearLinksToNoteIds(new[] { "other-id" });
        Assert.Equal(0, changeCount);

        board.ClearLinksToNoteIds(new[] { "note-id" });
        Assert.Equal(1, changeCount);
        Assert.Null(task.LinkedNoteId);
    }

    // v2.13.4 M16: 既存タスクが1件もない場合に右ペインのタスク欄を互換表示しないための判定
    [Fact]
    public void HasAnyTasks_FalseWhenAllGroupsEmpty_TrueAfterAddingToAnyGroup()
    {
        var board = new TaskBoardViewModel();
        Assert.False(board.HasAnyTasks);

        board.AddTask("backlog", "Task");
        Assert.True(board.HasAnyTasks);
    }
}
