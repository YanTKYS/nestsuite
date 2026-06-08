using System.Collections.ObjectModel;

namespace NoteNest.ViewModels;

/// <summary>現在開いているプロジェクトの識別情報、保存状態、最近使ったファイルを所有します。</summary>
public sealed class ProjectSessionViewModel : BaseViewModel
{
    private readonly Func<DateTime> _now;
    private string _projectId = Guid.NewGuid().ToString();
    private string _projectName = "";
    private string _statusMessage = "準備完了";
    private string? _currentFilePath;
    private bool _isModified;
    private bool _isSampleProject;
    private DateTime _unsavedSince;

    public ProjectSessionViewModel(Func<DateTime>? now = null)
    {
        _now = now ?? (() => DateTime.Now);
        RecentFiles.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasRecentFiles));
    }

    public string ProjectId => _projectId;

    public string ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string? CurrentFilePath => _currentFilePath;

    public string ProjectDisplayName =>
        _currentFilePath != null ? Path.GetFileName(_currentFilePath) : "新規プロジェクト";

    public bool IsModified
    {
        get => _isModified;
        set
        {
            if (!SetProperty(ref _isModified, value)) return;
            if (value) _unsavedSince = _now();
            OnPropertyChanged(nameof(UnsavedIndicatorText));
            OnPropertyChanged(nameof(IsUnsavedWarning));
        }
    }

    public string UnsavedIndicatorText
    {
        get
        {
            if (!_isModified) return "● 未保存";
            var minutes = (int)(_now() - _unsavedSince).TotalMinutes;
            return minutes >= 5 ? $"⚠ 未保存（{minutes}分）" : "● 未保存";
        }
    }

    public bool IsUnsavedWarning =>
        _isModified && (int)(_now() - _unsavedSince).TotalMinutes >= 5;

    public bool IsSampleProject
    {
        get => _isSampleProject;
        private set => SetProperty(ref _isSampleProject, value);
    }

    public ObservableCollection<RecentFileViewModel> RecentFiles { get; } = new();
    public bool HasRecentFiles => RecentFiles.Count > 0;

    public void Start(string projectId, string projectName, string? filePath)
    {
        SetProperty(ref _projectId, projectId, nameof(ProjectId));
        ProjectName = projectName;
        SetCurrentFilePath(filePath);
        IsSampleProject = filePath == null;
        IsModified = false;
    }

    public void MarkSaved(string filePath)
    {
        SetCurrentFilePath(filePath);
        IsSampleProject = false;
        IsModified = false;
    }

    public void ReplaceRecentFiles(IEnumerable<string> paths)
    {
        RecentFiles.Clear();
        foreach (var path in paths)
            RecentFiles.Add(new RecentFileViewModel(path));
    }

    public void RefreshUnsavedStatus()
    {
        if (!_isModified) return;
        OnPropertyChanged(nameof(UnsavedIndicatorText));
        OnPropertyChanged(nameof(IsUnsavedWarning));
    }

    private void SetCurrentFilePath(string? filePath)
    {
        if (!SetProperty(ref _currentFilePath, filePath, nameof(CurrentFilePath))) return;
        OnPropertyChanged(nameof(ProjectDisplayName));
    }
}
