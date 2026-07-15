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

    public NoteViewModel? PickNote(
        IEnumerable<(string NotebookTitle, NoteViewModel Note)> notes,
        NoteViewModel? preselect = null,
        bool selectFirstWhenNoMatch = true,
        string? windowTitle = null,
        string? promptText = null)
    {
        var items = notes.Select(note => new NotePickerItem(note.NotebookTitle, note.Note));
        var dialog = new NotePickerDialog(items, preselect, selectFirstWhenNoMatch, windowTitle, promptText) { Owner = _owner };
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

    // v2.14.1 FM-1: 新規保存 / 名前を付けて保存の既定拡張子は .nestsuite（filter 先頭 + DefaultExt）。
    // legacy 拡張子は Workspace 種別に応じた filter として残し、読み取り・上書き互換を維持する。

    public string? SelectProjectOpenPath()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "NoteNest プロジェクト (*.nestsuite;*.notenest)|*.nestsuite;*.notenest" +
                     "|NestSuite Workspace (*.nestsuite)|*.nestsuite" +
                     "|Legacy NoteNest (*.notenest)|*.notenest" +
                     "|すべてのファイル (*.*)|*.*",
            DefaultExt = ".nestsuite"
        };
        return dialog.ShowDialog(_owner) == true ? dialog.FileName : null;
    }

    public string? SelectProjectSavePath(string defaultFileName) =>
        SelectSaveFilePath(
            "NestSuite Workspace (*.nestsuite)|*.nestsuite|Legacy NoteNest (*.notenest)|*.notenest",
            ".nestsuite", defaultFileName);

    public string? SelectChatNestOpenPath()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "ChatNest ファイル (*.nestsuite;*.chatnest)|*.nestsuite;*.chatnest" +
                     "|NestSuite Workspace (*.nestsuite)|*.nestsuite" +
                     "|Legacy ChatNest (*.chatnest)|*.chatnest" +
                     "|すべてのファイル (*.*)|*.*",
            DefaultExt = ".nestsuite"
        };
        return dialog.ShowDialog(_owner) == true ? dialog.FileName : null;
    }

    public string? SelectChatNestSavePath(string defaultFileName) =>
        SelectSaveFilePath(
            "NestSuite Workspace (*.nestsuite)|*.nestsuite|Legacy ChatNest (*.chatnest)|*.chatnest",
            ".nestsuite", defaultFileName);

    public string? SelectIdeaNestOpenPath()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "IdeaNest ファイル (*.nestsuite;*.ideanest)|*.nestsuite;*.ideanest" +
                     "|NestSuite Workspace (*.nestsuite)|*.nestsuite" +
                     "|Legacy IdeaNest (*.ideanest)|*.ideanest" +
                     "|すべてのファイル (*.*)|*.*",
            DefaultExt = ".nestsuite"
        };
        return dialog.ShowDialog(_owner) == true ? dialog.FileName : null;
    }

    public IReadOnlyList<string> SelectNestSuiteOpenPaths()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "NestSuite対応ファイル (*.nestsuite;*.notenest;*.chatnest;*.ideanest)|*.nestsuite;*.notenest;*.chatnest;*.ideanest" +
                     "|NestSuite Workspace (*.nestsuite)|*.nestsuite" +
                     "|NoteNestファイル (*.notenest)|*.notenest" +
                     "|ChatNestファイル (*.chatnest)|*.chatnest" +
                     "|IdeaNestファイル (*.ideanest)|*.ideanest" +
                     "|すべてのファイル (*.*)|*.*",
            Multiselect = true
        };
        return dialog.ShowDialog(_owner) == true ? dialog.FileNames : [];
    }

    public string? SelectIdeaNestSavePath(string defaultFileName) =>
        SelectSaveFilePath(
            "NestSuite Workspace (*.nestsuite)|*.nestsuite|Legacy IdeaNest (*.ideanest)|*.ideanest",
            ".nestsuite", defaultFileName);

    public string? SelectMarkdownExportPath(string defaultFileName) =>
        SelectSaveFilePath("Markdown (*.md)|*.md|テキスト (*.txt)|*.txt|すべてのファイル (*.*)|*.*", ".md", defaultFileName);

    public string? SelectMigrationPackExportPath(string defaultFileName) =>
        SelectSaveFilePath("デバイス移行パック (*.zip)|*.zip", ".zip", defaultFileName);

    public string? SelectMigrationPackOpenPath()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "デバイス移行パック (*.zip)|*.zip|すべてのファイル (*.*)|*.*",
            DefaultExt = ".zip"
        };
        return dialog.ShowDialog(_owner) == true ? dialog.FileName : null;
    }

    public string? SelectMigrationPackImportFolder() =>
        SelectFolderPath("デバイス移行パックの展開先フォルダを選択してください");

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

    /// <summary>
    /// TN-3: 既定ボタンを「いいえ」にした確認ダイアログ。安全側（変更しない）を既定にしたい場面に使う。
    /// TempNest 昇格後の元スロット消去確認で、既定を「残す」にするために使用する。
    /// </summary>
    public bool ConfirmWithSafeDefault(string message, string title) =>
        MessageBox.Show(_owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
            == MessageBoxResult.Yes;

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
#if NET48
        using var fbDialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = title,
            ShowNewFolderButton = true
        };
        return fbDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
            ? fbDialog.SelectedPath
            : null;
#else
        var dialog = new OpenFolderDialog
        {
            Title = title
        };
        return dialog.ShowDialog(_owner) == true ? dialog.FolderName : null;
#endif
    }
}
