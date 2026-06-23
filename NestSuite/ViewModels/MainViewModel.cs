using System.Windows.Threading;
using System.ComponentModel;
using NestSuite.Services;

namespace NestSuite.ViewModels;

public partial class MainViewModel : BaseViewModel, IDisposable
{
    private readonly NoteWorkspaceViewModel _notes = new();
    private readonly TaskBoardViewModel _tasks = new();
    private readonly MarkerPanelViewModel _markers = new(new MarkerExtractorService());
    private readonly NoteLinkPanelViewModel _links = new();
    private readonly EditorStateViewModel _editor = new();
    private readonly ProjectSessionViewModel _session = new();
    private readonly WorkspaceChangeCoordinator _changeCoordinator;
    private readonly ProjectLifecycleService _lifecycle;
    private readonly ExportService _exports = new();
    private readonly DispatcherTimer _unsavedTimer;
    private readonly DispatcherTimer _autoSaveTimer;
    private bool _isAutoSaveEnabled;
    private bool _disposed;

    public MainViewModel()
    {
        _unsavedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _unsavedTimer.Tick += UnsavedTimer_Tick;
        _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        _autoSaveTimer.Tick += AutoSaveTimer_Tick;
        _autoSaveTimer.Start();

        _changeCoordinator = new WorkspaceChangeCoordinator(_notes, _tasks, _markers, _editor);
        _changeCoordinator.Changed += WorkspaceChanged;
        _session.PropertyChanged += SessionPropertyChanged;
        _lifecycle = new ProjectLifecycleService(_session, _notes, _tasks, _markers, _editor);
        _lifecycle.InitializeRecentFiles();

        OpenRecentCommand          = new RelayCommand(param => { if (param is string path) OpenRecentFile(path); });
        ClearRecentFilesCommand     = new RelayCommand(_ => ClearRecentFiles());
        ToggleLineNumbersCommand   = new RelayCommand(_ => ShowLineNumbers = !ShowLineNumbers);
        NewProjectCommand          = new RelayCommand(NewProject);
        OpenProjectCommand         = new RelayCommand(OpenProject);
        SaveProjectCommand         = new RelayCommand(SaveProject);
        SaveAsProjectCommand       = new RelayCommand(SaveProjectAs);
        ExitCommand                = new RelayCommand(Exit);
        AddNotebookCommand         = new RelayCommand(AddNotebook);
        AddTaskCommand             = new RelayCommand(param => AddTask(param as string ?? "today"));
        DeleteTaskCommand          = new RelayCommand(param => { if (param is TaskViewModel t) DeleteTask(t); });
        ToggleGroupCommand         = new RelayCommand(param => { if (param is TaskGroupViewModel g) g.IsExpanded = !g.IsExpanded; });
        MarkerClickCommand         = new RelayCommand(param => { if (param is MarkerViewModel m) NavigateToMarker?.Invoke(m); });

        _lifecycle.CreateNew();
    }

    private void SessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProjectSessionViewModel.IsModified))
        {
            if (_session.IsModified) _unsavedTimer.Start(); else _unsavedTimer.Stop();
            OnPropertyChanged(nameof(WindowTitle));
        }
        else if (e.PropertyName is nameof(ProjectSessionViewModel.ProjectName)
                 or nameof(ProjectSessionViewModel.ProjectDisplayName)
                 or nameof(ProjectSessionViewModel.CurrentFilePath))
        {
            OnPropertyChanged(nameof(WindowTitle));
        }
        if (e.PropertyName is nameof(ProjectSessionViewModel.ProjectName)
            or nameof(ProjectSessionViewModel.StatusMessage)
            or nameof(ProjectSessionViewModel.CurrentFilePath)
            or nameof(ProjectSessionViewModel.ProjectDisplayName)
            or nameof(ProjectSessionViewModel.IsModified)
            or nameof(ProjectSessionViewModel.UnsavedIndicatorText)
            or nameof(ProjectSessionViewModel.IsUnsavedWarning)
            or nameof(ProjectSessionViewModel.IsSampleProject)
            or nameof(ProjectSessionViewModel.HasRecentFiles)
            or nameof(ProjectSessionViewModel.LastSavedAt))
            OnPropertyChanged(e.PropertyName);
    }

    private void UnsavedTimer_Tick(object? sender, EventArgs e) => _session.RefreshUnsavedStatus();

    private void AutoSaveTimer_Tick(object? sender, EventArgs e) => AutoSave();

    private void WorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        if (e.IsDataChanged) IsModified = true;
        if (e.IsDataChanged || e.PropertyNames.Contains(nameof(SelectedNote)))
            RefreshLinks();
        foreach (var propertyName in e.PropertyNames)
            OnPropertyChanged(propertyName);
    }

    /// <summary>
    /// v1.9.5: タイマーを停止し、内部イベント購読を解除する。
    /// NestSuite で NoteNest タブを閉じる際（<see cref="NestSuite.NestSuiteShellWindow"/>）に呼ぶ。
    /// 停止しないと DispatcherTimer が Dispatcher の内部リストに残り、
    /// 閉じたタブの ViewModel が GC されず AutoSave が呼び続ける。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _autoSaveTimer.Stop();
        _autoSaveTimer.Tick -= AutoSaveTimer_Tick;
        _unsavedTimer.Stop();
        _unsavedTimer.Tick -= UnsavedTimer_Tick;
        _changeCoordinator.Changed -= WorkspaceChanged;
        _session.PropertyChanged -= SessionPropertyChanged;
    }
}
