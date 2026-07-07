using System.ComponentModel;
using NestSuite.ViewModels;

namespace NestSuite.Services;

/// <summary>エディタ操作を永続化対象の所有者へ伝播し、表示用プロパティ名を変換します。</summary>
public sealed class EditorChangeCoordinator
{
    private readonly NoteWorkspaceViewModel _notes;
    private readonly TaskBoardViewModel _tasks;
    private readonly EditorStateViewModel _editor;

    public EditorChangeCoordinator(NoteWorkspaceViewModel notes, TaskBoardViewModel tasks, EditorStateViewModel editor)
    {
        _notes = notes;
        _tasks = tasks;
        _editor = editor;
        _editor.ContentEdited += EditorContentEdited;
        _editor.RelatedNoteChanged += EditorRelatedNoteChanged;
        _editor.SettingsChanged += EditorSettingsChanged;
        _editor.PropertyChanged += EditorPropertyChanged;
    }

    public event EventHandler<WorkspaceChangeEventArgs>? Changed;

    private void EditorContentEdited(object? sender, EventArgs e)
    {
        if (_editor.IsTaskCommentMode && _editor.EditingTask != null)
            _tasks.UpdateComment(_editor.EditingTask, _editor.Content);
        else if (_editor.IsNoteEditMode && _editor.SelectedNote != null)
            _notes.UpdateContent(_editor.SelectedNote, _editor.Content);
    }

    private void EditorRelatedNoteChanged(object? sender, EventArgs e)
    {
        if (_editor.EditingTask != null)
            _tasks.SetRelatedNote(_editor.EditingTask, _editor.EditingTaskRelatedNote);
    }

    private void EditorSettingsChanged(object? sender, EventArgs e) => Publish(true);

    private void EditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var facadeProperty = e.PropertyName switch
        {
            nameof(EditorStateViewModel.Content) => nameof(MainViewModel.EditorContent),
            nameof(EditorStateViewModel.FontFamily) => nameof(MainViewModel.EditorFontFamily),
            nameof(EditorStateViewModel.FontSize) => nameof(MainViewModel.EditorFontSize),
            nameof(EditorStateViewModel.CaretPositionText) => nameof(MainViewModel.CaretPositionText),
            nameof(EditorStateViewModel.SelectedNote) => nameof(MainViewModel.SelectedNote),
            _ => e.PropertyName,
        };
        if (e.PropertyName is nameof(EditorStateViewModel.SelectedNote)
            or nameof(EditorStateViewModel.IsTaskCommentMode)
            or nameof(EditorStateViewModel.IsNoteEditMode)
            or nameof(EditorStateViewModel.EditorTitle))
            Publish(false, facadeProperty, nameof(MainViewModel.CurrentNoteTitle), nameof(MainViewModel.CurrentNoteTimestampSummary), nameof(MainViewModel.HasSelectedNote), nameof(MainViewModel.CurrentNotebookName));
        else
            Publish(false, facadeProperty);
    }

    private void Publish(bool isDataChanged, params string?[] propertyNames) =>
        Changed?.Invoke(this, WorkspaceChangeEventArgs.Create(isDataChanged, propertyNames));
}
