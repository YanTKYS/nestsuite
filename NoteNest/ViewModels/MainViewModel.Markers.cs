namespace NoteNest.ViewModels;

public partial class MainViewModel
{
    private void RefreshMarkers() => _markers.Refresh(AllNotes);
}
