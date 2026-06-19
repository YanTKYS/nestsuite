using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NoteNest.ViewModels;

namespace NoteNest.Views;

public partial class NoteNestWorkspaceView
{
    private void EditorBox_SelectionChanged(object sender, RoutedEventArgs e)
    {
        var caret     = EditorBox.CaretIndex;
        var lineIndex = EditorBox.GetLineIndexFromCharacterIndex(caret);
        if (lineIndex < 0) lineIndex = 0;
        var lineStart = EditorBox.GetCharacterIndexFromLineIndex(lineIndex);
        var col       = caret - lineStart + 1;
        ViewModel.CaretPositionText = $"{lineIndex + 1}:{col}";
    }

    private void Marker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MarkerViewModel m)
            ViewModel.MarkerClickCommand.Execute(m);
    }

    private void InsertMarker(string markerText) => InsertTextAtCaret($"{markerText} ");

    private void InsertTodo_Click(object sender, RoutedEventArgs e)  => InsertMarker("[TODO]");
    private void InsertFixme_Click(object sender, RoutedEventArgs e) => InsertMarker("[FIXME]");
    private void InsertNote_Click(object sender, RoutedEventArgs e)  => InsertMarker("[NOTE]");

    private void OpenNoteLink_Click(object sender, RoutedEventArgs e) => TryOpenNoteLink();

    private void InsertNoteLink_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsTaskCommentMode) return;
        var items = ViewModel.Notebooks
            .SelectMany(nb => nb.Notes.Select(n => (NotebookTitle: nb.Title, Note: n)))
            .ToList();
        if (items.Count == 0) { ShowInfo("リンクできるノートがありません。"); return; }
        var note = Host.PickNote(items);
        if (note == null) return;
        InsertTextAtCaret($"[[{note.Title}]]");
    }

    private void InsertNoteLinkFromNote_Click(object sender, RoutedEventArgs e)
    {
        var note = GetContextMenuDataContext<NoteViewModel>(sender);
        if (note == null) return;
        if (ViewModel.IsTaskCommentMode)
        {
            ShowInfo("タスクコメント編集中はノートリンクを挿入できません。\nノート本文を編集中のときに使用してください。");
            return;
        }
        if (ViewModel.SelectedNote == null) return;
        if (ViewModel.NoteNameExists(note.Title, excludeSelf: note))
        {
            if (!Confirm(
                $"「{note.Title}」という名前のノートが複数あります。\n" +
                $"[[{note.Title}]] リンクは最初に見つかったノートへ解決される場合があります。\n\n" +
                "このノートへのリンクを挿入しますか？",
                "同名ノートの警告"))
                return;
        }
        InsertTextAtCaret($"[[{note.Title}]]");
    }

    private void InsertTextAtCaret(string text)
    {
        var caret = EditorBox.CaretIndex;
        EditorBox.Select(caret, 0);
        EditorBox.SelectedText = text;
        EditorBox.CaretIndex = caret + text.Length;
        EditorBox.Focus();
    }

    private void EditorBox_Loaded(object sender, RoutedEventArgs e)
    {
        _editorScrollViewer     = GetDescendant<ScrollViewer>(EditorBox);
        _lineNumberScrollViewer = GetDescendant<ScrollViewer>(LineNumberBox);
        if (_editorScrollViewer != null)
            _editorScrollViewer.ScrollChanged += EditorScrollViewer_ScrollChanged;
        UpdateLineNumbers();
    }

    private void EditorBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateLineNumbers();

    private void EditorScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _lineNumberScrollViewer?.ScrollToVerticalOffset(e.VerticalOffset);
    }

    private void UpdateLineNumbers()
    {
        var count = EditorBox.Text.Count(c => c == '\n') + 1;
        LineNumberBox.Text = string.Join("\n", Enumerable.Range(1, count));
    }

    private void ToggleRightPane_Click(object sender, RoutedEventArgs e)
    {
        ToggleRightPane();
        RightPaneToggled?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? RightPaneToggled;

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
}
