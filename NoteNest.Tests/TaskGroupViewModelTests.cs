using NoteNest.Models;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

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
    public void VisibleTasks_HideCompletedFalse_ShowsAll()
    {
        var group = new TaskGroupViewModel("Today", "today");
        group.AddTask(Task("A", completed: true));
        group.AddTask(Task("B", completed: false));

        Assert.Equal(2, group.VisibleTasks.Count());
    }

    [Fact]
    public void VisibleTasks_HideCompletedTrue_ExcludesCompleted()
    {
        var group = new TaskGroupViewModel("Today", "today");
        group.AddTask(Task("A", completed: true));
        group.AddTask(Task("B", completed: false));
        group.HideCompleted = true;

        var visible = group.VisibleTasks.ToList();

        Assert.Single(visible);
        Assert.Equal("B", visible[0].Title);
    }

    [Fact]
    public void CountText_ReflectsIncompleteSlashTotal()
    {
        var group = new TaskGroupViewModel("Today", "today");
        group.AddTask(Task("A", completed: true));
        group.AddTask(Task("B", completed: false));

        Assert.Equal("1/2", group.CountText);
    }
}
