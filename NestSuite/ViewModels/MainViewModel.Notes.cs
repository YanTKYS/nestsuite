namespace NoteNest.ViewModels;

public partial class MainViewModel
{
    public void SelectNote(NoteViewModel note)
    {
        _editor.SelectNote(note);
    }

    public void AddNotebookWithTitle(string title)
    {
        _notes.AddNotebook(title);
        StatusMessage = $"ノートブック「{title}」を追加しました。";
    }

    public void RenameNotebook(NotebookViewModel notebook, string newTitle)
    {
        _notes.RenameNotebook(notebook, newTitle);
    }

    public void DeleteNotebook(NotebookViewModel notebook)
    {
        if (SelectedNote != null && notebook.Notes.Contains(SelectedNote)) ClearEditor();
        var deletedNoteIds = _notes.DeleteNotebook(notebook);
        ClearTaskLinksToNoteIds(deletedNoteIds);
    }

    public bool AddNoteToNotebook(NotebookViewModel notebook, string title)
    {
        var note = _notes.AddNote(notebook, title);
        if (note == null) return false;
        SelectNote(note);
        StatusMessage = $"ノート「{title}」を追加しました。";
        return true;
    }

    public bool RenameNote(NoteViewModel note, string newTitle)
    {
        if (!_notes.RenameNote(note, newTitle)) return false;
        return true;
    }

    public void DeleteNote(NoteViewModel note)
    {
        if (!_notes.DeleteNote(note)) return;
        ClearTaskLinksToNoteIds(new[] { note.Id });
        if (SelectedNote == note) ClearEditor();
    }

    public void MoveNoteUp(NoteViewModel note) => _notes.MoveNoteUp(note);
    public void MoveNoteDown(NoteViewModel note) => _notes.MoveNoteDown(note);
    public void MoveNotebookUp(NotebookViewModel notebook) => _notes.MoveNotebookUp(notebook);
    public void MoveNotebookDown(NotebookViewModel notebook) => _notes.MoveNotebookDown(notebook);

    public void MoveNoteToNotebook(NoteViewModel note, NotebookViewModel targetNotebook)
    {
        if (!_notes.MoveNoteToNotebook(note, targetNotebook)) return;
        StatusMessage = $"ノート「{note.Title}」を「{targetNotebook.Title}」に移動しました。";
    }

    public NoteViewModel? FindNoteById(string? id) => _notes.FindNoteById(id);
    public NoteViewModel? FindNoteByTitle(string title) => _notes.FindNoteByTitle(title);
    public bool NoteNameExists(string title, NoteViewModel? excludeSelf = null) => _notes.NoteNameExists(title, excludeSelf);
    public NotebookViewModel? FindNotebookOf(NoteViewModel note) => _notes.FindNotebookOf(note);

    public void NavigateToNote(NoteViewModel note)
    {
        SelectNote(note);
        SyncTreeSelectionCallback?.Invoke(note);
    }

    private void ClearTaskLinksToNoteIds(IEnumerable<string> deletedNoteIds)
    {
        var ids = deletedNoteIds.ToList();
        _tasks.ClearLinksToNoteIds(ids);
        if (_editor.EditingTaskRelatedNote != null && ids.Contains(_editor.EditingTaskRelatedNote.Id))
            _editor.EditingTaskRelatedNote = null;
    }

    private void AddNotebook()
    {
        var title = ShowInputDialog?.Invoke("ノートブック追加", "ノートブック名を入力してください:");
        if (!string.IsNullOrWhiteSpace(title)) AddNotebookWithTitle(title.Trim());
    }
}
