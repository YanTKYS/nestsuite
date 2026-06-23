using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NestSuite.NoteNest.Editor;

public partial class NoteEditorHost : UserControl
{
    private ScrollViewer? _editorScrollViewer;
    private ScrollViewer? _lineNumberScrollViewer;
    private int  _completionLinkStart     = -1;
    private bool _suppressCompletionUpdate;
    private bool _editorEventsAttached;

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
        IsVisibleChanged += NoteEditorHost_IsVisibleChanged;
        Unloaded += NoteEditorHost_Unloaded;
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

        UpdateLineNumbers();
        UpdateCurrentLineHighlight();
        EditorReady?.Invoke(this, EventArgs.Empty);
    }

    private void NoteEditorHost_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible) UpdateCurrentLineHighlight();
        else           CloseCompletion();
    }

    private void Editor_SelectionChanged(object? sender, EventArgs e)
    {
        UpdateCurrentLineHighlight();
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
        CloseCompletion();
        _editorScrollViewer = null;
        _lineNumberScrollViewer = null;
    }

    private void EditorBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateLineNumbers();
        UpdateCurrentLineHighlight();
        if (!_suppressCompletionUpdate) UpdateCompletion();
    }

    private void EditorScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _lineNumberScrollViewer?.ScrollToVerticalOffset(e.VerticalOffset);
        Dispatcher.InvokeAsync(UpdateCurrentLineHighlight, DispatcherPriority.Render);
    }

    // ── Line number helpers ────────────────────────────────────────────────

    private void UpdateLineNumbers()
    {
        var count = EditorBox.Text.Count(c => c == '\n') + 1;
        LineNumberBox.Text = string.Join("\n", Enumerable.Range(1, count));
    }

    private void UpdateCurrentLineHighlight()
    {
        if (Editor == null || LineNumberBox.LineCount == 0)
        {
            CurrentLineHighlight.Visibility = Visibility.Collapsed;
            return;
        }
        var lineIndex = Editor.GetLineIndexFromCharacterIndex(Editor.CaretIndex);
        if (lineIndex < 0) lineIndex = 0;
        if (lineIndex >= LineNumberBox.LineCount)
        {
            CurrentLineHighlight.Visibility = Visibility.Collapsed;
            return;
        }
        var charIdx = LineNumberBox.GetCharacterIndexFromLineIndex(lineIndex);
        if (charIdx < 0)
        {
            CurrentLineHighlight.Visibility = Visibility.Collapsed;
            return;
        }
        Rect rect;
        try   { rect = LineNumberBox.GetRectFromCharacterIndex(charIdx); }
        catch { CurrentLineHighlight.Visibility = Visibility.Collapsed; return; }
        if (rect.IsEmpty || rect.Height <= 0)
        {
            CurrentLineHighlight.Visibility = Visibility.Collapsed;
            return;
        }
        if (rect.Bottom <= 0 || rect.Top >= LineNumberBox.ActualHeight)
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

        var titles     = NoteTitleProvider?.Invoke() ?? Enumerable.Empty<string>();
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
