using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;

namespace NestSuite.ViewModels;

/// <summary>
/// XAML と MainWindow の既存契約を維持するためのファサードです。
/// 状態の所有者は責務別 ViewModel とし、ここでは中継・横断表示・UI境界だけを公開します。
/// </summary>
public partial class MainViewModel
{
    // MainWindow が登録する軽量な UI 境界。具体的なダイアログ型は公開しない。
    public Func<string, string, string?>? ShowInputDialog { get; set; }
    public Func<string, string, bool>? ShowConfirmDialog { get; set; }
    public Action<string, string>? ShowErrorDialog { get; set; }
    public Func<string?>? SelectOpenProjectPath { get; set; }
    public Func<string, string?>? SelectSaveProjectPath { get; set; }
    public Action? RequestClose { get; set; }
    public Action<int>? NavigateToLine { get; set; }
    public Action<MarkerViewModel>? NavigateToMarker { get; set; }
    public Action<NoteViewModel>? SyncTreeSelectionCallback { get; set; }

    // XAML 互換ファサード。値は責務別 ViewModel が所有する。
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
    public string ProjectDisplayName => _session.ProjectDisplayName;
    public string ProjectMarkerSummary => _markers.ProjectMarkerSummary;
    public bool FilterTodo { get => _markers.FilterTodo; set => _markers.FilterTodo = value; }
    public bool FilterFixme { get => _markers.FilterFixme; set => _markers.FilterFixme = value; }
    public bool FilterNote { get => _markers.FilterNote; set => _markers.FilterNote = value; }
    public int MarkerSortOrderIndex { get => _markers.SortOrderIndex; set => _markers.SortOrderIndex = value; }
    public IEnumerable<MarkerViewModel> FilteredMarkers => _markers.FilteredMarkers;
    public string FilteredMarkerCountText => _markers.FilteredMarkerCountText;
    public string TodoCountText  => _markers.TodoCountText;
    public string FixmeCountText => _markers.FixmeCountText;
    public string NoteCountText  => _markers.NoteCountText;
    public bool IsSampleProject => _session.IsSampleProject;
    public bool IsTaskCommentMode => _editor.IsTaskCommentMode;
    public bool IsNoteEditMode => _editor.IsNoteEditMode;
    public string EditorTitle => _editor.EditorTitle;
    /// <summary>L24: 関連ノート選択ボタン（タスク編集パネル）が対象タスクを特定するために使う。</summary>
    public TaskViewModel? EditingTask => _editor.EditingTask;
    public IEnumerable<NoteViewModel> RelatedNoteChoices => _notes.AllNotes;
    public NoteViewModel? EditingTaskRelatedNote
    {
        get => _editor.EditingTaskRelatedNote;
        set => _editor.EditingTaskRelatedNote = value;
    }
    public bool HasEditingTaskRelatedNote => _editor.HasEditingTaskRelatedNote;
    public ObservableCollection<NotebookViewModel> Notebooks => _notes.Notebooks;
    public ObservableCollection<TaskGroupViewModel> TaskGroups => _tasks.TaskGroups;
    public string TotalIncompleteTaskCountText => _tasks.TotalIncompleteTaskCountText;
    public bool HasAnyTasks => _tasks.HasAnyTasks;
    public bool HasNoTasks => !_tasks.HasAnyTasks;
    public ObservableCollection<MarkerViewModel> Markers => _markers.Markers;
    public int MarkerCount => _markers.MarkerCount;
    public IEnumerable<NoteViewModel> AllNotes => _notes.AllNotes;
    public bool IsNoteListEmpty => !_notes.AllNotes.Any();
    public bool HasSelectedNote => _editor.SelectedNote != null;
    public bool HasAnyNotes => _notes.AllNotes.Any();

    // L23: 空状態での次操作ガイド表示条件。優先順位はノートブック > ノート > タスク/マーカーの順で、
    // 上位が空の間は下位の案内を重複表示しない（docs/design/nestsuite-attractiveness-direction.md 4.5節）。
    public bool HasNotebooks => _notes.Notebooks.Count > 0;
    public bool ShowNotebookEmptyState => !HasNotebooks;
    public bool ShowNoteEmptyState => HasNotebooks && IsNoteListEmpty;
    public bool HasAnyMarkers => _markers.MarkerCount > 0;
    public bool HasNoMarkers => !HasAnyMarkers;
    public bool ShowTaskEmptyState => HasAnyNotes && HasNoTasks;
    public bool ShowMarkerEmptyState => HasAnyNotes && HasNoMarkers;

    /// <summary>v2.16.10 SH-30: Markdown エクスポート（選択ノート対象）の無効理由ツールチップ。</summary>
    public string MarkdownExportSelectedNoteTooltip =>
        NestSuite.Services.ShellCommandTooltipProvider.MarkdownExportSelectedNoteTooltip(HasSelectedNote);

    /// <summary>v2.16.10 SH-30: Markdown エクスポート（全ノート対象）の無効理由ツールチップ。</summary>
    public string MarkdownExportAllNotesTooltip =>
        NestSuite.Services.ShellCommandTooltipProvider.MarkdownExportAllNotesTooltip(HasAnyNotes);

    public string? CurrentNoteTitle => _editor.SelectedNote?.Title;
    public string? CurrentNotebookName =>
        _editor.SelectedNote == null ? null : FindNotebookOf(_editor.SelectedNote)?.Title;
    public ObservableCollection<RecentFileViewModel> RecentFiles => _session.RecentFiles;
    public bool HasRecentFiles => _session.HasRecentFiles;
    public string? CurrentFilePath => _session.CurrentFilePath;
    public DateTime? LastSavedAt => _session.LastSavedAt;
    public string CurrentNoteTimestampSummary => _editor.IsTaskCommentMode ? "" : _editor.SelectedNote?.TimestampSummary ?? "";

    // 責務所有者への明示的な入口。新規コードは単純中継よりこちらを優先する。
    public NoteWorkspaceViewModel Notes => _notes;
    public TaskBoardViewModel Tasks => _tasks;
    public MarkerPanelViewModel MarkerPanel => _markers;
    public NoteLinkPanelViewModel LinkPanel => _links;
    public EditorStateViewModel Editor => _editor;
    public ProjectSessionViewModel Session => _session;

    public static readonly IReadOnlyList<double> EditorFontSizeChoices = [12, 14, 16, 18, 20];

    /// <summary>
    /// L22: NoteNest 本文エディタで選択可能なフォント種類。既定は先頭の "Yu Gothic UI"。
    /// L21 時点では NoteNest 限定の一覧だったが、Workspace 共通設定への拡大に伴い
    /// IdeaNest / ChatNest / TempNest とも共有する <see cref="NestSuite.Services.UiSettingsService.ValidWorkspaceEditorFontFamilies"/> を参照する。
    /// </summary>
    public static readonly IReadOnlyList<string> EditorFontFamilyChoices =
        NestSuite.Services.UiSettingsService.ValidWorkspaceEditorFontFamilies;

    // 複数責務を組み合わせるため MainViewModel に残す派生表示。
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
                    ? informationalVersion.Substring(0, metadataSeparator)
                    : informationalVersion;
            }

            return typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "unknown";
        }
    }

    public string WindowTitle
    {
        get
        {
            var title = $"NestSuite - {_session.ProjectDisplayName}";
            if (_session.IsModified) title += " *";
            title += $" - ver{ApplicationVersion}";
            return title;
        }
    }

    public string ProjectInfo => $"プロジェクト名: {_session.ProjectName}\nファイル: {_session.CurrentFilePath ?? "未保存"}\nノートブック: {_notes.Notebooks.Count}\nノート: {_notes.AllNotes.Count()}\nタスク: {_tasks.TaskGroups.Sum(group => group.Tasks.Count)}\nマーカー: {_markers.MarkerCount}\n最終保存: {(_session.LastSavedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "未保存")}";

    // XAML コマンド入口。処理本体は責務別 partial またはサービスへ委譲する。
    public ICommand NewProjectCommand { get; }
    public ICommand OpenProjectCommand { get; }
    public ICommand SaveProjectCommand { get; }
    public ICommand SaveAsProjectCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand AddNotebookCommand { get; }
    public ICommand AddTaskCommand { get; }
    public ICommand DeleteTaskCommand { get; }
    public ICommand ToggleGroupCommand { get; }
    public ICommand MarkerClickCommand { get; }
    public ICommand OpenRecentCommand { get; }
    public ICommand ClearRecentFilesCommand { get; }
}
