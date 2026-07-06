using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

public class TaskGroupViewModelTests
{
    private static TaskViewModel Task(string title, bool completed = false)
        => new(new NoteTask { Title = title, IsCompleted = completed });

    [Fact]
    public void AddTask_IncreasesCount()
    {
        var group = new TaskGroupViewModel("Today", "today");
        group.AddTask(Task("A"));

        Assert.Single(group.Tasks);
    }

    [Fact]
    public void RemoveTask_ReturnsTrue_AndDecreasesCount()
    {
        var group = new TaskGroupViewModel("Today", "today");
        var t = Task("A");
        group.AddTask(t);

        var removed = group.RemoveTask(t);

        Assert.True(removed);
        Assert.Empty(group.Tasks);
    }

    [Fact]
    public void RemoveTask_AbsentTask_ReturnsFalse()
    {
        var group = new TaskGroupViewModel("Today", "today");
        var result = group.RemoveTask(Task("Ghost"));

        Assert.False(result);
    }

    [Fact]
    public void InsertTask_PlacedAtCorrectIndex()
    {
        var group = new TaskGroupViewModel("Today", "today");
        var a = Task("A");
        var b = Task("B");
        var c = Task("C");
        group.AddTask(a);
        group.AddTask(b);
        group.InsertTask(1, c);

        Assert.Equal("A", group.Tasks[0].Title);
        Assert.Equal("C", group.Tasks[1].Title);
        Assert.Equal("B", group.Tasks[2].Title);
    }

    [Fact]
    public void IncompleteTasks_And_CompletedTasks_CoverAllTasks()
    {
        var group = new TaskGroupViewModel("Today", "today");
        group.AddTask(Task("A", completed: true));
        group.AddTask(Task("B", completed: false));

        Assert.Equal(2, group.IncompleteTasks.Count() + group.CompletedTasks.Count());
    }

    [Fact]
    public void IncompleteTasks_ExcludesCompleted()
    {
        var group = new TaskGroupViewModel("Today", "today");
        group.AddTask(Task("A", completed: true));
        group.AddTask(Task("B", completed: false));

        var incomplete = group.IncompleteTasks.ToList();

        Assert.Single(incomplete);
        Assert.Equal("B", incomplete[0].Title);
    }

    [Fact]
    public void CountText_ReflectsIncompleteSlashTotal()
    {
        var group = new TaskGroupViewModel("Today", "today");
        group.AddTask(Task("A", completed: true));
        group.AddTask(Task("B", completed: false));

        Assert.Equal("1/2", group.CountText);
    }

    // v2.13.4 M16: 既存タスクがないグループを右ペインの互換表示から隠すための判定
    [Fact]
    public void HasTasks_FalseWhenEmpty_TrueAfterAdd_FalseAfterRemove()
    {
        var group = new TaskGroupViewModel("Today", "today");
        Assert.False(group.HasTasks);

        var task = Task("A");
        group.AddTask(task);
        Assert.True(group.HasTasks);

        group.RemoveTask(task);
        Assert.False(group.HasTasks);
    }

    [Fact]
    public void HasTasks_RaisesPropertyChanged_OnAddAndRemove()
    {
        var group = new TaskGroupViewModel("Today", "today");
        var changed = new List<string?>();
        group.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        var task = Task("A");
        group.AddTask(task);
        Assert.Contains(nameof(TaskGroupViewModel.HasTasks), changed);

        changed.Clear();
        group.RemoveTask(task);
        Assert.Contains(nameof(TaskGroupViewModel.HasTasks), changed);
    }
}
