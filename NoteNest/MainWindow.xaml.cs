using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NoteNest.Dialogs;
using NoteNest.ViewModels;

namespace NoteNest;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;
    private FindReplaceDialog? _findReplaceDialog;

    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainViewModel();
        DataContext = vm;

        // Wire up dialog callbacks
        vm.ShowInputDialog = (title, prompt) =>
        {
            var d = new InputDialog(title, prompt) { Owner = this };
            return d.ShowDialog() == true ? d.ResultText : null;
        };

        vm.ShowConfirmDialog = (title, message) =>
            MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
                == MessageBoxResult.Yes;

        vm.RequestClose = Close;

        vm.NavigateToLine = lineNumber =>
        {
            if (lineNumber < 1) lineNumber = 1;
            var lineIndex = lineNumber - 1;
            if (lineIndex >= EditorBox.LineCount) lineIndex = EditorBox.LineCount - 1;
            if (lineIndex < 0) return;

            EditorBox.ScrollToLine(lineIndex);
            var charIdx = EditorBox.GetCharacterIndexFromLineIndex(lineIndex);
            EditorBox.CaretIndex = charIdx;
            EditorBox.Focus();
        };

        vm.NavigateToMarker = m =>
        {
            bool switched = m.SourceNote != null && m.SourceNote != ViewModel.SelectedNote;
            if (switched)
                ViewModel.SelectNote(m.SourceNote!);

            var line = m.LineNumber;
            if (switched)
                // Defer navigation until after the TextBox has laid out new content
                Dispatcher.BeginInvoke(() => ViewModel.NavigateToLine?.Invoke(line),
                    System.Windows.Threading.DispatcherPriority.Loaded);
            else
                ViewModel.NavigateToLine?.Invoke(line);
        };
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control &&
            (e.Key == Key.F || e.Key == Key.H))
        {
            OpenFindReplace();
            e.Handled = true;
        }
    }

    // ── Tree events ────────────────────────────────────────────────────────

    private void NotebookTree_SelectedItemChanged(object sender,
        RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is NoteViewModel note)
            ViewModel.SelectNote(note);
    }

    // ── Left pane ─────────────────────────────────────────────────────────

    private void LeftPaneAdd_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        var addNb = new MenuItem { Header = "ノートブックを追加..." };
        addNb.Click += AddNotebook_Click;
        var addNote = new MenuItem { Header = "ノートを追加..." };
        addNote.Click += AddNote_Click;
        menu.Items.Add(addNb);
        menu.Items.Add(addNote);
        menu.PlacementTarget = (Button)sender;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void AddNotebook_Click(object sender, RoutedEventArgs e)
    {
        var d = new InputDialog("ノートブック追加", "ノートブック名を入力してください:") { Owner = this };
        if (d.ShowDialog() == true && !string.IsNullOrWhiteSpace(d.ResultText))
            ViewModel.AddNotebookWithTitle(d.ResultText.Trim());
    }

    private void AddNote_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetSelectedNotebook();
        if (nb == null)
        {
            MessageBox.Show("先にノートブックを選択または追加してください。", "情報",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var d = new InputDialog("ノート追加", "ノート名を入力してください:") { Owner = this };
        if (d.ShowDialog() == true && !string.IsNullOrWhiteSpace(d.ResultText))
            ViewModel.AddNoteToNotebook(nb, d.ResultText.Trim());
    }

    private void AddNoteToNotebook_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetDataContext<NotebookViewModel>(sender);
        if (nb == null) return;
        var d = new InputDialog("ノート追加", "ノート名を入力してください:") { Owner = this };
        if (d.ShowDialog() == true && !string.IsNullOrWhiteSpace(d.ResultText))
            ViewModel.AddNoteToNotebook(nb, d.ResultText.Trim());
    }

    private void RenameNotebook_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetDataContext<NotebookViewModel>(sender);
        if (nb == null) return;
        var d = new InputDialog("名前変更", "新しいノートブック名:", nb.Title) { Owner = this };
        if (d.ShowDialog() == true && !string.IsNullOrWhiteSpace(d.ResultText))
            ViewModel.RenameNotebook(nb, d.ResultText.Trim());
    }

    private void MoveNotebookUp_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetDataContext<NotebookViewModel>(sender);
        if (nb != null) ViewModel.MoveNotebookUp(nb);
    }

    private void MoveNotebookDown_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetDataContext<NotebookViewModel>(sender);
        if (nb != null) ViewModel.MoveNotebookDown(nb);
    }

    private void DeleteNotebook_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetDataContext<NotebookViewModel>(sender);
        if (nb == null) return;
        if (MessageBox.Show($"ノートブック「{nb.Title}」を削除しますか？\n含まれるノートもすべて削除されます。この操作は取り消せません。",
                            "削除の確認", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            ViewModel.DeleteNotebook(nb);
    }

    private void MoveNoteUp_Click(object sender, RoutedEventArgs e)
    {
        var note = GetDataContext<NoteViewModel>(sender);
        if (note != null) ViewModel.MoveNoteUp(note);
    }

    private void MoveNoteDown_Click(object sender, RoutedEventArgs e)
    {
        var note = GetDataContext<NoteViewModel>(sender);
        if (note != null) ViewModel.MoveNoteDown(note);
    }

    private void RenameNote_Click(object sender, RoutedEventArgs e)
    {
        var note = GetDataContext<NoteViewModel>(sender);
        if (note == null) return;
        var d = new InputDialog("名前変更", "新しいノート名:", note.Title) { Owner = this };
        if (d.ShowDialog() == true && !string.IsNullOrWhiteSpace(d.ResultText))
            ViewModel.RenameNote(note, d.ResultText.Trim());
    }

    private void DeleteNote_Click(object sender, RoutedEventArgs e)
    {
        var note = GetDataContext<NoteViewModel>(sender);
        if (note == null) return;
        var nbTitle = FindNotebookTitleOf(note);
        var location = nbTitle != null ? $"（{nbTitle}）" : "";
        if (MessageBox.Show($"ノート「{note.Title}」{location}を削除しますか？\nこの操作は取り消せません。",
                            "削除の確認", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            ViewModel.DeleteNote(note);
    }

    private void RenameSelectedNote_Click(object sender, RoutedEventArgs e)
    {
        var note = ViewModel.SelectedNote;
        if (note == null) return;
        var d = new InputDialog("名前変更", "新しいノート名:", note.Title) { Owner = this };
        if (d.ShowDialog() == true && !string.IsNullOrWhiteSpace(d.ResultText))
            ViewModel.RenameNote(note, d.ResultText.Trim());
    }

    private void DeleteSelectedNote_Click(object sender, RoutedEventArgs e)
    {
        var note = ViewModel.SelectedNote;
        if (note == null) return;
        var nbTitle = FindNotebookTitleOf(note);
        var location = nbTitle != null ? $"（{nbTitle}）" : "";
        if (MessageBox.Show($"ノート「{note.Title}」{location}を削除しますか？\nこの操作は取り消せません。",
                            "削除の確認", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            ViewModel.DeleteNote(note);
    }

    // ── Task events ────────────────────────────────────────────────────────

    private void AddTaskMenu_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        foreach (var group in ViewModel.TaskGroups)
        {
            var key = group.Key;
            var item = new MenuItem { Header = group.Title };
            item.Click += (_, _) => ViewModel.AddTaskCommand.Execute(key);
            menu.Items.Add(item);
        }
        menu.PlacementTarget = (Button)sender;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void MoveTaskToToday_Click(object sender, RoutedEventArgs e)   => MoveTaskFromMenu(sender, "today");
    private void MoveTaskToWeek_Click(object sender, RoutedEventArgs e)    => MoveTaskFromMenu(sender, "week");
    private void MoveTaskToBacklog_Click(object sender, RoutedEventArgs e) => MoveTaskFromMenu(sender, "backlog");

    private void MoveTaskFromMenu(object sender, string targetGroupKey)
    {
        // Walk up: sub-MenuItem → parent MenuItem → ContextMenu → PlacementTarget
        if (sender is MenuItem subItem &&
            subItem.Parent is MenuItem parentItem &&
            parentItem.Parent is ContextMenu cm &&
            cm.PlacementTarget is FrameworkElement fe &&
            fe.DataContext is TaskViewModel task)
        {
            ViewModel.MoveTask(task, targetGroupKey);
        }
    }

    private void RenameTask_Click(object sender, RoutedEventArgs e)
    {
        var task = GetDataContext<TaskViewModel>(sender);
        if (task == null) return;
        var d = new InputDialog("タスク名変更", "新しいタスク名:", task.Title) { Owner = this };
        if (d.ShowDialog() == true && !string.IsNullOrWhiteSpace(d.ResultText))
            ViewModel.RenameTask(task, d.ResultText.Trim());
    }

    private void DeleteTask_Click(object sender, RoutedEventArgs e)
    {
        var task = GetDataContext<TaskViewModel>(sender);
        if (task == null) return;
        ViewModel.DeleteTaskCommand.Execute(task);
    }

    // ── Marker events ──────────────────────────────────────────────────────

    private void Marker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MarkerViewModel m)
            ViewModel.MarkerClickCommand.Execute(m);
    }

    // ── Task comment editing ───────────────────────────────────────────────

    private void TaskTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && (sender as FrameworkElement)?.DataContext is TaskViewModel task)
        {
            ViewModel.SelectTask(task);
            e.Handled = true;
        }
    }

    // ── Editor marker insertion ────────────────────────────────────────────

    private void InsertMarker(string markerText)
    {
        var insertion = $"{markerText} ";
        var caret = EditorBox.CaretIndex;
        EditorBox.Select(caret, 0);
        EditorBox.SelectedText = insertion;
        EditorBox.CaretIndex = caret + insertion.Length;
        EditorBox.Focus();
    }

    private void InsertTodo_Click(object sender, RoutedEventArgs e)  => InsertMarker("[TODO]");
    private void InsertFixme_Click(object sender, RoutedEventArgs e) => InsertMarker("[FIXME]");
    private void InsertNote_Click(object sender, RoutedEventArgs e)  => InsertMarker("[NOTE]");

    // ── Edit menu actions ──────────────────────────────────────────────────

    private void ShowFindReplace_Click(object sender, RoutedEventArgs e) => OpenFindReplace();

    private void ShowFontSettings_Click(object sender, RoutedEventArgs e)
    {
        var d = new FontSettingsDialog(ViewModel.EditorFontFamily, ViewModel.EditorFontSize)
        {
            Owner = this
        };
        if (d.ShowDialog() == true)
            ViewModel.ApplyFontSettings(d.SelectedFontFamily, d.SelectedFontSize);
    }

    // ── Window events ──────────────────────────────────────────────────────

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!ViewModel.ConfirmCloseIfModified())
        {
            e.Cancel = true;
            return;
        }
        if (_findReplaceDialog != null)
        {
            _findReplaceDialog.ForceClose = true;
            _findReplaceDialog.Close();
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void OpenFindReplace()
    {
        if (_findReplaceDialog == null || !_findReplaceDialog.IsLoaded)
        {
            _findReplaceDialog = new FindReplaceDialog(EditorBox) { Owner = this };
        }
        _findReplaceDialog.Show();
        _findReplaceDialog.Activate();
    }

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

    private string? FindNotebookTitleOf(NoteViewModel note)
    {
        foreach (var nb in ViewModel.Notebooks)
            if (nb.Notes.Contains(note)) return nb.Title;
        return null;
    }

    // Gets DataContext from a MenuItem in a ContextMenu
    private static T? GetDataContext<T>(object sender) where T : class
    {
        if (sender is MenuItem mi &&
            mi.Parent is ContextMenu cm &&
            cm.PlacementTarget is FrameworkElement fe &&
            fe.DataContext is T value)
            return value;
        return null;
    }
}
