using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using NestSuite.Services;

namespace NestSuite.ViewModels;

public class TaskGroupViewModel : BaseViewModel
{
    private bool _isExpanded = true;
    private bool _isCompletedSectionExpanded = true;
    private string _filterText = string.Empty;

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

    /// <summary>
    /// 右ペイン共通絞り込み文字列（Trim済み）。<see cref="TaskBoardViewModel.FilterText"/> から
    /// 各グループへ配布される表示専用の一時状態で、保存対象ではない。
    /// </summary>
    public string FilterText
    {
        get => _filterText;
        set
        {
            if (!SetProperty(ref _filterText, value ?? string.Empty)) return;
            RefreshTaskViews();
        }
    }

    /// <summary>
    /// 絞り込み文字列がグループ名に一致した場合はグループ内の全タスクを表示し、
    /// それ以外はタスクタイトルが一致したものだけを表示する。
    /// </summary>
    public IEnumerable<TaskViewModel> IncompleteTasks => FilterTasks(Tasks.Where(t => !t.IsCompleted));
    public IEnumerable<TaskViewModel> CompletedTasks  => FilterTasks(Tasks.Where(t => t.IsCompleted));
    public bool HasCompletedTasks => CompletedTasks.Any();
    public string CompletedCountText => $"完了済み（{CompletedTasks.Count()}）";
    public string CountText => $"{Tasks.Count(t => !t.IsCompleted)}/{Tasks.Count}";

    /// <summary>既存タスクがないグループを右ペインに表示しないための判定。</summary>
    public bool HasTasks => Tasks.Count > 0;

    /// <summary>絞り込み後にこのグループへ表示する項目が1件以上あるかどうか。</summary>
    public bool HasVisibleTasks => IncompleteTasks.Any() || CompletedTasks.Any();

    private IEnumerable<TaskViewModel> FilterTasks(IEnumerable<TaskViewModel> source)
    {
        if (_filterText.Length == 0) return source;
        if (RightPaneFilterMatcher.Matches(Title, _filterText)) return source;
        return source.Where(t => RightPaneFilterMatcher.Matches(t.Title, _filterText));
    }

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
        OnPropertyChanged(nameof(HasVisibleTasks));
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
