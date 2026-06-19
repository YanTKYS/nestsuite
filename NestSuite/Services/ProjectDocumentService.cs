using NoteNest.Models;
using NoteNest.ViewModels;

namespace NoteNest.Services;

/// <summary>保存モデルと責務別 ViewModel 間の変換を担当します。</summary>
public sealed class ProjectDocumentService
{
    public NoteViewModel? Load(
        Project project,
        NoteWorkspaceViewModel notes,
        TaskBoardViewModel tasks,
        EditorStateViewModel editor)
    {
        notes.Load(project.Notebooks);
        tasks.Load(project.Tasks);
        editor.LoadSettings(project.Settings.FontFamily, project.Settings.FontSize);
        return notes.FindNoteById(project.Settings.LastOpenedNoteId)
            ?? notes.Notebooks.FirstOrDefault()?.Notes.FirstOrDefault();
    }

    public Project Build(
        string projectId,
        string projectName,
        NoteWorkspaceViewModel notes,
        TaskBoardViewModel tasks,
        EditorStateViewModel editor) => new()
    {
        Version = Project.CurrentSchemaVersion,
        ProjectId = projectId,
        ProjectName = projectName,
        Notebooks = notes.BuildModels(),
        Tasks = tasks.BuildModel(),
        Settings = new AppSettings
        {
            LastOpenedNoteId = editor.SelectedNote?.Id ?? "",
            FontFamily = editor.FontFamily,
            FontSize = editor.FontSize,
        },
    };
}
