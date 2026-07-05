using NestSuite.Models;
using NestSuite.ViewModels;

namespace NestSuite.Services;

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
            // v2.14.16 BUG: FontFamily は NestSuite UI 設定（v2.14.17 L22 以降 WorkspaceEditorFontFamily、
            // 旧 NoteNestEditorFontFamily）駆動の表示専用値のため、Workspace ファイルへは
            // 読込時点の値（SavedFontFamily）をそのまま書き戻す。editor.FontFamily（現在の表示値）を
            // 書き戻すと、UI 設定の変更が Workspace ファイル本体の差分になってしまうため使用しない。
            FontFamily = editor.SavedFontFamily,
            FontSize = editor.FontSize,
        },
    };
}
