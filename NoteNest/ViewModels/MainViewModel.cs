using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Threading;
using System.ComponentModel;
using NoteNest.Services;

namespace NoteNest.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly NoteWorkspaceViewModel _notes = new();
    private readonly TaskBoardViewModel _tasks = new();
    private readonly MarkerPanelViewModel _markers = new(new MarkerExtractorService());
    private readonly EditorStateViewModel _editor = new();
    private readonly ProjectSessionViewModel _session = new();
    private readonly WorkspaceChangeCoordinator _changeCoordinator;
    private readonly ProjectLifecycleService _lifecycle;
    private readonly DispatcherTimer _unsavedTimer;

    // Callbacks registered by MainWindow
    public Func<string, string, string?>? ShowInputDialog { get; set; }
    public Func<string, string, bool>? ShowConfirmDialog { get; set; }
    public Action<string, string>? ShowErrorDialog { get; set; }
    public Action? RequestClose { get; set; }
    public Action<int>? NavigateToLine { get; set; }
    public Action<MarkerViewModel>? NavigateToMarker { get; set; }
    public Action<NoteViewModel>? SyncTreeSelectionCallback { get; set; }

    public MainViewModel()
    {
        _unsavedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _unsavedTimer.Tick += (_, _) => _session.RefreshUnsavedStatus();

        _changeCoordinator = new WorkspaceChangeCoordinator(_notes, _tasks, _markers, _editor);
        _changeCoordinator.Changed += WorkspaceChanged;
        _session.PropertyChanged += SessionPropertyChanged;
        _lifecycle = new ProjectLifecycleService(_session, _notes, _tasks, _markers, _editor);
        _lifecycle.InitializeRecentFiles();

        OpenRecentCommand          = new RelayCommand(param => { if (param is string path) OpenRecentFile(path); });
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

    // ── Properties ──────────────────────────────────────────────────────────

    public string ProjectName { get => _session.ProjectName; set => _session.ProjectName = value; }

    public NoteViewModel? SelectedNote => _editor.SelectedNote;

    public string EditorContent { get => _editor.Content; set => _editor.Content = value; }
    public string EditorFontFamily { get => _editor.FontFamily; set => _editor.FontFamily = value; }
    public double EditorFontSize { get => _editor.FontSize; set => _editor.FontSize = value; }
    public string CaretPositionText { get => _editor.CaretPositionText; set => _editor.CaretPositionText = value; }

    public string StatusMessage { get => _session.StatusMessage; set => _session.StatusMessage = value; }
    public bool IsModified { get => _session.IsModified; set => _session.IsModified = value; }
    public string UnsavedIndicatorText => _session.UnsavedIndicatorText;
    public bool IsUnsavedWarning => _session.IsUnsavedWarning;

    public static string ApplicationVersion
    {
        get
        {
            var informationalVersion = typeof(MainViewModel).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                var metadataSeparator = informationalVersion.IndexOf('+');
                return metadataSeparator >= 0
                    ? informationalVersion[..metadataSeparator]
                    : informationalVersion;
            }

            return typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "unknown";
        }
    }

    public string WindowTitle
    {
        get
        {
            var title = $"NoteNest - {ProjectDisplayName}";
            if (IsModified) title += " *";
            title += $" - ver{ApplicationVersion}";
            return title;
        }
    }

    public string ProjectDisplayName => _session.ProjectDisplayName;

    public int MarkerCount => _markers.MarkerCount;
    public string? CurrentNoteTitle => SelectedNote?.Title;
    public string ProjectMarkerSummary => _markers.ProjectMarkerSummary;

    public bool FilterTodo { get => _markers.FilterTodo; set => _markers.FilterTodo = value; }
    public bool FilterFixme { get => _markers.FilterFixme; set => _markers.FilterFixme = value; }
    public bool FilterNote { get => _markers.FilterNote; set => _markers.FilterNote = value; }
    public int MarkerSortOrderIndex { get => _markers.SortOrderIndex; set => _markers.SortOrderIndex = value; }
    public IEnumerable<MarkerViewModel> FilteredMarkers => _markers.FilteredMarkers;
    public string FilteredMarkerCountText => _markers.FilteredMarkerCountText;

    public bool IsSampleProject => _session.IsSampleProject;

    public bool ShowLineNumbers { get => _editor.ShowLineNumbers; set => _editor.ShowLineNumbers = value; }
    public bool IsTaskCommentMode => _editor.IsTaskCommentMode;
    public bool IsNoteEditMode => _editor.IsNoteEditMode;
    public string EditorTitle => _editor.EditorTitle;

    // プロジェクト全体のノート列挙。全ノート横断処理（検索、リンク解決、マーカー集計等）はここを起点に書く。
    public IEnumerable<NoteViewModel> AllNotes => _notes.AllNotes;

    public IEnumerable<NoteViewModel> RelatedNoteChoices => AllNotes;

    public NoteViewModel? EditingTaskRelatedNote
    {
        get => _editor.EditingTaskRelatedNote;
        set => _editor.EditingTaskRelatedNote = value;
    }

    public bool HasEditingTaskRelatedNote => _editor.HasEditingTaskRelatedNote;

    public NoteWorkspaceViewModel Notes => _notes;
    public TaskBoardViewModel Tasks => _tasks;
    public MarkerPanelViewModel MarkerPanel => _markers;
    public EditorStateViewModel Editor => _editor;
    public ProjectSessionViewModel Session => _session;

    public ObservableCollection<NotebookViewModel> Notebooks => Notes.Notebooks;
    public ObservableCollection<TaskGroupViewModel> TaskGroups => Tasks.TaskGroups;
    public ObservableCollection<MarkerViewModel> Markers => MarkerPanel.Markers;
    public ObservableCollection<RecentFileViewModel> RecentFiles => _session.RecentFiles;
    public bool HasRecentFiles => _session.HasRecentFiles;

    // ── Commands ─────────────────────────────────────────────────────────────

    public ICommand NewProjectCommand   { get; }
    public ICommand OpenProjectCommand  { get; }
    public ICommand SaveProjectCommand  { get; }
    public ICommand SaveAsProjectCommand { get; }
    public ICommand ExitCommand         { get; }
    public ICommand AddNotebookCommand  { get; }
    public ICommand AddTaskCommand      { get; }
    public ICommand DeleteTaskCommand   { get; }
    public ICommand ToggleGroupCommand  { get; }
    public ICommand MarkerClickCommand  { get; }
    public ICommand OpenRecentCommand   { get; }
    public ICommand ToggleLineNumbersCommand { get; }

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
        OnPropertyChanged(e.PropertyName);
    }

    private void WorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        if (e.IsDataChanged) IsModified = true;
        foreach (var propertyName in e.PropertyNames)
            OnPropertyChanged(propertyName);
    }
}
