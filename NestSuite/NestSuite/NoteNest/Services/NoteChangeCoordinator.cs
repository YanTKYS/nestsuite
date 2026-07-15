using NestSuite.ViewModels;

namespace NestSuite.Services;

/// <summary>ノート変更に伴う派生表示の更新と意味的通知を担当します。</summary>
public sealed class NoteChangeCoordinator
{
    private readonly NoteWorkspaceViewModel _notes;
    private readonly MarkerPanelViewModel _markers;

    /// <summary>
    /// ノートの追加・削除・タイトル・本文・スター変更、および project 再読込のいずれでも
    /// 再評価が必要な派生表示プロパティ名。<see cref="NotesChanged"/>（データ変更あり）と
    /// <see cref="NotesReloaded"/>（L25: 読込のみ、データ変更なし）で同じ集合を再利用し、
    /// 複数箇所への重複を避ける。
    /// </summary>
    private static readonly string[] DerivedDisplayPropertyNames =
    [
        nameof(MainViewModel.RelatedNoteChoices),
        nameof(MainViewModel.CurrentNoteTitle),
        nameof(MainViewModel.CurrentNotebookName),
        nameof(MainViewModel.EditorTitle),
        nameof(MainViewModel.CurrentNoteTimestampSummary),
        nameof(MainViewModel.IsNoteListEmpty),
        nameof(MainViewModel.HasAnyNotes),
        nameof(MainViewModel.MarkdownExportAllNotesTooltip),
        nameof(MainViewModel.HasNotebooks),
        nameof(MainViewModel.ShowNotebookEmptyState),
        nameof(MainViewModel.ShowNoteEmptyState),
        nameof(MainViewModel.ShowTaskEmptyState),
        nameof(MainViewModel.HasAnyMarkers),
        nameof(MainViewModel.HasNoMarkers),
        nameof(MainViewModel.ShowMarkerEmptyState),
    ];

    public NoteChangeCoordinator(NoteWorkspaceViewModel notes, MarkerPanelViewModel markers)
    {
        _notes = notes;
        _markers = markers;
        _notes.Changed += NotesChanged;
        _notes.Reloaded += NotesReloaded;
    }

    public event EventHandler<WorkspaceChangeEventArgs>? Changed;

    private void NotesChanged(object? sender, EventArgs e)
    {
        _markers.Refresh(_notes.AllNotes);
        Changed?.Invoke(this, WorkspaceChangeEventArgs.Create(true, DerivedDisplayPropertyNames));
    }

    /// <summary>
    /// L25 (review7-fable5 REV7-2): project 読込直後、<see cref="NoteWorkspaceViewModel.Load"/> は
    /// 内部で <c>Changed</c> を発火しないため、空状態表示等の派生プロパティが古いまま残っていた。
    /// 読込はデータ変更ではないため isDataChanged=false で通知し、IsModified を変更しない。
    /// </summary>
    private void NotesReloaded(object? sender, EventArgs e)
    {
        _markers.Refresh(_notes.AllNotes);
        Changed?.Invoke(this, WorkspaceChangeEventArgs.Create(false, DerivedDisplayPropertyNames));
    }
}
