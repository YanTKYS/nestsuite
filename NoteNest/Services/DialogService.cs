using System.Windows;
using System.Windows.Controls;
using NoteNest.Dialogs;
using NoteNest.ViewModels;
using NoteNest.Models;

namespace NoteNest.Services;

/// <summary>
/// MainWindow が利用するダイアログの生成と Owner 設定を一箇所に集約します。
/// </summary>
public sealed class DialogService
{
    private readonly Window _owner;

    public DialogService(Window owner) => _owner = owner;

    public string? ShowInput(string title, string prompt, string initialText = "")
    {
        var dialog = new InputDialog(title, prompt, initialText) { Owner = _owner };
        return dialog.ShowDialog() == true ? dialog.ResultText : null;
    }

    public NoteViewModel? PickNote(IEnumerable<NotePickerItem> items)
    {
        var dialog = new NotePickerDialog(items) { Owner = _owner };
        return dialog.ShowDialog() == true ? dialog.SelectedNote : null;
    }

    public (string FontFamily, double FontSize)? ShowFontSettings(string currentFamily, double currentSize)
    {
        var dialog = new FontSettingsDialog(currentFamily, currentSize) { Owner = _owner };
        return dialog.ShowDialog() == true
            ? (FontFamily: dialog.SelectedFontFamily, FontSize: dialog.SelectedFontSize)
            : null;
    }

    public ExportOptions? ShowExportOptions()
    {
        var dialog = new ExportDialog { Owner = _owner };
        return dialog.ShowDialog() == true ? dialog.Options : null;
    }

    public void ShowProjectInfo(string information) =>
        new ProjectInfoDialog(information) { Owner = _owner }.ShowDialog();

    public FindReplaceDialog CreateFindReplace(TextBox editor) =>
        new(editor) { Owner = _owner };

    public void ShowTutorial() => new TutorialWindow { Owner = _owner }.Show();

    public void ShowError(string message, string title = "エラー") =>
        MessageBox.Show(_owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowInfo(string message, string title = "情報") =>
        MessageBox.Show(_owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public bool Confirm(string message, string title = "確認", MessageBoxImage icon = MessageBoxImage.Question) =>
        MessageBox.Show(_owner, message, title, MessageBoxButton.YesNo, icon) == MessageBoxResult.Yes;
}
