using System.IO;
using System.Text;
using NestSuite.Models;

namespace NestSuite.Services;

public class ExportService
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    private static readonly HashSet<string> ReservedNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        };


    public void Export(Project project, ExportOptions options, string outputPath, string? notebookId = null, string? noteId = null)
    {
        var notebooks = options.Target switch
        {
            ExportTarget.CurrentNotebook => project.Notebooks.Where(notebook => notebook.Id == notebookId).ToList(),
            ExportTarget.CurrentNote => project.Notebooks
                .Select(notebook => new Notebook
                {
                    Id = notebook.Id,
                    Title = notebook.Title,
                    Notes = notebook.Notes.Where(note => note.Id == noteId).ToList(),
                })
                .Where(notebook => notebook.Notes.Count > 0).ToList(),
            _ => project.Notebooks,
        };
        var scoped = new Project
        {
            ProjectName = project.ProjectName,
            Notebooks = notebooks,
            Tasks = ScopeTasks(project.Tasks, options.Target, notebooks),
        };
        var text = options.Format switch
        {
            ExportFormat.Markdown => BuildMarkdown(scoped, options),
            ExportFormat.Html => BuildHtml(scoped, options),
            _ => BuildText(scoped, options),
        };
        File.WriteAllText(outputPath, text, Encoding.UTF8);
    }


    private static TaskCollection ScopeTasks(TaskCollection tasks, ExportTarget target, IEnumerable<Notebook> scopedNotebooks)
    {
        if (target == ExportTarget.Project) return tasks;
        var noteIds = new HashSet<string>(scopedNotebooks.SelectMany(notebook => notebook.Notes).Select(note => note.Id));
        return new TaskCollection
        {
            Today = FilterLinkedTasks(tasks.Today, noteIds),
            Week = FilterLinkedTasks(tasks.Week, noteIds),
            Backlog = FilterLinkedTasks(tasks.Backlog, noteIds),
        };
    }

    private static List<NoteTask> FilterLinkedTasks(IEnumerable<NoteTask> tasks, HashSet<string> noteIds) =>
        tasks.Where(task => task.LinkedNoteId != null && noteIds.Contains(task.LinkedNoteId)).ToList();

    public static string GetExtension(ExportFormat format) => format switch
    {
        ExportFormat.Markdown => ".md",
        ExportFormat.Html => ".html",
        _ => ".txt",
    };

    private static string BuildText(Project project, ExportOptions options)
    {
        var sb = new StringBuilder(BuildProjectText(project));
        AppendOptionalSections(sb, project, options);
        return sb.ToString();
    }

    private static string BuildMarkdown(Project project, ExportOptions options)
    {
        var sb = new StringBuilder($"# {project.ProjectName}\n");
        foreach (var notebook in project.Notebooks)
        {
            sb.AppendLine($"\n## {notebook.Title}");
            foreach (var note in notebook.Notes)
                sb.AppendLine($"\n### {note.Title}\n\n{note.Content}");
        }
        AppendOptionalSections(sb, project, options, markdown: true);
        return sb.ToString();
    }

    private static string BuildHtml(Project project, ExportOptions options)
    {
        static string E(string value) => System.Net.WebUtility.HtmlEncode(value);
        var sb = new StringBuilder($"<!doctype html><html><head><meta charset=\"utf-8\"><title>{E(project.ProjectName)}</title></head><body><h1>{E(project.ProjectName)}</h1>");
        foreach (var notebook in project.Notebooks)
        {
            sb.Append($"<h2>{E(notebook.Title)}</h2>");
            foreach (var note in notebook.Notes)
                sb.Append($"<h3>{E(note.Title)}</h3><pre>{E(note.Content)}</pre>");
        }
        if (options.IncludeTasks)
        {
            sb.Append("<h2>Tasks</h2><ul>");
            foreach (var task in project.Tasks.Today.Concat(project.Tasks.Week).Concat(project.Tasks.Backlog))
                sb.Append($"<li>{(task.IsCompleted ? "✓" : "□")} {E(task.Title)}</li>");
            sb.Append("</ul>");
        }
        if (options.IncludeMarkers)
        {
            sb.Append("<h2>Markers</h2><ul>");
            foreach (var note in project.Notebooks.SelectMany(notebook => notebook.Notes))
                foreach (var marker in new MarkerExtractorService().Extract(note.Content, note.Title))
                    sb.Append($"<li>[{E(marker.Type)}] {E(note.Title)} L{marker.LineNumber}: {E(marker.Excerpt)}</li>");
            sb.Append("</ul>");
        }
        return sb.Append("</body></html>").ToString();
    }

    private static void AppendOptionalSections(StringBuilder sb, Project project, ExportOptions options, bool markdown = false)
    {
        if (options.IncludeTasks)
        {
            sb.AppendLine(markdown ? "\n## Tasks" : "\nTasks");
            foreach (var task in project.Tasks.Today.Concat(project.Tasks.Week).Concat(project.Tasks.Backlog))
                sb.AppendLine($"- [{(task.IsCompleted ? "x" : " ")}] {task.Title}");
        }
        if (options.IncludeMarkers)
        {
            sb.AppendLine(markdown ? "\n## Markers" : "\nMarkers");
            foreach (var note in project.Notebooks.SelectMany(notebook => notebook.Notes))
                foreach (var marker in new MarkerExtractorService().Extract(note.Content, note.Title))
                    sb.AppendLine($"- [{marker.Type}] {note.Title} L{marker.LineNumber}: {marker.Excerpt}");
        }
    }

    public void ExportProjectToText(Project project, string outputPath)
        => File.WriteAllText(outputPath, BuildProjectText(project), Encoding.UTF8);

    public void ExportNotebooksToTextFiles(Project project, string outputDirectory)
    {
        foreach (var notebook in project.Notebooks)
        {
            var safeName = SanitizeFileName(notebook.Title);
            var filePath  = GetUniqueFilePath(outputDirectory, safeName, ".txt");
            File.WriteAllText(filePath, BuildNotebookText(project, notebook), Encoding.UTF8);
        }
    }

    public static string BuildProjectText(Project project)
    {
        var sb = new StringBuilder();
        sb.AppendLine("NoteNest Export");
        sb.AppendLine($"Project: {project.ProjectName}");
        sb.AppendLine($"ExportedAt: {DateTime.Now:yyyy-MM-dd HH:mm}");

        foreach (var notebook in project.Notebooks)
        {
            sb.AppendLine();
            sb.AppendLine(new string('=', 60));
            sb.AppendLine($"Notebook: {notebook.Title}");
            sb.AppendLine(new string('=', 60));

            foreach (var note in notebook.Notes)
                AppendNote(sb, note);
        }

        return sb.ToString();
    }

    public static string BuildNotebookText(Project project, Notebook notebook)
    {
        var sb = new StringBuilder();
        sb.AppendLine("NoteNest Export");
        sb.AppendLine($"Project: {project.ProjectName}");
        sb.AppendLine($"Notebook: {notebook.Title}");
        sb.AppendLine($"ExportedAt: {DateTime.Now:yyyy-MM-dd HH:mm}");

        foreach (var note in notebook.Notes)
            AppendNote(sb, note);

        return sb.ToString();
    }

    public static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "notebook";

        var safe = new string(name.Select(c => InvalidFileNameChars.Contains(c) ? '_' : c).ToArray()).Trim();

        if (string.IsNullOrWhiteSpace(safe))
            return "notebook";

        // Check the stem (part before first dot) against Windows reserved device names.
        // Windows treats CON.txt, AUX.anything, etc. as reserved devices too.
        var dotIndex = safe.IndexOf('.');
        var stem     = dotIndex >= 0 ? safe.Substring(0, dotIndex) : safe;
        if (ReservedNames.Contains(stem))
            safe = dotIndex >= 0 ? stem + "_" + safe.Substring(dotIndex) : safe + "_";

        return safe;
    }

    public static string GetUniqueFilePath(string directory, string baseName, string extension)
    {
        var path = Path.Combine(directory, baseName + extension);
        if (!File.Exists(path)) return path;

        for (int i = 2; ; i++)
        {
            path = Path.Combine(directory, $"{baseName}_{i}{extension}");
            if (!File.Exists(path)) return path;
        }
    }

    private static void AppendNote(StringBuilder sb, Note note)
    {
        sb.AppendLine();
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"Note: {note.Title}");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine();
        sb.AppendLine(note.Content.TrimEnd('\r', '\n'));
    }
}
