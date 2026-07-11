using System.IO;
using NestSuite.Models;
using NestSuite.ViewModels;

namespace NestSuite.Services;

/// <summary>プロジェクトの新規作成・読込・保存と、セッション／ワークスペース同期を扱います。</summary>
public sealed class ProjectLifecycleService
{
    private readonly ProjectSessionViewModel _session;
    private readonly NoteWorkspaceViewModel _notes;
    private readonly TaskBoardViewModel _tasks;
    private readonly MarkerPanelViewModel _markers;
    private readonly EditorStateViewModel _editor;
    private readonly ProjectFileService _files;
    private readonly ProjectDocumentService _documents;
    private readonly SampleDataService _samples;
    private readonly RecentFilesService _recentFiles;

    public ProjectLifecycleService(
        ProjectSessionViewModel session,
        NoteWorkspaceViewModel notes,
        TaskBoardViewModel tasks,
        MarkerPanelViewModel markers,
        EditorStateViewModel editor,
        ProjectFileService? files = null,
        ProjectDocumentService? documents = null,
        SampleDataService? samples = null,
        RecentFilesService? recentFiles = null)
    {
        _session = session;
        _notes = notes;
        _tasks = tasks;
        _markers = markers;
        _editor = editor;
        _files = files ?? new ProjectFileService();
        _documents = documents ?? new ProjectDocumentService();
        _samples = samples ?? new SampleDataService();
        _recentFiles = recentFiles ?? new RecentFilesService();
    }

    public void InitializeRecentFiles() => _session.ReplaceRecentFiles(_recentFiles.Load());

    public void ClearRecentFiles() => _session.ReplaceRecentFiles(_recentFiles.ClearAndGetUpdatedList());

    public bool TryAutoSave()
    {
        if (!_session.IsModified || _session.CurrentFilePath == null) return false;
        Save(_session.CurrentFilePath);
        return true;
    }

    public void CreateNew() => Load(_samples.Create(), null, isSampleProject: true);

    /// <summary>サンプルデータなしの空プロジェクトを作成する。ユーザーが「新規プロジェクト」を操作した場合に使用。</summary>
    public void CreateEmpty() => Load(new Models.Project(), null, isSampleProject: false);

    public void Open(string path) => Load(_files.Load(path), path);

    /// <summary>
    /// v2.16.35 TD-59b-2 (nestsuite-double-read-design-review.md §8.6):
    /// probe（<see cref="NestSuiteTabFactory.TryPrepareOpen"/>）で得た <paramref name="context"/> を
    /// 追加読込なしで開く。現在ファイルとして関連付ける path は <see cref="WorkspaceFileOpenContext.FilePath"/>
    /// のみ（別引数の path は受けない。読み込んだ内容と保存先が乖離しない）。
    /// </summary>
    public void OpenPrepared(WorkspaceFileOpenContext context) =>
        Load(_files.LoadPrepared(context), context.FilePath);

    public void Save(string path) => Save(path, createBackup: true);

    /// <summary>v2.16.6 TD-64: createBackup=false の場合、正本は更新するが .bak は更新しない（自動保存向け）。</summary>
    public void Save(string path, bool createBackup)
    {
        _files.Save(path, CreateSnapshot(), createBackup);
        _session.MarkSaved(path);
        TrackRecentFile(path);
    }

    public Project CreateSnapshot() =>
        _documents.Build(_session.ProjectId, _session.ProjectName, _notes, _tasks, _editor);

    private void Load(Project project, string? filePath, bool isSampleProject = false)
    {
        _session.Start(project.ProjectId, project.ProjectName, filePath,
            filePath != null && File.Exists(filePath) ? File.GetLastWriteTime(filePath) : null,
            isSampleProject);
        var lastNote = _documents.Load(project, _notes, _tasks, _editor);
        _markers.Refresh(_notes.AllNotes);

        if (lastNote != null)
            _editor.SelectNote(lastNote);
        else
            _editor.Clear();

        _session.IsModified = false;
        if (filePath != null) TrackRecentFile(filePath);
    }

    private void TrackRecentFile(string path) =>
        _session.ReplaceRecentFiles(_recentFiles.Add(path));
}
