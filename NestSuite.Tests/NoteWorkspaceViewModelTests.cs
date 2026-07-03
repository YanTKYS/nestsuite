using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

public class NoteWorkspaceViewModelTests
{
    [Fact]
    public void AddNote_PreventsDuplicateNamesAndBuildsModels()
    {
        var workspace = new NoteWorkspaceViewModel();
        var notebook = workspace.AddNotebook("NB");
        var first = workspace.AddNote(notebook, "Note");

        Assert.Null(workspace.AddNote(notebook, "note"));
        Assert.Same(first, workspace.FindNoteByTitle("NOTE"));
        Assert.Equal("Note", Assert.Single(workspace.BuildModels()).Notes.Single().Title);
    }

    [Fact]
    public void DirectCollectionTitleAndContentChangesRaiseChanged()
    {
        var workspace = new NoteWorkspaceViewModel();
        var changeCount = 0;
        workspace.Changed += (_, _) => changeCount++;
        var notebook = new NotebookViewModel(new Notebook { Title = "NB" });
        workspace.Notebooks.Add(notebook);
        notebook.Title = "Renamed NB";
        var note = new NoteViewModel(new Note { Title = "Note" });
        notebook.Notes.Add(note);
        note.Title = "Renamed Note";
        note.Content = "[TODO] changed";

        Assert.Equal(5, changeCount);
    }

    [Fact]
    public void LoadDoesNotRaiseChanged()
    {
        var workspace = new NoteWorkspaceViewModel();
        var changed = false;
        workspace.Changed += (_, _) => changed = true;

        workspace.Load(new[] { new Notebook { Title = "Loaded" } });

        Assert.False(changed);
        Assert.Equal("Loaded", Assert.Single(workspace.Notebooks).Title);
    }

    // ── L11: ノート複製 ───────────────────────────────────────────────────────

    [Fact]
    public void DuplicateNote_AddsCopyWithSuffixInSameNotebook()
    {
        var workspace = new NoteWorkspaceViewModel();
        var notebook = workspace.AddNotebook("NB");
        var original = workspace.AddNote(notebook, "会議メモ")!;

        var copy = workspace.DuplicateNote(original);

        Assert.NotNull(copy);
        Assert.Equal(2, notebook.Notes.Count);
        Assert.Equal("会議メモ のコピー", copy.Title);
    }

    [Fact]
    public void DuplicateNote_CopiesContent()
    {
        var workspace = new NoteWorkspaceViewModel();
        var notebook = workspace.AddNotebook("NB");
        var original = workspace.AddNote(notebook, "ノート")!;
        original.Content = "本文テキスト";

        var copy = workspace.DuplicateNote(original)!;

        Assert.Equal("本文テキスト", copy.Content);
    }

    [Fact]
    public void DuplicateNote_HasDifferentId()
    {
        var workspace = new NoteWorkspaceViewModel();
        var notebook = workspace.AddNotebook("NB");
        var original = workspace.AddNote(notebook, "ノート")!;

        var copy = workspace.DuplicateNote(original)!;

        Assert.NotEqual(original.Id, copy.Id);
    }

    [Fact]
    public void DuplicateNote_HasNewTimestamps()
    {
        var workspace = new NoteWorkspaceViewModel();
        var notebook = workspace.AddNotebook("NB");
        var original = workspace.AddNote(notebook, "ノート")!;
        var before = DateTime.Now;

        var copy = workspace.DuplicateNote(original)!;

        Assert.True(copy.CreatedAt >= before);
        Assert.True(copy.UpdatedAt >= before);
    }

    [Fact]
    public void DuplicateNote_DoesNotModifyOriginal()
    {
        var workspace = new NoteWorkspaceViewModel();
        var notebook = workspace.AddNotebook("NB");
        var original = workspace.AddNote(notebook, "ノート")!;
        original.Content = "元の本文";
        var originalId      = original.Id;
        var originalTitle   = original.Title;
        var originalContent = original.Content;

        workspace.DuplicateNote(original);

        Assert.Equal(originalId,      original.Id);
        Assert.Equal(originalTitle,   original.Title);
        Assert.Equal(originalContent, original.Content);
    }

    [Fact]
    public void DuplicateNote_NumberedSuffixWhenCopyAlreadyExists()
    {
        var workspace = new NoteWorkspaceViewModel();
        var notebook = workspace.AddNotebook("NB");
        var original = workspace.AddNote(notebook, "ノート")!;
        workspace.DuplicateNote(original); // "ノート のコピー"

        var second = workspace.DuplicateNote(original)!;

        Assert.Equal("ノート のコピー 2", second.Title);
    }

    [Fact]
    public void DuplicateNote_IncrementsNumberUntilUnique()
    {
        var workspace = new NoteWorkspaceViewModel();
        var notebook = workspace.AddNotebook("NB");
        var original = workspace.AddNote(notebook, "ノート")!;
        workspace.DuplicateNote(original); // "ノート のコピー"
        workspace.DuplicateNote(original); // "ノート のコピー 2"

        var third = workspace.DuplicateNote(original)!;

        Assert.Equal("ノート のコピー 3", third.Title);
        Assert.Equal(4, notebook.Notes.Count); // original + 3 copies
    }

    // ── M12: ノートのスター（お気に入り） ────────────────────────────────────

    [Fact]
    public void TogglingIsStarred_RaisesChanged()
    {
        var workspace = new NoteWorkspaceViewModel();
        var notebook = workspace.AddNotebook("NB");
        var note = workspace.AddNote(notebook, "Note")!;
        var changed = false;
        workspace.Changed += (_, _) => changed = true;

        note.IsStarred = true;

        Assert.True(changed);
    }

    [Fact]
    public void ToggleStar_FlipsFalseToTrueToFalse()
    {
        var workspace = new NoteWorkspaceViewModel();
        var notebook = workspace.AddNotebook("NB");
        var note = workspace.AddNote(notebook, "Note")!;
        Assert.False(note.IsStarred);

        workspace.ToggleStar(note);
        Assert.True(note.IsStarred);

        workspace.ToggleStar(note);
        Assert.False(note.IsStarred);
    }

    [Fact]
    public void BuildModels_PreservesIsStarred()
    {
        var workspace = new NoteWorkspaceViewModel();
        var notebook = workspace.AddNotebook("NB");
        var note = workspace.AddNote(notebook, "Note")!;
        note.IsStarred = true;

        var models = workspace.BuildModels();

        Assert.True(models.Single().Notes.Single().IsStarred);
    }

    [Fact]
    public void DuplicateNote_CopiesIsStarred()
    {
        var workspace = new NoteWorkspaceViewModel();
        var notebook = workspace.AddNotebook("NB");
        var original = workspace.AddNote(notebook, "ノート")!;
        original.IsStarred = true;

        var copy = workspace.DuplicateNote(original)!;

        Assert.True(copy.IsStarred);
    }

    [Fact]
    public void IsStarred_DoesNotChangeUpdatedAt()
    {
        var workspace = new NoteWorkspaceViewModel();
        var notebook = workspace.AddNotebook("NB");
        var note = workspace.AddNote(notebook, "Note")!;
        var updatedAt = note.UpdatedAt;

        note.IsStarred = true;

        Assert.Equal(updatedAt, note.UpdatedAt);
    }
}
