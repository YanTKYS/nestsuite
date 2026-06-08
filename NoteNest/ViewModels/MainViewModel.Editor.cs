namespace NoteNest.ViewModels;

public partial class MainViewModel
{
    public void ApplyFontSettings(string fontFamily, double fontSize)
    {
        EditorFontFamily = fontFamily;
        EditorFontSize   = fontSize;
    }

    private void ClearEditor()
    {
        _editor.Clear();
        _markers.Refresh(_notes.AllNotes);
    }
}
