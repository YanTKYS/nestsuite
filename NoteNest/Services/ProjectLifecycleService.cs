using NoteNest.Models;
using NoteNest.ViewModels;

namespace NoteNest.Services;

/// <summary>プロジェクトの読込・保存・変換・最近使ったファイル更新を一つのライフサイクルとして扱います。</summary>
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
    private readonly ExportService _exports;

    public ProjectLifecycleService(
        ProjectSessionViewModel session,
        NoteWorkspaceViewModel notes,
        TaskBoardViewModel tasks,
        MarkerPanelViewModel markers,
        EditorStateViewModel editor,
        ProjectFileService? files = null,
        ProjectDocumentService? documents = null,
        SampleDataService? samples = null,
        RecentFilesService? recentFiles = null,
        ExportService? exports = null)
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
        _exports = exports ?? new ExportService();
    }

    public void InitializeRecentFiles() => _session.ReplaceRecentFiles(_recentFiles.Load());

    public void CreateNew() => Load(_samples.Create(), null);

    public void Open(string path) => Load(_files.Load(path), path);

    public void Save(string path)
    {
        _files.Save(path, Build());
        _session.MarkSaved(path);
        RecordRecentFile(path);
    }

    public Project Build() =>
        _documents.Build(_session.ProjectId, _session.ProjectName, _notes, _tasks, _editor);

    public void ExportProjectToText(string outputPath) =>
        _exports.ExportProjectToText(Build(), outputPath);

    public int ExportNotebooksToTextFiles(string outputDirectory)
    {
        var project = Build();
        _exports.ExportNotebooksToTextFiles(project, outputDirectory);
        return project.Notebooks.Count;
    }

    private void Load(Project project, string? filePath)
    {
        _session.Start(project.ProjectId, project.ProjectName, filePath);
        var lastNote = _documents.Load(project, _notes, _tasks, _editor);
        _markers.Refresh(_notes.AllNotes);

        if (lastNote != null)
            _editor.SelectNote(lastNote);
        else
            _editor.Clear();

        _session.IsModified = false;
        if (filePath != null) RecordRecentFile(filePath);
    }

    private void RecordRecentFile(string path)
    {
        _recentFiles.Add(path);
        _session.ReplaceRecentFiles(_recentFiles.Load());
    }
}
