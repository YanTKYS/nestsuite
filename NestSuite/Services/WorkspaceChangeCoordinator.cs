using System.ComponentModel;
using NestSuite.ViewModels;

namespace NestSuite.Services;

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
        tasks.Loaded  += (_, _) => Publish(false,
            nameof(MainViewModel.TotalIncompleteTaskCountText),
            nameof(MainViewModel.HasAnyTasks),
            nameof(MainViewModel.HasNoTasks),
            nameof(MainViewModel.ShowTaskEmptyState));
        tasks.Changed += (_, _) => Publish(true,
            nameof(MainViewModel.EditorTitle),
            nameof(MainViewModel.TotalIncompleteTaskCountText),
            nameof(MainViewModel.HasAnyTasks),
            nameof(MainViewModel.HasNoTasks),
            nameof(MainViewModel.ShowTaskEmptyState));
        markers.PropertyChanged += MarkerPropertyChanged;
    }

    public event EventHandler<WorkspaceChangeEventArgs>? Changed;

    private void Relay(object? sender, WorkspaceChangeEventArgs e) => Changed?.Invoke(this, e);

    private void MarkerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var facadeProperty = e.PropertyName == nameof(MarkerPanelViewModel.SortOrderIndex)
            ? nameof(MainViewModel.MarkerSortOrderIndex)
            : e.PropertyName;
        // L23: MarkerCount 変化時は空状態ガイドの表示条件（HasAnyMarkers/HasNoMarkers/ShowMarkerEmptyState）も
        // 併せて再評価させる。NoteChangeCoordinator 経由の通知が届かない Refresh() 呼び出し
        // （プロジェクト読込直後など）でも空状態表示が最新化されるようにするため。
        if (e.PropertyName == nameof(MarkerPanelViewModel.MarkerCount))
            Publish(false, facadeProperty,
                nameof(MainViewModel.HasAnyMarkers),
                nameof(MainViewModel.HasNoMarkers),
                nameof(MainViewModel.ShowMarkerEmptyState));
        else
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
