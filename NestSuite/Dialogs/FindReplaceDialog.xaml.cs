using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NoteNest.Dialogs;

public partial class FindReplaceDialog : Window
{
    private readonly TextBox _editor;
    private int _lastFoundIndex = -1;

    public FindReplaceDialog(TextBox editor)
    {
        InitializeComponent();
        _editor = editor;
        Loaded += (_, _) => FindBox.Focus();
        FindBox.TextChanged += (_, _) => _lastFoundIndex = -1;
    }

    private StringComparison Comparison =>
        CaseCheck.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    private void FindNext_Click(object sender, RoutedEventArgs e) => FindNext();

    private void FindNext()
    {
        var keyword = FindBox.Text;
        if (string.IsNullOrEmpty(keyword)) return;

        var text  = _editor.Text;
        var start = _lastFoundIndex >= 0 ? _lastFoundIndex + 1 : 0;
        if (start >= text.Length) start = 0;

        var idx = text.IndexOf(keyword, start, Comparison);
        if (idx < 0 && start > 0)
            idx = text.IndexOf(keyword, 0, Comparison);

        if (idx >= 0)
        {
            _lastFoundIndex = idx;
            _editor.Focus();
            _editor.Select(idx, keyword.Length);
            _editor.ScrollToLine(_editor.GetLineIndexFromCharacterIndex(idx));
        }
        else
        {
            _lastFoundIndex = -1;
            MessageBox.Show("見つかりませんでした。", "検索", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        var keyword = FindBox.Text;
        if (string.IsNullOrEmpty(keyword)) return;

        if (_editor.SelectionLength > 0 &&
            _editor.SelectedText.Equals(keyword, Comparison))
        {
            var caretBefore = _editor.SelectionStart;
            _editor.SelectedText = ReplaceBox.Text;
            _lastFoundIndex = caretBefore - 1;
        }
        FindNext();
    }

    private void ReplaceAll_Click(object sender, RoutedEventArgs e)
    {
        var keyword = FindBox.Text;
        if (string.IsNullOrEmpty(keyword)) return;

        var flags = CaseCheck.IsChecked == true
            ? RegexOptions.None
            : RegexOptions.IgnoreCase;

        var replacement = ReplaceBox.Text;
        var newText = Regex.Replace(_editor.Text, Regex.Escape(keyword), _ => replacement, flags);
        _editor.Text = newText;
        _lastFoundIndex = -1;
        MessageBox.Show("すべて置換しました。", "置換", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    internal bool ForceClose { get; set; }

    // Expose current state for saving across sessions
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
        if (e.Key == Key.Enter) FindNext();
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
