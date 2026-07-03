using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using NestSuite.Models;

namespace NestSuite.ViewModels;

/// <summary>ノートブックとノートのコレクション、およびコレクション内で完結する操作を所有します。</summary>
public sealed class NoteWorkspaceViewModel
{
    private readonly HashSet<NotebookViewModel> _trackedNotebooks = new();
    private readonly HashSet<NoteViewModel> _trackedNotes = new();
    private bool _suppressChanged;

    public NoteWorkspaceViewModel() => Notebooks.CollectionChanged += CollectionChanged;

    public event EventHandler? Changed;
    public ObservableCollection<NotebookViewModel> Notebooks { get; } = new();
    public IEnumerable<NoteViewModel> AllNotes => Notebooks.SelectMany(notebook => notebook.Notes);

    public void Load(IEnumerable<Notebook> notebooks)
    {
        _suppressChanged = true;
        try
        {
            Notebooks.Clear();
            foreach (var notebook in notebooks)
                Notebooks.Add(new NotebookViewModel(notebook));
        }
        finally
        {
            _suppressChanged = false;
        }
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
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt,
            IsStarred = note.IsStarred,
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

    /// <summary>v2.14.3 M12: ノートのスター状態を反転する。</summary>
    public void ToggleStar(NoteViewModel note) => note.IsStarred = !note.IsStarred;

    public bool DeleteNote(NoteViewModel note)
    {
        var notebook = FindNotebookOf(note);
        if (notebook == null) return false;
        notebook.Notes.Remove(note);
        notebook.Model.Notes.Remove(note.Model);
        return true;
    }

    public NoteViewModel? DuplicateNote(NoteViewModel source)
    {
        var notebook = FindNotebookOf(source);
        if (notebook == null) return null;
        var newTitle = GenerateCopyTitle(source.Title);
        var model = new Note
        {
            Title   = newTitle,
            Content = source.Content,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsStarred = source.IsStarred,
        };
        var copy = new NoteViewModel(model);
        notebook.Notes.Add(copy);
        notebook.Model.Notes.Add(model);
        return copy;
    }

    private string GenerateCopyTitle(string originalTitle)
    {
        var candidate = $"{originalTitle} のコピー";
        if (!NoteNameExists(candidate)) return candidate;
        for (var n = 2; ; n++)
        {
            var numbered = $"{originalTitle} のコピー {n}";
            if (!NoteNameExists(numbered)) return numbered;
        }
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

    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SynchronizeTracking();
        NotifyChanged();
    }

    private void SynchronizeTracking()
    {
        var notebooks = new HashSet<NotebookViewModel>(Notebooks);
        foreach (var notebook in _trackedNotebooks.Except(notebooks).ToList())
        {
            notebook.PropertyChanged -= NotebookPropertyChanged;
            notebook.Notes.CollectionChanged -= CollectionChanged;
            _trackedNotebooks.Remove(notebook);
        }
        foreach (var notebook in notebooks.Except(_trackedNotebooks))
        {
            notebook.PropertyChanged += NotebookPropertyChanged;
            notebook.Notes.CollectionChanged += CollectionChanged;
            _trackedNotebooks.Add(notebook);
        }

        var notes = new HashSet<NoteViewModel>(AllNotes);
        foreach (var note in _trackedNotes.Except(notes).ToList())
        {
            note.PropertyChanged -= NotePropertyChanged;
            _trackedNotes.Remove(note);
        }
        foreach (var note in notes.Except(_trackedNotes))
        {
            note.PropertyChanged += NotePropertyChanged;
            _trackedNotes.Add(note);
        }
    }

    private void NotebookPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NotebookViewModel.Title))
            NotifyChanged();
    }

    private void NotePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(NoteViewModel.Title)
            or nameof(NoteViewModel.Content)
            or nameof(NoteViewModel.IsStarred))
            NotifyChanged();
    }

    private void NotifyChanged()
    {
        if (!_suppressChanged) Changed?.Invoke(this, EventArgs.Empty);
    }

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
