using System.Windows;
using System.Windows.Controls;
using NoteNest.Services;
using NoteNest.ViewModels;

namespace NoteNest.Views;

public partial class NoteNestWorkspaceView : UserControl
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private DialogService? _dialogs;
    private DialogService Dialogs => _dialogs ??= new DialogService(Window.GetWindow(this)!);

    private bool _suppressTreeSelectionChanged;
    private readonly DragDropState _dragDrop = new();
    private ScrollViewer? _editorScrollViewer;
    private ScrollViewer? _lineNumberScrollViewer;

    private bool _isRightPaneCollapsed;
    private double _savedRightPaneWidth = 280;

    public NoteNestWorkspaceView()
    {
        InitializeComponent();
    }

    // ── Public API for MainWindow ──────────────────────────────────────────

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
            RightPaneColumn.Width = new GridLength(savedWidth);
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
        RightSplitterColumn.Width = new GridLength(0);
        RightPaneColumn.MinWidth = 0;
        RightPaneColumn.Width = new GridLength(0);
        _isRightPaneCollapsed = true;
        RightPaneExpandButton.Visibility = Visibility.Visible;
    }

    public void ExpandRightPane()
    {
        RightSplitterColumn.Width = new GridLength(4);
        RightPaneColumn.MinWidth = 200;
        RightPaneColumn.Width = new GridLength(_savedRightPaneWidth);
        _isRightPaneCollapsed = false;
        RightPaneExpandButton.Visibility = Visibility.Collapsed;
    }

    public void NavigateToLine(int lineNumber)
    {
        if (lineNumber < 1) lineNumber = 1;
        var lineIndex = lineNumber - 1;
        if (lineIndex >= EditorBox.LineCount) lineIndex = EditorBox.LineCount - 1;
        if (lineIndex < 0) return;
        EditorBox.ScrollToLine(lineIndex);
        var charIdx = EditorBox.GetCharacterIndexFromLineIndex(lineIndex);
        EditorBox.CaretIndex = charIdx;
        EditorBox.Focus();
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

    public void TryOpenNoteLink()
    {
        var linkTitle = NoteLinkService.ExtractLinkAtCursor(EditorBox.Text, EditorBox.CaretIndex);
        if (linkTitle == null) return;
        var note = ViewModel.FindNoteByTitle(linkTitle);
        if (note == null)
        {
            ShowInfo($"ノート「{linkTitle}」が見つかりません。", "リンク先なし");
            return;
        }
        ViewModel.NavigateToNote(note);
    }

    public void OpenFindReplace(string lastSearch, string lastReplace, double? left, double? top)
        => Dialogs.ShowFindReplace(EditorBox, lastSearch, lastReplace, left, top);

    public (string LastSearchText, string LastReplaceText, double? Left, double? Top) GetFindReplaceState(
        string fallbackSearch, string fallbackReplace, double? fallbackLeft, double? fallbackTop)
        => Dialogs.GetFindReplaceState(fallbackSearch, fallbackReplace, fallbackLeft, fallbackTop);

    public void CloseFindReplace() => Dialogs.CloseFindReplace();

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

    private void ShowError(string message, string title = "エラー") => Dialogs.ShowError(message, title);
    private void ShowInfo(string message, string title = "情報") => Dialogs.ShowInfo(message, title);
    private bool Confirm(string message, string title = "確認",
        MessageBoxImage icon = MessageBoxImage.Warning) => Dialogs.Confirm(message, title, icon);
}
