using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Threading;
using NoteNest.Services;

namespace NoteNest.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly ProjectFileService _fileService = new();
    private readonly SampleDataService _sampleService = new();
    private readonly RecentFilesService _recentFilesService = new();
    private readonly ExportService _exportService = new();

    private string _projectName = "";
    private NoteViewModel? _selectedNote;
    private string _editorContent = "";
    private string _editorFontFamily = "Yu Gothic UI";
    private double _editorFontSize = 14;
    private string _statusMessage = "準備完了";
    private bool _isModified = false;
    private string? _currentFilePath = null;
    private string _currentProjectId = Guid.NewGuid().ToString();
    private bool _isLoadingNote = false;
    private readonly NoteWorkspaceViewModel _notes = new();
    private readonly TaskBoardViewModel _tasks = new();
    private readonly MarkerPanelViewModel _markers = new(new MarkerExtractorService());
    private DateTime _unsavedSince;
    private DispatcherTimer? _unsavedTimer;

    private enum EditorMode { NoteEdit, TaskComment }
    private EditorMode _editorMode = EditorMode.NoteEdit;
    private TaskViewModel? _editingTask = null;
    private NoteViewModel? _editingTaskRelatedNote = null;
    private bool _isSampleProject = false;
    private bool _showLineNumbers = false;

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
        _tasks.Changed += (_, _) => IsModified = true;
        _markers.PropertyChanged += (_, e) => OnPropertyChanged(
            e.PropertyName == nameof(MarkerPanelViewModel.SortOrderIndex)
                ? nameof(MarkerSortOrderIndex)
                : e.PropertyName);

        OpenRecentCommand          = new RelayCommand(param => { if (param is string path) OpenRecentFile(path); });
        ToggleLineNumbersCommand   = new RelayCommand(_ => ShowLineNumbers = !ShowLineNumbers);

        foreach (var p in _recentFilesService.Load())
            RecentFiles.Add(new RecentFileViewModel(p));
        RecentFiles.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasRecentFiles));

        NewProjectCommand   = new RelayCommand(NewProject);
        OpenProjectCommand  = new RelayCommand(OpenProject);
        SaveProjectCommand  = new RelayCommand(SaveProject);
        SaveAsProjectCommand = new RelayCommand(SaveProjectAs);
        ExitCommand         = new RelayCommand(Exit);
        AddNotebookCommand  = new RelayCommand(AddNotebook);
        AddTaskCommand      = new RelayCommand(param => AddTask(param as string ?? "today"));
        DeleteTaskCommand   = new RelayCommand(param => { if (param is TaskViewModel t) DeleteTask(t); });
        ToggleGroupCommand  = new RelayCommand(param => { if (param is TaskGroupViewModel g) g.IsExpanded = !g.IsExpanded; });
        MarkerClickCommand  = new RelayCommand(param => { if (param is MarkerViewModel m) NavigateToMarker?.Invoke(m); });

        _unsavedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _unsavedTimer.Tick += (_, _) =>
        {
            OnPropertyChanged(nameof(UnsavedIndicatorText));
            OnPropertyChanged(nameof(IsUnsavedWarning));
        };

        LoadProject(_sampleService.Create(), null);
    }

    // ── Properties ──────────────────────────────────────────────────────────

    public string ProjectName
    {
        get => _projectName;
        set { SetProperty(ref _projectName, value); OnPropertyChanged(nameof(WindowTitle)); }
    }

    public NoteViewModel? SelectedNote
    {
        get => _selectedNote;
        private set => SetProperty(ref _selectedNote, value);
    }

    public string EditorContent
    {
        get => _editorContent;
        set
        {
            if (_editorContent == value) return;
            _editorContent = value;
            OnPropertyChanged();

            if (_isLoadingNote) return;

            if (_editorMode == EditorMode.TaskComment && _editingTask != null)
            {
                _tasks.UpdateComment(_editingTask, value);
            }
            else if (_editorMode == EditorMode.NoteEdit && _selectedNote != null)
            {
                _notes.UpdateContent(_selectedNote, value);
                IsModified = true;
                RefreshMarkers();
            }
        }
    }

    public string EditorFontFamily
    {
        get => _editorFontFamily;
        set => SetProperty(ref _editorFontFamily, value);
    }

    public double EditorFontSize
    {
        get => _editorFontSize;
        set => SetProperty(ref _editorFontSize, value);
    }

    private string _caretPositionText = "";
    public string CaretPositionText
    {
        get => _caretPositionText;
        set => SetProperty(ref _caretPositionText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsModified
    {
        get => _isModified;
        set
        {
            if (!SetProperty(ref _isModified, value)) return;
            OnPropertyChanged(nameof(WindowTitle));
            if (value)
            {
                _unsavedSince = DateTime.Now;
                _unsavedTimer?.Start();
            }
            else
            {
                _unsavedTimer?.Stop();
            }
            OnPropertyChanged(nameof(UnsavedIndicatorText));
            OnPropertyChanged(nameof(IsUnsavedWarning));
        }
    }

    public string UnsavedIndicatorText
    {
        get
        {
            if (!_isModified) return "● 未保存";
            var minutes = (int)(DateTime.Now - _unsavedSince).TotalMinutes;
            return minutes >= 5 ? $"⚠ 未保存（{minutes}分）" : "● 未保存";
        }
    }

    public bool IsUnsavedWarning =>
        _isModified && (int)(DateTime.Now - _unsavedSince).TotalMinutes >= 5;

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

    public string ProjectDisplayName =>
        _currentFilePath != null
            ? System.IO.Path.GetFileName(_currentFilePath)
            : "新規プロジェクト";

    public int MarkerCount => _markers.MarkerCount;
    public string? CurrentNoteTitle => SelectedNote?.Title;
    public string ProjectMarkerSummary => _markers.ProjectMarkerSummary;

    public bool FilterTodo { get => _markers.FilterTodo; set => _markers.FilterTodo = value; }
    public bool FilterFixme { get => _markers.FilterFixme; set => _markers.FilterFixme = value; }
    public bool FilterNote { get => _markers.FilterNote; set => _markers.FilterNote = value; }
    public int MarkerSortOrderIndex { get => _markers.SortOrderIndex; set => _markers.SortOrderIndex = value; }
    public IEnumerable<MarkerViewModel> FilteredMarkers => _markers.FilteredMarkers;
    public string FilteredMarkerCountText => _markers.FilteredMarkerCountText;

    public bool IsSampleProject
    {
        get => _isSampleProject;
        private set => SetProperty(ref _isSampleProject, value);
    }

    public bool ShowLineNumbers
    {
        get => _showLineNumbers;
        set => SetProperty(ref _showLineNumbers, value);
    }

    public bool IsTaskCommentMode => _editorMode == EditorMode.TaskComment;
    public bool IsNoteEditMode    => _editorMode == EditorMode.NoteEdit;

    public string EditorTitle =>
        _editorMode == EditorMode.TaskComment && _editingTask != null
            ? $"タスクコメント：{_editingTask.Title}"
            : SelectedNote?.Title ?? "";

    // プロジェクト全体のノート列挙。全ノート横断処理（検索、リンク解決、マーカー集計等）はここを起点に書く。
    public IEnumerable<NoteViewModel> AllNotes => _notes.AllNotes;

    public IEnumerable<NoteViewModel> RelatedNoteChoices => AllNotes;

    public NoteViewModel? EditingTaskRelatedNote
    {
        get => _editingTaskRelatedNote;
        set
        {
            if (!SetProperty(ref _editingTaskRelatedNote, value)) return;
            OnPropertyChanged(nameof(HasEditingTaskRelatedNote));
            if (_editingTask != null)
                _tasks.SetRelatedNote(_editingTask, value);
        }
    }

    public bool HasEditingTaskRelatedNote => _editingTaskRelatedNote != null;

    public NoteWorkspaceViewModel Notes => _notes;
    public TaskBoardViewModel Tasks => _tasks;
    public MarkerPanelViewModel MarkerPanel => _markers;

    public ObservableCollection<NotebookViewModel> Notebooks => Notes.Notebooks;
    public ObservableCollection<TaskGroupViewModel> TaskGroups => Tasks.TaskGroups;
    public ObservableCollection<MarkerViewModel> Markers => MarkerPanel.Markers;
    public ObservableCollection<RecentFileViewModel> RecentFiles { get; } = new();
    public bool HasRecentFiles => RecentFiles.Count > 0;

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

}
