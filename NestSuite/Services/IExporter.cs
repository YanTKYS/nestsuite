using NoteNest.Models;

namespace NoteNest.Services;

public interface IExporter
{
    string FileFilter { get; }
    string DefaultExtension { get; }
    string Export(Project project);
}
