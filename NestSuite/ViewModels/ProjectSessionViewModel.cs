using System.Collections.ObjectModel;
using System.IO;

namespace NestSuite.ViewModels;

/// <summary>現在開いているプロジェクトの識別情報、保存状態、最近使ったファイルを所有します。</summary>
public sealed class ProjectSessionViewModel : BaseViewModel
{
    private static readonly TimeSpan MaxPlausibleUnsavedDuration = TimeSpan.FromDays(365);

    private readonly Func<DateTime> _now;
    private string _projectId = Guid.NewGuid().ToString();
    private string _projectName = "";
    private string _statusMessage = "準備完了";
    private string? _currentFilePath;
    private bool _isModified;
    private bool _isSampleProject;
    private DateTime _unsavedSince;
    private DateTime? _lastSavedAt;

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
    public DateTime? LastSavedAt { get => _lastSavedAt; private set => SetProperty(ref _lastSavedAt, value); }

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
            return SafeUnsavedMinutes() is int minutes && minutes >= 5 ? $"⚠ 未保存（{minutes}分）" : "● 未保存";
        }
    }

    public bool IsUnsavedWarning =>
        _isModified && SafeUnsavedMinutes() is int minutes && minutes >= 5;

    /// <summary>
    /// バグ修正 v2.14.14: 未保存経過分数を安全に算出する。
    /// _unsavedSince が未初期化（<see cref="DateTime.MinValue"/> 相当）だったり、
    /// 時計のずれで負値・数十万分単位の異常値になったりした場合は <c>null</c> を返し、
    /// 呼び出し側は経過分数なしの「● 未保存」表示にフォールバックする。
    /// </summary>
    private int? SafeUnsavedMinutes()
    {
        if (_unsavedSince == default) return null;
        var elapsed = _now() - _unsavedSince;
        if (elapsed < TimeSpan.Zero || elapsed > MaxPlausibleUnsavedDuration) return null;
        return (int)elapsed.TotalMinutes;
    }

    public bool IsSampleProject
    {
        get => _isSampleProject;
        private set => SetProperty(ref _isSampleProject, value);
    }

    public ObservableCollection<RecentFileViewModel> RecentFiles { get; } = new();
    public bool HasRecentFiles => RecentFiles.Count > 0;

    public void Start(string projectId, string projectName, string? filePath, DateTime? lastSavedAt = null, bool isSampleProject = false)
    {
        SetProperty(ref _projectId, projectId, nameof(ProjectId));
        ProjectName = projectName;
        SetCurrentFilePath(filePath);
        LastSavedAt = lastSavedAt;
        IsSampleProject = isSampleProject;
        // バグ修正 v2.14.14: 既存ファイルを開いた（＝新しいプロジェクトの追跡を開始する）直後、
        // _unsavedSince を常に安全な現在時刻へ初期化する。IsModified が false→true へ
        // 遷移しないまま何らかの経路で true として観測された場合でも、
        // DateTime.MinValue との差分による異常な経過分数表示を防ぐ。
        _unsavedSince = _now();
        IsModified = false;
    }

    public void MarkSaved(string filePath)
    {
        SetCurrentFilePath(filePath);
        IsSampleProject = false;
        LastSavedAt = _now();
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
