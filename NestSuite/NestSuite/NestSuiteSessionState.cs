namespace NestSuite;

public class NestSuiteSessionState
{
    public List<string> FilePaths { get; set; } = [];
    public string? ActiveFilePath { get; set; }
    public List<NestSuiteSessionTabState> Tabs { get; set; } = [];
}

public class NestSuiteSessionTabState
{
    public string FilePath { get; set; } = string.Empty;
    public string? WorkspaceKind { get; set; }
    public bool IsPinned { get; set; }
}
