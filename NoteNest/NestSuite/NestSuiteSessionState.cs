namespace NoteNest.NestSuite;

public class NestSuiteSessionState
{
    public List<string> FilePaths { get; set; } = [];
    public string? ActiveFilePath { get; set; }

    public static readonly NestSuiteSessionState Empty = new();
}
