using System.Windows;
using NoteNest.Models;

namespace NoteNest;

/// <summary>Project, notebook, and note export event handling.</summary>
public partial class MainWindow
{
    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var options = _dialogs.ShowExportOptions();
        if (options == null) return;
        if (options.Target != ExportTarget.Project && ViewModel.SelectedNote == null)
        {
            ShowInfo("現在のノートを選択してください。");
            return;
        }
        var outputPath = _dialogs.SelectExportOutputPath(options, GetExportDefaultFileName(options));
        if (outputPath == null) return;
        try
        {
            ViewModel.Export(options, outputPath);
            ViewModel.StatusMessage = $"エクスポートしました: {Path.GetFileName(outputPath)}";
        }
        catch (Exception ex) { ShowError($"エクスポートに失敗しました。\n{ex.Message}"); }
    }

    private string GetExportDefaultFileName(ExportOptions options) => options.Target switch
    {
        ExportTarget.CurrentNote => ViewModel.SelectedNote?.Title ?? ViewModel.ProjectName,
        ExportTarget.CurrentNotebook => ViewModel.SelectedNote == null
            ? ViewModel.ProjectName
            : ViewModel.FindNotebookOf(ViewModel.SelectedNote)?.Title ?? ViewModel.ProjectName,
        _ => ViewModel.ProjectName,
    };

    private void ExportProjectText_Click(object sender, RoutedEventArgs e)
    {
        var outputPath = _dialogs.SelectProjectTextExportPath(ViewModel.ProjectName);
        if (outputPath == null) return;

        try
        {
            ViewModel.ExportProjectToText(outputPath);
            ViewModel.StatusMessage = $"エクスポートしました: {Path.GetFileName(outputPath)}";
        }
        catch (Exception ex)
        {
            ShowError($"エクスポートに失敗しました。\n{ex.Message}");
        }
    }

    private void ExportNotebooksText_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Notebooks.Count == 0) { ShowInfo("エクスポートするノートブックがありません。"); return; }

        var directory = _dialogs.SelectNotebookExportFolder();
        if (directory == null) return;
        if (!Directory.Exists(directory)) { ShowError("選択したフォルダが存在しません。"); return; }

        try
        {
            var count = ViewModel.ExportNotebooksToTextFiles(directory);
            ShowInfo($"{count} 件のノートブックをエクスポートしました。\n出力先: {directory}", "エクスポート完了");
            ViewModel.StatusMessage = $"{count} 件のノートブックをエクスポートしました。";
        }
        catch (Exception ex)
        {
            ShowError($"エクスポートに失敗しました。\n{ex.Message}");
        }
    }
}
