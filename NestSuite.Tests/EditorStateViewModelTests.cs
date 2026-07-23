using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

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

    // v2.14.16 BUG: FontFamily は NestSuite UI 設定駆動の表示専用値。
    // SavedFontFamily（Workspace ファイルへ書き戻す値）とは独立して扱われることを固定する。

    [Fact]
    public void LoadSettings_SetsBothFontFamilyAndSavedFontFamily()
    {
        var editor = new EditorStateViewModel();

        editor.LoadSettings("MS Gothic", 16);

        Assert.Equal("MS Gothic", editor.FontFamily);
        Assert.Equal("MS Gothic", editor.SavedFontFamily);
    }

    [Fact]
    public void DirectFontFamilyChange_DoesNotAffectSavedFontFamily()
    {
        var editor = new EditorStateViewModel();
        editor.LoadSettings("MS Gothic", 16);

        // UI 設定（NoteNestEditorFontFamily）駆動の表示変更を模擬する。
        editor.FontFamily = "Consolas";

        Assert.Equal("Consolas", editor.FontFamily);
        Assert.Equal("MS Gothic", editor.SavedFontFamily);
    }

    [Fact]
    public void DirectFontFamilyChange_DoesNotRaiseSettingsChanged()
    {
        var editor = new EditorStateViewModel();
        var changed = false;
        editor.SettingsChanged += (_, _) => changed = true;

        editor.FontFamily = "Consolas";

        Assert.False(changed);
    }

    // ── v2.19.3 L4: NoteNest 本文エディタの折り返し表示（表示専用の UI 設定） ──

    [Fact]
    public void WordWrap_DefaultsToTrue()
    {
        var editor = new EditorStateViewModel();

        Assert.True(editor.WordWrap);
    }

    [Fact]
    public void WordWrap_CanBeSetToFalse_AndDoesNotRaiseSettingsChanged()
    {
        var editor = new EditorStateViewModel();
        var changed = false;
        editor.SettingsChanged += (_, _) => changed = true;

        editor.WordWrap = false;

        Assert.False(editor.WordWrap);
        Assert.False(changed);
    }

    [Fact]
    public void WordWrap_DoesNotAffectFontOrSavedFontFamily()
    {
        var editor = new EditorStateViewModel();
        editor.LoadSettings("MS Gothic", 16);

        editor.WordWrap = false;

        Assert.Equal("MS Gothic", editor.FontFamily);
        Assert.Equal("MS Gothic", editor.SavedFontFamily);
        Assert.Equal(16, editor.FontSize);
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
