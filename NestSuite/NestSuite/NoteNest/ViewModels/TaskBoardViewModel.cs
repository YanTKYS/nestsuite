using System.Collections.ObjectModel;
using System.ComponentModel;
using NestSuite.Models;

namespace NestSuite.ViewModels;

/// <summary>タスクグループとタスクのライフサイクルを所有します。</summary>
public sealed class TaskBoardViewModel
{
    public TaskBoardViewModel()
    {
        TaskGroups = new ObservableCollection<TaskGroupViewModel>
        {
            new("今日のタスク", TaskGroupKeys.Today),
            new("今週のタスク", TaskGroupKeys.Week),
            new("バックログ", TaskGroupKeys.Backlog),
        };
    }

    public event EventHandler? Changed;
    public event EventHandler? Loaded;
    public ObservableCollection<TaskGroupViewModel> TaskGroups { get; }

    /// <summary>v2.13.4 M16: タスク欄を互換表示するかどうかの判定。既存タスクが1件もない場合は false。</summary>
    public bool HasAnyTasks => TaskGroups.Any(g => g.Tasks.Count > 0);

    public string TotalIncompleteTaskCountText
    {
        get
        {
            var total = TaskGroups.Sum(g => g.Tasks.Count);
            if (total == 0) return "";
            var incomplete = TaskGroups.Sum(g => g.Tasks.Count(t => !t.IsCompleted));
            return $"（未完了 {incomplete}）";
        }
    }

    public void Load(TaskCollection tasks)
    {
        foreach (var group in TaskGroups)
            foreach (var task in group.Tasks.ToList())
            {
                task.PropertyChanged -= TaskPropertyChanged;
                group.RemoveTask(task);
            }
        LoadGroup(TaskGroupKeys.Today, tasks.Today);
        LoadGroup(TaskGroupKeys.Week, tasks.Week);
        LoadGroup(TaskGroupKeys.Backlog, tasks.Backlog);
        Loaded?.Invoke(this, EventArgs.Empty);
    }

    public TaskCollection BuildModel() => new()
    {
        Today = GetGroup(TaskGroupKeys.Today).Tasks.Select(task => task.Model).ToList(),
        Week = GetGroup(TaskGroupKeys.Week).Tasks.Select(task => task.Model).ToList(),
        Backlog = GetGroup(TaskGroupKeys.Backlog).Tasks.Select(task => task.Model).ToList(),
    };

    public TaskViewModel? AddTask(string groupKey, string title)
    {
        var group = TaskGroups.FirstOrDefault(candidate => candidate.Key == groupKey);
        if (group == null) return null;
        var task = new TaskViewModel(new NoteTask { Title = title });
        Track(task);
        group.AddTask(task);
        Changed?.Invoke(this, EventArgs.Empty);
        return task;
    }

    public bool DeleteTask(TaskViewModel task)
    {
        foreach (var group in TaskGroups)
        {
            if (!group.RemoveTask(task)) continue;
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }
        return false;
    }

    public bool MoveTaskToGroupAt(TaskViewModel source, TaskViewModel target)
    {
        var sourceGroup = TaskGroups.FirstOrDefault(group => group.Tasks.Contains(source));
        var targetGroup = TaskGroups.FirstOrDefault(group => group.Tasks.Contains(target));
        if (sourceGroup == null || targetGroup == null || source == target) return false;
        if (sourceGroup == targetGroup)
            sourceGroup.Tasks.Move(sourceGroup.Tasks.IndexOf(source), targetGroup.Tasks.IndexOf(target));
        else
        {
            sourceGroup.RemoveTask(source);
            targetGroup.InsertTask(targetGroup.Tasks.IndexOf(target), source);
        }
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public TaskGroupViewModel? MoveTask(TaskViewModel task, string targetGroupKey)
    {
        var sourceGroup = TaskGroups.FirstOrDefault(group => group.Tasks.Contains(task));
        var targetGroup = TaskGroups.FirstOrDefault(group => group.Key == targetGroupKey);
        if (sourceGroup == null || targetGroup == null || sourceGroup == targetGroup) return null;
        sourceGroup.RemoveTask(task);
        targetGroup.AddTask(task);
        Changed?.Invoke(this, EventArgs.Empty);
        return targetGroup;
    }

    public void RenameTask(TaskViewModel task, string newTitle) => task.Title = newTitle;

    public void UpdateComment(TaskViewModel task, string comment) => task.Comment = comment;

    public void SetRelatedNote(TaskViewModel task, NoteViewModel? note) => task.LinkedNoteId = note?.Id;

    public void ClearLinksToNoteIds(IEnumerable<string> deletedNoteIds)
    {
        var ids = new HashSet<string>(deletedNoteIds);
        foreach (var task in TaskGroups.SelectMany(group => group.Tasks))
            if (task.LinkedNoteId != null && ids.Contains(task.LinkedNoteId)) task.LinkedNoteId = null;
    }

    private TaskGroupViewModel GetGroup(string key) => TaskGroups.First(group => group.Key == key);

    private void LoadGroup(string key, IEnumerable<NoteTask> tasks)
    {
        var group = GetGroup(key);
        foreach (var model in tasks)
        {
            var task = new TaskViewModel(model);
            Track(task);
            group.AddTask(task);
        }
    }

    private void Track(TaskViewModel task) => task.PropertyChanged += TaskPropertyChanged;

    private void TaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TaskViewModel.Title)
            or nameof(TaskViewModel.IsCompleted)
            or nameof(TaskViewModel.Comment)
            or nameof(TaskViewModel.Priority)
            or nameof(TaskViewModel.DueDate)
            or nameof(TaskViewModel.LinkedNoteId))
            Changed?.Invoke(this, EventArgs.Empty);
    }
}
