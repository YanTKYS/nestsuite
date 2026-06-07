namespace NoteNest.ViewModels;

public partial class MainViewModel
{
    public void SelectNote(NoteViewModel note)
    {
        _editorMode = EditorMode.NoteEdit;
        _editingTask = null;
        _isLoadingNote = true;
        SelectedNote = note;
        _editorContent = note.Content;
        OnPropertyChanged(nameof(EditorContent));
        OnPropertyChanged(nameof(CurrentNoteTitle));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(IsTaskCommentMode));
        OnPropertyChanged(nameof(IsNoteEditMode));
        _isLoadingNote = false;
        RefreshMarkers();
    }

    public void AddNotebookWithTitle(string title)
    {
        _notes.AddNotebook(title);
        IsModified = true;
        StatusMessage = $"ノートブック「{title}」を追加しました。";
    }

    public void RenameNotebook(NotebookViewModel notebook, string newTitle)
    {
        _notes.RenameNotebook(notebook, newTitle);
        IsModified = true;
    }

    public void DeleteNotebook(NotebookViewModel notebook)
    {
        if (SelectedNote != null && notebook.Notes.Contains(SelectedNote)) ClearEditor();
        var deletedNoteIds = _notes.DeleteNotebook(notebook);
        ClearTaskLinksToNoteIds(deletedNoteIds);
        IsModified = true;
        RefreshMarkers();
    }

    public bool AddNoteToNotebook(NotebookViewModel notebook, string title)
    {
        var note = _notes.AddNote(notebook, title);
        if (note == null) return false;
        IsModified = true;
        OnPropertyChanged(nameof(RelatedNoteChoices));
        SelectNote(note);
        StatusMessage = $"ノート「{title}」を追加しました。";
        return true;
    }

    public bool RenameNote(NoteViewModel note, string newTitle)
    {
        if (!_notes.RenameNote(note, newTitle)) return false;
        if (SelectedNote == note)
        {
            OnPropertyChanged(nameof(CurrentNoteTitle));
            OnPropertyChanged(nameof(EditorTitle));
            RefreshMarkers();
        }
        IsModified = true;
        return true;
    }

    public void DeleteNote(NoteViewModel note)
    {
        if (!_notes.DeleteNote(note)) return;
        ClearTaskLinksToNoteIds(new[] { note.Id });
        if (SelectedNote == note) ClearEditor();
        IsModified = true;
        OnPropertyChanged(nameof(RelatedNoteChoices));
        RefreshMarkers();
    }

    public void MoveNoteUp(NoteViewModel note) { if (_notes.MoveNoteUp(note)) IsModified = true; }
    public void MoveNoteDown(NoteViewModel note) { if (_notes.MoveNoteDown(note)) IsModified = true; }
    public void MoveNotebookUp(NotebookViewModel notebook) { if (_notes.MoveNotebookUp(notebook)) IsModified = true; }
    public void MoveNotebookDown(NotebookViewModel notebook) { if (_notes.MoveNotebookDown(notebook)) IsModified = true; }

    public void MoveNoteToNotebook(NoteViewModel note, NotebookViewModel targetNotebook)
    {
        if (!_notes.MoveNoteToNotebook(note, targetNotebook)) return;
        IsModified = true;
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
        if (_editingTaskRelatedNote != null && ids.Contains(_editingTaskRelatedNote.Id))
        {
            _editingTaskRelatedNote = null;
            OnPropertyChanged(nameof(EditingTaskRelatedNote));
            OnPropertyChanged(nameof(HasEditingTaskRelatedNote));
        }
    }

    private void AddNotebook()
    {
        var title = ShowInputDialog?.Invoke("ノートブック追加", "ノートブック名を入力してください:");
        if (!string.IsNullOrWhiteSpace(title)) AddNotebookWithTitle(title.Trim());
    }
}
