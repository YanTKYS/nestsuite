using NoteNest.Models;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

public class EditorStateViewModelTests
{
    [Fact]
    public void SelectNoteOwnsNoteEditingStateWithoutRaisingContentEdited()
    {
        var editor = new EditorStateViewModel();
        var edited = false;
        editor.ContentEdited += (_, _) => edited = true;
        var note = new NoteViewModel(new Note { Title = "Note", Content = "body" });

        editor.SelectNote(note);

        Assert.Same(note, editor.SelectedNote);
        Assert.Equal("body", editor.Content);
        Assert.True(editor.IsNoteEditMode);
        Assert.False(edited);
    }

    [Fact]
    public void LoadSettingsDoesNotRaiseSettingsChanged()
    {
        var editor = new EditorStateViewModel();
        var changed = false;
        editor.SettingsChanged += (_, _) => changed = true;

        editor.LoadSettings("Meiryo UI", 18);

        Assert.False(changed);
        Assert.Equal("Meiryo UI", editor.FontFamily);
        Assert.Equal(18, editor.FontSize);
    }

    [Fact]
    public void DirectRelatedNoteChangeRaisesEventButSelectionDoesNot()
    {
        var editor = new EditorStateViewModel();
        var task = new TaskViewModel(new NoteTask { Title = "Task" });
        var note = new NoteViewModel(new Note { Title = "Note" });
        var changeCount = 0;
        editor.RelatedNoteChanged += (_, _) => changeCount++;

        editor.SelectTask(task, null);
        editor.EditingTaskRelatedNote = note;
        editor.EditingTaskRelatedNote = null;

        Assert.Equal(2, changeCount);
    }

    [Fact]
    public void SelectTaskAndEditRaiseContentEdited()
    {
        var editor = new EditorStateViewModel();
        var task = new TaskViewModel(new NoteTask { Title = "Task", Comment = "before" });
        var editCount = 0;
        editor.ContentEdited += (_, _) => editCount++;

        editor.SelectTask(task, null);
        editor.Content = "after";

        Assert.True(editor.IsTaskCommentMode);
        Assert.Equal("タスクコメント：Task", editor.EditorTitle);
        Assert.Equal(1, editCount);
    }
}
