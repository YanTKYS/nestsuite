using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using NestSuite.Services;

namespace NestSuite.NoteNest.Editor;

public partial class NoteEditorHost : UserControl
{
    private ScrollViewer? _editorScrollViewer;
    private ScrollViewer? _lineNumberScrollViewer;
    private int  _completionLinkStart     = -1;
    private bool _suppressCompletionUpdate;
    private bool _editorEventsAttached;
    private IReadOnlyList<LineHighlightInfo> _markerHighlights = Array.Empty<LineHighlightInfo>();
    private TextBoxLineLayoutAdapter? _lineLayout;

    public ITextEditorAdapter Editor { get; private set; } = null!;

    // Set by NoteNestWorkspaceView — provides note titles for autocomplete candidates
    public Func<IEnumerable<string>>? NoteTitleProvider     { get; set; }
    // Set by NoteNestWorkspaceView — returns false in task-comment mode to suppress autocomplete
    public Func<bool>?                IsNoteEditModeProvider { get; set; }

    public event RoutedEventHandler? OpenNoteLinkClicked;
    public event RoutedEventHandler? InsertNoteLinkClicked;
    public event EventHandler?       EditorReady;

    public NoteEditorHost()
    {
        InitializeComponent();
        IsVisibleChanged    += NoteEditorHost_IsVisibleChanged;
        Unloaded            += NoteEditorHost_Unloaded;
        DataContextChanged  += NoteEditorHost_DataContextChanged;
        MarkerHighlightCanvas.SizeChanged += (_, _) =>
            Dispatcher.InvokeAsync(UpdateLayoutDependentUI, DispatcherPriority.Render);
    }

    // ── Initialisation ────────────────────────────────────────────────────

    private void EditorBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (_editorEventsAttached) return;
        _editorEventsAttached = true;

        _editorScrollViewer     = GetDescendant<ScrollViewer>(EditorBox);
        _lineNumberScrollViewer = GetDescendant<ScrollViewer>(LineNumberBox);
        if (_editorScrollViewer != null)
            _editorScrollViewer.ScrollChanged += EditorScrollViewer_ScrollChanged;

        CompletionPopup.PlacementTarget = EditorBox;

        Editor = new TextBoxEditorAdapter(EditorBox);
        Editor.SelectionChanged += Editor_SelectionChanged;

        EditorBox.PreviewKeyDown += EditorBox_PreviewKeyDown;
        EditorBox.LostFocus      += EditorBox_LostFocus;

        _lineLayout = new TextBoxLineLayoutAdapter(EditorBox);
        _markerHighlights = MarkerLineDetector.Detect(EditorBox.Text);
        ThemeService.ThemeChanged += OnThemeServiceThemeChanged;
        UpdateCurrentLineHighlight();
        UpdateStatusBar();
        Dispatcher.InvokeAsync(UpdateLayoutDependentUI, DispatcherPriority.Render);
        EditorReady?.Invoke(this, EventArgs.Empty);
    }

    private void NoteEditorHost_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            UpdateCurrentLineHighlight();
            Dispatcher.InvokeAsync(UpdateLayoutDependentUI, DispatcherPriority.Render);
        }
        else
        {
            CloseCompletion();
        }
    }

    private void Editor_SelectionChanged(object? sender, EventArgs e)
    {
        UpdateCurrentLineHighlight();
        UpdateStatusBar();
        if (!_suppressCompletionUpdate) UpdateCompletion();
    }

    private void NoteEditorHost_Unloaded(object sender, RoutedEventArgs e)
    {
        if (!_editorEventsAttached) return;
        _editorEventsAttached = false;

        if (_editorScrollViewer != null)
            _editorScrollViewer.ScrollChanged -= EditorScrollViewer_ScrollChanged;
        if (Editor != null)
        {
            Editor.SelectionChanged -= Editor_SelectionChanged;
            if (Editor is IDisposable disposableEditor)
                disposableEditor.Dispose();
        }
        EditorBox.PreviewKeyDown -= EditorBox_PreviewKeyDown;
        EditorBox.LostFocus      -= EditorBox_LostFocus;
        if (DataContext is INotifyPropertyChanged vm)
            vm.PropertyChanged -= OnViewModelPropertyChanged;
        ThemeService.ThemeChanged -= OnThemeServiceThemeChanged;
        CloseCompletion();
        MarkerHighlightCanvas.Children.Clear();
        _editorScrollViewer = null;
        _lineNumberScrollViewer = null;
    }

    private void EditorBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // v2.9.5 SH-21 hotfix: Unloaded 後（DataContext 解除中含む）は補完更新をスキップする。
        // DetachedWorkspaceWindow.OnClosed で Children.Clear() → DataContext=null の順に処理するが、
        // WPF が TextChanged を同期的に発火する場合に UpdateCompletion 内で NullReferenceException が
        // 起こりうる。_editorEventsAttached = false は NoteEditorHost_Unloaded で設定される。
        if (!_editorEventsAttached) return;
        _markerHighlights = MarkerLineDetector.Detect(EditorBox.Text);
        UpdateCurrentLineHighlight();
        UpdateStatusBar();
        Dispatcher.InvokeAsync(UpdateLayoutDependentUI, DispatcherPriority.Render);
        if (!_suppressCompletionUpdate) UpdateCompletion();
    }

    private void EditorScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _lineNumberScrollViewer?.ScrollToVerticalOffset(e.VerticalOffset);
        Dispatcher.InvokeAsync(UpdateCurrentLineHighlight, DispatcherPriority.Render);
        Dispatcher.InvokeAsync(UpdateMarkerHighlights,     DispatcherPriority.Render);
    }

    // ── Layout-dependent UI (line numbers + marker highlights) ────────────

    private void UpdateLayoutDependentUI()
    {
        UpdateLineNumbersFromLayout();
        UpdateCurrentLineHighlight();
        UpdateMarkerHighlights();
    }

    private void UpdateLineNumbersFromLayout()
    {
        var text = EditorBox.Text;
        LineNumberBox.Text = _lineLayout != null
            ? _lineLayout.BuildLineNumberText(text)
            : string.Join("\n", Enumerable.Range(1, text.Count(c => c == '\n') + 1));
    }

    private void UpdateCurrentLineHighlight()
    {
        if (Editor == null || LineNumberBox.LineCount == 0)
        {
            CurrentLineHighlight.Visibility = Visibility.Collapsed;
            return;
        }

        // v2.15.4 BUG: The gutter highlight must be anchored to the editor
        // TextBox's own rendered caret line.  Using LineNumberBox coordinates
        // makes any small font/layout metric difference accumulate over long
        // documents (for example line 118 highlighting line 112).
        var lineIndex = Editor.GetLineIndexFromCharacterIndex(Editor.CaretIndex);
        if (lineIndex < 0) lineIndex = 0;

        var lineStartIndex = Editor.GetCharacterIndexFromLineIndex(lineIndex);
        if (lineStartIndex < 0)
        {
            CurrentLineHighlight.Visibility = Visibility.Collapsed;
            return;
        }

        Rect rect;
        try   { rect = EditorBox.GetRectFromCharacterIndex(lineStartIndex); }
        catch { CurrentLineHighlight.Visibility = Visibility.Collapsed; return; }
        if (rect.IsEmpty || rect.Height <= 0)
        {
            CurrentLineHighlight.Visibility = Visibility.Collapsed;
            return;
        }
        if (rect.Bottom <= 0 || rect.Top >= EditorBox.ActualHeight)
        {
            CurrentLineHighlight.Visibility = Visibility.Collapsed;
            return;
        }

        Canvas.SetTop(CurrentLineHighlight, rect.Top);
        CurrentLineHighlight.Height = rect.Height;
        CurrentLineHighlight.Width  = LineHighlightCanvas.ActualWidth > 0
            ? LineHighlightCanvas.ActualWidth
            : LineNumberBox.ActualWidth;
        CurrentLineHighlight.Visibility = Visibility.Visible;
    }

    // ── L14 / L15: Status bar — caret position, char count, line count ───

    private void UpdateStatusBar()
    {
        if (Editor == null) return;
        var caretIndex = Editor.CaretIndex;
        var text = EditorBox.Text;

        var lineIndex = Editor.GetLineIndexFromCharacterIndex(caretIndex);
        if (lineIndex < 0) lineIndex = 0;
        var lineStart = Editor.GetCharacterIndexFromLineIndex(lineIndex);
        var col = lineStart >= 0 ? caretIndex - lineStart : 0;

        var charCount = text.Length;
        var lineCount = string.IsNullOrEmpty(text) ? 1 : text.Count(c => c == '\n') + 1;

        EditorStatusBar.Text = $"行 {lineIndex + 1}, 列 {col + 1}  |  文字数 {charCount}  |  行数 {lineCount}";
    }

    // ── H3b: Marker line highlights (TODO / FIXME / NOTE) ────────────────

    private void NoteEditorHost_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldVm)
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        if (e.NewValue is INotifyPropertyChanged newVm)
            newVm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "EditorFontSize")
            Dispatcher.InvokeAsync(UpdateLayoutDependentUI, DispatcherPriority.Render);
    }

    private void OnThemeServiceThemeChanged(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(UpdateMarkerHighlights, DispatcherPriority.Render);
    }

    private Brush? BrushForKind(LineHighlightKind kind) =>
        TryFindResource(kind switch
        {
            LineHighlightKind.Fixme    => "MarkerLineHighlightFixmeBrush",
            LineHighlightKind.Todo     => "MarkerLineHighlightTodoBrush",
            LineHighlightKind.Note     => "MarkerLineHighlightNoteBrush",
            LineHighlightKind.NoteLink => "NoteLinkLineHighlightBrush",
            _                          => "MarkerLineHighlightBrush",
        }) as Brush;

    private void UpdateMarkerHighlights()
    {
        MarkerHighlightCanvas.Children.Clear();
        if (Editor == null || _lineLayout == null) return;

        var highlights = _markerHighlights;
        if (highlights.Count == 0) return;

        var canvasHeight = MarkerHighlightCanvas.ActualHeight;
        var canvasWidth  = MarkerHighlightCanvas.ActualWidth;
        if (canvasWidth <= 0 || canvasHeight <= 0) return;

        var text = EditorBox.Text;
        foreach (var hi in highlights)
        {
            var brush = BrushForKind(hi.Kind);
            if (brush == null) continue;

            var (top, height) = _lineLayout.HighlightBounds(text, hi.LogicalIndex);
            if (height <= 0) continue;
            if (top + height <= 0 || top >= canvasHeight) continue;

            var r = new Rectangle { Fill = brush, Width = canvasWidth, Height = height };
            Canvas.SetTop(r, top);
            MarkerHighlightCanvas.Children.Add(r);
        }
    }

    // ── Note-link autocomplete (H1a) ──────────────────────────────────────

    private void UpdateCompletion()
    {
        // Suppress in task-comment mode
        if (IsNoteEditModeProvider?.Invoke() == false)
        {
            CloseCompletion();
            return;
        }

        var ctx = TryExtractCompletionContext();
        if (!ctx.Found)
        {
            CloseCompletion();
            return;
        }

        // v2.9.5 SH-21 hotfix: provider 呼び出しが例外を投げても補完を閉じるだけにとどめる。
        IEnumerable<string> titles;
        try
        {
            titles = NoteTitleProvider?.Invoke() ?? Enumerable.Empty<string>();
        }
        catch
        {
            CloseCompletion();
            return;
        }
        var candidates = FilterCandidates(titles, ctx.Query);
        if (candidates.Count == 0)
        {
            CloseCompletion();
            return;
        }

        _completionLinkStart = ctx.LinkStart;
        CompletionList.ItemsSource = candidates;
        if (CompletionList.SelectedIndex < 0 || CompletionList.SelectedIndex >= candidates.Count)
            CompletionList.SelectedIndex = 0;

        PositionAndOpenPopup();
    }

    // Case-insensitive partial-match; "starts with" ranked first; max 20 items
    private static List<string> FilterCandidates(IEnumerable<string> titles, string query)
    {
        if (string.IsNullOrEmpty(query))
            return titles
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToList();

        return titles
            .Where(t => t.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(t => t.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();
    }

    private void PositionAndOpenPopup()
    {
        try
        {
            var rect = EditorBox.GetRectFromCharacterIndex(EditorBox.CaretIndex);
            if (!rect.IsEmpty && rect.Bottom > 0 && rect.Top < EditorBox.ActualHeight)
            {
                CompletionPopup.HorizontalOffset = Math.Max(0, rect.Left);
                CompletionPopup.VerticalOffset   = rect.Bottom;
            }
            else
            {
                CompletionPopup.HorizontalOffset = 0;
                CompletionPopup.VerticalOffset   = 0;
            }
        }
        catch
        {
            CompletionPopup.HorizontalOffset = 0;
            CompletionPopup.VerticalOffset   = 0;
        }
        CompletionPopup.IsOpen = true;
    }

    public void CloseCompletion()
    {
        CompletionPopup.IsOpen     = false;
        CompletionList.ItemsSource = null;
        _completionLinkStart       = -1;
    }

    private void ConfirmCompletion(string noteTitle)
    {
        if (_completionLinkStart < 0) return;
        _suppressCompletionUpdate = true;
        try
        {
            var caret     = Editor.CaretIndex;
            var selectLen = Math.Max(0, caret - _completionLinkStart);
            Editor.Select(_completionLinkStart, selectLen);
            Editor.ReplaceSelection($"[[{noteTitle}]]");
        }
        finally
        {
            _suppressCompletionUpdate = false;
        }
        CloseCompletion();
        Editor.Focus();
    }

    // Searches backward from caret for an incomplete [[... sequence.
    // Returns (Found, LinkStart, Query). Cancels on ]], newline, or lone ].
    private (bool Found, int LinkStart, string Query) TryExtractCompletionContext()
    {
        if (Editor == null) return (false, -1, "");
        var caret = Editor.CaretIndex;
        var text  = Editor.Text;
        if (caret <= 0 || caret > text.Length) return (false, -1, "");

        var searchFrom = Math.Max(0, caret - 300);
        var prefix     = text.Substring(searchFrom, caret - searchFrom);

        var bracketIdx = prefix.LastIndexOf("[[", StringComparison.Ordinal);
        if (bracketIdx < 0) return (false, -1, "");

        var query = prefix.Substring(bracketIdx + 2);

        if (query.Contains("]]") || query.IndexOf('\n') >= 0 || query.IndexOf('\r') >= 0 || query.IndexOf(']') >= 0)
            return (false, -1, "");

        return (true, searchFrom + bracketIdx, query);
    }

    private void MoveCompletionSelection(int delta)
    {
        if (CompletionList.Items.Count == 0) return;
        var newIdx = (CompletionList.SelectedIndex + delta + CompletionList.Items.Count)
                     % CompletionList.Items.Count;
        CompletionList.SelectedIndex = newIdx;
        CompletionList.ScrollIntoView(CompletionList.SelectedItem);
    }

    // ── Keyboard / focus handlers ─────────────────────────────────────────

    private void EditorBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!CompletionPopup.IsOpen) return;
        switch (e.Key)
        {
            case Key.Down:
                MoveCompletionSelection(1);
                e.Handled = true;
                break;
            case Key.Up:
                MoveCompletionSelection(-1);
                e.Handled = true;
                break;
            case Key.Tab:
            {
                var title = CompletionList.SelectedItem as string
                            ?? (CompletionList.Items.Count > 0
                                ? CompletionList.Items[0] as string
                                : null);
                if (title != null) { ConfirmCompletion(title); e.Handled = true; }
                break;
            }
            case Key.Escape:
                CloseCompletion();
                e.Handled = true;
                break;
            // Enter is intentionally NOT handled here.
            // AcceptsReturn inserts a newline, which puts \n in the query and auto-closes the popup.
            // This avoids misinterpreting IME composition Enter as autocomplete confirmation.
        }
    }

    private void EditorBox_LostFocus(object sender, RoutedEventArgs e)
    {
        // Delay close to let popup mouse-click handlers run first
        Dispatcher.InvokeAsync(CloseCompletion, DispatcherPriority.Input);
    }

    private void CompletionList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var item = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (item?.DataContext is string title)
        {
            ConfirmCompletion(title);
            e.Handled = true;
        }
    }

    // ── Context menu handlers ──────────────────────────────────────────────

    private void OpenNoteLink_ItemClick(object sender, RoutedEventArgs e) =>
        OpenNoteLinkClicked?.Invoke(this, e);

    private void InsertNoteLink_ItemClick(object sender, RoutedEventArgs e) =>
        InsertNoteLinkClicked?.Invoke(this, e);

    // ── Helpers ──────────────────────────────────────────────────────────

    private static T? GetDescendant<T>(DependencyObject obj) where T : DependencyObject
    {
        if (obj is T t) return t;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var result = GetDescendant<T>(VisualTreeHelper.GetChild(obj, i));
            if (result != null) return result;
        }
        return null;
    }

    private static T? FindAncestor<T>(DependencyObject? obj) where T : DependencyObject
    {
        while (obj != null)
        {
            if (obj is T t) return t;
            obj = VisualTreeHelper.GetParent(obj);
        }
        return null;
    }
}
