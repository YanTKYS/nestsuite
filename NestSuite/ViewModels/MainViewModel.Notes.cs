using NestSuite.Services;

namespace NestSuite.ViewModels;

public partial class MainViewModel
{
    public void SelectNote(NoteViewModel note)
    {
        _editor.SelectNote(note);
        // M14: 選択切替は「入力中」ではない明示的な区切りのため、更新日順の表示へ
        // 直近の編集結果を反映する自然なタイミングとして表示順を再計算する。
        _notes.RefreshDisplayOrder();
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

    /// <summary>
    /// TN-3: TempNest スロット本文を新規ノートとして追加する。
    /// タイトルは <see cref="PromotedNoteTitleGenerator"/> で本文から生成し、既存ノートと重複する場合は
    /// 連番を付与して一意化する。本文はそのまま設定し、タグ・マーカー・スター等は追加しない。
    /// 対象ノートブックが存在しない場合（空プロジェクト）は新規ノートブックを作成する。
    /// </summary>
    public NoteViewModel? CreateNoteFromTransfer(string content)
    {
        var notebook = _notes.Notebooks.FirstOrDefault() ?? _notes.AddNotebook("ノート");
        var title = MakeUniqueNoteTitle(PromotedNoteTitleGenerator.Generate(content));

        var note = _notes.AddNote(notebook, title);
        if (note == null) return null;

        note.Content = content;
        SelectNote(note);
        StatusMessage = $"ノート「{note.Title}」を追加しました。";
        return note;
    }

    private string MakeUniqueNoteTitle(string baseTitle)
    {
        if (!NoteNameExists(baseTitle)) return baseTitle;
        for (var n = 2; ; n++)
        {
            var candidate = $"{baseTitle} ({n})";
            if (!NoteNameExists(candidate)) return candidate;
        }
    }

    public NoteViewModel? DuplicateNote(NoteViewModel source)
    {
        var copy = _notes.DuplicateNote(source);
        if (copy == null) return null;
        SelectNote(copy);
        SyncTreeSelectionCallback?.Invoke(copy);
        StatusMessage = $"ノート「{copy.Title}」を作成しました。";
        return copy;
    }

    /// <summary>v2.14.3 M12: ノートのスター（お気に入り）状態を反転する。</summary>
    public void ToggleNoteStar(NoteViewModel note) => _notes.ToggleStar(note);

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
