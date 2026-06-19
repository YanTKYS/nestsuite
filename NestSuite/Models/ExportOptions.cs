namespace NoteNest.Models;

public enum ExportTarget { Project, CurrentNotebook, CurrentNote }
public enum ExportFormat { Text, Markdown, Html }

public sealed record ExportOptions(
    ExportTarget Target,
    ExportFormat Format,
    bool IncludeTasks,
    bool IncludeMarkers);
