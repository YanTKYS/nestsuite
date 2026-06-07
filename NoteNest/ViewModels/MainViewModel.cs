using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Threading;
using NoteNest.Models;
using NoteNest.Services;

namespace NoteNest.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly ProjectFileService _fileService = new();
    private readonly SampleDataService _sampleService = new();
    private readonly MarkerExtractorService _markerService = new();
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
    private int _projectTodoCount;
    private int _projectFixmeCount;
    private int _projectNoteCount;
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
        TaskGroups = new ObservableCollection<TaskGroupViewModel>
        {
            new("今日のタスク", "today"),
            new("今週のタスク", "week"),
            new("バックログ", "backlog"),
        };

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
                _editingTask.Comment = value;
                IsModified = true;
            }
            else if (_editorMode == EditorMode.NoteEdit && _selectedNote != null)
            {
                _selectedNote.Content = value;
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

    public int MarkerCount => Markers.Count;
    public string? CurrentNoteTitle => SelectedNote?.Title;

    public string ProjectMarkerSummary =>
        $"全体  TODO: {_projectTodoCount}  FIXME: {_projectFixmeCount}  NOTE: {_projectNoteCount}";

    // ── Marker filters ────────────────────────────────────────────────────────

    private bool _filterTodo  = true;
    private bool _filterFixme = true;
    private bool _filterNote  = true;

    public bool FilterTodo
    {
        get => _filterTodo;
        set { SetProperty(ref _filterTodo, value); RaiseFilteredMarkersChanged(); }
    }

    public bool FilterFixme
    {
        get => _filterFixme;
        set { SetProperty(ref _filterFixme, value); RaiseFilteredMarkersChanged(); }
    }

    public bool FilterNote
    {
        get => _filterNote;
        set { SetProperty(ref _filterNote, value); RaiseFilteredMarkersChanged(); }
    }

    // 0=抽出順, 1=種別順, 2=ノート順, 3=行番号順
    private int _markerSortOrderIndex = 0;

    public int MarkerSortOrderIndex
    {
        get => _markerSortOrderIndex;
        set { SetProperty(ref _markerSortOrderIndex, value); RaiseFilteredMarkersChanged(); }
    }

    public IEnumerable<MarkerViewModel> FilteredMarkers
    {
        get
        {
            var filtered = Markers.Where(m =>
                (m.Type == "TODO"  && _filterTodo)  ||
                (m.Type == "FIXME" && _filterFixme) ||
                (m.Type == "NOTE"  && _filterNote));
            return _markerSortOrderIndex switch
            {
                1 => filtered.OrderBy(m => MarkerTypeOrder(m.Type)).ThenBy(m => m.NoteTitle).ThenBy(m => m.LineNumber),
                2 => filtered.OrderBy(m => m.NoteTitle).ThenBy(m => m.LineNumber),
                3 => filtered.OrderBy(m => m.LineNumber),
                _ => filtered,
            };
        }
    }

    public string FilteredMarkerCountText
    {
        get
        {
            var total    = Markers.Count;
            var filtered = FilteredMarkers.Count();
            return filtered == total ? $"{total}個" : $"{filtered}/{total}個";
        }
    }

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
    public IEnumerable<NoteViewModel> AllNotes => Notebooks.SelectMany(nb => nb.Notes);

    public IEnumerable<NoteViewModel> RelatedNoteChoices => AllNotes;

    public NoteViewModel? EditingTaskRelatedNote
    {
        get => _editingTaskRelatedNote;
        set
        {
            if (!SetProperty(ref _editingTaskRelatedNote, value)) return;
            OnPropertyChanged(nameof(HasEditingTaskRelatedNote));
            if (_editingTask != null)
            {
                _editingTask.LinkedNoteId = value?.Id;
                IsModified = true;
            }
        }
    }

    public bool HasEditingTaskRelatedNote => _editingTaskRelatedNote != null;

    public ObservableCollection<NotebookViewModel> Notebooks { get; } = new();
    public ObservableCollection<TaskGroupViewModel> TaskGroups { get; }
    public ObservableCollection<MarkerViewModel> Markers { get; } = new();
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
