using System.Windows;
using Microsoft.Win32;
using NestSuite.Dialogs;
using NestSuite.Models;
using NestSuite.NoteNest.Editor;
using NestSuite.ViewModels;

namespace NestSuite.Services;

/// <summary>
/// NestSuite および各 Workspace が利用するダイアログの生成、Owner 設定、ファイル選択を一箇所に集約します。
/// </summary>
public sealed class DialogService
{
    private readonly Window _owner;
    private FindReplaceDialog? _findReplaceDialog;

    public DialogService(Window owner) => _owner = owner;

    public string? ShowInput(string title, string prompt, string initialText = "")
    {
        var dialog = new InputDialog(title, prompt, initialText) { Owner = _owner };
        return dialog.ShowDialog() == true ? dialog.ResultText : null;
    }

    public NoteViewModel? PickNote(IEnumerable<(string NotebookTitle, NoteViewModel Note)> notes)
    {
        var items = notes.Select(note => new NotePickerItem(note.NotebookTitle, note.Note));
        var dialog = new NotePickerDialog(items) { Owner = _owner };
        return dialog.ShowDialog() == true ? dialog.SelectedNote : null;
    }

    public NoteViewModel? CheckBrokenLinks(IEnumerable<NoteViewModel> notes)
    {
        var results = BrokenLinkCheckerService.Check(notes);
        var dialog = new BrokenLinksDialog(results) { Owner = _owner };
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

    public string? SelectExportOutputPath(ExportOptions options, string defaultFileName)
    {
        var extension = ExportService.GetExtension(options.Format);
        return SelectSaveFilePath($"{extension} ファイル (*{extension})|*{extension}", extension, defaultFileName);
    }

    public string? SelectProjectTextExportPath(string defaultFileName) =>
        SelectSaveFilePath("テキストファイル (*.txt)|*.txt", ".txt", defaultFileName);

    public string? SelectNotebookExportFolder() =>
        SelectFolderPath("出力先フォルダを選択してください");

    public string? SelectProjectOpenPath()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "NoteNest プロジェクト (*.notenest)|*.notenest|すべてのファイル (*.*)|*.*",
            DefaultExt = ".notenest"
        };
        return dialog.ShowDialog(_owner) == true ? dialog.FileName : null;
    }

    public string? SelectProjectSavePath(string defaultFileName) =>
        SelectSaveFilePath("NoteNest プロジェクト (*.notenest)|*.notenest", ".notenest", defaultFileName);

    public string? SelectChatNestOpenPath()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "ChatNest ファイル (*.chatnest)|*.chatnest|すべてのファイル (*.*)|*.*",
            DefaultExt = ".chatnest"
        };
        return dialog.ShowDialog(_owner) == true ? dialog.FileName : null;
    }

    public string? SelectChatNestSavePath(string defaultFileName) =>
        SelectSaveFilePath("ChatNest ファイル (*.chatnest)|*.chatnest", ".chatnest", defaultFileName);

    public string? SelectIdeaNestOpenPath()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "IdeaNest ファイル (*.ideanest)|*.ideanest|すべてのファイル (*.*)|*.*",
            DefaultExt = ".ideanest"
        };
        return dialog.ShowDialog(_owner) == true ? dialog.FileName : null;
    }

    public IReadOnlyList<string> SelectNestSuiteOpenPaths()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "NestSuite対応ファイル (*.notenest;*.chatnest;*.ideanest)|*.notenest;*.chatnest;*.ideanest" +
                     "|NoteNestファイル (*.notenest)|*.notenest" +
                     "|ChatNestファイル (*.chatnest)|*.chatnest" +
                     "|IdeaNestファイル (*.ideanest)|*.ideanest" +
                     "|すべてのファイル (*.*)|*.*",
            Multiselect = true
        };
        return dialog.ShowDialog(_owner) == true ? dialog.FileNames : [];
    }

    public string? SelectIdeaNestSavePath(string defaultFileName) =>
        SelectSaveFilePath("IdeaNest ファイル (*.ideanest)|*.ideanest", ".ideanest", defaultFileName);

    public void ShowProjectInfo(string information) =>
        new ProjectInfoDialog(information) { Owner = _owner }.ShowDialog();

    public void ShowFindReplace(ITextEditorAdapter editor, IEnumerable<NoteViewModel>? allNotes,
        Action<NoteViewModel>? navigateToNote, string lastSearchText, string lastReplaceText,
        double? left, double? top)
    {
        if (_findReplaceDialog == null || !_findReplaceDialog.IsLoaded)
        {
            _findReplaceDialog = new FindReplaceDialog(editor) { Owner = _owner };
            _findReplaceDialog.RestoreState(lastSearchText, lastReplaceText, left, top);
        }
        else
        {
            _findReplaceDialog.SetEditor(editor);
        }

        if (allNotes != null && navigateToNote != null)
            _findReplaceDialog.SetAllNotes(allNotes, navigateToNote);

        _findReplaceDialog.Show();
        _findReplaceDialog.Activate();
    }

    public (string LastSearchText, string LastReplaceText, double? Left, double? Top) GetFindReplaceState(
        string fallbackSearchText,
        string fallbackReplaceText,
        double? fallbackLeft,
        double? fallbackTop) =>
        (
            LastSearchText: _findReplaceDialog?.SearchText ?? fallbackSearchText,
            LastReplaceText: _findReplaceDialog?.ReplaceText ?? fallbackReplaceText,
            Left: _findReplaceDialog?.IsLoaded == true ? _findReplaceDialog.Left : fallbackLeft,
            Top: _findReplaceDialog?.IsLoaded == true ? _findReplaceDialog.Top : fallbackTop
        );

    public void CloseFindReplace()
    {
        if (_findReplaceDialog == null) return;
        _findReplaceDialog.ForceClose = true;
        _findReplaceDialog.Close();
    }

    public void ShowTutorial() => new TutorialWindow { Owner = _owner }.Show();

    public void ShowError(string message, string title = "エラー") =>
        MessageBox.Show(_owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowInfo(string message, string title = "情報") =>
        MessageBox.Show(_owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public bool Confirm(string message, string title = "確認", MessageBoxImage icon = MessageBoxImage.Question) =>
        MessageBox.Show(_owner, message, title, MessageBoxButton.YesNo, icon) == MessageBoxResult.Yes;

    private string? SelectSaveFilePath(string filter, string defaultExtension, string defaultFileName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            DefaultExt = defaultExtension,
            FileName = defaultFileName
        };
        return dialog.ShowDialog(_owner) == true ? dialog.FileName : null;
    }

    private string? SelectFolderPath(string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title
        };
        return dialog.ShowDialog(_owner) == true ? dialog.FolderName : null;
    }
}
