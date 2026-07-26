using System.Text;
using System.Windows;
using System.Windows.Controls;
using NestSuite.NoteNest.Editor;
using NestSuite.Services;
using NestSuite.ViewModels;

namespace NestSuite.Views;

public partial class NoteNestWorkspaceView : UserControl
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public IWorkspaceDialogHost? DialogHost { get; set; }
    private IWorkspaceDialogHost Host =>
        DialogHost ?? throw new InvalidOperationException("DialogHost が設定されていません。");

    private bool _suppressTreeSelectionChanged;
    private readonly DragDropState _dragDrop = new();

    private bool _isRightPaneCollapsed;
    private double _savedRightPaneWidth = 280;

    public NoteNestWorkspaceView()
    {
        InitializeComponent();
        InitNoteFilter();
        CollapseRightPane();
        EditorHost.EditorReady += (_, _) =>
        {
            EditorHost.Editor.SelectionChanged += EditorAdapter_SelectionChanged;
            // v2.9.5 SH-21 hotfix: DataContext が null の間は空を返す。
            // DetachedWorkspaceWindow.OnClosed で DataContext を解除する際、WPF Binding 更新により
            // EditorBox_TextChanged → UpdateCompletion → provider が呼ばれる経路で NullReferenceException
            // が発生していた。DataContext が MainViewModel でない間は安全に空候補を返す。
            EditorHost.NoteTitleProvider = () =>
            {
                if (DataContext is not MainViewModel vm)
                    return Enumerable.Empty<string>();
                return vm.Notebooks
                    .SelectMany(nb => nb.Notes)
                    .Where(n => !string.IsNullOrWhiteSpace(n.Title))
                    .Select(n => n.Title);
            };
            EditorHost.IsNoteEditModeProvider = () =>
                DataContext is MainViewModel vm && vm.IsNoteEditMode;
        };
    }

    // ── Public API for AppShell（NestSuiteShellWindow）──────────────────────

    public double LeftPaneWidth
    {
        get => LeftPaneColumn.Width.Value;
        set => LeftPaneColumn.Width = new GridLength(value);
    }

    public bool IsRightPaneCollapsed => _isRightPaneCollapsed;

    public double ActualRightPaneWidth =>
        _isRightPaneCollapsed ? _savedRightPaneWidth : RightPaneColumn.Width.Value;

    public void InitRightPane(double savedWidth, bool collapsed)
    {
        _savedRightPaneWidth = savedWidth;
        if (collapsed)
            CollapseRightPane();
        else
            ExpandRightPane();
    }

    public bool ToggleRightPane()
    {
        if (_isRightPaneCollapsed) ExpandRightPane(); else CollapseRightPane();
        return _isRightPaneCollapsed;
    }

    public void CollapseRightPane()
    {
        var width = RightPaneColumn.Width.Value;
        if (width > 0) _savedRightPaneWidth = width;
        RightGridSplitter.Visibility = Visibility.Collapsed;
        RightPaneColumn.MinWidth = 0;
        RightPaneColumn.Width = new GridLength(0);
        _isRightPaneCollapsed = true;
        RightPaneExpandButton.Content = "»";
        RightPaneExpandButton.ToolTip = "右ペインを表示";
    }

    public void ExpandRightPane()
    {
        RightGridSplitter.Visibility = Visibility.Visible;
        RightPaneColumn.MinWidth = 200;
        RightPaneColumn.Width = new GridLength(_savedRightPaneWidth);
        _isRightPaneCollapsed = false;
        RightPaneExpandButton.Content = "«";
        RightPaneExpandButton.ToolTip = "右ペインを閉じる";
    }

    public void NavigateToLine(int lineNumber)
    {
        if (lineNumber < 1) lineNumber = 1;
        var lineIndex = lineNumber - 1;
        if (lineIndex >= EditorHost.Editor.LineCount) lineIndex = EditorHost.Editor.LineCount - 1;
        if (lineIndex < 0) return;
        EditorHost.Editor.ScrollToLine(lineIndex);
        var charIdx = EditorHost.Editor.GetCharacterIndexFromLineIndex(lineIndex);
        EditorHost.Editor.CaretIndex = charIdx;
        EditorHost.Editor.Focus();
    }

    public void SyncTreeSelection(NoteViewModel note)
    {
        foreach (var nb in ViewModel.Notebooks)
        {
            if (!nb.Notes.Contains(note)) continue;
            var nbItem = NotebookTree.ItemContainerGenerator.ContainerFromItem(nb) as TreeViewItem;
            if (nbItem == null) continue;
            if (!nbItem.IsExpanded) { nbItem.IsExpanded = true; nbItem.UpdateLayout(); }
            var noteItem = nbItem.ItemContainerGenerator.ContainerFromItem(note) as TreeViewItem;
            if (noteItem == null) continue;
            _suppressTreeSelectionChanged = true;
            noteItem.IsSelected = true;
            noteItem.BringIntoView();
            _suppressTreeSelectionChanged = false;
            return;
        }
    }

    public void CheckBrokenLinks()
    {
        var sourceNote = Host.CheckBrokenLinks(ViewModel.AllNotes);
        if (sourceNote != null) ViewModel.NavigateToNote(sourceNote);
    }

    public void TryOpenNoteLink()
    {
        var linkTitle = NoteLinkService.ExtractLinkAtCursor(EditorHost.Editor.Text, EditorHost.Editor.CaretIndex);
        if (linkTitle == null) return;
        var note = ViewModel.FindNoteByTitle(linkTitle);
        if (note == null)
        {
            ShowInfo($"ノート「{linkTitle}」が見つかりません。", "リンク先なし");
            return;
        }
        ViewModel.NavigateToNote(note);
    }

    public void OpenFindReplace(string lastSearch, string lastReplace, double? left, double? top,
        Action<NoteViewModel>? navigateToNote = null)
        => Host.ShowFindReplace(EditorHost.Editor, ViewModel.AllNotes, navigateToNote, lastSearch, lastReplace, left, top);

    public (string LastSearchText, string LastReplaceText, double? Left, double? Top) GetFindReplaceState(
        string fallbackSearch, string fallbackReplace, double? fallbackLeft, double? fallbackTop)
        => Host.GetFindReplaceState(fallbackSearch, fallbackReplace, fallbackLeft, fallbackTop);

    public void CloseFindReplace() => Host.CloseFindReplace();

    // ── Private helpers ────────────────────────────────────────────────────

    private NotebookViewModel? GetSelectedNotebook()
    {
        if (NotebookTree.SelectedItem is NotebookViewModel nb) return nb;
        if (NotebookTree.SelectedItem is NoteViewModel note)
        {
            foreach (var n in ViewModel.Notebooks)
                if (n.Notes.Contains(note)) return n;
        }
        return ViewModel.Notebooks.Count > 0 ? ViewModel.Notebooks[0] : null;
    }

    private string? FindNotebookTitleOf(NoteViewModel note) =>
        ViewModel.FindNotebookOf(note)?.Title;

    private void ShowError(string message, string title = "エラー") => Host.ShowError(message, title);
    private void ShowInfo(string message, string title = "情報") => Host.ShowInfo(message, title);
    private bool Confirm(string message, string title = "確認",
        MessageBoxImage icon = MessageBoxImage.Warning) => Host.Confirm(message, title, icon);

    // ── NoteNest Markdown エクスポート（SH-25: Shell File メニューから移管） ──

    private void ExportNoteMarkdownCopy_Click(object sender, RoutedEventArgs e)
    {
        var note = ViewModel.SelectedNote;
        if (note == null) return;
        var markdown = NoteNestMarkdownExportService.BuildCurrentNoteMarkdown(note);
        try
        {
            Clipboard.SetText(markdown);
            Host.ShowTransientStatus("コピーしました");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("MarkdownCopyToClipboard", ex, "NoteNest");
            ShowError("クリップボードへのコピーに失敗しました。", "コピーエラー");
        }
    }

    // ── v2.19.4 M15: 右ペイン（マーカー／タスク）の一括コピー ────────────────

    private void CopyAllMarkers_Click(object sender, RoutedEventArgs e)
    {
        var markers = ViewModel.FilteredMarkers.ToList();
        var markdown = NoteNestRightPaneMarkdownFormatter.FormatMarkers(markers);
        if (string.IsNullOrEmpty(markdown)) return;
        try
        {
            Clipboard.SetText(markdown);
            Host.ShowTransientStatus($"{markers.Count}件のマーカーをコピーしました");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("MarkerListMarkdownCopyToClipboard", ex, "NoteNest");
            ShowError("クリップボードへのコピーに失敗しました。", "コピーエラー");
        }
    }

    private void CopyAllTasks_Click(object sender, RoutedEventArgs e)
    {
        var tasks = ViewModel.TaskGroups.SelectMany(g => g.Tasks).ToList();
        var markdown = NoteNestRightPaneMarkdownFormatter.FormatTasks(ViewModel.TaskGroups);
        if (string.IsNullOrEmpty(markdown)) return;
        try
        {
            Clipboard.SetText(markdown);
            Host.ShowTransientStatus($"{tasks.Count}件のタスクをコピーしました");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("TaskListMarkdownCopyToClipboard", ex, "NoteNest");
            ShowError("クリップボードへのコピーに失敗しました。", "コピーエラー");
        }
    }

    private void ExportNoteMarkdownSave_Click(object sender, RoutedEventArgs e)
    {
        var note = ViewModel.SelectedNote;
        if (note == null) return;
        var defaultName = BuildMarkdownDefaultFileName(note.Title);
        var path = Host.SelectMarkdownSavePath(defaultName);
        if (path == null) return;
        var markdown = NoteNestMarkdownExportService.BuildCurrentNoteMarkdown(note);
        SaveMarkdownFile(path, markdown);
    }

    private void ExportAllNotesMarkdownSave_Click(object sender, RoutedEventArgs e)
    {
        var defaultName = BuildMarkdownDefaultFileName(ViewModel.ProjectName);
        var path = Host.SelectMarkdownSavePath(defaultName);
        if (path == null) return;
        var markdown = NoteNestMarkdownExportService.BuildAllNotesMarkdown(ViewModel.ProjectName, ViewModel.AllNotes);
        SaveMarkdownFile(path, markdown);
    }

    private void SaveMarkdownFile(string path, string content)
    {
        try
        {
            AtomicFileWriter.WriteAllText(path, content, Encoding.UTF8);
            Host.ShowTransientStatus("Markdown をエクスポートしました");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("MarkdownExport", ex, "NoteNest");
            ShowError("Markdown ファイルの保存に失敗しました。", "保存エラー");
        }
    }

    private static string BuildMarkdownDefaultFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "note.md";
        var safe = ExportService.SanitizeFileName(name);
        return string.IsNullOrWhiteSpace(safe) ? "note.md" : safe + ".md";
    }
}
