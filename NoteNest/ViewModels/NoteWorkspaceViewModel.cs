using System.Collections.ObjectModel;
using NoteNest.Models;

namespace NoteNest.ViewModels;

/// <summary>ノートブックとノートのコレクション、およびコレクション内で完結する操作を所有します。</summary>
public sealed class NoteWorkspaceViewModel
{
    public ObservableCollection<NotebookViewModel> Notebooks { get; } = new();
    public IEnumerable<NoteViewModel> AllNotes => Notebooks.SelectMany(notebook => notebook.Notes);

    public void Load(IEnumerable<Notebook> notebooks)
    {
        Notebooks.Clear();
        foreach (var notebook in notebooks)
            Notebooks.Add(new NotebookViewModel(notebook));
    }

    public List<Notebook> BuildModels() => Notebooks.Select(notebook => new Notebook
    {
        Id = notebook.Id,
        Title = notebook.Title,
        Notes = notebook.Notes.Select(note => new Note
        {
            Id = note.Id,
            Title = note.Title,
            Content = note.Content,
        }).ToList(),
    }).ToList();

    public NotebookViewModel AddNotebook(string title)
    {
        var notebook = new NotebookViewModel(new Notebook { Title = title });
        Notebooks.Add(notebook);
        return notebook;
    }

    public void RenameNotebook(NotebookViewModel notebook, string newTitle) => notebook.Title = newTitle;

    public IReadOnlyList<string> DeleteNotebook(NotebookViewModel notebook)
    {
        var deletedNoteIds = notebook.Notes.Select(note => note.Id).ToList();
        Notebooks.Remove(notebook);
        return deletedNoteIds;
    }

    public NoteViewModel? AddNote(NotebookViewModel notebook, string title)
    {
        if (NoteNameExists(title)) return null;
        var model = new Note { Title = title };
        var note = new NoteViewModel(model);
        notebook.Notes.Add(note);
        notebook.Model.Notes.Add(model);
        return note;
    }

    public bool RenameNote(NoteViewModel note, string newTitle)
    {
        if (NoteNameExists(newTitle, note)) return false;
        note.Title = newTitle;
        return true;
    }

    public void UpdateContent(NoteViewModel note, string content) => note.Content = content;

    public bool DeleteNote(NoteViewModel note)
    {
        var notebook = FindNotebookOf(note);
        if (notebook == null) return false;
        notebook.Notes.Remove(note);
        notebook.Model.Notes.Remove(note.Model);
        return true;
    }

    public bool MoveNoteUp(NoteViewModel note) => MoveNote(note, -1);
    public bool MoveNoteDown(NoteViewModel note) => MoveNote(note, 1);
    public bool MoveNotebookUp(NotebookViewModel notebook) => MoveNotebook(notebook, -1);
    public bool MoveNotebookDown(NotebookViewModel notebook) => MoveNotebook(notebook, 1);

    public bool MoveNoteToNotebook(NoteViewModel note, NotebookViewModel targetNotebook)
    {
        var sourceNotebook = FindNotebookOf(note);
        if (sourceNotebook == null || sourceNotebook == targetNotebook) return false;
        sourceNotebook.Notes.Remove(note);
        sourceNotebook.Model.Notes.Remove(note.Model);
        targetNotebook.Notes.Add(note);
        targetNotebook.Model.Notes.Add(note.Model);
        return true;
    }

    public NoteViewModel? FindNoteById(string? id) =>
        id == null ? null : AllNotes.FirstOrDefault(note => note.Id == id);

    public NoteViewModel? FindNoteByTitle(string title) =>
        AllNotes.FirstOrDefault(note => string.Equals(note.Title, title, StringComparison.OrdinalIgnoreCase));

    public bool NoteNameExists(string title, NoteViewModel? excludeSelf = null) =>
        AllNotes.Any(note => note != excludeSelf && string.Equals(note.Title, title, StringComparison.OrdinalIgnoreCase));

    public NotebookViewModel? FindNotebookOf(NoteViewModel note) =>
        Notebooks.FirstOrDefault(notebook => notebook.Notes.Contains(note));

    private bool MoveNote(NoteViewModel note, int offset)
    {
        var notebook = FindNotebookOf(note);
        if (notebook == null) return false;
        var index = notebook.Notes.IndexOf(note);
        var target = index + offset;
        if (index < 0 || target < 0 || target >= notebook.Notes.Count) return false;
        notebook.Notes.Move(index, target);
        return true;
    }

    private bool MoveNotebook(NotebookViewModel notebook, int offset)
    {
        var index = Notebooks.IndexOf(notebook);
        var target = index + offset;
        if (index < 0 || target < 0 || target >= Notebooks.Count) return false;
        Notebooks.Move(index, target);
        return true;
    }
}
