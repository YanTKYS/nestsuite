namespace NoteNest.ViewModels;

public partial class MainViewModel
{
    public void ApplyFontSettings(string fontFamily, double fontSize)
    {
        _editor.FontFamily = fontFamily;
        _editor.FontSize   = fontSize;
    }

    private void ClearEditor()
    {
        _editor.Clear();
        RefreshMarkers();
    }
}
