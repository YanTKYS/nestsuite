using NestSuite.Models;

namespace NestSuite.Services;

public interface IExporter
{
    string FileFilter { get; }
    string DefaultExtension { get; }
    string Export(Project project);
}
