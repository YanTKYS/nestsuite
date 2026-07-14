using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

// v2.8.6: TD-14 — MainViewModel partial regression tests.
// Covers delegation behaviours in Notes/Tasks/Editor partials that are not
// already tested by the existing composition, facade, or sub-ViewModel tests.
public class MainViewModelPartialTests
{
    // ── Notes.cs ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddNotebookWithTitle_SetsStatusMessage()
    {
        var main = new MainViewModel();
        main.AddNotebookWithTitle("テストノートブック");
        Assert.Contains("テストノートブック", main.StatusMessage);
    }

    [Fact]
    public void AddNoteToNotebook_ReturnsTrue_SelectsNote_SetsStatusMessage()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");

        var result = main.AddNoteToNotebook(nb, "New Note");

        Assert.True(result);
        Assert.Equal("New Note", main.SelectedNote?.Title);
        Assert.Contains("New Note", main.StatusMessage);
    }

    [Fact]
    public void AddNoteToNotebook_DuplicateTitle_ReturnsFalse()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        main.Notes.AddNote(nb, "Existing");

        Assert.False(main.AddNoteToNotebook(nb, "Existing"));
    }

    // ── TN-3 (v2.18.0): CreateNoteFromTransfer ──────────────────────────

    [Fact]
    public void CreateNoteFromTransfer_AddsNoteToFirstNotebook_WithBodyAsIs()
    {
        var main = new MainViewModel();
        var firstNotebook = main.Notebooks.First();
        var countBefore = firstNotebook.Notes.Count;

        var note = main.CreateNoteFromTransfer("最初の行\n本文2行目");

        Assert.NotNull(note);
        Assert.Equal("最初の行\n本文2行目", note!.Content);
        Assert.Equal(countBefore + 1, firstNotebook.Notes.Count);
        Assert.Contains(note, firstNotebook.Notes);
    }

    [Fact]
    public void CreateNoteFromTransfer_GeneratesTitleFromFirstNonEmptyLine()
    {
        var main = new MainViewModel();
        var note = main.CreateNoteFromTransfer("タイトル候補\n本文");
        Assert.Equal("タイトル候補", note!.Title);
    }

    [Fact]
    public void CreateNoteFromTransfer_BlankFirstLine_UsesFallbackTitle()
    {
        var main = new MainViewModel();
        var note = main.CreateNoteFromTransfer("\n\n本文のみ");
        Assert.Equal("TempNestから昇格", note!.Title);
    }

    [Fact]
    public void CreateNoteFromTransfer_LongTitleLine_TruncatesWithEllipsis()
    {
        var main = new MainViewModel();
        var longLine = new string('あ', 60);
        var note = main.CreateNoteFromTransfer(longLine);
        Assert.Equal(new string('あ', 40) + "…", note!.Title);
    }

    [Fact]
    public void CreateNoteFromTransfer_SelectsCreatedNote()
    {
        var main = new MainViewModel();
        var note = main.CreateNoteFromTransfer("選択確認\n本文");
        Assert.Same(note, main.SelectedNote);
    }

    [Fact]
    public void CreateNoteFromTransfer_SetsStatusMessage()
    {
        var main = new MainViewModel();
        var note = main.CreateNoteFromTransfer("ステータス確認\n本文");
        Assert.Contains(note!.Title, main.StatusMessage);
    }

    [Fact]
    public void CreateNoteFromTransfer_DuplicateTitle_AppendsNumberSuffix()
    {
        var main = new MainViewModel();
        var nb = main.Notebooks.First();
        main.Notes.AddNote(nb, "重複タイトル");

        var note = main.CreateNoteFromTransfer("重複タイトル\n本文");

        Assert.Equal("重複タイトル (2)", note!.Title);
    }

    [Fact]
    public void CreateNoteFromTransfer_DoesNotAddMarkersOrStar()
    {
        var main = new MainViewModel();
        var note = main.CreateNoteFromTransfer("普通の断片\n本文のみ");
        Assert.False(note!.IsStarred);
        Assert.False(note.HasMarkers);
    }

    [Fact]
    public void CreateNoteFromTransfer_CreatesExactlyOneNote()
    {
        var main = new MainViewModel();
        var countBefore = main.AllNotes.Count();

        main.CreateNoteFromTransfer("1件のみ\n本文");

        Assert.Equal(countBefore + 1, main.AllNotes.Count());
    }

    [Fact]
    public void RenameNote_ValidName_ReturnsTrueAndUpdatesTitle()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "Old Name")!;

        Assert.True(main.RenameNote(note, "New Name"));
        Assert.Equal("New Name", note.Title);
    }

    [Fact]
    public void RenameNote_DuplicateName_ReturnsFalseAndTitleUnchanged()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        main.Notes.AddNote(nb, "Note A");
        var noteB = main.Notes.AddNote(nb, "Note B")!;

        Assert.False(main.RenameNote(noteB, "Note A"));
        Assert.Equal("Note B", noteB.Title);
    }

    [Fact]
    public void DeleteNote_WhenSelectedNote_ClearsEditorSelection()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "Note")!;
        main.SelectNote(note);
        Assert.Equal(note, main.SelectedNote);

        main.DeleteNote(note);

        Assert.Null(main.SelectedNote);
    }

    [Fact]
    public void DeleteNote_WhenDifferentNoteSelected_DoesNotClearEditor()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        var noteA = main.Notes.AddNote(nb, "Note A")!;
        var noteB = main.Notes.AddNote(nb, "Note B")!;
        main.SelectNote(noteA);

        main.DeleteNote(noteB);

        Assert.Equal(noteA, main.SelectedNote);
    }

    [Fact]
    public void DeleteNote_ClearsLinkedNoteIdFromTasks()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "Note")!;
        var task = main.Tasks.AddTask("today", "Task")!;
        task.LinkedNoteId = note.Id;

        main.DeleteNote(note);

        Assert.Null(task.LinkedNoteId);
    }

    [Fact]
    public void DeleteNotebook_RemovesAllNotesAndClearsTaskLinks()
    {
        var main = new MainViewModel();
        int baseline = main.Notes.AllNotes.Count();
        var nb = main.Notes.AddNotebook("NB");
        var noteA = main.Notes.AddNote(nb, "A")!;
        main.Notes.AddNote(nb, "B");
        var task = main.Tasks.AddTask("today", "Task")!;
        task.LinkedNoteId = noteA.Id;

        main.DeleteNotebook(nb);

        Assert.Equal(baseline, main.Notes.AllNotes.Count());
        Assert.Null(task.LinkedNoteId);
    }

    [Fact]
    public void DeleteNotebook_WhenSelectedNoteIsInside_ClearsEditorSelection()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "Note")!;
        main.SelectNote(note);

        main.DeleteNotebook(nb);

        Assert.Null(main.SelectedNote);
    }

    [Fact]
    public void DuplicateNote_SelectsNewNote_SetsStatusMessage()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "Original")!;

        var copy = main.DuplicateNote(note);

        Assert.NotNull(copy);
        Assert.Equal(copy, main.SelectedNote);
        Assert.Contains(copy!.Title, main.StatusMessage);
    }

    [Fact]
    public void FindNoteById_ReturnsMatchingNote()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "Target")!;

        Assert.Same(note, main.FindNoteById(note.Id));
    }

    [Fact]
    public void FindNoteById_UnknownId_ReturnsNull()
    {
        var main = new MainViewModel();
        Assert.Null(main.FindNoteById("no-such-id"));
    }

    [Fact]
    public void FindNoteByTitle_CaseInsensitive_ReturnsNote()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "Meeting Notes")!;

        Assert.Same(note, main.FindNoteByTitle("meeting notes"));
    }

    [Fact]
    public void FindNoteByTitle_NotExists_ReturnsNull()
    {
        var main = new MainViewModel();
        Assert.Null(main.FindNoteByTitle("Nonexistent"));
    }

    [Fact]
    public void NoteNameExists_ReturnsTrueWhenExists()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        main.Notes.AddNote(nb, "Existing");
        Assert.True(main.NoteNameExists("Existing"));
    }

    [Fact]
    public void NoteNameExists_ReturnsFalseWhenNotExists()
    {
        var main = new MainViewModel();
        Assert.False(main.NoteNameExists("Nonexistent"));
    }

    [Fact]
    public void NoteNameExists_ExcludesSelf_AllowsSameName()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "Note")!;
        Assert.False(main.NoteNameExists("Note", excludeSelf: note));
    }

    [Fact]
    public void NavigateToNote_SelectsNoteAndInvokesSyncCallback()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "Target")!;
        NoteViewModel? synced = null;
        main.SyncTreeSelectionCallback = n => synced = n;

        main.NavigateToNote(note);

        Assert.Equal(note, main.SelectedNote);
        Assert.Equal(note, synced);
    }

    [Fact]
    public void MoveNoteToNotebook_SetsStatusMessageWithDestinationTitle()
    {
        var main = new MainViewModel();
        var nb1 = main.Notes.AddNotebook("Source");
        var nb2 = main.Notes.AddNotebook("Destination");
        var note = main.Notes.AddNote(nb1, "Note")!;

        main.MoveNoteToNotebook(note, nb2);

        Assert.Contains("Destination", main.StatusMessage);
    }

    // ── Tasks.cs ──────────────────────────────────────────────────────────────

    [Fact]
    public void RenameTask_UpdatesTitle()
    {
        var main = new MainViewModel();
        var task = main.Tasks.AddTask("today", "Old Title")!;

        main.RenameTask(task, "New Title");

        Assert.Equal("New Title", task.Title);
    }

    [Fact]
    public void SetTaskRelatedNote_WhenEditingTask_UpdatesLinkedNoteIdViaEditor()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "Note")!;
        var task = main.Tasks.AddTask("today", "Task")!;
        main.SelectTask(task);

        main.SetTaskRelatedNote(task, note);

        Assert.Equal(note.Id, task.LinkedNoteId);
        Assert.Contains(note.Title, main.StatusMessage);
    }

    [Fact]
    public void SetTaskRelatedNote_WhenNotEditingTask_SetsLinkedNoteIdDirectly()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "Note")!;
        var task = main.Tasks.AddTask("today", "Task")!;

        main.SetTaskRelatedNote(task, note);

        Assert.Equal(note.Id, task.LinkedNoteId);
    }

    [Fact]
    public void ClearTaskRelatedNote_WhenEditingTask_ClearsLinkedNoteId()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "Note")!;
        var task = main.Tasks.AddTask("today", "Task")!;
        task.LinkedNoteId = note.Id;
        main.SelectTask(task);

        main.ClearTaskRelatedNote(task);

        Assert.Null(task.LinkedNoteId);
    }

    [Fact]
    public void ClearTaskRelatedNote_WhenNotEditingTask_ClearsLinkedNoteId()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "Note")!;
        var task = main.Tasks.AddTask("today", "Task")!;
        task.LinkedNoteId = note.Id;

        main.ClearTaskRelatedNote(task);

        Assert.Null(task.LinkedNoteId);
    }

    // ── Editor.cs ─────────────────────────────────────────────────────────────

    [Fact]
    public void ApplyFontSettings_UpdatesEditorFontFamilyAndSize()
    {
        var main = new MainViewModel();

        main.ApplyFontSettings("Meiryo UI", 18);

        Assert.Equal("Meiryo UI", main.Editor.FontFamily);
        Assert.Equal(18, main.Editor.FontSize);
    }

    [Fact]
    public void ApplyFontSettings_ReflectedInEditorFontSizeFacadeProperty()
    {
        var main = new MainViewModel();

        main.ApplyFontSettings("Arial", 16);

        Assert.Equal(16, main.EditorFontSize);
    }

    // ── Markers (multi-note aggregation via MainViewModel) ────────────────────

    [Fact]
    public void MarkerPanel_AggregatesMarkersAcrossMultipleNotes()
    {
        var main = new MainViewModel();
        int baseline = main.MarkerCount;
        var nb = main.Notes.AddNotebook("NB");
        var note1 = main.Notes.AddNote(nb, "N1")!;
        var note2 = main.Notes.AddNote(nb, "N2")!;

        note1.Content = "[TODO] task one";
        note2.Content = "[FIXME] bug one\n[NOTE] info one";

        Assert.Equal(baseline + 3, main.MarkerCount);
    }

    [Fact]
    public void MarkerPanel_EmptyNoteContent_ProducesNoMarkers()
    {
        var main = new MainViewModel();
        int baseline = main.MarkerCount;
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "Empty")!;
        note.Content = "";
        Assert.Equal(baseline, main.MarkerCount);
    }

    [Fact]
    public void MarkerPanel_DeleteNote_RemovesItsMarkers()
    {
        var main = new MainViewModel();
        int baseline = main.MarkerCount;
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "N")!;
        note.Content = "[TODO] task\n[FIXME] bug";
        Assert.Equal(baseline + 2, main.MarkerCount);

        main.DeleteNote(note);

        Assert.Equal(baseline, main.MarkerCount);
    }

    // ── Links (NoteLinkPanel refresh via MainViewModel) ───────────────────────

    [Fact]
    public void LinkPanel_RefreshesOnNoteSelection_ShowsOutboundLinks()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        var noteA = main.Notes.AddNote(nb, "A")!;
        var noteB = main.Notes.AddNote(nb, "B")!;
        noteA.Content = "See [[B]] here";

        main.SelectNote(noteA);

        Assert.True(main.LinkPanel.HasNote);
        Assert.Single(main.LinkPanel.OutboundLinks);
        Assert.Equal("B", main.LinkPanel.OutboundLinks[0].LinkName);
    }

    [Fact]
    public void LinkPanel_UnresolvableLink_IsMarkedBroken()
    {
        var main = new MainViewModel();
        var nb = main.Notes.AddNotebook("NB");
        var note = main.Notes.AddNote(nb, "A")!;
        note.Content = "[[NonexistentNote]]";

        main.SelectNote(note);

        Assert.True(main.LinkPanel.OutboundLinks[0].IsBroken);
    }
}
