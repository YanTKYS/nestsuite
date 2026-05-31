using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using NoteNest.Models;
using NoteNest.Services;

namespace NoteNest.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly ProjectFileService _fileService = new();
    private readonly SampleDataService _sampleService = new();
    private readonly MarkerExtractorService _markerService = new();

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

    private enum EditorMode { NoteEdit, TaskComment }
    private EditorMode _editorMode = EditorMode.NoteEdit;
    private TaskViewModel? _editingTask = null;
    private bool _isSampleProject = false;

    // Callbacks registered by MainWindow
    public Func<string, string, string?>? ShowInputDialog { get; set; }
    public Func<string, string, bool>? ShowConfirmDialog { get; set; }
    public Action? RequestClose { get; set; }
    public Action<int>? NavigateToLine { get; set; }

    public MainViewModel()
    {
        TaskGroups = new ObservableCollection<TaskGroupViewModel>
        {
            new("今日のタスク", "today"),
            new("今週のタスク", "week"),
            new("バックログ", "backlog"),
        };

        NewProjectCommand   = new RelayCommand(NewProject);
        OpenProjectCommand  = new RelayCommand(OpenProject);
        SaveProjectCommand  = new RelayCommand(SaveProject);
        SaveAsProjectCommand = new RelayCommand(SaveProjectAs);
        ExitCommand         = new RelayCommand(Exit);
        AddNotebookCommand  = new RelayCommand(AddNotebook);
        AddTaskCommand      = new RelayCommand(param => AddTask(param as string ?? "today"));
        DeleteTaskCommand   = new RelayCommand(param => { if (param is TaskViewModel t) DeleteTask(t); });
        ToggleGroupCommand  = new RelayCommand(param => { if (param is TaskGroupViewModel g) g.IsExpanded = !g.IsExpanded; });
        MarkerClickCommand  = new RelayCommand(param => { if (param is MarkerViewModel m) NavigateToLine?.Invoke(m.LineNumber); });

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

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsModified
    {
        get => _isModified;
        set { SetProperty(ref _isModified, value); OnPropertyChanged(nameof(WindowTitle)); }
    }

    public string WindowTitle
    {
        get
        {
            var title = $"NoteNest - {ProjectName}";
            if (!string.IsNullOrEmpty(_currentFilePath))
                title += $" [{System.IO.Path.GetFileName(_currentFilePath)}]";
            if (IsModified) title += " *";
            return title;
        }
    }

    public int MarkerCount => Markers.Count;
    public string? CurrentNoteTitle => SelectedNote?.Title;

    public string ProjectMarkerSummary =>
        $"全体  TODO: {_projectTodoCount}  FIXME: {_projectFixmeCount}  NOTE: {_projectNoteCount}";

    public bool IsSampleProject
    {
        get => _isSampleProject;
        private set => SetProperty(ref _isSampleProject, value);
    }

    public bool IsTaskCommentMode => _editorMode == EditorMode.TaskComment;

    public string EditorTitle =>
        _editorMode == EditorMode.TaskComment && _editingTask != null
            ? $"タスクコメント：{_editingTask.Title}"
            : SelectedNote?.Title ?? "";

    public ObservableCollection<NotebookViewModel> Notebooks { get; } = new();
    public ObservableCollection<TaskGroupViewModel> TaskGroups { get; }
    public ObservableCollection<MarkerViewModel> Markers { get; } = new();

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

    // ── Public methods called from code-behind ───────────────────────────────

    public void SelectNote(NoteViewModel note)
    {
        _editorMode = EditorMode.NoteEdit;
        _editingTask = null;
        _isLoadingNote = true;
        SelectedNote = note;
        _editorContent = note.Content;
        OnPropertyChanged(nameof(EditorContent));
        OnPropertyChanged(nameof(CurrentNoteTitle));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(IsTaskCommentMode));
        _isLoadingNote = false;
        RefreshMarkers();
    }

    public void SelectTask(TaskViewModel task)
    {
        _editorMode = EditorMode.TaskComment;
        _editingTask = task;
        _isLoadingNote = true;
        _editorContent = task.Comment;
        OnPropertyChanged(nameof(EditorContent));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(IsTaskCommentMode));
        _isLoadingNote = false;

        Markers.Clear();
        OnPropertyChanged(nameof(MarkerCount));
    }

    public void AddNotebookWithTitle(string title)
    {
        var model = new Notebook { Title = title };
        var vm = new NotebookViewModel(model);
        Notebooks.Add(vm);
        IsModified = true;
        StatusMessage = $"ノートブック「{title}」を追加しました。";
    }

    public void RenameNotebook(NotebookViewModel nb, string newTitle)
    {
        nb.Title = newTitle;
        IsModified = true;
    }

    public void DeleteNotebook(NotebookViewModel nb)
    {
        if (SelectedNote != null && nb.Notes.Contains(SelectedNote))
        {
            SelectedNote = null;
            ClearEditor();
        }
        Notebooks.Remove(nb);
        IsModified = true;
        RefreshProjectMarkers();
    }

    public void AddNoteToNotebook(NotebookViewModel notebook, string title)
    {
        var model = new Note { Title = title };
        var vm = new NoteViewModel(model);
        notebook.Notes.Add(vm);
        notebook.Model.Notes.Add(model);
        IsModified = true;
        SelectNote(vm);
        StatusMessage = $"ノート「{title}」を追加しました。";
    }

    public void RenameNote(NoteViewModel note, string newTitle)
    {
        note.Title = newTitle;
        if (SelectedNote == note)
        {
            OnPropertyChanged(nameof(CurrentNoteTitle));
            OnPropertyChanged(nameof(EditorTitle));
            RefreshMarkers();
        }
        IsModified = true;
    }

    public void DeleteNote(NoteViewModel note)
    {
        foreach (var nb in Notebooks)
        {
            if (nb.Notes.Remove(note))
            {
                nb.Model.Notes.Remove(note.Model);
                if (SelectedNote == note) ClearEditor();
                IsModified = true;
                RefreshProjectMarkers();
                return;
            }
        }
    }

    public void RenameTask(TaskViewModel task, string newTitle)
    {
        task.Title = newTitle;
        if (_editingTask == task)
            OnPropertyChanged(nameof(EditorTitle));
        IsModified = true;
    }

    public void ApplyFontSettings(string fontFamily, double fontSize)
    {
        EditorFontFamily = fontFamily;
        EditorFontSize   = fontSize;
        IsModified       = true;
    }

    public bool ConfirmCloseIfModified()
    {
        if (!IsModified) return true;
        return ShowConfirmDialog?.Invoke("未保存の変更", "保存されていない変更があります。終了しますか？") ?? true;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void ClearEditor()
    {
        _editorMode = EditorMode.NoteEdit;
        _editingTask = null;
        SelectedNote = null;
        _isLoadingNote = true;
        _editorContent = "";
        OnPropertyChanged(nameof(EditorContent));
        OnPropertyChanged(nameof(CurrentNoteTitle));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(IsTaskCommentMode));
        _isLoadingNote = false;
        Markers.Clear();
        OnPropertyChanged(nameof(MarkerCount));
    }

    private void NewProject()
    {
        if (IsModified && ShowConfirmDialog?.Invoke("未保存の変更", "保存されていない変更があります。新規プロジェクトを作成しますか？") != true)
            return;
        LoadProject(_sampleService.Create(), null);
        StatusMessage = "新規プロジェクトを作成しました。";
    }

    private void OpenProject()
    {
        if (IsModified && ShowConfirmDialog?.Invoke("未保存の変更", "保存されていない変更があります。続けますか？") != true)
            return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "NoteNest プロジェクト (*.notenest)|*.notenest|すべてのファイル (*.*)|*.*",
            DefaultExt = ".notenest"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var project = _fileService.Load(dialog.FileName);
            LoadProject(project, dialog.FileName);
            StatusMessage = $"プロジェクトを開きました: {System.IO.Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ファイルを開けませんでした。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveProject()
    {
        if (_currentFilePath == null) { SaveProjectAs(); return; }
        DoSave(_currentFilePath);
    }

    private void SaveProjectAs()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "NoteNest プロジェクト (*.notenest)|*.notenest",
            DefaultExt = ".notenest",
            FileName = ProjectName
        };

        if (dialog.ShowDialog() != true) return;
        if (DoSave(dialog.FileName))
        {
            _currentFilePath = dialog.FileName;
            OnPropertyChanged(nameof(WindowTitle));
        }
    }

    // Returns true only on success; _currentFilePath must NOT be updated on failure.
    private bool DoSave(string path)
    {
        try
        {
            _fileService.Save(path, BuildProject());
            IsModified = false;
            IsSampleProject = false;
            StatusMessage = $"保存しました: {System.IO.Path.GetFileName(path)}";
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存に失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void Exit()
    {
        if (ConfirmCloseIfModified()) RequestClose?.Invoke();
    }

    private void AddNotebook()
    {
        var title = ShowInputDialog?.Invoke("ノートブック追加", "ノートブック名を入力してください:");
        if (!string.IsNullOrWhiteSpace(title))
            AddNotebookWithTitle(title.Trim());
    }

    private void AddTask(string groupKey)
    {
        var title = ShowInputDialog?.Invoke("タスク追加", "タスク名を入力してください:");
        if (string.IsNullOrWhiteSpace(title)) return;

        var group = TaskGroups.FirstOrDefault(g => g.Key == groupKey);
        if (group == null) return;

        var task = new TaskViewModel(new NoteTask { Title = title.Trim() });
        TrackTaskCompletion(task);
        group.AddTask(task);
        IsModified = true;
        StatusMessage = $"タスク「{title.Trim()}」を追加しました。";
    }

    // Subscribe to IsCompleted so toggling a checkbox marks the project as modified.
    private void TrackTaskCompletion(TaskViewModel task)
    {
        task.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TaskViewModel.IsCompleted))
                IsModified = true;
        };
    }

    private void DeleteTask(TaskViewModel task)
    {
        if (_editingTask == task)
        {
            _editorMode = EditorMode.NoteEdit;
            _editingTask = null;
            _isLoadingNote = true;
            _editorContent = _selectedNote?.Content ?? "";
            OnPropertyChanged(nameof(EditorContent));
            OnPropertyChanged(nameof(EditorTitle));
            OnPropertyChanged(nameof(IsTaskCommentMode));
            _isLoadingNote = false;
            if (_selectedNote != null) RefreshMarkers();
        }

        foreach (var group in TaskGroups)
        {
            if (group.Tasks.Remove(task))
            {
                group.RefreshCount();
                IsModified = true;
                return;
            }
        }
    }

    private void LoadProject(Project project, string? filePath)
    {
        _editorMode = EditorMode.NoteEdit;
        _editingTask = null;
        _currentProjectId = project.ProjectId;
        ProjectName = project.ProjectName;
        _currentFilePath = filePath;
        IsSampleProject = filePath == null;

        Notebooks.Clear();
        foreach (var nb in project.Notebooks)
            Notebooks.Add(new NotebookViewModel(nb));

        foreach (var group in TaskGroups)
            group.Tasks.Clear();

        var taskMap = new Dictionary<string, List<NoteTask>>
        {
            { "today",   project.Tasks.Today   },
            { "week",    project.Tasks.Week     },
            { "backlog", project.Tasks.Backlog  }
        };

        foreach (var group in TaskGroups)
        {
            if (taskMap.TryGetValue(group.Key, out var tasks))
                foreach (var t in tasks)
                {
                    var taskVm = new TaskViewModel(t);
                    TrackTaskCompletion(taskVm);
                    group.AddTask(taskVm);
                }
        }

        EditorFontFamily = project.Settings.FontFamily;
        EditorFontSize   = project.Settings.FontSize;

        // Restore last opened note
        NoteViewModel? lastNote = null;
        if (!string.IsNullOrEmpty(project.Settings.LastOpenedNoteId))
        {
            foreach (var nb in Notebooks)
            {
                lastNote = nb.Notes.FirstOrDefault(n => n.Id == project.Settings.LastOpenedNoteId);
                if (lastNote != null) break;
            }
        }
        if (lastNote == null && Notebooks.Count > 0 && Notebooks[0].Notes.Count > 0)
            lastNote = Notebooks[0].Notes[0];

        if (lastNote != null)
            SelectNote(lastNote);
        else
            ClearEditor();

        IsModified = false;
        OnPropertyChanged(nameof(WindowTitle));
        RefreshProjectMarkers();
    }

    private Project BuildProject()
    {
        if (_editorMode == EditorMode.NoteEdit && _selectedNote != null)
            _selectedNote.Content = _editorContent;

        return new Project
        {
            Version = "0.1.2",
            ProjectId = _currentProjectId,
            ProjectName = ProjectName,
            Notebooks = Notebooks.Select(nb => new Notebook
            {
                Id    = nb.Id,
                Title = nb.Title,
                Notes = nb.Notes.Select(n => new Note
                {
                    Id      = n.Id,
                    Title   = n.Title,
                    Content = n.Content
                }).ToList()
            }).ToList(),
            Tasks = new TaskCollection
            {
                Today   = TaskGroups.First(g => g.Key == "today")  .Tasks.Select(t => t.Model).ToList(),
                Week    = TaskGroups.First(g => g.Key == "week")   .Tasks.Select(t => t.Model).ToList(),
                Backlog = TaskGroups.First(g => g.Key == "backlog").Tasks.Select(t => t.Model).ToList()
            },
            Settings = new AppSettings
            {
                LastOpenedNoteId = SelectedNote?.Id ?? "",
                FontFamily       = EditorFontFamily,
                FontSize         = EditorFontSize
            }
        };
    }

    private void RefreshMarkers()
    {
        Markers.Clear();
        if (_selectedNote != null)
        {
            foreach (var m in _markerService.Extract(_editorContent, _selectedNote.Title))
                Markers.Add(new MarkerViewModel(m));
        }
        OnPropertyChanged(nameof(MarkerCount));
        RefreshProjectMarkers();
    }

    private void RefreshProjectMarkers()
    {
        int todo = 0, fixme = 0, note = 0;
        foreach (var nb in Notebooks)
            foreach (var n in nb.Notes)
                foreach (var m in _markerService.Extract(n.Content, n.Title))
                {
                    if (m.Type == "TODO")       todo++;
                    else if (m.Type == "FIXME") fixme++;
                    else if (m.Type == "NOTE")  note++;
                }
        _projectTodoCount  = todo;
        _projectFixmeCount = fixme;
        _projectNoteCount  = note;
        OnPropertyChanged(nameof(ProjectMarkerSummary));
    }
}
