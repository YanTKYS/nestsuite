using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NestSuite.NoteNest.Editor;
using NestSuite.Services;
using NestSuite.ViewModels;

namespace NestSuite.Dialogs;

internal sealed record AllNoteMatchItem(
    NoteViewModel Note,
    int CharIndex,
    string Display,
    string TitlePrefix,
    string MatchBefore,
    string MatchText,
    string MatchAfter,
    bool HasMatch)
{
    public static AllNoteMatchItem Create(
        NoteViewModel note,
        int charIndex,
        string context,
        string keyword,
        StringComparison comparison)
    {
        var segments = SearchMatchSegments.Split(context, keyword, comparison);
        return new AllNoteMatchItem(
            note,
            charIndex,
            $"{note.Title}: {context}",
            $"{note.Title}: ",
            segments.Before,
            segments.Match,
            segments.After,
            segments.HasMatch);
    }
}

public partial class FindReplaceDialog : Window
{
    private ITextEditorAdapter _editor;
    private IEnumerable<NoteViewModel>? _allNotes;
    private Action<NoteViewModel>? _navigateToNote;

    private int _lastFoundIndex = -1;
    private List<int> _matchPositions = new();
    private int _currentMatchIndex = -1;

    private const int AllNotesMaxResults = 200;

    private static readonly Brush _noMatchBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0x44, 0x44));
    private static readonly Brush _normalBrush  = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));

    static FindReplaceDialog()
    {
        _noMatchBrush.Freeze();
        _normalBrush.Freeze();
    }

    public FindReplaceDialog(ITextEditorAdapter editor)
    {
        InitializeComponent();
        _editor = editor;
        Loaded += (_, _) => FindBox.Focus();
        FindBox.TextChanged += (_, _) => OnSearchTermChanged();
        _editor.TextChanged += OnEditorTextChanged;
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (IsVisible && AllNotesCheck.IsChecked != true)
            UpdateMatchCount();
    }

    internal void SetEditor(ITextEditorAdapter adapter)
    {
        if (ReferenceEquals(_editor, adapter)) return;
        _editor.TextChanged -= OnEditorTextChanged;
        _editor = adapter;
        _editor.TextChanged += OnEditorTextChanged;
        _lastFoundIndex = -1;
        _currentMatchIndex = -1;
        _matchPositions.Clear();
        UpdateMatchCount();
    }

    internal void SetAllNotes(IEnumerable<NoteViewModel> allNotes, Action<NoteViewModel> navigateToNote)
    {
        _allNotes = allNotes;
        _navigateToNote = navigateToNote;
        AllNotesResultList.ItemsSource = null;
        AllNotesResultBorder.Visibility = Visibility.Collapsed;
    }

    private StringComparison Comparison =>
        CaseCheck.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    // ── 検索語・条件変更ハンドラ ──────────────────────────────────────────────

    private void OnSearchTermChanged()
    {
        _lastFoundIndex = -1;
        _currentMatchIndex = -1;
        StatusText.Text = "";
        UpdateMatchCount();
    }

    private void CaseCheck_Changed(object sender, RoutedEventArgs e)
    {
        _lastFoundIndex = -1;
        _currentMatchIndex = -1;
        StatusText.Text = "";
        UpdateMatchCount();
    }

    private void AllNotes_Changed(object sender, RoutedEventArgs e)
    {
        var isAllNotes = AllNotesCheck.IsChecked == true;
        ReplaceButton.IsEnabled    = !isAllNotes;
        ReplaceAllButton.IsEnabled = !isAllNotes;

        if (isAllNotes)
        {
            MatchCountText.Text = "";
            if (Height < 440) Height = 440;
        }
        else
        {
            AllNotesResultBorder.Visibility = Visibility.Collapsed;
            AllNotesResultList.ItemsSource  = null;
            UpdateMatchCount();
        }
    }

    // ── 件数管理 ────────────────────────────────────────────────────────────

    private void UpdateMatchCount()
    {
        if (AllNotesCheck.IsChecked == true) return;

        var keyword = FindBox.Text;
        if (string.IsNullOrEmpty(keyword))
        {
            MatchCountText.Text = "";
            _matchPositions.Clear();
            _currentMatchIndex = -1;
            return;
        }

        ComputeMatchPositions(keyword, _editor.Text);
        UpdateCountDisplay();
    }

    private void ComputeMatchPositions(string keyword, string text)
    {
        _matchPositions.Clear();
        _matchPositions.AddRange(FindReplaceLogicService.ComputeMatchPositions(keyword, text, Comparison));
        // 前回の位置を現在の一致リストと照合して再同期する
        _currentMatchIndex = _lastFoundIndex >= 0
            ? _matchPositions.IndexOf(_lastFoundIndex)
            : -1;
    }

    private void UpdateCountDisplay()
    {
        var keyword = FindBox.Text;
        if (string.IsNullOrEmpty(keyword))
        {
            MatchCountText.Text = "";
            return;
        }

        int count = _matchPositions.Count;
        if (count == 0)
        {
            MatchCountText.Text       = "一致なし";
            MatchCountText.Foreground = _noMatchBrush;
        }
        else if (_currentMatchIndex >= 0)
        {
            MatchCountText.Text       = $"{_currentMatchIndex + 1} / {count}";
            MatchCountText.Foreground = _normalBrush;
        }
        else
        {
            MatchCountText.Text       = $"{count} 件";
            MatchCountText.Foreground = _normalBrush;
        }
    }

    // ── 次を検索 ────────────────────────────────────────────────────────────

    private void FindNext_Click(object sender, RoutedEventArgs e) => FindNext();

    private void FindNext()
    {
        var keyword = FindBox.Text;
        if (string.IsNullOrEmpty(keyword)) return;

        if (AllNotesCheck.IsChecked == true) { SearchAllNotes(); return; }

        ComputeMatchPositions(keyword, _editor.Text);

        if (_matchPositions.Count == 0)
        {
            UpdateCountDisplay();
            return;
        }

        bool wrapped = false;
        if (_currentMatchIndex < _matchPositions.Count - 1)
            _currentMatchIndex++;
        else
        {
            _currentMatchIndex = 0;
            wrapped = true;
        }

        NavigateToCurrentMatch(keyword);
        StatusText.Text = wrapped ? "末尾まで検索したため、先頭から検索しました" : "";
        UpdateCountDisplay();
    }

    // ── 前を検索 ────────────────────────────────────────────────────────────

    private void FindPrev_Click(object sender, RoutedEventArgs e) => FindPrev();

    private void FindPrev()
    {
        var keyword = FindBox.Text;
        if (string.IsNullOrEmpty(keyword)) return;

        if (AllNotesCheck.IsChecked == true) return;

        ComputeMatchPositions(keyword, _editor.Text);

        if (_matchPositions.Count == 0)
        {
            UpdateCountDisplay();
            return;
        }

        bool wrapped = false;
        if (_currentMatchIndex > 0)
            _currentMatchIndex--;
        else
        {
            _currentMatchIndex = _matchPositions.Count - 1;
            wrapped = true;
        }

        NavigateToCurrentMatch(keyword);
        StatusText.Text = wrapped ? "先頭まで検索したため、末尾から検索しました" : "";
        UpdateCountDisplay();
    }

    private void NavigateToCurrentMatch(string keyword)
    {
        if (_currentMatchIndex < 0 || _currentMatchIndex >= _matchPositions.Count) return;
        var idx = _matchPositions[_currentMatchIndex];
        _lastFoundIndex = idx;
        _editor.Focus();
        _editor.Select(idx, keyword.Length);
        _editor.ScrollToLine(_editor.GetLineIndexFromCharacterIndex(idx));
    }

    // ── 置換 ────────────────────────────────────────────────────────────────

    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        var keyword = FindBox.Text;
        if (string.IsNullOrEmpty(keyword)) return;

        if (_editor.SelectionLength > 0 &&
            _editor.SelectedText.Equals(keyword, Comparison))
        {
            var caretBefore = _editor.SelectionStart;
            _editor.ReplaceSelection(ReplaceBox.Text);
            _lastFoundIndex = caretBefore - 1;
            _currentMatchIndex = -1;
        }
        FindNext();
    }

    private void ReplaceAll_Click(object sender, RoutedEventArgs e)
    {
        var keyword = FindBox.Text;
        if (string.IsNullOrEmpty(keyword)) return;

        _editor.Text = FindReplaceLogicService.ReplaceAll(_editor.Text, keyword, ReplaceBox.Text, Comparison);
        _lastFoundIndex = -1;
        _currentMatchIndex = -1;
        StatusText.Text = "すべて置換しました。";
        UpdateMatchCount();
    }

    // ── 全ノート検索 ────────────────────────────────────────────────────────

    private void SearchAllNotes()
    {
        var keyword = FindBox.Text;
        if (string.IsNullOrEmpty(keyword) || _allNotes == null)
        {
            MatchCountText.Text = "";
            AllNotesResultBorder.Visibility = Visibility.Collapsed;
            return;
        }

        var results = new List<AllNoteMatchItem>();
        foreach (var note in _allNotes)
        {
            if (results.Count >= AllNotesMaxResults) break;
            var content = note.Content;
            int pos = 0;
            while (pos < content.Length && results.Count < AllNotesMaxResults)
            {
                var idx = content.IndexOf(keyword, pos, Comparison);
                if (idx < 0) break;
                results.Add(AllNoteMatchItem.Create(
                    note,
                    idx,
                    FindReplaceLogicService.BuildMatchContext(content, idx, keyword),
                    keyword,
                    Comparison));
                pos = idx + 1;
            }
        }

        AllNotesResultList.ItemsSource  = results;
        AllNotesResultBorder.Visibility = results.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (results.Count == 0)
        {
            MatchCountText.Text       = "一致なし";
            MatchCountText.Foreground = _noMatchBrush;
        }
        else
        {
            var countText = results.Count >= AllNotesMaxResults
                ? $"{AllNotesMaxResults} 件以上"
                : $"{results.Count} 件";
            MatchCountText.Text       = countText;
            MatchCountText.Foreground = _normalBrush;
        }
        StatusText.Text = "";
    }

    private void AllNotesResult_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (AllNotesResultList.SelectedItem is AllNoteMatchItem item)
            NavigateToAllNoteMatch(item);
    }

    private void AllNotesResult_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && AllNotesResultList.SelectedItem is AllNoteMatchItem item)
            NavigateToAllNoteMatch(item);
    }

    private void NavigateToAllNoteMatch(AllNoteMatchItem item)
    {
        var charIdx = item.CharIndex;
        var keyword = FindBox.Text;
        _navigateToNote?.Invoke(item.Note);
        Dispatcher.BeginInvoke(() =>
        {
            if (charIdx + keyword.Length <= _editor.TextLength)
            {
                _editor.Focus();
                _editor.Select(charIdx, keyword.Length);
                _editor.ScrollToLine(_editor.GetLineIndexFromCharacterIndex(charIdx));
            }
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    // ── ユーティリティ ───────────────────────────────────────────────────────

    internal bool ForceClose { get; set; }

    internal string SearchText  => FindBox.Text;
    internal string ReplaceText => ReplaceBox.Text;

    internal void RestoreState(string searchText, string replaceText, double? left, double? top)
    {
        FindBox.Text    = searchText;
        ReplaceBox.Text = replaceText;
        if (left.HasValue && top.HasValue)
        {
            Left = left.Value;
            Top  = top.Value;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Hide();

    private void FindBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                FindPrev();
            else
                FindNext();
            e.Handled = true;
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!ForceClose)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
