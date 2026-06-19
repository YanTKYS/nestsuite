using System.Collections.ObjectModel;
using System.ComponentModel;

namespace NoteNest.ViewModels;

public class TaskGroupViewModel : BaseViewModel
{
    private bool _isExpanded = true;
    private bool _hideCompleted = false;

    public TaskGroupViewModel(string title, string key)
    {
        Title = title;
        Key = key;
        Tasks = new ObservableCollection<TaskViewModel>();
        Tasks.CollectionChanged += (_, _) =>
        {
            RefreshCount();
            OnPropertyChanged(nameof(VisibleTasks));
        };
    }

    public string Title { get; }
    public string Key { get; }
    public ObservableCollection<TaskViewModel> Tasks { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool HideCompleted
    {
        get => _hideCompleted;
        set
        {
            SetProperty(ref _hideCompleted, value);
            OnPropertyChanged(nameof(VisibleTasks));
        }
    }

    // When HideCompleted=false, return Tasks directly so WPF observes CollectionChanged.
    // When HideCompleted=true, return a snapshot; OnPropertyChanged(VisibleTasks) is raised
    // whenever a task's IsCompleted changes or the collection changes.
    public IEnumerable<TaskViewModel> VisibleTasks =>
        _hideCompleted ? Tasks.Where(t => !t.IsCompleted) : Tasks;

    public string CountText => $"{Tasks.Count(t => !t.IsCompleted)}/{Tasks.Count}";

    public void AddTask(TaskViewModel task)
    {
        task.PropertyChanged += Task_PropertyChanged;
        Tasks.Add(task);
    }

    public void InsertTask(int index, TaskViewModel task)
    {
        task.PropertyChanged += Task_PropertyChanged;
        Tasks.Insert(index, task);
    }

    public bool RemoveTask(TaskViewModel task)
    {
        task.PropertyChanged -= Task_PropertyChanged;
        return Tasks.Remove(task);
    }

    public void RefreshCount() => OnPropertyChanged(nameof(CountText));

    private void Task_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TaskViewModel.IsCompleted))
        {
            RefreshCount();
            OnPropertyChanged(nameof(VisibleTasks));
        }
    }
}
