using System.Windows;

namespace NoteNest;

/// <summary>Dialog launchers and shared notification helpers.</summary>
public partial class MainWindow
{
    private void ShowFindReplace_Click(object sender, RoutedEventArgs e) => OpenFindReplace();

    private void ShowTutorial_Click(object sender, RoutedEventArgs e)
        => _dialogs.ShowTutorial();

    private void ShowFontSettings_Click(object sender, RoutedEventArgs e)
    {
        var settings = _dialogs.ShowFontSettings(ViewModel.EditorFontFamily, ViewModel.EditorFontSize);
        if (settings is { } value)
            ViewModel.ApplyFontSettings(value.FontFamily, value.FontSize);
    }

    private void ShowError(string message, string title = "エラー") => _dialogs.ShowError(message, title);
    private void ShowInfo(string message, string title = "情報") => _dialogs.ShowInfo(message, title);

    private bool Confirm(string message, string title = "確認",
        MessageBoxImage icon = MessageBoxImage.Warning) => _dialogs.Confirm(message, title, icon);
}
