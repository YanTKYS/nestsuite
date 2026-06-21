using System.Windows;
using System.Windows.Input;
using NestSuite.ViewModels;

namespace NestSuite.Views;

public partial class NoteNestWorkspaceView
{
    private void EditorAdapter_SelectionChanged(object? sender, EventArgs e)
    {
        var caret     = EditorHost.Editor.CaretIndex;
        var lineIndex = EditorHost.Editor.GetLineIndexFromCharacterIndex(caret);
        if (lineIndex < 0) lineIndex = 0;
        var lineStart = EditorHost.Editor.GetCharacterIndexFromLineIndex(lineIndex);
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
        EditorHost.Editor.InsertTextAtCaret(text);
        EditorHost.Editor.Focus();
    }

    private void ToggleRightPane_Click(object sender, RoutedEventArgs e)
    {
        ToggleRightPane();
        RightPaneToggled?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? RightPaneToggled;
}
