using NestSuite.ViewModels;

namespace NestSuite.Services;

/// <summary>ノート変更に伴う派生表示の更新と意味的通知を担当します。</summary>
public sealed class NoteChangeCoordinator
{
    private readonly NoteWorkspaceViewModel _notes;
    private readonly MarkerPanelViewModel _markers;

    public NoteChangeCoordinator(NoteWorkspaceViewModel notes, MarkerPanelViewModel markers)
    {
        _notes = notes;
        _markers = markers;
        _notes.Changed += NotesChanged;
    }

    public event EventHandler<WorkspaceChangeEventArgs>? Changed;

    private void NotesChanged(object? sender, EventArgs e)
    {
        _markers.Refresh(_notes.AllNotes);
        Changed?.Invoke(this, WorkspaceChangeEventArgs.Create(true,
        [
            nameof(MainViewModel.RelatedNoteChoices),
            nameof(MainViewModel.CurrentNoteTitle),
            nameof(MainViewModel.CurrentNotebookName),
            nameof(MainViewModel.EditorTitle),
            nameof(MainViewModel.CurrentNoteTimestampSummary),
            nameof(MainViewModel.IsNoteListEmpty),
            nameof(MainViewModel.HasAnyNotes),
            nameof(MainViewModel.MarkdownExportAllNotesTooltip),
        ]));
    }
}
