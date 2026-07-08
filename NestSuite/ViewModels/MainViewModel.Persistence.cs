using NestSuite.Models;
using NestSuite.Services;

namespace NestSuite.ViewModels;

public partial class MainViewModel
{
    public void Export(ExportOptions options, string outputPath)
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

    /// <summary>
    /// 確認なしで新規プロジェクトを作成する。
    /// NestSuite がタブ閉じ操作でユーザー確認を完了済みの場合に呼ぶ。
    /// 通常の新規作成（ユーザー操作）には <see cref="NewProjectCommand"/> を使用すること。
    /// </summary>
    public void CreateNewProjectDirect() => _lifecycle.CreateNew();

    private bool EnsureCanDiscardChanges(string question)
    {
        if (!_session.IsModified) return true;
        return ShowConfirmDialog?.Invoke("未保存の変更", question) ?? true;
    }

    private void NewProject()
    {
        if (!EnsureCanDiscardChanges("保存されていない変更があります。新規プロジェクトを作成しますか？"))
            return;
        _lifecycle.CreateEmpty();
        StatusMessage = "新規プロジェクトを作成しました。";
    }

    private void OpenProject()
    {
        if (!EnsureCanDiscardChanges("保存されていない変更があります。続けますか？")) return;

        var path = SelectOpenProjectPath?.Invoke();
        if (path != null) TryOpenProject(path);
    }

    private void SaveProject()
    {
        if (_session.CurrentFilePath == null) { SaveProjectAs(); return; }
        DoSave(_session.CurrentFilePath);
    }

    private void SaveProjectAs()
    {
        var path = SelectSaveProjectPath?.Invoke(_session.ProjectName);
        if (path != null) DoSave(path);
    }

    /// <summary>Shell が重複パス検出後にパス指定で保存するためのエントリポイント。</summary>
    public bool SaveToPath(string path) => DoSave(path, notifyOnError: true, createBackup: true);

    /// <summary>
    /// v2.14.12 SH-33: 自動保存など、失敗をユーザーへ都度ダイアログ通知したくない呼び出し用。
    /// 失敗時も <see cref="ErrorLogService"/> へは記録するが、<see cref="ShowErrorDialog"/> は呼ばない。
    /// </summary>
    public bool SaveToPath(string path, bool notifyOnError) => DoSave(path, notifyOnError, createBackup: true);

    /// <summary>
    /// v2.16.6 TD-64: 自動保存など、正本は更新するが .bak を更新したくない呼び出し用。
    /// createBackup=false でも atomic write（tmp 経由の安全な書き込み）は維持する。
    /// </summary>
    public bool SaveToPath(string path, bool notifyOnError, bool createBackup) =>
        DoSave(path, notifyOnError, createBackup);

    private bool DoSave(string path) => DoSave(path, notifyOnError: true, createBackup: true);

    private bool DoSave(string path, bool notifyOnError) => DoSave(path, notifyOnError, createBackup: true);

    private bool DoSave(string path, bool notifyOnError, bool createBackup)
    {
        try
        {
            _lifecycle.Save(path, createBackup);
            StatusMessage = $"保存しました: {System.IO.Path.GetFileName(path)}";
            return true;
        }
        catch (Exception ex)
        {
            bool logged = ErrorLogService.Log("NoteNestSave", ex, "NoteNest", path);
            if (notifyOnError)
            {
                var logHint = logged ? "\n\n詳細はエラーログに記録されました。" : "";
                ShowErrorDialog?.Invoke("保存エラー",
                    $"保存に失敗しました。\n{FileErrorMessages.ForSave(ex)}{logHint}");
            }
            return false;
        }
    }

    private void Exit()
    {
        // v2.9.7: 未保存確認は Shell の OnClosing で Save / Discard / Cancel として行う。
        RequestClose?.Invoke();
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
            bool logged = ErrorLogService.Log("NoteNestLoad", ex, "NoteNest", path);
            var logHint = logged ? "\n\n詳細はエラーログに記録されました。" : "";
            ShowErrorDialog?.Invoke("読込エラー",
                $"ファイルを開けませんでした。\n{FileErrorMessages.ForLoad(ex, path)}{logHint}");
            return false;
        }
    }
}
