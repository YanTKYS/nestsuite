using System.Windows.Input;

namespace NoteNest;

/// <summary>Window-wide keyboard shortcuts that are not represented by XAML commands.</summary>
public partial class MainWindow
{
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;

        if (e.Key is Key.F or Key.H)
            OpenFindReplace();
        else if (e.Key == Key.Enter)
            TryOpenNoteLink();
        else if (e.Key is Key.OemPlus or Key.Add)
            ChangeEditorFontSize(1);
        else if (e.Key is Key.OemMinus or Key.Subtract)
            ChangeEditorFontSize(-1);
        else
            return;

        e.Handled = true;
    }

    private void ChangeEditorFontSize(double delta)
    {
        var next = Math.Clamp(ViewModel.EditorFontSize + delta, 8d, 36d);
        if (next != ViewModel.EditorFontSize)
            ViewModel.ApplyFontSettings(ViewModel.EditorFontFamily, next);
    }
}
