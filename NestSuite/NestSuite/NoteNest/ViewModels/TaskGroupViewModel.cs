using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace NestSuite.ViewModels;

public class TaskGroupViewModel : BaseViewModel
{
    private bool _isExpanded = true;
    private bool _isCompletedSectionExpanded = true;

    public TaskGroupViewModel(string title, string key)
    {
        Title = title;
        Key = key;
        Tasks = new ObservableCollection<TaskViewModel>();
        Tasks.CollectionChanged += (_, _) =>
        {
            RefreshCount();
            RefreshTaskViews();
        };
        ToggleCompletedSectionCommand = new RelayCommand(() => IsCompletedSectionExpanded = !IsCompletedSectionExpanded);
    }

    public string Title { get; }
    public string Key { get; }
    public ObservableCollection<TaskViewModel> Tasks { get; }
    public ICommand ToggleCompletedSectionCommand { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsCompletedSectionExpanded
    {
        get => _isCompletedSectionExpanded;
        set => SetProperty(ref _isCompletedSectionExpanded, value);
    }

    public IEnumerable<TaskViewModel> IncompleteTasks => Tasks.Where(t => !t.IsCompleted);
    public IEnumerable<TaskViewModel> CompletedTasks  => Tasks.Where(t => t.IsCompleted);
    public bool HasCompletedTasks => Tasks.Any(t => t.IsCompleted);
    public string CompletedCountText => $"完了済み（{Tasks.Count(t => t.IsCompleted)}）";
    public string CountText => $"{Tasks.Count(t => !t.IsCompleted)}/{Tasks.Count}";

    /// <summary>v2.13.4 M16: 既存タスクがないグループを右ペインに表示しないための判定。</summary>
    public bool HasTasks => Tasks.Count > 0;

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

    public void RefreshCount()
    {
        OnPropertyChanged(nameof(CountText));
        OnPropertyChanged(nameof(HasTasks));
    }

    private void RefreshTaskViews()
    {
        OnPropertyChanged(nameof(IncompleteTasks));
        OnPropertyChanged(nameof(CompletedTasks));
        OnPropertyChanged(nameof(HasCompletedTasks));
        OnPropertyChanged(nameof(CompletedCountText));
    }

    private void Task_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TaskViewModel.IsCompleted))
        {
            RefreshCount();
            RefreshTaskViews();
        }
    }
}
