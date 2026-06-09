using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NoteNest.ViewModels;

namespace NoteNest;

public partial class MainWindow
{
    private void NotebookTree_SelectedItemChanged(object sender,
        RoutedPropertyChangedEventArgs<object> e)
    {
        if (_suppressTreeSelectionChanged) return;
        if (e.NewValue is NoteViewModel note)
            ViewModel.SelectNote(note);
    }

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
        var title = _dialogs.ShowInput("ノートブック追加", "ノートブック名を入力してください:");
        if (!string.IsNullOrWhiteSpace(title))
            ViewModel.AddNotebookWithTitle(title.Trim());
    }

    private void AddNote_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetSelectedNotebook();
        if (nb == null) { ShowInfo("先にノートブックを選択または追加してください。"); return; }
        AddNoteToNotebookViaDialog(nb);
    }

    private void AddNoteToNotebook_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetContextMenuDataContext<NotebookViewModel>(sender);
        if (nb != null) AddNoteToNotebookViaDialog(nb);
    }

    private void AddNoteToNotebookViaDialog(NotebookViewModel nb)
    {
        var input = _dialogs.ShowInput("ノート追加", "ノート名を入力してください:");
        if (string.IsNullOrWhiteSpace(input)) return;
        var title = input.Trim();
        if (!ViewModel.AddNoteToNotebook(nb, title))
            ShowError($"ノート名「{title}」は既に使用されています。");
    }

    private void RenameNotebook_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetContextMenuDataContext<NotebookViewModel>(sender);
        if (nb == null) return;
        var title = _dialogs.ShowInput("名前変更", "新しいノートブック名:", nb.Title);
        if (!string.IsNullOrWhiteSpace(title))
            ViewModel.RenameNotebook(nb, title.Trim());
    }

    private void MoveNotebookUp_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetContextMenuDataContext<NotebookViewModel>(sender);
        if (nb != null) ViewModel.MoveNotebookUp(nb);
    }

    private void MoveNotebookDown_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetContextMenuDataContext<NotebookViewModel>(sender);
        if (nb != null) ViewModel.MoveNotebookDown(nb);
    }

    private void DeleteNotebook_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetContextMenuDataContext<NotebookViewModel>(sender);
        if (nb == null) return;
        if (Confirm($"ノートブック「{nb.Title}」を削除しますか？\n含まれるノートもすべて削除されます。この操作は取り消せません。",
                    "削除の確認"))
            ViewModel.DeleteNotebook(nb);
    }

    private void MoveNoteUp_Click(object sender, RoutedEventArgs e)
    {
        var note = GetContextMenuDataContext<NoteViewModel>(sender);
        if (note != null) ViewModel.MoveNoteUp(note);
    }

    private void MoveNoteDown_Click(object sender, RoutedEventArgs e)
    {
        var note = GetContextMenuDataContext<NoteViewModel>(sender);
        if (note != null) ViewModel.MoveNoteDown(note);
    }

    private void RenameNote_Click(object sender, RoutedEventArgs e)
        => RenameNoteWithDialog(GetContextMenuDataContext<NoteViewModel>(sender));

    private void DeleteNote_Click(object sender, RoutedEventArgs e)
        => DeleteNoteWithConfirm(GetContextMenuDataContext<NoteViewModel>(sender));

    private void RenameSelectedNote_Click(object sender, RoutedEventArgs e)
        => RenameNoteWithDialog(ViewModel.SelectedNote);

    private void DeleteSelectedNote_Click(object sender, RoutedEventArgs e)
        => DeleteNoteWithConfirm(ViewModel.SelectedNote);

    private void RenameNoteWithDialog(NoteViewModel? note)
    {
        if (note == null) return;
        var input = _dialogs.ShowInput("名前変更", "新しいノート名:", note.Title);
        if (string.IsNullOrWhiteSpace(input)) return;
        var newTitle = input.Trim();
        if (!ViewModel.RenameNote(note, newTitle))
            ShowError($"ノート名「{newTitle}」は既に使用されています。");
    }

    private void DeleteNoteWithConfirm(NoteViewModel? note)
    {
        if (note == null) return;
        var nbTitle = FindNotebookTitleOf(note);
        var location = nbTitle != null ? $"（{nbTitle}）" : "";
        if (Confirm($"ノート「{note.Title}」{location}を削除しますか？\nこの操作は取り消せません。", "削除の確認"))
            ViewModel.DeleteNote(note);
    }
}
