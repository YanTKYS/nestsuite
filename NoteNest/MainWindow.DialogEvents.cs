using System.Windows;

namespace NoteNest;

public partial class MainWindow
{
    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var options = _dialogs.ShowExportOptions();
        if (options == null) return;
        if (options.Target != NoteNest.Models.ExportTarget.Project && ViewModel.SelectedNote == null)
        {
            ShowInfo("現在のノートを選択してください。");
            return;
        }
        var outputPath = _dialogs.SelectExportOutputPath(options, GetExportDefaultFileName(options));
        if (outputPath == null) return;
        try
        {
            ViewModel.Export(options, outputPath);
            ViewModel.StatusMessage = $"エクスポートしました: {System.IO.Path.GetFileName(outputPath)}";
        }
        catch (Exception ex) { ShowError($"エクスポートに失敗しました。\n{ex.Message}"); }
    }


    private string GetExportDefaultFileName(NoteNest.Models.ExportOptions options) => options.Target switch
    {
        NoteNest.Models.ExportTarget.CurrentNote => ViewModel.SelectedNote?.Title ?? ViewModel.ProjectName,
        NoteNest.Models.ExportTarget.CurrentNotebook => ViewModel.SelectedNote == null
            ? ViewModel.ProjectName
            : ViewModel.FindNotebookOf(ViewModel.SelectedNote)?.Title ?? ViewModel.ProjectName,
        _ => ViewModel.ProjectName,
    };

    private void ClearRecentFiles_Click(object sender, RoutedEventArgs e)
    {
        if (Confirm("最近使ったファイルの履歴をクリアしますか？", "履歴のクリア"))
            ViewModel.ClearRecentFilesCommand.Execute(null);
    }

    private void ShowProjectInfo_Click(object sender, RoutedEventArgs e) => _dialogs.ShowProjectInfo(ViewModel.ProjectInfo);

    private void ExportProjectText_Click(object sender, RoutedEventArgs e)
    {
        var outputPath = _dialogs.SelectProjectTextExportPath(ViewModel.ProjectName);
        if (outputPath == null) return;

        try
        {
            ViewModel.ExportProjectToText(outputPath);
            ViewModel.StatusMessage = $"エクスポートしました: {System.IO.Path.GetFileName(outputPath)}";
        }
        catch (Exception ex)
        {
            ShowError($"エクスポートに失敗しました。\n{ex.Message}");
        }
    }

    private void ExportNotebooksText_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Notebooks.Count == 0) { ShowInfo("エクスポートするノートブックがありません。"); return; }

        var dir = _dialogs.SelectNotebookExportFolder();
        if (dir == null) return;
        if (!System.IO.Directory.Exists(dir)) { ShowError("選択したフォルダが存在しません。"); return; }

        try
        {
            var count = ViewModel.ExportNotebooksToTextFiles(dir);
            ShowInfo($"{count} 件のノートブックをエクスポートしました。\n出力先: {dir}", "エクスポート完了");
            ViewModel.StatusMessage = $"{count} 件のノートブックをエクスポートしました。";
        }
        catch (Exception ex)
        {
            ShowError($"エクスポートに失敗しました。\n{ex.Message}");
        }
    }

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

    private void OpenFindReplace() =>
        _dialogs.ShowFindReplace(EditorBox,
            _uiSettings.LastSearchText,
            _uiSettings.LastReplaceText,
            _uiSettings.FindReplaceLeft,
            _uiSettings.FindReplaceTop);
}
