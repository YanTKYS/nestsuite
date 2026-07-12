namespace NestSuite.Services;

public static class DraftCandidatePolicy
{
    public static bool IsSupportedWorkspace(NestSuiteWorkspaceKind workspaceKind) =>
        workspaceKind is NestSuiteWorkspaceKind.NoteNest or NestSuiteWorkspaceKind.IdeaNest or NestSuiteWorkspaceKind.ChatNest;

    public static bool IsCandidate(NestSuiteWorkspaceKind workspaceKind, string? filePath, bool hasDraftableChanges) =>
        IsSupportedWorkspace(workspaceKind)
        && filePath == null
        && hasDraftableChanges;
}
