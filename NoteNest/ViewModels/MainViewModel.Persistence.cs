namespace NoteNest.ViewModels;

public partial class MainViewModel
{
    public void Export(NoteNest.Models.ExportOptions options, string outputPath)
    {
        var notebookId = SelectedNote == null ? null : FindNotebookOf(SelectedNote)?.Id;
        _exports.Export(_lifecycle.CreateSnapshot(), options, outputPath, notebookId, SelectedNote?.Id);
    }

    public void ExportProjectToText(string outputPath) =>
        _exports.ExportProjectToText(_lifecycle.CreateSnapshot(), outputPath);

    public int ExportNotebooksToTextFiles(string outputDirectory)
    {
        var project = _lifecycle.CreateSnapshot();
        _exports.ExportNotebooksToTextFiles(project, outputDirectory);
        return project.Notebooks.Count;
    }

    public bool OpenFileAtStartup(string path) => TryOpenProject(path);

    public bool ConfirmCloseIfModified()
    {
        if (!IsModified) return true;
        return ShowConfirmDialog?.Invoke("未保存の変更", "保存されていない変更があります。終了しますか？") ?? true;
    }

    private bool EnsureCanDiscardChanges(string question)
    {
        if (!IsModified) return true;
        return ShowConfirmDialog?.Invoke("未保存の変更", question) ?? true;
    }

    private void NewProject()
    {
        if (!EnsureCanDiscardChanges("保存されていない変更があります。新規プロジェクトを作成しますか？"))
            return;
        _lifecycle.CreateNew();
        StatusMessage = "新規プロジェクトを作成しました。";
    }

    private void OpenProject()
    {
        if (!EnsureCanDiscardChanges("保存されていない変更があります。続けますか？")) return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "NoteNest プロジェクト (*.notenest)|*.notenest|すべてのファイル (*.*)|*.*",
            DefaultExt = ".notenest"
        };

        if (dialog.ShowDialog() == true) TryOpenProject(dialog.FileName);
    }

    private void SaveProject()
    {
        if (_session.CurrentFilePath == null) { SaveProjectAs(); return; }
        DoSave(_session.CurrentFilePath);
    }

    private void SaveProjectAs()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "NoteNest プロジェクト (*.notenest)|*.notenest",
            DefaultExt = ".notenest",
            FileName = ProjectName
        };

        if (dialog.ShowDialog() == true) DoSave(dialog.FileName);
    }

    private bool DoSave(string path)
    {
        try
        {
            _lifecycle.Save(path);
            StatusMessage = $"保存しました: {System.IO.Path.GetFileName(path)}";
            return true;
        }
        catch (Exception ex)
        {
            ShowErrorDialog?.Invoke("エラー", $"保存に失敗しました。\n{ex.Message}");
            return false;
        }
    }

    private void Exit()
    {
        if (ConfirmCloseIfModified()) RequestClose?.Invoke();
    }

    private void OpenRecentFile(string path)
    {
        if (!EnsureCanDiscardChanges("保存されていない変更があります。続けますか？")) return;
        TryOpenProject(path);
    }

    private void ClearRecentFiles()
    {
        _lifecycle.ClearRecentFiles();
        StatusMessage = "最近使ったファイルをクリアしました。";
    }

    private void AutoSave()
    {
        if (!IsAutoSaveEnabled) return;
        try
        {
            if (_lifecycle.TryAutoSave()) StatusMessage = "自動保存しました。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"自動保存に失敗しました: {ex.Message}";
        }
    }

    private bool TryOpenProject(string path)
    {
        try
        {
            _lifecycle.Open(path);
            StatusMessage = $"プロジェクトを開きました: {System.IO.Path.GetFileName(path)}";
            return true;
        }
        catch (Exception ex)
        {
            ShowErrorDialog?.Invoke("エラー", $"ファイルを開けませんでした。\n{ex.Message}");
            return false;
        }
    }
}
