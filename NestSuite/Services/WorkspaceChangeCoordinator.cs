using System.ComponentModel;
using NoteNest.ViewModels;

namespace NoteNest.Services;

/// <summary>責務別 Coordinator の意味的通知を集約し、MainViewModel へ単一の通知経路を提供します。</summary>
public sealed class WorkspaceChangeCoordinator
{
    private readonly NoteChangeCoordinator _noteChanges;
    private readonly EditorChangeCoordinator _editorChanges;

    public WorkspaceChangeCoordinator(
        NoteWorkspaceViewModel notes,
        TaskBoardViewModel tasks,
        MarkerPanelViewModel markers,
        EditorStateViewModel editor)
    {
        _noteChanges = new NoteChangeCoordinator(notes, markers);
        _editorChanges = new EditorChangeCoordinator(notes, tasks, editor);
        _noteChanges.Changed += Relay;
        _editorChanges.Changed += Relay;
        tasks.Changed += (_, _) => Publish(true, nameof(MainViewModel.EditorTitle));
        markers.PropertyChanged += MarkerPropertyChanged;
    }

    public event EventHandler<WorkspaceChangeEventArgs>? Changed;

    private void Relay(object? sender, WorkspaceChangeEventArgs e) => Changed?.Invoke(this, e);

    private void MarkerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var facadeProperty = e.PropertyName == nameof(MarkerPanelViewModel.SortOrderIndex)
            ? nameof(MainViewModel.MarkerSortOrderIndex)
            : e.PropertyName;
        Publish(false, facadeProperty);
    }

    private void Publish(bool isDataChanged, params string?[] propertyNames) =>
        Changed?.Invoke(this, WorkspaceChangeEventArgs.Create(isDataChanged, propertyNames));
}

public sealed record WorkspaceChangeEventArgs(bool IsDataChanged, IReadOnlyList<string> PropertyNames)
{
    public static WorkspaceChangeEventArgs Create(bool isDataChanged, IEnumerable<string?> propertyNames) =>
        new(isDataChanged, propertyNames.OfType<string>().Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToArray());
}
