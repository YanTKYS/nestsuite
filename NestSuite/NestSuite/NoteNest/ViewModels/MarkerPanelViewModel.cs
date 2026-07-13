using System.Collections.ObjectModel;
using NestSuite.Services;

namespace NestSuite.ViewModels;

/// <summary>マーカー抽出結果、フィルター、並び順を所有します。</summary>
public sealed class MarkerPanelViewModel : BaseViewModel
{
    private readonly MarkerExtractorService _extractor;
    private bool _filterTodo = true;
    private bool _filterFixme = true;
    private bool _filterNote = true;
    private int _sortOrderIndex;
    private int _todoCount;
    private int _fixmeCount;
    private int _noteCount;

    public MarkerPanelViewModel(MarkerExtractorService extractor) => _extractor = extractor;

    public ObservableCollection<MarkerViewModel> Markers { get; } = new();
    public int MarkerCount => Markers.Count;
    public string ProjectMarkerSummary => $"全体  TODO: {_todoCount}  FIXME: {_fixmeCount}  NOTE: {_noteCount}";
    public string TodoCountText  => $"TODO（{_todoCount}）";
    public string FixmeCountText => $"FIXME（{_fixmeCount}）";
    public string NoteCountText  => $"NOTE（{_noteCount}）";

    public bool FilterTodo { get => _filterTodo; set { if (SetProperty(ref _filterTodo, value)) RaiseFilteredChanged(); } }
    public bool FilterFixme { get => _filterFixme; set { if (SetProperty(ref _filterFixme, value)) RaiseFilteredChanged(); } }
    public bool FilterNote { get => _filterNote; set { if (SetProperty(ref _filterNote, value)) RaiseFilteredChanged(); } }
    public int SortOrderIndex { get => _sortOrderIndex; set { if (SetProperty(ref _sortOrderIndex, value)) RaiseFilteredChanged(); } }

    public IEnumerable<MarkerViewModel> FilteredMarkers
    {
        get
        {
            var filtered = Markers.Where(marker =>
                (marker.Type == MarkerTypeNames.Todo && FilterTodo) ||
                (marker.Type == MarkerTypeNames.Fixme && FilterFixme) ||
                (marker.Type == MarkerTypeNames.Note && FilterNote));
            return SortOrderIndex switch
            {
                1 => filtered.OrderBy(marker => MarkerTypeNames.SortOrder(marker.Type)).ThenBy(marker => marker.NoteTitle).ThenBy(marker => marker.LineNumber),
                2 => filtered.OrderBy(marker => marker.NoteTitle).ThenBy(marker => marker.LineNumber),
                3 => filtered.OrderBy(marker => marker.LineNumber),
                _ => filtered,
            };
        }
    }

    public string FilteredMarkerCountText
    {
        get
        {
            var filtered = FilteredMarkers.Count();
            return filtered == Markers.Count ? $"{Markers.Count}個" : $"{filtered}/{Markers.Count}個";
        }
    }

    public void Refresh(IEnumerable<NoteViewModel> notes)
    {
        Markers.Clear();
        _todoCount = _fixmeCount = _noteCount = 0;
        foreach (var note in notes)
        foreach (var marker in _extractor.Extract(note.Content, note.Title))
        {
            Markers.Add(new MarkerViewModel(marker, note));
            if (marker.Type == MarkerTypeNames.Todo) _todoCount++;
            else if (marker.Type == MarkerTypeNames.Fixme) _fixmeCount++;
            else if (marker.Type == MarkerTypeNames.Note) _noteCount++;
        }
        OnPropertyChanged(nameof(MarkerCount));
        OnPropertyChanged(nameof(ProjectMarkerSummary));
        OnPropertyChanged(nameof(TodoCountText));
        OnPropertyChanged(nameof(FixmeCountText));
        OnPropertyChanged(nameof(NoteCountText));
        RaiseFilteredChanged();
    }

    private void RaiseFilteredChanged()
    {
        OnPropertyChanged(nameof(FilteredMarkers));
        OnPropertyChanged(nameof(FilteredMarkerCountText));
    }
}
